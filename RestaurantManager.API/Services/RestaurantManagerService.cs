using RestaurantManager.API.Exceptions;
using RestaurantManager.API.Interfaces;
using RestaurantManager.API.Models;
using RestaurantManager.API.Utils;

namespace RestaurantManager.API.Services;

public class RestaurantManagerService : IRestaurantManager
{
    private readonly IClientsGroupsRepository _clientsGroupsRepository;
    private readonly ILogger<RestaurantManagerService> _logger;

    private readonly IOrderedEnumerable<Table> _allTables;
    private readonly IDictionary<int, IList<Table>> _emptyTables;
    private readonly IList<ClientsGroup> _clientsGroupsQueue;

    private readonly object _lockObject = new();

    public RestaurantManagerService(
        IReadOnlyCollection<Table> tables,
        IClientsGroupsRepository clientsGroupsRepository,
        ILogger<RestaurantManagerService> logger,
        IEnumerable<ClientsGroup> clientsGroupsQueue
    )
    {
        _clientsGroupsRepository =
            clientsGroupsRepository ?? throw new ArgumentNullException(nameof(clientsGroupsRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _clientsGroupsQueue =
            clientsGroupsQueue.ToList() ?? throw new ArgumentNullException(nameof(clientsGroupsQueue));
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

        _clientsGroupsQueue.Add(group);
        _logger.LogDebug(LoggingEvents.RestaurantManagerOnArrive, "Group {Group} added to queue", group);

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
                _logger.LogDebug(LoggingEvents.RestaurantManagerOnLeave, "Group {Group} removed from queue", group);

                return;
            }

            _clientsGroupsRepository.Remove(group.Id);
            group.RemoveTable(table);

            if (table.IsEmpty())
            {
                _emptyTables[table.Size].Add(table);
            }
        }
    }

    public Table? Lookup(Guid groupId)
    {
        var group = _clientsGroupsRepository.Get(groupId);

        return group?.Table;
    }

    public void ProcessQueue()
    {
        _logger.LogDebug(LoggingEvents.RestaurantManagerProcessQueueStarted, "Queue processing has started");

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

        _logger.LogDebug(LoggingEvents.RestaurantManagerProcessQueueFinished, "Queue processing has finished");

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