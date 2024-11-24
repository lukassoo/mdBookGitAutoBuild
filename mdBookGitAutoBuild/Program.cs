using mdBookGitAutoBuild.Utilities;
using Serilog;
using ILogger = Serilog.ILogger;

namespace mdBookGitAutoBuild;

internal static class Program
{
    static ILogger log = null!;
    static string lastCommitTime = string.Empty;

    static async Task Main()
    {
        LoggerConfiguration loggerConfig = new();
        loggerConfig.MinimumLevel.Verbose();
        loggerConfig.WriteTo.Console(outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}][{Level:u3}][{Properties:j}] {Message:lj}{NewLine}{Exception}");
        
        Log.Logger = loggerConfig.CreateLogger(); 
        log = Log.Logger.ForContext(typeof(Program));
        
        log.Information("Starting");

        if (!SshKeygen.HasKey())
        {
            log.Information("No ssh keys found, generating...");
        
            bool keyGenerated = await SshKeygen.GenerateKey();
            if (!keyGenerated)
            {
                log.Error("Failed to generate new ssh keys - can't work");
                goto shutdown;
            }
        
            log.Information("New ssh keys generated, public key:");
            log.Information("--------------------------------------------------------------------\n\n");
            log.Information("{publicKey}\n",SshKeygen.GetPublicKey());
            log.Information("--------------------------------------------------------------------");
            log.Information("Use this to grant access to private repositories - grant read-only permissions of course");
            log.Information("Exiting since you need to add the key to git first - do that and restart the container");
            
            goto shutdown;
        }
        
        if (!Git.IsRepoCloned())
        {
            string? repoLink = Environment.GetEnvironmentVariable("GIT_REPO_LINK");
            if (string.IsNullOrEmpty(repoLink))
            {
                log.Error("No repo link provided - set the GIT_REPO_LINK environment variable with a valid git repo link");
                goto shutdown;
            }

            log.Information("Scanning repo ssh keys and adding to trusted");
            bool keyScanCompleted = await SshKeyScan.ScanAndTrust(repoLink);
            if (!keyScanCompleted)
            {
                log.Error("Scanning failed, check repo link");
                goto shutdown;
            }
            
            bool cloneCompleted = await Git.Clone(repoLink);

            if (!cloneCompleted)
            {
                log.Error("Clone failed, exiting");
                goto shutdown;
            }
        }
        else
        {
            log.Information("Git Repo is already cloned");
            log.Information("Pulling...");
            
            bool pullCompleted = await Git.Pull();
            if (!pullCompleted)
            {
                log.Warning("Failed to pull repo");
            }
        }

        lastCommitTime = await Git.GetLastCommitTime();
        DateTime lastCommitDateTime = DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(lastCommitTime)).DateTime;
        
        log.Information("Last commit time: " + lastCommitDateTime.ToString("yyyy-MM-dd HH:mm:ss"));
        
        log.Information("Building mdBook");
        await MdBook.Build();
        
        HttpServer.Start();

        bool usingAnyUpdateMethod = false;
        
        string? shouldUseWebHook = Environment.GetEnvironmentVariable("USE_WEB_HOOK");
        if (!string.IsNullOrEmpty(shouldUseWebHook))
        {
            log.Information("Detected USE_WEB_HOOK - starting http web hook listener");
        
            StartWebHook();
            usingAnyUpdateMethod = true;
        }
        
        string? shouldPullOnInterval = Environment.GetEnvironmentVariable("USE_PULL_ON_INTERVAL");
        if (!string.IsNullOrEmpty(shouldPullOnInterval))
        {
            log.Information("Detected USE_PULL_ON_INTERVAL - will pull repo every set time interval");
        
            StartRepoPullOnInterval();
            usingAnyUpdateMethod = true;
        }
        
        if (!usingAnyUpdateMethod)
        {
            log.Error("No update method used, define environment variables: USE_WEB_HOOK or USE_PULL_ON_INTERVAL (optionally both)");
            log.Error("No point running without a way to update - exiting");
            
            goto shutdown;
        }
        
        await Task.Delay(-1);
        
        shutdown:
        await Log.CloseAndFlushAsync();
    }

    static void StartWebHook()
    {
        WebHookListener.ReceivedUpdate +=
            async () =>
            {
                log.Information("Web hook request received");
                log.Information("Pulling repo...");
                await Git.Pull();

                string newCommitTime = await Git.GetLastCommitTime();
                if (lastCommitTime == newCommitTime)
                {
                    log.Information("No new commits - skipping mdBook rebuild");
                    return;
                }
                
                lastCommitTime = newCommitTime;

                log.Information("Building mdBook...");
                await MdBook.Build();
                
                log.Information("Update completed");
            };

        WebHookListener.Start();
    }

    static void StartRepoPullOnInterval()
    {
        Task.Factory.StartNew(async () =>
        {
            // Default pull every 24h
            TimeSpan pullIntervalTimeSpan = TimeSpan.FromHours(24);
            
            string? repoPullInterval = Environment.GetEnvironmentVariable("REPO_PULL_INTERVAL_HOURS");
            
            if (!string.IsNullOrEmpty(repoPullInterval))
            {
                log.Information("Detected REPO_PULL_INTERVAL_HOURS variable");

                if (int.TryParse(repoPullInterval, out int repoPullIntervalHours))
                {
                    pullIntervalTimeSpan = TimeSpan.FromHours(repoPullIntervalHours);
                    
                    log.Information("Successfully parsed target interval: " + repoPullIntervalHours + 
                                    " (pull every " + pullIntervalTimeSpan.TotalHours + " hours)");
                }
                else
                {
                    log.Information("Failed to parse REPO_PULL_INTERVAL_HOURS, using default pull interval: " + pullIntervalTimeSpan.TotalHours + " hours");
                }
            }
            else
            {
                log.Information("No REPO_PULL_INTERVAL_HOURS variable detected - default interval: " + pullIntervalTimeSpan.TotalHours + " hours");
            }

            while (!Environment.HasShutdownStarted)
            {
                await Task.Delay((int)pullIntervalTimeSpan.TotalMilliseconds);
                
                log.Information("Pulling on interval");
                
                bool successfulPull = await Git.Pull();
                if (!successfulPull)
                {
                    log.Warning("Failed to pull");
                    continue;
                }

                string newCommitTime = await Git.GetLastCommitTime();
                if (lastCommitTime != newCommitTime)
                {
                    lastCommitTime = newCommitTime;

                    log.Information("Detected a new commit, rebuilding mdBook");
                    
                    await MdBook.Build();
                }
                else
                {
                    log.Information("No new commits - skipping mdBook rebuild");
                }
            }
            
        }, TaskCreationOptions.LongRunning);
    }
}
