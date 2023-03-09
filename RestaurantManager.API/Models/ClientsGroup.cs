namespace RestaurantManager.API.Models;

public class ClientsGroup
{
    public ClientsGroup(int size)
    {
        Size = size;
    }

    /// <summary>
    /// Represents clients group identity
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Represents number of clients in group
    /// </summary>
    public int Size { get; }

    public Table? Table { get; private set; }

    /// <summary>
    /// Seats client group to the table
    /// </summary>
    public void AssignTable(Table table)
    {
        table.AddClientsGroup(Size);
        Table = table;
    }
    
    /// <summary>
    /// Removes client group from the table
    /// </summary>
    public void RemoveTable(Table table)
    {
        table.RemoveClientsGroup(Size);
        Table = null;
    }

    public override string ToString()
    {
        return $"Id: {Id}; Size: {Size}";
    }
}