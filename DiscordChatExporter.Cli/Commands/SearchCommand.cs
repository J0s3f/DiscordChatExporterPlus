using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CliFx.Binding;
using CliFx;
using CliFx.Infrastructure;
using DiscordChatExporter.Core.Exporting.Library;

namespace DiscordChatExporter.Cli.Commands;

[Command("search", Description = "Full-text search across SQLite (.db) exports.")]
public partial class SearchCommand : ICommand
{
    [CommandParameter(0, Description = "Search query.")]
    public required string Query { get; set; }

    [CommandOption(
        "dir",
        'd',
       
        Description = "Folder(s) to scan recursively for SQLite exports."
    )]
    public required IReadOnlyList<string> DirPaths { get; set; }

    [CommandOption("limit", 'l', Description = "Maximum number of hits per database.")]
    public int Limit { get; set; } = 20;

    public async ValueTask ExecuteAsync(IConsole console)
    {
        var cancellationToken = console.RegisterCancellationHandler();

        var databases = DirPaths
            .Select(Path.GetFullPath)
            .Where(Directory.Exists)
            .SelectMany(d => Directory.EnumerateFiles(d, "*.db", SearchOption.AllDirectories))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (databases.Length == 0)
            throw new CommandException("No SQLite (.db) exports found in the specified folder(s).");

        var hits = await SqliteExportReader.SearchAcrossAsync(
            databases,
            Query,
            Math.Max(1, Limit),
            cancellationToken
        );

        if (hits.Count == 0)
        {
            await console.Output.WriteLineAsync("No matching messages.");
            return;
        }

        foreach (var hit in hits)
        {
            using (console.WithForegroundColor(ConsoleColor.DarkGray))
                await console.Output.WriteAsync($"{Path.GetFileName(hit.DatabaseFilePath)} | {hit.Timestamp} | ");

            using (console.WithForegroundColor(ConsoleColor.White))
                await console.Output.WriteAsync(hit.AuthorName + ": ");

            await console.Output.WriteLineAsync(hit.Snippet.ReplaceLineEndings(" "));
        }
    }
}
