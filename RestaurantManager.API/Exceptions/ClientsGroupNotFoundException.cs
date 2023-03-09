namespace RestaurantManager.API.Exceptions;

public class ClientsGroupNotFoundException : Exception
{
    public ClientsGroupNotFoundException(Guid groupId) : base(
        message: $"Clients Group with id: {groupId} not found")
    {
    }
}