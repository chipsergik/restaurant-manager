using System.ComponentModel.DataAnnotations;

namespace RestaurantManager.API.Configuration;

public class BackgroundTaskQueueOptions
{
    public const string Name = "ProcessingQueue";

    [Required] public int Capacity { get; init; }
}