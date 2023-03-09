using RestaurantManager.API.Interfaces;
using RestaurantManager.API.Models;

namespace RestaurantManager.API.Persistence;

public class TablesInMemoryRepository : ITablesRepository
{
    private readonly IOrderedEnumerable<Table> _allTables;
    private readonly IDictionary<int, IList<Table>> _emptyTables;

    public TablesInMemoryRepository(IReadOnlyCollection<Table> tables)
    {
        _allTables = tables.OrderBy(table => table.Size) ?? throw new ArgumentNullException(nameof(tables));
        _emptyTables = new Dictionary<int, IList<Table>>(
            Enumerable.Range(2, 5).Select(size =>
                new KeyValuePair<int, IList<Table>>(
                    size,
                    _allTables.Where(table => table.Size == size && table.IsEmpty()).ToList()
                )));
    }

    public void AddEmptyTable(Table table)
    {
        _emptyTables[table.Size].Add(table);
    }

    public void RemoveEmptyTable(Table matchingTable)
    {
        _emptyTables[matchingTable.Size].Remove(matchingTable);
    }

    public IEnumerable<Table> GetAll()
    {
        return _allTables;
    }

    public IEnumerable<Table>? GetEmptyTablesForGroup(int groupSize)
    {
        return _emptyTables.ContainsKey(groupSize)
            ? _emptyTables[groupSize]
            : null;
    }

    public IDictionary<int, IList<Table>> GetGroupedEmptyTables()
    {
        return _emptyTables;
    }
}