using System.Text;
using CliWrap;
using Serilog;

namespace mdBookGitAutoBuild.Utilities;

public static class MdBook
{
    static readonly Serilog.ILogger log = Log.ForContext(typeof(MdBook));

    static readonly string targetDirectory = Directory.GetCurrentDirectory() + "/GitRepo";

    public static async Task<bool> Build()
    {
        StringBuilder output = new();
        
        try
        {
            await Cli.Wrap("mdbook")
                .WithArguments("build")
                .WithWorkingDirectory(targetDirectory)
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(output))
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(output))
                .ExecuteAsync();

            return true;
        }
        catch (Exception)
        {
            log.Error("Failed to build mdBook, output: \n{output}", output.ToString());
            return true;
        }
    }
}