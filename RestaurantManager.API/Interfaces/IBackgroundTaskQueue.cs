namespace RestaurantManager.API.Interfaces;

public interface IBackgroundTaskQueue
{
    ValueTask QueueBackgroundWorkItemAsync(Action workItem);

    ValueTask<Action> DequeueAsync(
        CancellationToken cancellationToken);
}