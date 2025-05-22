using AliveChecker.Application.Auth;
using AliveChecker.Application.Database;
using AliveChecker.Application.Endpoints;
using AliveChecker.Application.Files;
using AliveChecker.Application.Utils;
using Microsoft.Extensions.DependencyInjection;


namespace AliveChecker.Application.IoC;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationDependencies(this IServiceCollection services)
    {
        services.AddHttpClient<IAliveCheckerEndpoint, AliveCheckerEndpoint>()
            .ConfigurePrimaryHttpMessageHandler(_ => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true
            });
        services.AddHttpClient<IAuthService, AuthService>()
            .ConfigurePrimaryHttpMessageHandler(_ => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (_, _, _, _) => true
            });

        var dbPath = Path.Combine(Directory.GetCurrentDirectory(), "Checker.db");
        services.AddSqlite<CheckerContext>($"Data Source={dbPath}");

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<ICsvReadService,CsvReadService>();
        services.AddScoped<ICsvWriteService,CsvWriteService>();
        services.AddScoped<IAliveCheckerService, AliveCheckerService>();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<ITokenCreator, TokenCreator>();
        services.AddScoped<ICheckerRepository, CheckerRepository>();
        services.AddScoped<IBodyCreationService, BodyCreationService>();
        services.AddScoped<IDateProvider, DateProvider>();
        services.AddScoped<IHashService, HashService>();

        return services;
    }
}