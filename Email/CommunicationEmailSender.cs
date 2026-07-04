using Azure.Communication.Email;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Skyddsrum.Functions.DTOs;

namespace Skyddsrum.Functions.Email;

public interface IEmailSender
{
    Task SendContactFormAsync(ContactFormDto contactForm, CancellationToken cancellationToken);
}

public sealed class CommunicationEmailSender(
    IOptions<EmailOptions> options,
    ILogger<CommunicationEmailSender> logger) : IEmailSender
{
    public async Task SendContactFormAsync(ContactFormDto contactForm, CancellationToken cancellationToken)
    {
        var value = options.Value;
        if (string.IsNullOrWhiteSpace(value.ConnectionString) ||
            string.IsNullOrWhiteSpace(value.SenderAddress) ||
            string.IsNullOrWhiteSpace(value.RecipientAddress))
        {
            logger.LogWarning("Email settings are missing. Contact form from {Email} was not sent.", contactForm.Email);
            return;
        }

        var client = new EmailClient(value.ConnectionString);
        var subject = $"Kontaktformulär: {contactForm.Name}";
        var body = $"""
        Namn: {contactForm.Name}
        E-post: {contactForm.Email}
        Telefon: {contactForm.Phone}

        Meddelande:
        {contactForm.Message}
        """;

        var message = new EmailMessage(
            senderAddress: value.SenderAddress,
            recipientAddress: value.RecipientAddress,
            content: new EmailContent(subject)
            {
                PlainText = body
            });

        await client.SendAsync(Azure.WaitUntil.Started, message, cancellationToken);
    }
}
