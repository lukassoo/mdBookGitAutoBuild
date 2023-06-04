using System.Net;

namespace mdBookGitAutoBuild.Utilities;

public static class WebHookListener
{
    static HttpListener httpListener = new();

    public static event Action? ReceivedUpdate;
    
    public static void Start()
    {
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