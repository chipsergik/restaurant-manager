using Microsoft.AspNetCore.Mvc;
using RestaurantManager.API.Exceptions;
using RestaurantManager.API.Interfaces;
using RestaurantManager.API.Models;

namespace RestaurantManager.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientsGroupsController : ControllerBase
{
    private readonly IRestaurantManager _restaurantManager;
    private readonly IBackgroundTaskQueue _taskQueue;

    public ClientsGroupsController(IRestaurantManager restaurantManager, IBackgroundTaskQueue taskQueue)
    {
        _restaurantManager = restaurantManager ?? throw new ArgumentNullException();
        _taskQueue = taskQueue ?? throw new ArgumentNullException();
    }

    [HttpGet(Name = "{groupId}")]
    public ActionResult<Table> Get(Guid groupId)
    {
        try
        {
            var table = _restaurantManager.Lookup(groupId);

            if (table == null)
            {
                return NoContent();
            }

            return table;
        }
        catch (ClientsGroupNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost(Name = "{size}")]
    [ProducesResponseType(201)]
    public async Task<ActionResult<Guid>> OnArrive(int size)
    {
        try
        {
            var clientsGroupId = _restaurantManager.OnArrive(size);

            await _taskQueue.QueueBackgroundWorkItemAsync(_restaurantManager.ProcessQueue);

            return clientsGroupId;
        }
        catch (InvalidClientsGroupSizeException)
        {
            return UnprocessableEntity();
        }
    }

    [HttpDelete(Name = "{groupId}")]
    public async Task<ActionResult> OnLeave(Guid groupId)
    {
        try
        {
            _restaurantManager.OnLeave(groupId);

            await _taskQueue.QueueBackgroundWorkItemAsync(_restaurantManager.ProcessQueue);

            return Ok();
        }
        catch (ClientsGroupNotFoundException)
        {
            return NotFound();
        }
    }
}