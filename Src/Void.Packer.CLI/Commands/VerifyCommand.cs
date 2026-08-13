namespace Void.Packer.CLI.Commands;

[Verb("verify", HelpText = "Verify pack integrity")]
public class VerifyCommand
{
    [Option("pack", Required = true, HelpText = "Pack file to verify")]
    public string PackPath { get; set; }

    [Option("key", HelpText = "Key file (required if encrypted)")]
    public string KeyPath { get; set; }

    [Option('v', "verbose", Default = false, HelpText = "Verbose output")]
    public bool Verbose { get; set; }

    [Option("no-wait", Default = false, HelpText = "Don't wait for key press after completion")]
    public bool NoWait { get; set; }

    [Option("no-color", Default = false, HelpText = "Disable colored output")]
    public bool NoColor { get; set; }
}