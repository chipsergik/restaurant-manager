using System.Collections.Concurrent;
using RestaurantManager.API.Exceptions;
using RestaurantManager.API.Interfaces;
using RestaurantManager.API.Models;

namespace RestaurantManager.API.Persistence;

public class ClientsGroupsInMemoryRepository : IClientsGroupsRepository
{
    private readonly ConcurrentDictionary<Guid, ClientsGroup> _clientsGroups;

    public ClientsGroupsInMemoryRepository()
    {
        _clientsGroups = new ConcurrentDictionary<Guid, ClientsGroup>();
    }

    public Guid Create(ClientsGroup group)
    {
        var groupId = Guid.NewGuid();
        group.Id = groupId;

        _clientsGroups[groupId] = group;
        return groupId;
    }

    public void Remove(Guid groupId)
    {
        if (!_clientsGroups.Remove(groupId, out _))
        {
            throw new ClientsGroupNotFoundException(groupId);
        }
    }

    public ClientsGroup? Get(Guid groupId)
    {
        return !_clientsGroups.ContainsKey(groupId) ? null : _clientsGroups[groupId];
    }
}