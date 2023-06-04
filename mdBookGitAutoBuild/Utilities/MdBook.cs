using System.Text;
using CliWrap;

namespace mdBookGitAutoBuild.Utilities;

public static class MdBook
{
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
            Console.WriteLine("Failed to build mdBook, output: ");
            Console.WriteLine(output.ToString());
            return true;
        }
    }
}