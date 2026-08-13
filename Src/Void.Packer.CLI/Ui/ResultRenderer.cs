namespace Void.Packer.CLI.UI;

public static class ResultRenderer
{
    public static void ShowBuildResult(
        PackResult result, 
        string outputPath, 
        List<string> outputFiles,
        List<Exception> errors,
        ProgressData progress)
    {
        var elapsed = DateTime.Now - progress.StartTime;

        var grid = new Grid();
        grid.AddColumn();
        grid.AddColumn();

        // Header
        grid.AddRow(new Text("✅ Pack complete!", new Style(Color.Green, decoration: Decoration.Bold)));

        // Stats
        grid.AddRow("  📁 Files", $"{result.TotalFilesPacked}");
        grid.AddRow("  📦 Packs", $"{result.Packs.Count}");
        grid.AddRow("  💾 Original", FormatSize(result.TotalOriginalSize));
        grid.AddRow("  📦 Packed", FormatSize(result.TotalPackedSize));
        grid.AddRow("  🔥 Compression", $"{result.CompressionRatio * 100:F1}% saved");
        grid.AddRow("  ⏱ Time", FormatTime(elapsed));

        if (errors.Count > 0)
        {
            grid.AddRow("  ⚠️ Errors", $"[red]{errors.Count}[/] files failed");
        }

        // Output files
        grid.AddRow("");
        grid.AddRow("  [bold]Output files:[/]", "");
        foreach (var file in outputFiles)
        {
            var fileName = Path.GetFileName(file);
            var fileSize = new System.IO.FileInfo(file).Length;
            var isKey = fileName.EndsWith(".key");
            var icon = isKey ? "🔑" : "📦";
            grid.AddRow($"    {icon} {fileName}", FormatSize(fileSize));
        }

        // Footer
        grid.AddRow("");
        grid.AddRow("  📂 Output", outputPath);

        AnsiConsole.Clear();
        AnsiConsole.Write(grid);
        AnsiConsole.MarkupLine($"\n[grey]Press any key to exit...[/]");
    }

    public static void ShowExtractResult(UnpackResult result, string outputPath, TimeSpan elapsed)
    {
        var grid = new Grid();
        grid.AddColumn();
        grid.AddColumn();

        grid.AddRow(new Text("✅ Extract complete!", new Style(Color.Green, decoration: Decoration.Bold)));
        grid.AddRow("  📁 Files extracted", $"{result.Files.Count}");
        
        var totalSize = result.Files.Sum(f => f.Data?.Length ?? 0);
        grid.AddRow("  💾 Total size", FormatSize(totalSize));
        grid.AddRow("  ⏱ Time", FormatTime(elapsed));
        grid.AddRow("");
        grid.AddRow("  📂 Output", outputPath);

        AnsiConsole.Clear();
        AnsiConsole.Write(grid);
    }

    public static void ShowVerifyResult(bool valid, int totalFiles, int corruptedFiles, TimeSpan elapsed)
    {
        var grid = new Grid();
        grid.AddColumn();
        grid.AddColumn();

        var status = valid ? "✅ PASSED" : "❌ FAILED";
        var color = valid ? Color.Green : Color.Red;
        grid.AddRow(new Text($"Verification complete: {status}", new Style(color, decoration: Decoration.Bold)));
        grid.AddRow("  📁 Total files", $"{totalFiles}");
        grid.AddRow("  ✅ Valid files", $"{totalFiles - corruptedFiles}");
        grid.AddRow("  ❌ Corrupted files", $"{corruptedFiles}");
        grid.AddRow("  ⏱ Time", FormatTime(elapsed));

        AnsiConsole.Clear();
        AnsiConsole.Write(grid);
    }

    public static void ShowListResult(List<string> files, bool detailed, byte[] packData, byte[] key = null)
    {
        var grid = new Grid();
        grid.AddColumn();
        grid.AddColumn();
        grid.AddColumn();
        grid.AddColumn();

        if (detailed)
        {
            grid.AddRow("[bold]Path[/]", "[bold]Size[/]", "[bold]Compressed[/]", "[bold]CRC32[/]");
            
            using var reader = new SolidPackReader(packData, key);
            foreach (var path in files)
            {
                var info = reader.GetFileInfo(path);
                grid.AddRow(
                    path,
                    FormatSize(info.UncompressedSize),
                    info.IsCompressed ? "✓" : "—",
                    $"{info.CRC32:X8}"
                );
            }
        }
        else
        {
            grid.AddRow("[bold]Path[/]");
            foreach (var path in files)
            {
                grid.AddRow(path);
            }
        }

        grid.AddRow("");
        grid.AddRow($"Total: {files.Count} files");

        AnsiConsole.Clear();
        AnsiConsole.Write(grid);
    }

    private static string FormatSize(long bytes)
    {
        string[] sizes = ["B", "KB", "MB", "GB", "TB"];
        double len = bytes;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    private static string FormatTime(TimeSpan time)
    {
        if (time.TotalHours >= 1)
            return $"{time:hh\\:mm\\:ss}";
        if (time.TotalMinutes >= 1)
            return $"{time:mm\\:ss}";
        return $"{time:ss\\s}";
    }
}