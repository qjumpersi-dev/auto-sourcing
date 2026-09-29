using System.Threading.Channels;

namespace AutoSourcing.API.BackgroundServices;

public interface ICampaignRunQueue
{
    ValueTask EnqueueAsync(int campaignId, CancellationToken cancellationToken = default);
    IAsyncEnumerable<int> ReadAllAsync(CancellationToken cancellationToken = default);
}

public class CampaignRunQueue : ICampaignRunQueue
{
    private readonly Channel<int> _channel = Channel.CreateUnbounded<int>();

    public ValueTask EnqueueAsync(int campaignId, CancellationToken cancellationToken = default)
    {
        return _channel.Writer.WriteAsync(campaignId, cancellationToken);
    }

    public IAsyncEnumerable<int> ReadAllAsync(CancellationToken cancellationToken = default)
    {
        return _channel.Reader.ReadAllAsync(cancellationToken);
    }
}
