using System.Text;
using CliWrap;

namespace mdBookGitAutoBuild.Utilities;

public static class SshKeyScan
{
    public static async Task<bool> ScanAndTrust(string repoLink)
    {
        string domain;
        int port = 22;

        bool isLongVersion = repoLink.Contains("ssh://");
        
        if (isLongVersion)
        {
            if (!GetDomainAndPortForLongVersion(repoLink, out domain, out port))
            {
                return false;
            }
        }
        else
        {
            if (!GetDomainForNormalVersion(repoLink, out domain))
            {
                return false;
            }
        }

        string arguments;
        
        if (isLongVersion)
        {
            arguments = "-p " + port + " " + domain;
        }
        else
        {
            arguments = domain;
        }

        StringBuilder output = new();

        try
        {
            await Cli.Wrap("ssh-keyscan")
                .WithArguments(arguments)
                .WithStandardOutputPipe(PipeTarget.ToStringBuilder(output))
                .WithStandardErrorPipe(PipeTarget.ToStringBuilder(output))
                .ExecuteAsync();
        }
        catch (Exception)
        {
            Console.WriteLine("Failed to run ssh-keyscan, output: ");
            Console.WriteLine(output.ToString());
            return false;
        }

        const string knownHostsFile = "/root/.ssh/known_hosts";

#if DEBUG
        Console.WriteLine("Keyscan output:");
        Console.WriteLine(output.ToString());
#endif
        
        if (File.Exists(knownHostsFile))
        {
            await File.AppendAllTextAsync(knownHostsFile, output.ToString());
        }
        else
        {
            await File.WriteAllTextAsync(knownHostsFile, output.ToString());
        }

        return true;
    }

    static bool GetDomainAndPortForLongVersion(string repoLink, out string domain, out int port)
    {
        domain = string.Empty;
        port = 0;
        
        try
        {
            string[] splitFront = repoLink.Split('@');
            string[] splitRear = splitFront[1].Split(':');

            domain = splitRear[0];

            string[] splitPort = splitRear[1].Split("/");

            port = int.Parse(splitPort[0]);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    static bool GetDomainForNormalVersion(string repoLink, out string domain)
    {
        domain = string.Empty;
        
        try
        {
            string[] splitFront = repoLink.Split('@');
            string[] splitRear = splitFront[1].Split(':');

            domain = splitRear[0];
            
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}