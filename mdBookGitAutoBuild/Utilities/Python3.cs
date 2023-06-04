using System.Text;
using CliWrap;

namespace mdBookGitAutoBuild.Utilities;

public static class Python3
{
    static readonly string targetDirectory = Directory.GetCurrentDirectory() + "/GitRepo/book";

    public static void StartHttpServer()
    {
        // For some reason the http server needs to have the Standard Error Pipe pointed at something - else it just sends back empty responses
        // So we give it something - a StringBuilder with a maximum capacity of 0
        StringBuilder output = new(0);

        Cli.Wrap("python3")
            .WithArguments("-m http.server 80")
            .WithWorkingDirectory(targetDirectory)
            .WithStandardErrorPipe(PipeTarget.ToStringBuilder(output))
            .ExecuteAsync();
    }
}