using System;
using System.IO;
using DiscordChatExporter.Core.Discord;
using DiscordChatExporter.Core.Discord.Data;
using DiscordChatExporter.Core.Exporting;
using DiscordChatExporter.Core.Exporting.Filtering;
using DiscordChatExporter.Core.Exporting.Manifest;
using DiscordChatExporter.Core.Exporting.Partitioning;
using FluentAssertions;
using Xunit;

namespace DiscordChatExporter.Cli.Tests.Specs.Manifest;

public class ManifestExportSettingsSpecs
{
    private static Guild Guild() => new(new Snowflake(1), "Guild", "");

    private static Channel Channel() =>
        new(
            new Snowflake(2),
            ChannelKind.GuildTextChat,
            new Snowflake(1),
            null,
            "general",
            0,
            null,
            null,
            false,
            null
        );

    private static ExportRequest Request(
        bool markdown = true,
        bool downloadAssets = true,
        bool reuseAssets = true
    )
    {
        var output = Path.Combine(Path.GetTempPath(), "archive.json");
        var assets = Path.Combine(Path.GetTempPath(), "archive-assets");

        return new ExportRequest(
            Guild(),
            Channel(),
            output,
            assets,
            ExportFormat.Json,
            new Snowflake(10),
            new Snowflake(100),
            PartitionLimit.Null,
            MessageFilter.Parse("from:\"alice\" & has:file"),
            isReverseMessageOrder: false,
            shouldFormatMarkdown: markdown,
            shouldDownloadAssets: downloadAssets,
            shouldReuseAssets: reuseAssets,
            locale: "en-AU",
            isUtcNormalizationEnabled: true
        );
    }

    [Fact]
    public void Settings_round_trip_the_options_needed_for_continuation()
    {
        var original = Request();
        var settings = ManifestExportSettings.FromRequest(original);

        settings.ShouldDownloadAssets.Should().BeTrue();
        settings.ShouldReuseAssets.Should().BeTrue();
        settings.ShouldFormatMarkdown.Should().BeTrue();
        settings.MessageFilter.Should().Be("(from:\"alice\") & (has:file)");
        settings.Locale.Should().Be("en-AU");
        settings.IsUtcNormalizationEnabled.Should().BeTrue();
        settings.UsesDefaultAssetsDir.Should().BeFalse();

        var continued = settings.CreateContinuationRequest(
            original.Guild,
            original.Channel,
            original.OutputFilePath,
            Path.Combine(Path.GetTempPath(), "incremental.json"),
            original.Format,
            new Snowflake(90),
            before: null
        );

        continued.After.Should().Be(new Snowflake(90));
        continued.Before.Should().BeNull();
        continued.ShouldDownloadAssets.Should().BeTrue();
        continued.ShouldReuseAssets.Should().BeTrue();
        continued.ShouldFormatMarkdown.Should().BeTrue();
        continued.MessageFilter.ToExpression().Should().Be(settings.MessageFilter);
        continued.AssetsDirPath.Should().Be(original.AssetsDirPath);
        continued.Locale.Should().Be("en-AU");
        continued.IsUtcNormalizationEnabled.Should().BeTrue();
    }

    [Fact]
    public void Compatibility_detects_changed_export_settings()
    {
        var original = Request(markdown: true);
        var settings = ManifestExportSettings.FromRequest(original);

        settings.IsCompatibleWith(original).Should().BeTrue();
        settings.IsCompatibleWith(Request(markdown: false)).Should().BeFalse();
    }

    [Theory]
    [InlineData("hello", "\"hello\"")]
    [InlineData("from:alice", "from:\"alice\"")]
    [InlineData("mentions:bob | has:image", "(mentions:\"bob\") | (has:image)")]
    [InlineData("~reaction:party", "~(reaction:\"party\")")]
    public void Filter_expressions_can_be_serialized_and_parsed_again(
        string input,
        string expected
    )
    {
        var filter = MessageFilter.Parse(input);
        var expression = filter.ToExpression();

        expression.Should().Be(expected);
        MessageFilter.Parse(expression!).Should().NotBeNull();
    }

    [Theory]
    [InlineData("100")]
    [InlineData("10mb")]
    public void Partition_expressions_can_be_serialized_and_parsed_again(string input)
    {
        var partition = PartitionLimit.Parse(input);
        var expression = partition.ToExpression();

        expression.Should().NotBeNullOrWhiteSpace();
        PartitionLimit.Parse(expression!).Should().NotBeNull();
    }
}
