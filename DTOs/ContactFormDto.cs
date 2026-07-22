namespace Skyddsrum.Functions.DTOs;

public sealed class ContactFormDto
{
    public string? SubmissionId { get; init; }
    public string? Name { get; init; }
    public string? Email { get; init; }
    public string? Phone { get; init; }
    public string? Organization { get; init; }
    public string? PropertyAddress { get; init; }
    public string? Message { get; init; }
    public bool ConsentAccepted { get; init; }

    // Honeypot. Real users never see or fill this field.
    public string? Website { get; init; }
}
