using System.Text.Encodings.Web;
using Azure;
using Azure.Communication.Email;
using Microsoft.Extensions.Options;
using Skyddsrum.Functions.DTOs;

namespace Skyddsrum.Functions.Email;

public interface IEmailSender
{
    Task SendContactFormAsync(ContactFormDto contactForm, string referenceId, CancellationToken cancellationToken);
}

public sealed class CommunicationEmailSender(
    EmailClient client,
    IOptions<EmailOptions> options) : IEmailSender
{
    public async Task SendContactFormAsync(
        ContactFormDto contactForm,
        string referenceId,
        CancellationToken cancellationToken)
    {
        var value = options.Value;
        var internalMessage = new EmailMessage(
            value.SenderAddress!,
            value.RecipientAddress!,
            new EmailContent($"Ny förfrågan från {contactForm.Name} - {referenceId}")
            {
                PlainText = CreateInternalPlainText(contactForm, referenceId),
                Html = CreateInternalHtml(contactForm, referenceId)
            });

        internalMessage.ReplyTo.Add(new EmailAddress(contactForm.Email!, contactForm.Name));

        await client.SendAsync(WaitUntil.Started, internalMessage, cancellationToken);

        var confirmationMessage = new EmailMessage(
            value.SenderAddress!,
            contactForm.Email!,
            new EmailContent($"Vi har tagit emot din fråga - {referenceId}")
            {
                PlainText = CreateConfirmationPlainText(contactForm, referenceId, value.SiteUrl),
                Html = CreateConfirmationHtml(contactForm, referenceId, value.SiteUrl)
            });

        await client.SendAsync(WaitUntil.Started, confirmationMessage, cancellationToken);
    }

    private static string CreateInternalPlainText(ContactFormDto form, string referenceId) => $"""
        Ny förfrågan via skyddsrumsgruppen.se
        Referens: {referenceId}

        Namn: {form.Name}
        E-post: {form.Email}
        Telefon: {form.Phone ?? "Ej angivet"}
        Organisation: {form.Organization ?? "Ej angivet"}
        Fastighet/adress: {form.PropertyAddress ?? "Ej angivet"}

        Meddelande:
        {form.Message}
        """;

    private static string CreateConfirmationPlainText(ContactFormDto form, string referenceId, string siteUrl) => $"""
        Hej {form.Name},

        Tack för din förfrågan. Vi har tagit emot ditt meddelande och återkommer så snart vi kan.

        Referens: {referenceId}

        Vänliga hälsningar
        Skyddsrumsgruppen
        {siteUrl}
        """;

    private static string CreateInternalHtml(ContactFormDto form, string referenceId) =>
        EmailLayout(
            "Ny förfrågan",
            $"Referens {Encode(referenceId)}",
            $"""
            <table role="presentation" style="width:100%;border-collapse:collapse;font-size:15px;line-height:1.6;color:#334155">
              {Row("Namn", form.Name)}
              {Row("E-post", form.Email)}
              {Row("Telefon", form.Phone ?? "Ej angivet")}
              {Row("Organisation", form.Organization ?? "Ej angivet")}
              {Row("Fastighet/adress", form.PropertyAddress ?? "Ej angivet")}
            </table>
            <div style="margin-top:24px;padding:20px;border-radius:8px;background:#f1f5f9;color:#102033;white-space:pre-wrap">{Encode(form.Message)}</div>
            """);

    private static string CreateConfirmationHtml(ContactFormDto form, string referenceId, string siteUrl) =>
        EmailLayout(
            $"Tack {Encode(form.Name)}!",
            "Vi har tagit emot din fråga",
            $"""
            <p style="margin:0 0 18px;color:#46576b;font-size:16px;line-height:1.7">
              Vi återkommer så snart vi kan. Spara gärna referensen nedan om du behöver kontakta oss om samma ärende.
            </p>
            <div style="display:inline-block;padding:10px 16px;border-radius:999px;background:#e8eef6;color:#154f9f;font-weight:700">{Encode(referenceId)}</div>
            <p style="margin:28px 0 0"><a href="{Encode(siteUrl)}" style="display:inline-block;padding:12px 20px;border-radius:6px;background:#154f9f;color:#ffffff;text-decoration:none;font-weight:700">Besök Skyddsrumsgruppen</a></p>
            """);

    private static string EmailLayout(string title, string subtitle, string content) => $"""
        <!doctype html>
        <html lang="sv">
          <body style="margin:0;padding:0;background:#eef3f8;font-family:Arial,sans-serif">
            <table role="presentation" style="width:100%;border-collapse:collapse;background:#eef3f8">
              <tr><td style="padding:32px 16px">
                <table role="presentation" style="width:100%;max-width:620px;margin:0 auto;border-collapse:collapse;background:#ffffff;border-radius:12px;overflow:hidden">
                  <tr><td style="padding:22px 28px;background:#132238;color:#ffffff"><span style="display:inline-block;width:12px;height:12px;margin-right:8px;background:#d85f24"></span><strong>SKYDDSRUMSGRUPPEN</strong></td></tr>
                  <tr><td style="padding:34px 28px">
                    <p style="margin:0 0 8px;color:#d85f24;font-size:12px;font-weight:700;letter-spacing:.08em;text-transform:uppercase">{subtitle}</p>
                    <h1 style="margin:0 0 24px;color:#102033;font-size:28px;line-height:1.2">{title}</h1>
                    {content}
                  </td></tr>
                  <tr><td style="padding:18px 28px;background:#f8fafc;color:#64748b;font-size:12px">Skyddsrumsgruppen - sakkunnig hjälp med skyddsrum</td></tr>
                </table>
              </td></tr>
            </table>
          </body>
        </html>
        """;

    private static string Row(string label, string? value) =>
        $"<tr><th style=\"padding:7px 12px 7px 0;text-align:left;vertical-align:top;color:#102033\">{Encode(label)}</th><td style=\"padding:7px 0\">{Encode(value)}</td></tr>";

    private static string Encode(string? value) => HtmlEncoder.Default.Encode(value ?? string.Empty);
}
