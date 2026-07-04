namespace Skyddsrum.Functions.Email;

public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string? ConnectionString { get; init; }
    public string? SenderAddress { get; init; }
    public string? RecipientAddress { get; init; }
}
