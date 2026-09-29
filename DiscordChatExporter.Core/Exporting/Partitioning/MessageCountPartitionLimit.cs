using System.Globalization;

namespace DiscordChatExporter.Core.Exporting.Partitioning;

internal class MessageCountPartitionLimit(long limit) : PartitionLimit
{
    public override bool IsReached(long messagesWritten, long bytesWritten) =>
        messagesWritten >= limit;

    public override string ToExpression() => limit.ToString(CultureInfo.InvariantCulture);
}
