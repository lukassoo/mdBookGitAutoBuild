using CliWrap;

namespace mdBookGitAutoBuild.Utilities;

public static class SshKeygen
{
    public static bool HasKey()
    {
        return File.Exists("/root/.ssh/id_ed25519") &&
               File.Exists("/root/.ssh/id_ed25519.pub");
    }

    public static void RemoveKey()
    {
        try
        {
            File.Delete("/root/.ssh/id_ed25519");
        }
        catch (Exception e)
        {
            Console.WriteLine("Could not delete id_ed25519 file");
        }

        try
        {
            File.Delete("/root/.ssh/id_ed25519.pub");
        }
        catch (Exception e)
        {
            Console.WriteLine("Could not delete id_ed25519.pub file");
        }
    }

    public static async Task<bool> GenerateKey()
    {
        if (HasKey())
        {
            RemoveKey();
        }

        try
        {
            await Cli.Wrap("ssh-keygen")
                .WithArguments("-t ed25519 -f /root/.ssh/id_ed25519 -C \"mdBookGitAutoBuilder\" -N \"\"")
                .ExecuteAsync();

            return true;
        }
        catch (Exception e)
        {
            return false;
        }
    }

    public static string GetPublicKey()
    {
        if (!HasKey())
        {
            Console.WriteLine("No keys found, returning empty string");
            return string.Empty;
        }

        return File.ReadAllText("/root/.ssh/id_ed25519.pub");
    }
}