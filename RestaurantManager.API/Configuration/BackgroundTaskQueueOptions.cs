using System.ComponentModel.DataAnnotations;

public class BackgroundTaskQueueOptions
{
    public const string Name = "ProcessingQueue";

    [Required] public int Capacity { get; init; }
}