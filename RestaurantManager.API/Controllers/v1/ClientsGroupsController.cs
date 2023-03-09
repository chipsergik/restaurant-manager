using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using RestaurantManager.API.Exceptions;
using RestaurantManager.API.Interfaces;
using RestaurantManager.API.Models;

namespace RestaurantManager.API.Controllers.v1;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
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
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<Table> Get([Required]Guid groupId)
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
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<Guid>> OnArrive(ClientsGroupArriveRequest request)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var clientsGroupId = _restaurantManager.OnArrive(request.Size);

            await _taskQueue.QueueBackgroundWorkItemAsync(_restaurantManager.ProcessQueue);

            return clientsGroupId;
        }
        catch (InvalidClientsGroupSizeException)
        {
            return UnprocessableEntity();
        }
    }

    [HttpDelete(Name = "{groupId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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