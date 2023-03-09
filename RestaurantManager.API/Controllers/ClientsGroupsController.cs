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

    public ClientsGroupsController(IRestaurantManager restaurantManager)
    {
        _restaurantManager = restaurantManager ?? throw new ArgumentNullException();
    }

    [HttpGet(Name = "{groupId}")]
    public ActionResult<Table> Get(Guid groupId)
    {
        try
        {
            var table = _restaurantManager.Lookup(groupId);

            return table;
        }
        catch (ClientsGroupNotFoundException _)
        {
            return NotFound();
        }
    }

    [HttpPost(Name = "{size}")]
    [ProducesResponseType(201)]
    public ActionResult<Guid> OnArrive(int size)
    {
        try
        {
            var clientsGroupId = _restaurantManager.OnArrive(size);

            return clientsGroupId;
        }
        catch (InvalidClientsGroupSizeException _)
        {
            return UnprocessableEntity();
        }
    }

    [HttpDelete(Name = "{groupId}")]
    public ActionResult OnLeave(Guid groupId)
    {
        try
        {
            _restaurantManager.OnLeave(groupId);

            return Ok();
        }
        catch (ClientsGroupNotFoundException _)
        {
            return NotFound();
        }
    }
}