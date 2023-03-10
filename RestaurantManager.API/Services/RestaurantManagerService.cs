using RestaurantManager.API.Exceptions;
using RestaurantManager.API.Interfaces;
using RestaurantManager.API.Models;
using RestaurantManager.API.Utils;

namespace RestaurantManager.API.Services;

public class RestaurantManagerService : IRestaurantManager
{
    private readonly ILogger<RestaurantManagerService> _logger;
    private readonly IClientsGroupsRepository _clientsGroupsRepository;
    private readonly ITablesRepository _tablesRepository;

    private readonly IList<ClientsGroup> _clientsGroupsQueue;

    private readonly object _lockObject = new();

    public RestaurantManagerService(
        ITablesRepository tablesRepository,
        IClientsGroupsRepository clientsGroupsRepository,
        ILogger<RestaurantManagerService> logger,
        IEnumerable<ClientsGroup> clientsGroupsQueue
    )
    {
        _tablesRepository = tablesRepository ?? throw new ArgumentNullException(nameof(tablesRepository));
        _clientsGroupsRepository =
            clientsGroupsRepository ?? throw new ArgumentNullException(nameof(clientsGroupsRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _clientsGroupsQueue =
            clientsGroupsQueue.ToList() ?? throw new ArgumentNullException(nameof(clientsGroupsQueue));
    }

    public Guid OnArrive(int groupSize)
    {
        var group = new ClientsGroup(groupSize);

        var groupId = _clientsGroupsRepository.Create(group);

        _clientsGroupsQueue.Add(group);
        _logger.LogInformation(LoggingEvents.RestaurantManagerOnArrive, "Group {Group} added to queue", group);

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
                _logger.LogInformation(LoggingEvents.RestaurantManagerOnLeave, "Group {Group} removed from queue", group);

                return;
            }

            _clientsGroupsRepository.Remove(group.Id);
            group.RemoveTable(table);

            if (table.IsEmpty())
            {
                _tablesRepository.AddEmptyTable(table);
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
        _logger.LogInformation(LoggingEvents.RestaurantManagerProcessQueueStarted,
            "ClientsGroups processing has started. Queue length: {Length}", _clientsGroupsQueue.Count());

        foreach (var group in _clientsGroupsQueue.ToList())
        {
            var matchingTable =
                FindExactlyMatchingEmptyTable(group.Size) ??
                FindMinimalSizeEmptySuitableTable(group.Size) ??
                FindNonEmptySuitableTable(group.Size);

            if (matchingTable == null)
            {
                continue;
            }

            _logger.LogInformation(LoggingEvents.RestaurantManagerProcessQueueTableFound,
                "Table with size: {TableSize} found for {Group}", matchingTable.Size, group);
            if (matchingTable.IsEmpty())
            {
                _tablesRepository.RemoveEmptyTable(matchingTable);
            }

            group.AssignTable(matchingTable);

            _clientsGroupsQueue.Remove(group);
        }

        _logger.LogInformation(LoggingEvents.RestaurantManagerProcessQueueFinished, "Queue processing has finished");

        Table? FindExactlyMatchingEmptyTable(int groupSize) =>
            _tablesRepository.GetEmptyTablesForGroup(groupSize)?.FirstOrDefault();


        Table? FindMinimalSizeEmptySuitableTable(int groupSize) =>
            _tablesRepository.GetGroupedEmptyTables().FirstOrDefault(tablesSet =>
                                  tablesSet.Key > groupSize && tablesSet.Value.Any())
                             .Value?.FirstOrDefault();

        Table? FindNonEmptySuitableTable(int groupSize) =>
            _tablesRepository.GetAll().FirstOrDefault(table => table.IsEnoughRoom(groupSize));
    }
}