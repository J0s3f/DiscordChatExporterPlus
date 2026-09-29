using System.Globalization;

namespace DiscordChatExporter.Core.Exporting.Partitioning;

internal class FileSizePartitionLimit(long limit) : PartitionLimit
{
    public override bool IsReached(long messagesWritten, long bytesWritten) =>
        bytesWritten >= limit;

    public override string ToExpression() => limit.ToString(CultureInfo.InvariantCulture) + "b";
}
