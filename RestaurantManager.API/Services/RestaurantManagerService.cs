using RestaurantManager.API.Exceptions;
using RestaurantManager.API.Interfaces;
using RestaurantManager.API.Models;

namespace RestaurantManager.API.Services;

public class RestaurantManagerService : IRestaurantManager
{
    private readonly IOrderedEnumerable<Table> _allTables;
    private readonly IDictionary<int, IList<Table>> _emptyTables;
    private readonly IList<ClientsGroup> _clientsGroupsQueue;
    private readonly IClientsGroupsRepository _clientsGroupsRepository;

    private readonly object _lockObject = new();

    public RestaurantManagerService(
        IReadOnlyCollection<Table> tables,
        IClientsGroupsRepository clientsGroupsRepository,
        IEnumerable<ClientsGroup> clientsGroupsQueue
    )
    {
        _clientsGroupsRepository = clientsGroupsRepository;
        _clientsGroupsQueue = clientsGroupsQueue.ToList();
        _allTables = tables.OrderBy(table => table.Size) ?? throw new ArgumentNullException(nameof(tables));
        _emptyTables = new Dictionary<int, IList<Table>>(
            Enumerable.Range(2, 5).Select(size =>
                new KeyValuePair<int, IList<Table>>(
                    size,
                    _allTables.Where(table => table.Size == size && table.IsEmpty()).ToList()
                )));
    }

    public Guid OnArrive(int groupSize)
    {
        var group = new ClientsGroup(groupSize);

        var groupId = _clientsGroupsRepository.Create(group);

        lock (_lockObject)
        {
            _clientsGroupsQueue.Add(group);

            ProcessQueue();
        }

        return groupId;
    }

    public void OnLeave(Guid groupId)
    {
        var group = _clientsGroupsRepository.Get(groupId);

        if (group == null)
        {
            throw new ClientsGroupNotFoundException(groupId);
        }

        lock (_lockObject)
        {
            var table = group.Table;
            
            if (table == null)
            {
                _clientsGroupsQueue.Remove(group);
                _clientsGroupsRepository.Remove(group.Id);
                return;
            }

            _clientsGroupsRepository.Remove(group.Id);
            group.RemoveTable(table);

            if (table.IsEmpty())
            {
                _emptyTables[table.Size].Add(table);
            }

            ProcessQueue();
        }
    }

    public Table? Lookup(Guid groupId)
    {
        var group = _clientsGroupsRepository.Get(groupId);

        return group?.Table;
    }

    private void ProcessQueue()
    {
        foreach (var group in _clientsGroupsQueue.ToList())
        {
            var matchingTable =
                FindExactlyMatchingEmptyTable(group.Size) ??
                FindMinimalSizeEmptySuitableTable(group.Size) ??
                FindNonEmptySuitableTable(group.Size);

            if (matchingTable == null) continue;

            if (matchingTable.IsEmpty())
            {
                _emptyTables[matchingTable.Size].Remove(matchingTable);
            }

            group.AssignTable(matchingTable);
            
            _clientsGroupsQueue.Remove(group);
        }

        Table? FindExactlyMatchingEmptyTable(int groupSize) =>
            _emptyTables.ContainsKey(groupSize)
                ? _emptyTables[groupSize].FirstOrDefault()
                : null;

        Table? FindMinimalSizeEmptySuitableTable(int groupSize) =>
            _emptyTables.FirstOrDefault(tablesSet =>
                             tablesSet.Key > groupSize && tablesSet.Value.Any())
                        .Value?.FirstOrDefault();

        Table? FindNonEmptySuitableTable(int groupSize) =>
            _allTables.FirstOrDefault(table => table.IsEnoughRoom(groupSize));
    }
}