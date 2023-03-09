using RestaurantManager.API.Exceptions;
using RestaurantManager.API.Models;

namespace RestaurantManager.API.Interfaces;

public interface IRestaurantManager
{
    /// <summary>
    /// Adds clients group to queue
    /// </summary>
    /// <param name="groupSize">Size of clients group</param>
    /// <returns>Returns identity of added group</returns>
    /// <exception cref="InvalidClientsGroupSizeException"></exception>
    Guid OnArrive(int groupSize);

    /// <summary>
    /// Removes clients group from queue or table 
    /// </summary>
    /// <param name="groupId">Identity of leaving clients group</param>
    /// <exception cref="ClientsGroupNotFoundException"></exception>
    void OnLeave(Guid groupId);

    /// <summary>
    /// Returns table where a given clients group is seated, or null if it is still queuing or has already left
    /// </summary>
    /// <param name="groupId">Identity of clients group</param>
    /// <returns></returns>
    public Table? Lookup(Guid groupId);

    /// <summary>
    /// Starts clients group queue processing 
    /// </summary>
    public void ProcessQueue();
}