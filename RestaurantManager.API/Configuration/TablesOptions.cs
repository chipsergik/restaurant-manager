using RestaurantManager.API.Models;

namespace RestaurantManager.API.Configuration;

public class TablesOptions
{
    public const string Name = "Tables";

    public int[] Sizes { get; init; }

    public IEnumerable<Table> Tables => Sizes.Select(size => new Table(size));
}