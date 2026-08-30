namespace Void.Packer.CLI;

public static class Program
{
    private static readonly PackService _packService = new();

    public static async Task<int> Main(string[] args)
    {
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
                CaseSensitive = opts.CaseSensitive,
                ChunkSizeKB = (ushort)opts.ChunkSizeKB
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
            Directory.CreateDirectory(opts.OutputPath);

            byte[] key = LoadKey(opts.PackPath, opts.KeyPath);

            if (!Packer.TryLoadPack(opts.PackPath, key, out var reader, out var error))
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {GetErrorMessage(error)}");
                return 1;
            }

            using (reader)
            {
                var progress = new ProgressData
                {
                    TotalFiles = 0,
                    CurrentFile = string.Empty,
                    CompletedFiles = 0,
                    StartTime = DateTime.Now
                };

                var errors = new List<Exception>();
                var extractedFiles = new List<string>();

                await AnsiConsole.Status()
                    .Spinner(Spinner.Known.Dots)
                    .StartAsync("Extracting files...", async ctx =>
                    {
                        await Task.Run(() =>
                        {
                            var files = reader.ListFiles().ToList();
                            progress.TotalFiles = files.Count;

                            foreach (var path in files)
                            {
                                if (token.IsCancellationRequested)
                                    break;

                                try
                                {
                                    progress.CurrentFile = path;

                                    var data = reader.ReadFile(path);
                                    progress.CurrentFileSize = data.Length;

                                    string fullPath = Path.Combine(opts.OutputPath, path.Replace('/', Path.DirectorySeparatorChar));
                                    string directory = Path.GetDirectoryName(fullPath);

                                    if (!string.IsNullOrEmpty(directory))
                                        Directory.CreateDirectory(directory);

                                    File.WriteAllBytes(fullPath, data);

                                    progress.CompletedFiles++;
                                    progress.TotalBytesProcessed += data.Length;

                                    extractedFiles.Add(path);

                                    ctx.Status = $"Extracting: {path}";
                                }
                                catch (Exception ex)
                                {
                                    errors.Add(ex);
                                    if (opts.Verbose)
                                        AnsiConsole.MarkupLine($"[yellow]Warning:[/] Failed to extract {path}: {ex.Message}");
                                }
                            }
                        }, token);
                    });

                if (token.IsCancellationRequested)
                {
                    AnsiConsole.MarkupLine("[yellow]Operation cancelled.[/]");
                    return 1;
                }

                var elapsed = DateTime.Now - progress.StartTime;

                var grid = new Grid();
                grid.AddColumn();
                grid.AddColumn();

                grid.AddRow(new Text("✅ Extraction complete!", new Style(Color.Green, decoration: Decoration.Bold)));
                grid.AddRow("  📁 Files extracted", $"{progress.CompletedFiles}");
                grid.AddRow("  💾 Total size", FormatSize(progress.TotalBytesProcessed));
                grid.AddRow("  ⏱ Time", FormatTime(elapsed));

                if (errors.Count > 0)
                {
                    grid.AddRow("  ⚠️ Failed", $"[red]{errors.Count}[/] files");
                }

                grid.AddRow("");
                grid.AddRow("  📂 Output", opts.OutputPath);

                AnsiConsole.Clear();
                AnsiConsole.Write(grid);
                AnsiConsole.MarkupLine($"\n[grey]Press any key to exit...[/]");

                return errors.Count > 0 ? 1 : 0;
            }
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
            byte[] key = LoadKey(opts.PackPath, opts.KeyPath);

            if (!Packer.TryLoadPack(opts.PackPath, key, out var reader, out var error))
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {GetErrorMessage(error)}");
                return 1;
            }

            using (reader)
            {
                bool valid = reader.VerifyIntegrity();

                if (valid)
                    AnsiConsole.MarkupLine("[bold green]✅ Pack is valid![/]");
                else
                    AnsiConsole.MarkupLine("[bold red]❌ Pack is corrupted or tampered![/]");

                return valid ? 0 : 1;
            }
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
            byte[] key = LoadKey(opts.PackPath, opts.KeyPath);

            if (!Packer.TryLoadPack(opts.PackPath, key, out var reader, out var error))
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {GetErrorMessage(error)}");
                return 1;
            }

            using (reader)
            {
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
            byte[] key = LoadKey(opts.PackPath, opts.KeyPath);

            if (!Packer.TryLoadPack(opts.PackPath, key, out var reader, out var error))
            {
                AnsiConsole.MarkupLine($"[red]Error:[/] {GetErrorMessage(error)}");
                return 1;
            }

            using (reader)
            {
                // Gather files to add
                var filesToAdd = new List<PackFile>();
                if (opts.AddPaths != null)
                {
                    foreach (var path in opts.AddPaths)
                    {
                        if (Directory.Exists(path))
                        {
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

                AnsiConsole.MarkupLine("[yellow]Updating pack...[/]");

                var result = await Task.Run(() =>
                {
                    var options = new PackOptions
                    {
                        Encrypt = true,
                        Compression = CompressionAlgorithm.Deflate,
                        AdaptiveCompression = true,
                        MaxFilesPerPack = 65535,
                        CompressionLevel = 6,
                        CaseSensitive = false,
                        ChunkSizeKB = 1024
                    };

                    var packData = File.ReadAllBytes(opts.PackPath);

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

                string outputPath = opts.OutputPath ?? opts.PackPath;
                File.WriteAllBytes(outputPath, result.Data);

                if (result.Key != null)
                {
                    string keyPath = Path.ChangeExtension(outputPath, ".key");
                    File.WriteAllBytes(keyPath, result.Key);
                    AnsiConsole.MarkupLine($"  [green]✓[/] Updated key: {Path.GetFileName(keyPath)}");
                }

                AnsiConsole.MarkupLine("\n[bold green]✅ Update complete![/]");
                AnsiConsole.MarkupLine($"  ➕ Added: [yellow]{result.AddedFiles.Count}[/] files");
                AnsiConsole.MarkupLine($"  🔄 Updated: [yellow]{result.UpdatedFiles.Count}[/] files");
                AnsiConsole.MarkupLine($"  ❌ Removed: [yellow]{result.RemovedFiles.Count}[/] files");

                if (!string.IsNullOrEmpty(opts.OutputPath) && opts.OutputPath != opts.PackPath)
                {
                    AnsiConsole.MarkupLine($"  📂 Output: [green]{opts.OutputPath}[/]");
                }

                return 0;
            }
        }
        catch (Exception ex)
        {
            AnsiConsole.MarkupLine($"[red]Error:[/] {ex.Message}");
            if (opts.Verbose)
                AnsiConsole.WriteException(ex);
            return 1;
        }
    }

    private static byte[] LoadKey(string packPath, string keyPath)
    {
        if (!string.IsNullOrEmpty(keyPath))
        {
            if (!File.Exists(keyPath))
            {
                throw new FileNotFoundException($"Key file not found: {keyPath}");
            }
            return File.ReadAllBytes(keyPath);
        }

        string autoKeyPath = Path.ChangeExtension(packPath, ".key");
        if (File.Exists(autoKeyPath))
            return File.ReadAllBytes(autoKeyPath);

        return null;
    }

    private static string GetErrorMessage(PackError error) => error switch
    {
        PackError.None => "No error.",
        PackError.PackNotFound => "Pack file not found.",
        PackError.InvalidMagicBytes => "File is not a valid SolidPack archive.",
        PackError.UnsupportedVersion => "Pack was created with a newer version.",
        PackError.PackTooSmall => "Pack file is too small to be valid.",
        PackError.MissingKey => "Pack is encrypted but no key was found.",
        PackError.InvalidKey => "Invalid encryption key.",
        PackError.HeaderCorrupted => "Pack header is corrupted.",
        PackError.FileTableCorrupted => "Pack file table is corrupted.",
        PackError.ChunkTableCorrupted => "Pack chunk table is corrupted.",
        PackError.FileNotFound => "File not found in pack.",
        PackError.ChunkCorrupted => "Pack chunk is corrupted or tampered.",
        PackError.ChunkOutOfRange => "Pack chunk index is out of range.",
        PackError.DataTruncated => "Pack data is truncated.",
        PackError.ChecksumMismatch => "Pack checksum mismatch.",
        PackError.DecompressionFailed => "File decompression failed.",
        PackError.CompressionNotSupported => "Compression algorithm not supported.",
        PackError.InvalidChunkSize => "Invalid chunk size in pack header.",
        PackError.NoFilesToPack => "No files to pack.",
        PackError.TooManyFiles => "Too many files for a single pack.",
        PackError.DuplicatePath => "Duplicate file path found.",
        PackError.EmptyVirtualPath => "File has empty virtual path.",
        PackError.FileReadFailed => "Failed to read source file.",
        PackError.PackAlreadyMounted => "Pack is already mounted.",
        PackError.PackIsDisposed => "Pack reader has been disposed.",
        _ => $"Unknown error: {error}"
    };

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