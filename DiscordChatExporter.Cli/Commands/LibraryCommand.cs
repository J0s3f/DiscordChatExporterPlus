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

[Command(
    "library",
    Description = "Lists previous exports catalogued by manifest.json files found under the given folder(s)."
)]
public partial class LibraryCommand : ICommand
{
    [CommandOption("dir", 'd', Description = "Folder(s) to scan recursively.")]
    public required IReadOnlyList<string> DirPaths { get; set; }

    public async ValueTask ExecuteAsync(IConsole console)
    {
        var cancellationToken = console.RegisterCancellationHandler();

        var dirs = new List<string>();
        foreach (var root in DirPaths)
            dirs.AddRange(await ExportCatalogBuilder.ScanForExportDirsAsync(Path.GetFullPath(root), cancellationToken));

        var entries = await ExportCatalogBuilder.BuildFromDirectoriesAsync(dirs, cancellationToken);
        if (entries.Count == 0)
            throw new CommandException("No exports catalogued in the specified folder(s).");

        foreach (var entry in entries)
        {
            using (console.WithForegroundColor(ConsoleColor.White))
                await console.Output.WriteAsync($"{entry.GuildName} / {entry.ChannelName}");

            using (console.WithForegroundColor(ConsoleColor.DarkGray))
            {
                await console.Output.WriteLineAsync(
                    $" | {entry.Format} | {entry.MessageCount:N0} messages | "
                        + $"{entry.FirstMessageTimestamp:yyyy-MM-dd} .. {entry.LastMessageTimestamp:yyyy-MM-dd} | "
                        + $"exported {entry.ExportedAt:yyyy-MM-dd HH:mm}"
                );
            }

            await console.Output.WriteLineAsync("    " + entry.File);
        }
    }
}
