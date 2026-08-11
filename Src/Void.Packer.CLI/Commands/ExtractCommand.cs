using CommandLine;

namespace Void.Packer.CLI.Commands;

[Verb("extract", HelpText = "Extract all files from a pack")]
public class ExtractCommand
{
    [Option("pack", Required = true, HelpText = "Pack file to extract")]
    public string PackPath { get; set; }

    [Option('o', "output", Required = true, HelpText = "Output directory")]
    public string OutputPath { get; set; }

    [Option("key", HelpText = "Key file (required if encrypted)")]
    public string KeyPath { get; set; }

    [Option('v', "verbose", Default = false, HelpText = "Verbose output")]
    public bool Verbose { get; set; }

    [Option("no-wait", Default = false, HelpText = "Don't wait for key press after completion")]
    public bool NoWait { get; set; }

    [Option("no-color", Default = false, HelpText = "Disable colored output")]
    public bool NoColor { get; set; }
}