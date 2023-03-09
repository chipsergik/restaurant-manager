using System.Threading.Channels;
using Microsoft.Extensions.Options;

public class BackgroundTaskQueue : IBackgroundTaskQueue
{
    private readonly Channel<Action> _queue;

    public BackgroundTaskQueue(IOptions<BackgroundTaskQueueOptions> settings)
    {
        // Capacity should be set based on the expected application load and
        // number of concurrent threads accessing the queue.            
        // BoundedChannelFullMode.Wait will cause calls to WriteAsync() to return a task,
        // which completes only when space became available. This leads to backpressure,
        // in case too many publishers/calls start accumulating.
        var options = new BoundedChannelOptions(settings.Value.Capacity)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        _queue = Channel.CreateBounded<Action>(options);
    }

    public async ValueTask QueueBackgroundWorkItemAsync(
        Action workItem)
    {
        if (workItem == null)
        {
            throw new ArgumentNullException(nameof(workItem));
        }

        await _queue.Writer.WriteAsync(workItem);
    }

    public async ValueTask<Action> DequeueAsync(
        CancellationToken cancellationToken)
    {
        var workItem = await _queue.Reader.ReadAsync(cancellationToken);

        return workItem;
    }
}