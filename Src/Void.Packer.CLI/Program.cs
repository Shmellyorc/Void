namespace Void.Packer.CLI;

public static class Program
{
    private static readonly PackService _packService = new();

    public static async Task<int> Main(string[] args)
    {
        // Setup cancellation
        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (s, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
            AnsiConsole.MarkupLine("\n[yellow]Cancelling... (please wait)[/]");
        };

        return await Parser.Default.ParseArguments<
            BuildCommand,
            ExtractCommand,
            VerifyCommand,
            ListCommand,
            UpdateCommand
        >(args)
        .MapResult(
            async (BuildCommand opts) => await RunBuild(opts, cts.Token),
            async (ExtractCommand opts) => await RunExtract(opts, cts.Token),
            async (VerifyCommand opts) => await RunVerify(opts, cts.Token),
            async (ListCommand opts) => await RunList(opts, cts.Token),
            async (UpdateCommand opts) => await RunUpdate(opts, cts.Token),
            errs => Task.FromResult(1)
        );
    }

    private static async Task<int> RunBuild(BuildCommand opts, CancellationToken token)
    {
        try
        {
            // Validate
            if (!Directory.Exists(opts.ContentPath))
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] Content directory not found: {opts.ContentPath}");
                return 1;
            }

            Directory.CreateDirectory(opts.OutputPath);

            // Scan files
            var scannedFiles = _packService.ScanFiles(
                opts.ContentPath,
                opts.IncludePatterns ?? Enumerable.Empty<string>(),
                opts.ExcludePatterns ?? Enumerable.Empty<string>()
            );

            if (scannedFiles.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]Warning:[/] No files found matching patterns");
                return 0;
            }

            // Build options
            var options = new PackOptions
            {
                Encrypt = opts.Encrypt,
                Compression = opts.Compression,
                AdaptiveCompression = opts.AdaptiveCompression,
                MaxFilesPerPack = (ushort)opts.MaxFilesPerPack,
                CompressionLevel = opts.CompressionLevel,
                CaseSensitive = opts.CaseSensitive
            };

            // Progress tracking
            var progress = new ProgressData
            {
                TotalFiles = scannedFiles.Count,
                CurrentFile = string.Empty,
                CompletedFiles = 0,
                StartTime = DateTime.Now
            };

            // Pack with progress
            PackResult result = null;
            var errors = new List<Exception>();

            await AnsiConsole.Status()
                .Spinner(Spinner.Known.Dots)
                .StartAsync("Packing files...", async ctx =>
                {
                    result = await Task.Run(() =>
                    {
                        var packFiles = new List<PackFile>();
                        var processed = 0;

                        foreach (var (virtualPath, fullPath) in scannedFiles)
                        {
                            if (token.IsCancellationRequested)
                                break;

                            try
                            {
                                progress.CurrentFile = virtualPath;
                                progress.CurrentFileSize = new System.IO.FileInfo(fullPath).Length;
                                progress.CompletedFiles = processed;

                                // Update status
                                ctx.Status = $"Packing: {virtualPath} ({FormatSize(progress.CurrentFileSize)})";

                                var data = File.ReadAllBytes(fullPath);
                                packFiles.Add(new PackFile
                                {
                                    VirtualPath = virtualPath,
                                    Data = data
                                });

                                processed++;
                            }
                            catch (Exception ex)
                            {
                                errors.Add(ex);
                                if (opts.Verbose)
                                    AnsiConsole.MarkupLine($"[yellow]Warning:[/] Failed to pack {virtualPath}: {ex.Message}");
                            }
                        }

                        progress.CompletedFiles = processed;

                        if (packFiles.Count == 0)
                            throw new InvalidOperationException("No files could be packed");

                        return _packService.Build(packFiles, options);
                    }, token);
                });

            if (token.IsCancellationRequested)
            {
                AnsiConsole.MarkupLine("[yellow]Operation cancelled.[/]");
                return 1;
            }

            // Write packs
            var outputFiles = new List<string>();
            for (int i = 0; i < result.Packs.Count; i++)
            {
                var pack = result.Packs[i];
                string suffix = result.Packs.Count > 1 ? $".{i + 1}" : "";
                string packName = opts.Name ?? $"GameAssets{suffix}";
                string packPath = Path.Combine(opts.OutputPath, $"{packName}.pack");
                string keyPath = Path.Combine(opts.OutputPath, $"{packName}.key");

                File.WriteAllBytes(packPath, pack.Data);
                outputFiles.Add(packPath);

                if (pack.Key != null)
                {
                    File.WriteAllBytes(keyPath, pack.Key);
                    outputFiles.Add(keyPath);
                }
            }

            // Show result
            ResultRenderer.ShowBuildResult(result, opts.OutputPath, outputFiles, errors, progress);

            return errors.Count > 0 ? 1 : 0;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            if (opts.Verbose)
                AnsiConsole.WriteException(ex);
            return 1;
        }
    }

    private static async Task<int> RunExtract(ExtractCommand opts, CancellationToken token)
    {
        try
        {
            // ... similar pattern for extract
            // Use ProgressRenderer + ResultRenderer

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            if (opts.Verbose)
                AnsiConsole.WriteException(ex);
            return 1;
        }
    }

    private static async Task<int> RunVerify(VerifyCommand opts, CancellationToken token)
    {
        try
        {
            if (!File.Exists(opts.PackPath))
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] Pack file not found: {opts.PackPath}");
                return 1;
            }

            byte[] packData = File.ReadAllBytes(opts.PackPath);
            byte[] key = null;

            if (!string.IsNullOrEmpty(opts.KeyPath))
            {
                if (!File.Exists(opts.KeyPath))
                {
                    AnsiConsole.MarkupLine($"[red]Error:[/] Key file not found: {opts.KeyPath}");
                    return 1;
                }
                key = File.ReadAllBytes(opts.KeyPath);
            }
            else
            {
                string autoKeyPath = Path.ChangeExtension(opts.PackPath, ".key");
                if (File.Exists(autoKeyPath))
                    key = File.ReadAllBytes(autoKeyPath);
            }

            bool valid = _packService.Verify(packData, key);

            if (valid)
                AnsiConsole.MarkupLine("[bold green]✅ Pack is valid![/]");
            else
                AnsiConsole.MarkupLine("[bold red]❌ Pack is corrupted or tampered![/]");

            return valid ? 0 : 1;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            if (opts.Verbose)
                AnsiConsole.WriteException(ex);
            return 1;
        }
    }

    private static async Task<int> RunList(ListCommand opts, CancellationToken token)
    {
        try
        {
            if (!File.Exists(opts.PackPath))
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] Pack file not found: {opts.PackPath}");
                return 1;
            }

            byte[] packData = File.ReadAllBytes(opts.PackPath);
            byte[] key = null;

            if (!string.IsNullOrEmpty(opts.KeyPath))
            {
                if (!File.Exists(opts.KeyPath))
                {
                    AnsiConsole.MarkupLine($"[red]Error:[/] Key file not found: {opts.KeyPath}");
                    return 1;
                }
                key = File.ReadAllBytes(opts.KeyPath);
            }
            else
            {
                string autoKeyPath = Path.ChangeExtension(opts.PackPath, ".key");
                if (File.Exists(autoKeyPath))
                    key = File.ReadAllBytes(autoKeyPath);
            }

            // List files
            using var reader = new SolidPackReader(packData, key);
            var files = reader.ListFiles().ToList();

            if (files.Count == 0)
            {
                AnsiConsole.MarkupLine("[yellow]No files found in pack.[/]");
                return 0;
            }

            if (opts.Detailed)
            {
                var table = new Table();
                table.AddColumn("Path");
                table.AddColumn("Size", c => c.Alignment = Justify.Right);
                table.AddColumn("Compressed");
                table.AddColumn("CRC32");

                foreach (var path in files)
                {
                    var info = reader.GetFileInfo(path);
                    table.AddRow(
                        path,
                        FormatSize(info.UncompressedSize),
                        info.IsCompressed ? "✓" : "—",
                        $"{info.CRC32:X8}"
                    );
                }

                AnsiConsole.Write(table);
            }
            else
            {
                foreach (var path in files)
                    AnsiConsole.MarkupLine($"  {path}");
                AnsiConsole.MarkupLine($"\n[green]Total: {files.Count} files[/]");
            }

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            if (opts.Verbose)
                AnsiConsole.WriteException(ex);
            return 1;
        }
    }

    private static async Task<int> RunUpdate(UpdateCommand opts, CancellationToken token)
    {
        try
        {
            if (!File.Exists(opts.PackPath))
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] Pack file not found: {opts.PackPath}");
                return 1;
            }

            byte[] packData = File.ReadAllBytes(opts.PackPath);
            byte[] key = null;

            if (!string.IsNullOrEmpty(opts.KeyPath))
            {
                if (!File.Exists(opts.KeyPath))
                {
                    AnsiConsole.MarkupLine($"[red]Error:[/] Key file not found: {opts.KeyPath}");
                    return 1;
                }
                key = File.ReadAllBytes(opts.KeyPath);
            }
            else
            {
                string autoKeyPath = Path.ChangeExtension(opts.PackPath, ".key");
                if (File.Exists(autoKeyPath))
                    key = File.ReadAllBytes(autoKeyPath);
            }

            // Gather files to add
            var filesToAdd = new List<PackFile>();
            if (opts.AddPaths != null)
            {
                foreach (var path in opts.AddPaths)
                {
                    if (Directory.Exists(path))
                    {
                        // Scan directory recursively
                        var allFiles = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
                        foreach (var file in allFiles)
                        {
                            string virtualPath = Path.GetRelativePath(path, file).Replace('\\', '/');
                            filesToAdd.Add(new PackFile
                            {
                                VirtualPath = virtualPath,
                                Data = File.ReadAllBytes(file)
                            });
                        }
                    }
                    else if (File.Exists(path))
                    {
                        filesToAdd.Add(new PackFile
                        {
                            VirtualPath = Path.GetFileName(path),
                            Data = File.ReadAllBytes(path)
                        });
                    }
                    else
                    {
                        AnsiConsole.MarkupLine($"[yellow]Warning:[/] Path not found: {path}");
                    }
                }
            }

            if (filesToAdd.Count == 0 && (opts.RemovePaths == null || !opts.RemovePaths.Any()))
            {
                AnsiConsole.MarkupLine("[yellow]Warning:[/] No files to add or remove");
                return 0;
            }

            // Show progress
            AnsiConsole.MarkupLine("[yellow]Updating pack...[/]");

            // Perform update
            var result = await Task.Run(() =>
            {
                var options = new PackOptions
                {
                    Encrypt = true,
                    Compression = CompressionAlgorithm.Deflate,
                    AdaptiveCompression = true,
                    MaxFilesPerPack = 65535,
                    CompressionLevel = 6,
                    CaseSensitive = false
                };

                return _packService.Update(
                    packData,
                    filesToAdd,
                    opts.RemovePaths,
                    key,
                    options
                );
            }, token);

            if (token.IsCancellationRequested)
            {
                AnsiConsole.MarkupLine("[yellow]Operation cancelled.[/]");
                return 1;
            }

            // Write updated pack
            string outputPath = opts.OutputPath ?? opts.PackPath;
            File.WriteAllBytes(outputPath, result.Data);

            if (result.Key != null)
            {
                string keyPath = Path.ChangeExtension(outputPath, ".key");
                File.WriteAllBytes(keyPath, result.Key);
                AnsiConsole.MarkupLine($"  [green]✓[/] Updated key: {Path.GetFileName(keyPath)}");
            }

            // Show result
            AnsiConsole.MarkupLine("\n[bold green]✅ Update complete![/]");
            AnsiConsole.MarkupLine($"  ➕ Added: [yellow]{result.AddedFiles.Count}[/] files");
            AnsiConsole.MarkupLine($"  🔄 Updated: [yellow]{result.UpdatedFiles.Count}[/] files");
            AnsiConsole.MarkupLine($"  ❌ Removed: [yellow]{result.RemovedFiles.Count}[/] files");
            AnsiConsole.MarkupLine($"  📁 Total files in pack: [yellow]{result.AddedFiles.Count + result.UpdatedFiles.Count + (result.RemovedFiles.Count > 0 ? "..." : "")}[/]");

            if (!string.IsNullOrEmpty(opts.OutputPath) && opts.OutputPath != opts.PackPath)
            {
                AnsiConsole.MarkupLine($"  📂 Output: [green]{opts.OutputPath}[/]");
            }

            return 0;
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            if (opts.Verbose)
                AnsiConsole.WriteException(ex);
            return 1;
        }
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
}