using RestaurantManager.API.Models;

namespace RestaurantManager.API.Interfaces;

public interface ITablesRepository
{
    void AddEmptyTable(Table table);

    void RemoveEmptyTable(Table matchingTable);

    IEnumerable<Table> GetAll();

    IEnumerable<Table>? GetEmptyTablesForGroup(int groupSize);

    IDictionary<int, IList<Table>> GetGroupedEmptyTables();
}