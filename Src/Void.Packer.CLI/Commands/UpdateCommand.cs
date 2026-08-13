namespace Void.Packer.CLI.Commands;

[Verb("update", HelpText = "Update an existing pack")]
public class UpdateCommand
{
    [Option("pack", Required = true, HelpText = "Pack file to update")]
    public string PackPath { get; set; }

    [Option('a', "add", HelpText = "Files/folders to add (can specify multiple)")]
    public IEnumerable<string> AddPaths { get; set; }

    [Option('r', "remove", HelpText = "Files to remove (can specify multiple)")]
    public IEnumerable<string> RemovePaths { get; set; }

    [Option("key", HelpText = "Key file (required if encrypted)")]
    public string KeyPath { get; set; }

    [Option('o', "output", HelpText = "Output path for updated pack (default: overwrite)")]
    public string OutputPath { get; set; }

    [Option('v', "verbose", Default = false, HelpText = "Verbose output")]
    public bool Verbose { get; set; }

    [Option("no-wait", Default = false, HelpText = "Don't wait for key press after completion")]
    public bool NoWait { get; set; }

    [Option("no-color", Default = false, HelpText = "Disable colored output")]
    public bool NoColor { get; set; }
}