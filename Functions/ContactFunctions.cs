using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Skyddsrum.Functions.DTOs;
using Skyddsrum.Functions.Email;

namespace Skyddsrum.Functions.Functions;

public sealed class ContactFunctions(IEmailSender emailSender)
{
    [Function("SubmitContactForm")]
    public async Task<HttpResponseData> SubmitContactForm(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "contact")] HttpRequestData request,
        CancellationToken cancellationToken)
    {
        var payload = await JsonSerializer.DeserializeAsync<ContactFormDto>(request.Body, JsonOptions.Default, cancellationToken);
        if (payload is null ||
            string.IsNullOrWhiteSpace(payload.Name) ||
            string.IsNullOrWhiteSpace(payload.Email) ||
            string.IsNullOrWhiteSpace(payload.Message))
        {
            return await request.ErrorAsync("Name, email and message are required.", HttpStatusCode.BadRequest);
        }

        await emailSender.SendContactFormAsync(payload, cancellationToken);
        return await request.JsonAsync(new { ok = true });
    }
}
