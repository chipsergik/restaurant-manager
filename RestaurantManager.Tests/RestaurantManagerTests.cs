using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using RestaurantManager.API;
using RestaurantManager.API.Interfaces;
using RestaurantManager.API.Models;
using RestaurantManager.API.Persistence;
using RestaurantManager.API.Services;

namespace RestaurantManager.Tests;

public class RestaurantManagerTests
{
    private IClientsGroupsRepository _clientsGroupsRepository;
    private ILogger<RestaurantManagerService> _logger;

    [SetUp]
    public void Init()
    {
        _clientsGroupsRepository = new ClientsGroupsInMemoryRepository();
        _logger = new Logger<RestaurantManagerService>(new NullLoggerFactory());
    }

    private IRestaurantManager GetRestaurantManagerWithEmptyTables(IEnumerable<ClientsGroup> clientsGroupsQueue)
    {
        var tables = new List<Table>
        {
            new(size: 2),
            new(size: 3),
            new(size: 4),
            new(size: 5),
            new(size: 6)
        };

        return new API.Services.RestaurantManagerService(
            tables,
            _clientsGroupsRepository,
            new NullLogger<RestaurantManagerService>(),
            clientsGroupsQueue);
    }

    private IRestaurantManager GetRestaurantManagerWithSetUpTables(IReadOnlyCollection<Table> tables,
        IEnumerable<ClientsGroup> clientsGroupsQueue)
    {
        return new API.Services.RestaurantManagerService(
            tables,
            _clientsGroupsRepository,
            _logger,
            clientsGroupsQueue);
    }

    private ClientsGroup CreateClientsGroup(int size)
    {
        var clientsGroup = new ClientsGroup(size);
        _clientsGroupsRepository.Create(clientsGroup);
        return clientsGroup;
    }

    [Test]
    public void Lookup_WhenClientsGroupDoesNotExist_ShouldReturnNull()
    {
        var clientsGroup = CreateClientsGroup(size: 2);

        var restaurantManager =
            GetRestaurantManagerWithEmptyTables(clientsGroupsQueue: Enumerable.Empty<ClientsGroup>());

        var actualTable = restaurantManager.Lookup(clientsGroup.Id);

        Assert.That(actualTable, Is.Null);
    }

    [Test]
    public void Lookup_WhenClientsGroupInQueue_ShouldReturnNull()
    {
        var clientsGroup = CreateClientsGroup(size: 2);

        var restaurantManager = GetRestaurantManagerWithEmptyTables(
            clientsGroupsQueue: new[]
            {
                clientsGroup
            });

        var actualTable = restaurantManager.Lookup(clientsGroup.Id);

        Assert.That(actualTable, Is.Null);
    }

    [Test]
    public void Lookup_WhenClientsGroupOnTable_ShouldReturnTable()
    {
        var clientsGroup = CreateClientsGroup(size: 2);
        var tableWithClientsGroup = new Table(2);
        clientsGroup.AssignTable(tableWithClientsGroup);

        var restaurantManager = GetRestaurantManagerWithSetUpTables(
            tables: new[]
            {
                tableWithClientsGroup
            },
            clientsGroupsQueue: Enumerable.Empty<ClientsGroup>());

        var actualTable = restaurantManager.Lookup(clientsGroup.Id);
        var expectedTable = tableWithClientsGroup;

        Assert.That(actualTable, Is.EqualTo(expectedTable));
    }

    [Test]
    [TestCase(2)]
    [TestCase(3)]
    [TestCase(4)]
    [TestCase(5)]
    [TestCase(6)]
    public void OnArrive_WhenAllTablesAreEmptyAndNoClientsInQueue_ClientsGroupShouldChooseExactlyFittingTable(
        int groupSize)
    {
        var restaurantManager =
            GetRestaurantManagerWithEmptyTables(clientsGroupsQueue: Enumerable.Empty<ClientsGroup>());

        var clientsGroupId = restaurantManager.OnArrive(groupSize);

        var actualTableSize = restaurantManager.Lookup(clientsGroupId)?.Size;

        Assert.That(actualTableSize, Is.EqualTo(groupSize));
    }

    [Test]
    public void OnArrive_WhenOnlyPartiallyEmptyTableAvailable_ClientsGroupShouldChoosePartiallyEmptyTable()
    {
        var expectedTableSize = 4;

        var partiallyEmptyTable = new Table(expectedTableSize);
        partiallyEmptyTable.AddClientsGroup(2);

        var restaurantManager = GetRestaurantManagerWithSetUpTables(
            tables: new[]
            {
                partiallyEmptyTable,
            },
            Enumerable.Empty<ClientsGroup>()
        );

        var arrivedClientsGroupId = restaurantManager.OnArrive(2);

        var actualTableSize = restaurantManager.Lookup(arrivedClientsGroupId)?.Size;

        Assert.That(actualTableSize, Is.EqualTo(expectedTableSize));
    }

    [Test]
    public void OnArrive_WhenPartiallyEmptyAndEmptyTablesAvailable_ClientsGroupShouldChooseEmptyTable()
    {
        var emptyTableSize = 5;

        var partiallyEmptyTable = new Table(4);

        var clientsGroupOnTable = CreateClientsGroup(2);
        clientsGroupOnTable.AssignTable(partiallyEmptyTable);

        var restaurantManager = GetRestaurantManagerWithSetUpTables(
            tables: new[]
            {
                partiallyEmptyTable,
                new Table(emptyTableSize),
            },
            Enumerable.Empty<ClientsGroup>()
        );

        var arrivedClientsGroupId = restaurantManager.OnArrive(2);

        var actualTableSize = restaurantManager.Lookup(arrivedClientsGroupId)?.Size;
        var exceptedTableSize = emptyTableSize;

        Assert.That(actualTableSize, Is.EqualTo(exceptedTableSize));
    }

    [Test]
    public void OnArrive_WhenAllTablesAreEmptyAndClientsGroupsInQueue_clientsGroupsQueueShouldBeProcessedInOrder()
    {
        var clientGroupInQueue1 = CreateClientsGroup(size: 2);
        var clientGroupInQueue2 = CreateClientsGroup(size: 2);
        var clientGroupInQueue3 = CreateClientsGroup(size: 2);
        var clientGroupInQueue4 = CreateClientsGroup(size: 2);

        var restaurantManager = GetRestaurantManagerWithEmptyTables(new[]
        {
            clientGroupInQueue1,
            clientGroupInQueue2,
            clientGroupInQueue3,
            clientGroupInQueue4
        });

        var arrivedClientsGroupId = restaurantManager.OnArrive(groupSize: 2);

        var actualTableSize1 = restaurantManager.Lookup(clientGroupInQueue1.Id)?.Size;
        var expectedTableSize1 = 2;

        var actualTableSize2 = restaurantManager.Lookup(clientGroupInQueue2.Id)?.Size;
        var expectedTableSize2 = 3;

        var actualTableSize3 = restaurantManager.Lookup(clientGroupInQueue3.Id)?.Size;
        var expectedTableSize3 = 4;

        var actualTableSize4 = restaurantManager.Lookup(clientGroupInQueue4.Id)?.Size;
        var expectedTableSize4 = 5;

        var actualTableSize5 = restaurantManager.Lookup(arrivedClientsGroupId)?.Size;
        var expectedTableSize5 = 6;

        Assert.Multiple(() =>
            {
                Assert.That(actualTableSize1, Is.EqualTo(expectedTableSize1));
                Assert.That(actualTableSize2, Is.EqualTo(expectedTableSize2));
                Assert.That(actualTableSize3, Is.EqualTo(expectedTableSize3));
                Assert.That(actualTableSize4, Is.EqualTo(expectedTableSize4));
                Assert.That(actualTableSize5, Is.EqualTo(expectedTableSize5));
            }
        );
    }

    [Test]
    public void OnArrive_WhenGroupInQueueCantBeProcessedAndArrivedGroupCanBeProcessed_ShouldProcessArrivedGroup()
    {
        var clientGroupInQueue = CreateClientsGroup(size: 4);

        var restaurantManager = GetRestaurantManagerWithSetUpTables(
            tables: new[]
            {
                new Table(2)
            },
            clientsGroupsQueue: new[]
            {
                clientGroupInQueue
            });

        var arrivedClientsGroupId = restaurantManager.OnArrive(2);

        var actualTableSize1 = restaurantManager.Lookup(clientGroupInQueue.Id)?.Size;
        var expectedTableSize1 = (int?)null;

        var actualTableSize2 = restaurantManager.Lookup(arrivedClientsGroupId)?.Size;
        var expectedTableSize2 = 2;

        Assert.Multiple(() =>
            {
                Assert.That(actualTableSize1, Is.EqualTo(expectedTableSize1));
                Assert.That(actualTableSize2, Is.EqualTo(expectedTableSize2));
            }
        );
    }

    [Test]
    public void OnLeave_WhenClientsGroupIsOnTable_ShouldLeaveTable()
    {
        var clientsGroup = CreateClientsGroup(size: 2);

        var tableWithClientsGroup = new Table(2);
        tableWithClientsGroup.AddClientsGroup(clientsGroup.Size);

        var restaurantManager = GetRestaurantManagerWithSetUpTables(
            tables: new[]
            {
                tableWithClientsGroup
            },
            clientsGroupsQueue: Enumerable.Empty<ClientsGroup>());

        restaurantManager.OnLeave(clientsGroup.Id);

        var actualTable = restaurantManager.Lookup(clientsGroup.Id);

        Assert.Multiple(() => { Assert.That(actualTable, Is.Null); });
    }

    [Test]
    public void OnLeave_WhenClientsGroupIsOnTableAndAnotherGroupInQueue_AnotherGroupShouldTakeTheTable()
    {
        var clientsGroup1 = CreateClientsGroup(size: 2);
        var clientsGroup2 = CreateClientsGroup(size: 2);

        var tableWithClientsGroup = new Table(2);
        clientsGroup1.AssignTable(tableWithClientsGroup);

        var restaurantManager = GetRestaurantManagerWithSetUpTables(
            tables: new[]
            {
                tableWithClientsGroup
            },
            clientsGroupsQueue: new[]
            {
                clientsGroup2
            });

        restaurantManager.OnLeave(clientsGroup1.Id);

        var actualTable = restaurantManager.Lookup(clientsGroup2.Id);
        var expectedTable = tableWithClientsGroup;

        Assert.That(actualTable, Is.EqualTo(expectedTable));
    }
}