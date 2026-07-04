using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Skyddsrum.Functions.Authentication;
using Skyddsrum.Functions.Email;
using Skyddsrum.Functions.Repositories;
using Skyddsrum.Functions.Services;
using Skyddsrum.Functions.Storage;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        var configuration = context.Configuration;

        services.Configure<CosmosOptions>(configuration.GetSection(CosmosOptions.SectionName));
        services.Configure<BlobStorageOptions>(configuration.GetSection(BlobStorageOptions.SectionName));
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));

        services.AddSingleton(_ =>
        {
            var connectionString = configuration["Cosmos:ConnectionString"];
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException("Missing Cosmos:ConnectionString setting.");
            }

            return new CosmosClient(connectionString);
        });

        services.AddSingleton<ICurrentUserReader, CurrentUserReader>();
        services.AddSingleton<IAdminAuthorization, AdminAuthorization>();
        services.AddSingleton<IArticleRepository, ArticleRepository>();
        services.AddSingleton<IArticleService, ArticleService>();
        services.AddSingleton<IBlobStorageService, BlobStorageService>();
        services.AddSingleton<IEmailSender, CommunicationEmailSender>();
    })
    .Build();

host.Run();
