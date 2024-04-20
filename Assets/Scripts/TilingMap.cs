using System.Collections.Generic;
using UnityEngine;



//TilingMap class represents a map consisting of a grid of slots in a 3D space. 
//Inherits from AbstractMap.

public class TilingMap : AbstractMap
{
    public readonly Vector3Int size;

    // The grid of slots in the map
    private readonly Slot[,,] slots;

    public TilingMap(Vector3Int size) : base()
    {
        this.size = size;
        slots = new Slot[size.x, size.y, size.z];

        // Loop through each coordinate in the 3D grid
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                for (int z = 0; z < size.z; z++)
                {
                    // Create a new Slot object at the current coordinate
                    // and assign it to the corresponding position in the slots array
                    slots[x, y, z] = new Slot(new Vector3Int(x, y, z), this);
                }
            }
        }
    }

    // Override method to get a slot at a specific position
    public override Slot GetSlot(Vector3Int position)
    {
        // Check if the y-coordinate is out of bounds
        if (position.y < 0 || position.y >= size.y)
        {
            return null;
        }
        // Calculate the wrapped coordinates for x and z dimensions
        int xWrapped = position.x % size.x + (position.x % size.x < 0 ? size.x : 0);
        int zWrapped = position.z % size.z + (position.z % size.z < 0 ? size.z : 0);

        return slots[xWrapped, position.y, zWrapped];
    }

    // Override method to get all slots in the map
    public override IEnumerable<Slot> GetAllSlots()
    {
        // Iterate through each coordinate in the 3D grid
        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                for (int z = 0; z < size.z; z++)
                {
                    // Yield return each slot in the slots array
                    yield return slots[x, y, z];
                }
            }
        }
    }

    public override void ApplyBoundaryConstraints(IEnumerable<BoundaryConstraint> constraints){}
}
