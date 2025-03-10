using System.Threading.Channels;
using System.Threading.Tasks;

namespace CIBC.SourcesUsesAllocation;

public interface IChannelWriter<T>
{
    Task WriteAsync(Channel<T> channel, T item, string itemType);
}