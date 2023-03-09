﻿namespace RestaurantManager.API.Models;

public class Table
{
    private int _emptyPlaces;

    /// <param name="size">Maximum size of clients group table can fit (from 2 to 6 persons)</param>
    public Table(int size)
    {
        if (size is < 2 or > 6) throw new ArgumentOutOfRangeException(nameof(size));
        _emptyPlaces = Size = size;
    }

    /// <summary>
    /// Number of chairs
    /// </summary>
    public int Size { get; private set; }

    public bool IsEmpty() => _emptyPlaces == Size;

    /// <summary>
    /// Gets empty chairs left
    /// </summary>
    /// <returns>Empty chairs number</returns>
    public bool IsEnoughRoom(int groupSize) => groupSize <= _emptyPlaces;

    /// <summary>
    /// Occupies number of places by group size
    /// </summary>
    public void AddClientsGroup(int groupSize)
    {
        _emptyPlaces -= groupSize;
    }

    /// <summary>
    /// Frees number of places by group size
    /// </summary>
    public void RemoveClientsGroup(int groupSize)
    {
        _emptyPlaces += groupSize;
    }
}