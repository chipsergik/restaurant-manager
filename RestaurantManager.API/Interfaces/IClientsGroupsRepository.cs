using RestaurantManager.API.Models;

namespace RestaurantManager.API.Interfaces;

public interface IClientsGroupsRepository
{
    public Guid Create(ClientsGroup group);

    public void Remove(Guid groupId);

    public ClientsGroup? Get(Guid groupId);
}