using System.Net;
using Serilog;

namespace mdBookGitAutoBuild.Utilities;

public static class WebHookListener
{
    static readonly Serilog.ILogger log = Log.ForContext(typeof(WebHookListener));
    
    static readonly HttpListener httpListener = new();

    public static event Action? ReceivedUpdate;
    
    public static void Start()
    {
        log.Information("Starting listening on \":8080/hook\"");
        httpListener.Prefixes.Add("http://+:8080/hook/");
        httpListener.Start();

        Task.Factory.StartNew(async () =>
        {
            while (!Environment.HasShutdownStarted)
            {
                HttpListenerContext context = await httpListener.GetContextAsync();
                
                ReceivedUpdate?.Invoke();

                // Closing the response shows te other side that the request was successful 
                context.Response.OutputStream.Close();
            }
        }, TaskCreationOptions.LongRunning);
    }
}