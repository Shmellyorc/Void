using CommandLine;

using Void.Packer.Encryption;

namespace Void.Packer.CLI.Commands;

[Verb("build", HelpText = "Build packs from content directory")]
public class BuildCommand
{
    [Option('c', "content", Required = true, HelpText = "Content directory to pack")]
    public string ContentPath { get; set; }

    [Option('o', "output", Required = true, HelpText = "Output directory for .pack and .key files")]
    public string OutputPath { get; set; }

    [Option('n', "name", Default = "GameAssets",  HelpText = "Base name for output files")]
    public string Name { get; set; }

    [Option('i', "include", Separator = ',', HelpText = "Include patterns (e.g., **/*.png) - can specify multiple")]
    public IEnumerable<string> IncludePatterns { get; set; }

    [Option('e', "exclude", Separator = ',', HelpText = "Exclude patterns (e.g., **/Backup/**) - can specify multiple")]
    public IEnumerable<string> ExcludePatterns { get; set; }

    [Option("encrypt", Default = true, HelpText = "Enable encryption")]
    public bool Encrypt { get; set; }

    [Option("compress", Default = CompressionAlgorithm.Deflate, HelpText = "Compression algorithm: None, Deflate, Brotli")]
    public CompressionAlgorithm Compression { get; set; }

    [Option("adaptive", Default = true, HelpText = "Use adaptive compression")]
    public bool AdaptiveCompression { get; set; }

    [Option("max-files", Default = 65535, HelpText = "Maximum files per pack")]
    public int MaxFilesPerPack { get; set; }

    [Option("compression-level", Default = 6, HelpText = "Compression level (1-9)")]
    public int CompressionLevel { get; set; }

    [Option("case-sensitive", Default = false, HelpText = "Case sensitive virtual paths")]
    public bool CaseSensitive { get; set; }

    [Option('v', "verbose", Default = false, HelpText = "Verbose output")]
    public bool Verbose { get; set; }

    [Option("no-wait", Default = false, HelpText = "Don't wait for key press after completion")]
    public bool NoWait { get; set; }

    [Option("no-color", Default = false, HelpText = "Disable colored output")]
    public bool NoColor { get; set; }
}