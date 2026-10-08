using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CliFx.Binding;
using CliFx;
using CliFx.Infrastructure;
using DiscordChatExporter.Core.Exporting;
using DiscordChatExporter.Core.Exporting.Conversion;

namespace DiscordChatExporter.Cli.Commands;

[Command(
    "convert",
    Description = "Converts JSON exports to another format (HTML, TXT, CSV, SQLite) without connecting to Discord."
)]
public partial class ConvertCommand : ICommand
{
    [CommandOption("input", 'i', Description = "JSON export file(s) to convert.")]
    public required IReadOnlyList<string> InputPaths { get; set; }

    [CommandOption(
        "output",
        'o',
        Description = "Output directory. Defaults to the directory of each input file."
    )]
    public string? OutputDirPath { get; set; }

    [CommandOption(
        "format",
        'f',
        Description = "Target format(s): PlainText, HtmlDark, HtmlLight, Csv, Db. Can be repeated."
    )]
    public IReadOnlyList<ExportFormat> Formats { get; set; } = [ExportFormat.HtmlDark];

    public async ValueTask ExecuteAsync(IConsole console)
    {
        var cancellationToken = console.RegisterCancellationHandler();

        if (Formats.Contains(ExportFormat.Json))
            throw new CommandException("JSON is not a supported conversion target.");

        var failed = 0;
        foreach (var input in InputPaths)
        {
            var inputPath = Path.GetFullPath(input);
            var outputDir = OutputDirPath is not null
                ? Path.GetFullPath(OutputDirPath)
                : Path.GetDirectoryName(inputPath)!;
            Directory.CreateDirectory(outputDir);

            foreach (var format in Formats.Distinct())
            {
                var suffix = format switch
                {
                    ExportFormat.HtmlLight => ".light",
                    _ => "",
                };
                var outputPath = Path.Combine(
                    outputDir,
                    Path.GetFileNameWithoutExtension(inputPath) + suffix + "." + format.GetFileExtension()
                );

                try
                {
                    var result = await ExportConverter.ConvertAsync(
                        inputPath,
                        outputPath,
                        format,
                        cancellationToken
                    );

                    await console.Output.WriteLineAsync(
                        $"{Path.GetFileName(inputPath)} -> {outputPath} ({result.MessageCount:N0} messages)"
                    );
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    failed++;
                    using (console.WithForegroundColor(ConsoleColor.Red))
                    {
                        await console.Error.WriteLineAsync(
                            $"Failed to convert '{inputPath}' to {format.GetDisplayName()}: {ex.Message}"
                        );
                    }
                }
            }
        }

        if (failed > 0)
            throw new CommandException($"{failed} conversion(s) failed.");
    }
}
