using System.ComponentModel.DataAnnotations;

namespace RestaurantManager.API.Controllers.Dto;

public class ClientsGroupArriveRequest
{
    [Required]
    [Range(1, 6, ErrorMessage = "Clients Group should contain from 1 to 6 clients")]
    public int Size { get; set; }
}