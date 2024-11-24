using System.Text;
using CliWrap;
using Serilog;

namespace mdBookGitAutoBuild.Utilities;

public static class Git
{
    static readonly Serilog.ILogger log = Log.ForContext(typeof(Git));
    
    static readonly string repoDirectory = Directory.GetCurrentDirectory() + "/GitRepo";

    public static bool IsRepoCloned()
    {
        return Directory.Exists(repoDirectory) &&
               Directory.Exists(repoDirectory + "/.git");
    }
    
    public static async Task<bool> Clone(string repositoryLink)
    {
        if (IsRepoCloned())
        {
            log.Information("Repository already cloned");
            return false;
        }
        
        if (!Directory.Exists(repoDirectory))
        {
            Directory.CreateDirectory(repoDirectory);
        }

        log.Information("Cloning...");

        StringBuilder output = new();
        
        try
        {
            await Cli.Wrap("git")
                .WithArguments("clone " + repositoryLink + " .")
                .WithWorkingDirectory(repoDirectory)
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(output))
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(output))
                .ExecuteAsync();
        }
        catch (Exception)
        {
            log.Error("Failed to clone, output: \n{output}", output.ToString());
            return false;
        }
        
        log.Information("Cloning completed");
        return true;
    }
    
    public static async Task<bool> Pull()
    {
        StringBuilder output = new();

        try
        {
            await Cli.Wrap("git")
                .WithArguments("pull")
                .WithWorkingDirectory(repoDirectory)
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(output))
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(output))
                .ExecuteAsync();
            
            return true;
        }
        catch (Exception)
        {
            log.Error("Failed to pull repo, output: \n{output}", output.ToString());
            return false;
        }
    }
    
    public static async Task<string> GetLastCommitTime()
    {
        StringBuilder commandOutput = new();
        
        await Cli.Wrap("git")
            .WithArguments("log -1 --format=%at")
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(commandOutput))
            .WithStandardErrorPipe(PipeTarget.ToStringBuilder(commandOutput))
            .WithWorkingDirectory(repoDirectory)
            .WithValidation(CommandResultValidation.None)
            .ExecuteAsync();
        
        return commandOutput.ToString().TrimEnd('\n');
    }
}