using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Skyddsrum.Functions.DTOs;
using Skyddsrum.Functions.Email;
using Skyddsrum.Functions.Security;
using Skyddsrum.Functions.Validation;

namespace Skyddsrum.Functions.Functions;

public sealed class ContactFunctions(
    IEmailSender emailSender,
    IContactSubmissionGuard submissionGuard,
    ILogger<ContactFunctions> logger)
{
    private const int MaxRequestBytes = 16 * 1024;

    [Function("SubmitContactForm")]
    public async Task<HttpResponseData> SubmitContactForm(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "contact")] HttpRequestData request,
        CancellationToken cancellationToken)
    {
        if (!HasJsonContentType(request))
            return await request.ErrorAsync("Content-Type måste vara application/json.", HttpStatusCode.UnsupportedMediaType);

        ContactFormDto? payload;
        try
        {
            var body = await ReadBodyAsync(request.Body, cancellationToken);
            if (body is null)
                return await request.ErrorAsync("Formuläret är för stort.", HttpStatusCode.RequestEntityTooLarge);

            payload = JsonSerializer.Deserialize<ContactFormDto>(body, JsonOptions.Default);
        }
        catch (JsonException)
        {
            return await request.ErrorAsync("Formuläret innehåller ogiltig JSON.", HttpStatusCode.BadRequest);
        }

        if (payload is null)
            return await request.ErrorAsync("Formuläret saknar innehåll.", HttpStatusCode.BadRequest);

        // Silently accept bot submissions so the honeypot cannot be probed.
        if (!string.IsNullOrWhiteSpace(payload.Website))
            return await request.JsonAsync(new { ok = true, referenceId = "received" });

        var (contactForm, errors) = ContactFormValidator.NormalizeAndValidate(payload);
        if (errors.Count > 0)
            return await request.JsonAsync(new { error = "Kontrollera formuläret.", fields = errors }, HttpStatusCode.BadRequest);

        var submissionId = contactForm.SubmissionId!;
        var referenceId = CreateReferenceId(submissionId);
        var fingerprint = CreateFingerprint(contactForm.Email!, contactForm.Message!);
        var decision = submissionGuard.TryAcquire(GetClientKey(request), submissionId, fingerprint);

        if (decision == SubmissionDecision.Duplicate)
            return await request.JsonAsync(new { ok = true, duplicate = true, referenceId });

        if (decision == SubmissionDecision.RateLimited)
        {
            var response = await request.JsonAsync(
                new { error = "Du har nyligen skickat flera meddelanden. Vänta tio minuter och försök igen." },
                HttpStatusCode.TooManyRequests);
            response.Headers.Add("Retry-After", "600");
            return response;
        }

        try
        {
            await emailSender.SendContactFormAsync(contactForm, referenceId, cancellationToken);
            return await request.JsonAsync(new
            {
                ok = true,
                referenceId,
                submittedAtUtc = DateTimeOffset.UtcNow
            }, HttpStatusCode.Accepted);
        }
        catch (Exception exception)
        {
            submissionGuard.Release(submissionId, fingerprint);
            logger.LogError(exception, "Contact form email delivery failed for reference {ReferenceId}.", referenceId);
            return await request.ErrorAsync(
                "Meddelandet kunde inte skickas just nu. Försök igen senare eller kontakta oss via telefon.",
                HttpStatusCode.BadGateway);
        }
    }

    private static bool HasJsonContentType(HttpRequestData request) =>
        request.Headers.TryGetValues("Content-Type", out var values) &&
        values.Any(value => value.StartsWith("application/json", StringComparison.OrdinalIgnoreCase));

    private static async Task<byte[]?> ReadBodyAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var memory = new MemoryStream();
        var buffer = new byte[4096];
        int bytesRead;

        while ((bytesRead = await stream.ReadAsync(buffer, cancellationToken)) > 0)
        {
            if (memory.Length + bytesRead > MaxRequestBytes) return null;
            await memory.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
        }

        return memory.ToArray();
    }

    private static string GetClientKey(HttpRequestData request)
    {
        foreach (var header in new[] { "X-Azure-ClientIP", "X-Forwarded-For" })
        {
            if (!request.Headers.TryGetValues(header, out var values)) continue;
            var value = values.FirstOrDefault()?.Split(',')[0].Trim();
            if (!string.IsNullOrWhiteSpace(value)) return value;
        }

        return "unknown-client";
    }

    private static string CreateFingerprint(string email, string message)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{email}\n{message}"));
        return Convert.ToHexString(bytes);
    }

    private static string CreateReferenceId(string submissionId) =>
        $"SG-{Guid.Parse(submissionId):N}"[..11].ToUpperInvariant();
}
