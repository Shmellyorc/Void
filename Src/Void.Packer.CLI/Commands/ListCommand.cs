using CommandLine;

namespace Void.Packer.CLI.Commands;

[Verb("list", HelpText = "List files in a pack")]
public class ListCommand
{
    [Option("pack", Required = true, HelpText = "Pack file to list")]
    public string PackPath { get; set; }

    [Option("key", HelpText = "Key file (required if encrypted)")]
    public string KeyPath { get; set; }

    [Option("detailed", Default = false, HelpText = "Show detailed info (size, compression, CRC)")]
    public bool Detailed { get; set; }

    [Option('v', "verbose", Default = false, HelpText = "Verbose output")]
    public bool Verbose { get; set; }

    [Option("no-wait", Default = false, HelpText = "Don't wait for key press after completion")]
    public bool NoWait { get; set; }

    [Option("no-color", Default = false, HelpText = "Disable colored output")]
    public bool NoColor { get; set; }
}