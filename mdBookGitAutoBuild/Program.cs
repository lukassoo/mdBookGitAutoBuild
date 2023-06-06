using mdBookGitAutoBuild.Utilities;

namespace mdBookGitAutoBuild;

internal static class Program
{
    static string lastCommitTime = string.Empty;

    static async Task Main()
    {
        Console.WriteLine("Starting");

        if (!SshKeygen.HasKey())
        {
            Console.WriteLine("No ssh keys found, generating...");

            bool keyGenerated = await SshKeygen.GenerateKey();
            if (!keyGenerated)
            {
                Console.WriteLine("Failed to generate new ssh keys - can't work");
                
                Environment.Exit(1);
            }

            Console.WriteLine("New ssh keys generated, public key:");
            Console.WriteLine("--------------------------------------------------------------------");
            Console.WriteLine();
            Console.WriteLine(SshKeygen.GetPublicKey());
            Console.WriteLine("--------------------------------------------------------------------");
            Console.WriteLine("Use this to grant access to private repositories - grant read-only permissions of course)");
            Console.WriteLine("Exiting since you need to add the key to git first - do that and restart the container");
            
            Environment.Exit(1);
        }
        
        if (!Git.IsRepoCloned())
        {
            string? repoLink = Environment.GetEnvironmentVariable("GIT_REPO_LINK");
            if (string.IsNullOrEmpty(repoLink))
            {
                Console.WriteLine("No repo link provided - set the GIT_REPO_LINK environment variable with a valid git repo link");
                
                Environment.Exit(1);
            }

            Console.WriteLine("Scanning repo ssh keys and adding to trusted");
            bool keyScanCompleted = await SshKeyScan.ScanAndTrust(repoLink);
            if (!keyScanCompleted)
            {
                Console.WriteLine("Scanning failed, check repo link");
                
                Environment.Exit(1);
            }
            
            bool cloneCompleted = await Git.Clone(repoLink);

            if (!cloneCompleted)
            {
                Console.WriteLine("Clone failed, exiting");
                
                Environment.Exit(1);
            }
        }
        else
        {
            Console.WriteLine("Git Repo is already cloned");
            Console.WriteLine("Pulling...");
            
            bool pullCompleted = await Git.Pull();
            if (!pullCompleted)
            {
                Console.WriteLine("Failed to pull repo");
            }
        }

        lastCommitTime = await Git.GetLastCommitTime();
        Console.WriteLine("Last commit time: " + lastCommitTime);
        
        Console.WriteLine("Building mdBook");
        await MdBook.Build();
        
        Console.WriteLine("Staring http server (port 80)");
        Python3.StartHttpServer();

        bool usingAnyUpdateMethod = false;
        
        string? shouldUseWebHook = Environment.GetEnvironmentVariable("USE_WEB_HOOK");
        if (!string.IsNullOrEmpty(shouldUseWebHook))
        {
            Console.WriteLine("Detected USE_WEB_HOOK - starting http web hook listener");

            StartWebHook();
            usingAnyUpdateMethod = true;
        }

        string? shouldPullOnInterval = Environment.GetEnvironmentVariable("USE_PULL_ON_INTERVAL");
        if (!string.IsNullOrEmpty(shouldPullOnInterval))
        {
            Console.WriteLine("Detected USE_PULL_ON_INTERVAL - will pull repo every set time interval");

            StartRepoPullOnInterval();
            usingAnyUpdateMethod = true;
        }

        if (!usingAnyUpdateMethod)
        {
            Console.WriteLine("No update method used, define environment variables: USE_WEB_HOOK or USE_PULL_ON_INTERVAL (optionally both)");
            Console.WriteLine("No point running without a way to update - exiting");
            
            Environment.Exit(1);
        }
        
        while (!Environment.HasShutdownStarted)
        {
            await Task.Delay(10_000);
        }
    }

    static void StartWebHook()
    {
        WebHookListener.ReceivedUpdate +=
            async () =>
            {
                Console.WriteLine("Web hook request received");
                Console.WriteLine("Pulling repo...");
                await Git.Pull();

                string newCommitTime = await Git.GetLastCommitTime();
                if (lastCommitTime == newCommitTime)
                {
                    Console.WriteLine("No new commits - skipping mdBook rebuild");
                    return;
                }
                
                lastCommitTime = newCommitTime;

                Console.WriteLine("Building mdBook...");
                await MdBook.Build();
                
                Console.WriteLine("Update completed");
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
                Console.WriteLine("Detected REPO_PULL_INTERVAL_HOURS variable");

                if (int.TryParse(repoPullInterval, out int repoPullIntervalHours))
                {
                    pullIntervalTimeSpan = TimeSpan.FromHours(repoPullIntervalHours);
                    
                    Console.WriteLine("Successfully parsed target interval: " + repoPullIntervalHours + 
                                      " (pull every " + pullIntervalTimeSpan.TotalHours + " hours)");
                }
                else
                {
                    Console.WriteLine("Failed to parse REPO_PULL_INTERVAL_HOURS, using default pull interval: " + pullIntervalTimeSpan.TotalHours + " hours");
                }
            }
            else
            {
                Console.WriteLine("No REPO_PULL_INTERVAL_HOURS variable detected - default interval: " + pullIntervalTimeSpan.TotalHours + " hours");
            }

            while (!Environment.HasShutdownStarted)
            {
                await Task.Delay((int)pullIntervalTimeSpan.TotalMilliseconds);
                
                Console.WriteLine("Pulling on interval");
                
                bool successfulPull = await Git.Pull();
                if (!successfulPull)
                {
                    Console.WriteLine("Failed to pull");
                    continue;
                }

                string newCommitTime = await Git.GetLastCommitTime();
                if (lastCommitTime != newCommitTime)
                {
                    lastCommitTime = newCommitTime;

                    Console.WriteLine("Detected a new commit, rebuilding mdBook");
                    
                    await MdBook.Build();
                }
                else
                {
                    Console.WriteLine("No new commits - skipping mdBook rebuild");
                }
            }
            
        }, TaskCreationOptions.LongRunning);
    }
}
