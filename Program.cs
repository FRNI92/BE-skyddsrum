using Azure.Communication.Email;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Skyddsrum.Functions.Email;
using Skyddsrum.Functions.Security;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        services
            .AddOptions<EmailOptions>()
            .Bind(context.Configuration.GetSection(EmailOptions.SectionName))
            .Validate(options => !string.IsNullOrWhiteSpace(options.ConnectionString), "Email connection string is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.SenderAddress), "Email sender address is required.")
            .Validate(options => !string.IsNullOrWhiteSpace(options.RecipientAddress), "Email recipient address is required.")
            .ValidateOnStart();

        services.AddSingleton(provider =>
        {
            var options = provider.GetRequiredService<IOptions<EmailOptions>>().Value;
            return new EmailClient(options.ConnectionString);
        });

        services.AddSingleton<IContactSubmissionGuard, ContactSubmissionGuard>();
        services.AddSingleton<IEmailSender, CommunicationEmailSender>();
    })
    .Build();

host.Run();
