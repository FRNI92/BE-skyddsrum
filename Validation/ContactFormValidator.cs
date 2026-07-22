using System.Net.Mail;
using System.Text.RegularExpressions;
using Skyddsrum.Functions.DTOs;

namespace Skyddsrum.Functions.Validation;

public static partial class ContactFormValidator
{
    public static (ContactFormDto Value, Dictionary<string, string> Errors) NormalizeAndValidate(ContactFormDto input)
    {
        var value = new ContactFormDto
        {
            SubmissionId = input.SubmissionId?.Trim(),
            Name = input.Name?.Trim(),
            Email = input.Email?.Trim().ToLowerInvariant(),
            Phone = NullIfWhiteSpace(input.Phone),
            Organization = NullIfWhiteSpace(input.Organization),
            PropertyAddress = NullIfWhiteSpace(input.PropertyAddress),
            Message = input.Message?.Trim(),
            ConsentAccepted = input.ConsentAccepted,
            Website = NullIfWhiteSpace(input.Website)
        };

        var errors = new Dictionary<string, string>();

        if (!Guid.TryParse(value.SubmissionId, out _)) errors["submissionId"] = "Ogiltigt formulär-ID.";
        if (value.Name is null or { Length: < 2 or > 100 }) errors["name"] = "Ange ett namn med 2-100 tecken.";
        if (value.Email is null or { Length: > 254 } || !MailAddress.TryCreate(value.Email, out _)) errors["email"] = "Ange en giltig e-postadress.";
        if (value.Phone is { Length: > 30 } || value.Phone is not null && !PhoneRegex().IsMatch(value.Phone)) errors["phone"] = "Ange ett giltigt telefonnummer.";
        if (value.Organization is { Length: > 120 }) errors["organization"] = "Organisationen får innehålla högst 120 tecken.";
        if (value.PropertyAddress is { Length: > 160 }) errors["propertyAddress"] = "Adressen får innehålla högst 160 tecken.";
        if (value.Message is null or { Length: < 20 or > 4000 }) errors["message"] = "Meddelandet måste innehålla 20-4000 tecken.";
        if (!value.ConsentAccepted) errors["consentAccepted"] = "Du måste godkänna att uppgifterna behandlas för att vi ska kunna svara.";

        return (value, errors);
    }

    private static string? NullIfWhiteSpace(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    [GeneratedRegex(@"^[0-9+()\-\s]{5,30}$", RegexOptions.CultureInvariant)]
    private static partial Regex PhoneRegex();
}
