using System;
using System.Threading.Channels;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace CIBC.SourcesUsesAllocation;

public class ChannelWriter<T> : IChannelWriter<T>
{
    private readonly ILogger<ChannelWriter<T>> _logger;

    public ChannelWriter(ILogger<ChannelWriter<T>> logger) => _logger = logger;

    public async Task WriteAsync(Channel<T> channel, T item, string itemType)
    {
        try
        {
            if (!await channel.Writer.WaitToWriteAsync())
            {
                _logger.LogWarning("Channel closed while attempting to write {ItemType}", itemType);
                throw new ChannelClosedException($"Channel closed while writing {itemType}");
            }

            await channel.Writer.WriteAsync(item);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write {ItemType} to channel", itemType);
            throw new InvalidOperationException($"Failed to write {itemType} to channel", ex);
        }
    }
}