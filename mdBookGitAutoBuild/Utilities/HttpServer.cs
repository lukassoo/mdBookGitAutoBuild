using Serilog;

namespace mdBookGitAutoBuild.Utilities;

public static class HttpServer
{
    static readonly Serilog.ILogger log = Log.ForContext(typeof(HttpServer));

    static readonly string targetDirectory = Directory.GetCurrentDirectory() + "/GitRepo/book";
    static WebApplication webApp = null!;
    static readonly CancellationTokenSource cancellationTokenSource = new();
    
    public static bool Start()
    {
        try
        {
            log.Information("Staring http server (port 80)");

            WebApplicationOptions options = new()
            {
                WebRootPath = targetDirectory,
                ContentRootPath = targetDirectory
            };
            
            WebApplicationBuilder builder = WebApplication.CreateBuilder(options);

            builder.WebHost.ConfigureKestrel(kestrelServerOptions =>
            {
                kestrelServerOptions.ListenAnyIP(80);
            });
            builder.Logging.ClearProviders();
            
            webApp = builder.Build();
            webApp.UseDefaultFiles();
            webApp.UseStaticFiles();
            webApp.RunAsync(cancellationTokenSource.Token);

            return true;
        }
        catch(Exception exception)
        {
            log.Error(exception, "Error while starting http server");
            return false;
        }
    }

    public static void Stop()
    {
        cancellationTokenSource.Cancel();
    }
}