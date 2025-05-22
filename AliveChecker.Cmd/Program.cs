using AliveChecker.Application;
using AliveChecker.Application.Configuration;
using AliveChecker.Application.Files;
using AliveChecker.Application.IoC;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;


var builder = Host.CreateApplicationBuilder(args);

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .WriteTo.Async(wt => wt.Console(LogEventLevel.Debug, theme: AnsiConsoleTheme.Code))
    .WriteTo.File("alive-checker.log", rollingInterval: RollingInterval.Day, restrictedToMinimumLevel: LogEventLevel.Debug)
    .CreateLogger();

builder.Services.AddLogging(configure => configure.AddSerilog(Log.Logger));

builder.Configuration
    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
    .Build();


builder.Services.AddSingleton(provider =>
{
    var configuration = provider.GetRequiredService<IConfiguration>();

    var clientConfiguration = new ClientConfiguration();
    configuration.GetSection(nameof(ClientConfiguration)).Bind(clientConfiguration);
    return clientConfiguration;
});

builder.Services.AddApplicationDependencies();

var host = builder.Build();

var services = host.Services;

var cancellationToken = new CancellationTokenSource();

Console.CancelKeyPress += (sender, e) =>
{
    e.Cancel = true; // needed to soft kill the application
    cancellationToken.Cancel();
};
//resolve and run the application
var configuration = services.GetRequiredService<ClientConfiguration>();
var app = services.GetRequiredService<IAliveCheckerService>();

try
{
    Log.Information("Start Checking....");
    CheckLicenseExpiration();

    void CheckLicenseExpiration()
    {
        if (DateTime.Now > new DateTime(2024, 12, 31, 23, 59, 59))
            throw new ApplicationException("Application can't run without an update");
    }

    await app.Run(new FileService(configuration.CsvFilePath), cancellationToken);
    Log.Information("End Checking....");
}
catch (Exception e)
{
    Log.Error(e, "Application error. Press any key to exit...");
    Console.ReadKey();
    throw;
}