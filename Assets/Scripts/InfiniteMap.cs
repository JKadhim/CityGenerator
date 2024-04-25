using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class InfiniteMap : Map
{
    private readonly Dictionary<Vector3Int, Slot> slots;

    public readonly int Height;

    public Vector3Int rangeLimitCenter;
    public int RangeLimit = 80;

    private readonly TilingMap defaultColumn;

    public InfiniteMap(int height) : base()
    {
        Height = height;
        slots = new Dictionary<Vector3Int, Slot>();
        defaultColumn = new TilingMap(new Vector3Int(1, height, 1));

        // Check if module data is available
        if (ModuleData.current == null || ModuleData.current.Length == 0)
        {
            throw new InvalidOperationException("Module data deos not exist");
        }
    }

    // Method to get a slot at a specific position
    public override Slot GetSlot(Vector3Int position)
    {
        // Check if the position is outside the height bounds of the map
        if (position.y >= Height || position.y < 0)
        {
            return null;
        }

        // Check if the slot already exists in the dictionary
        if (slots.ContainsKey(position))
        {
            return slots[position];
        }

        // Check if the position is outside the range limit
        if (IsOutsideOfRangeLimit(position))
        {
            return null;
        }

        // Create a new slot at the position and add it to the dictionary
        slots[position] = new Slot(position, this, defaultColumn.GetSlot(position));
        return slots[position];
    }

    // Method to check if a position is outside the range limit
    public bool IsOutsideOfRangeLimit(Vector3Int position)
    {
        return (position - rangeLimitCenter).magnitude > RangeLimit;
    }

    // Method to apply boundary constraints to the map
    public override void ApplyBoundaryConstraints(IEnumerable<BoundaryConstraint> constraints)
    {
        foreach (var constraint in constraints)
        {
            int y = constraint.relativeY;
            // Adjust y value if it's negative
            if (y < 0)
            {
                y += Height;
            }
            int[] directions = null;
            // Determine directions based on the constraint direction
            switch (constraint.direction)
            {
                case BoundaryConstraint.ConstraintDirection.Horizontal:
                    directions = Directions.PossibleDirections; break;
            }

            // Apply constraints based on the mode
            foreach (int d in directions)
            {
                switch (constraint.mode)
                {
                    case BoundaryConstraint.ConstraintMode.EnforceConnector:
                        defaultColumn.GetSlot(new Vector3Int(0, y, 0)).EnforceConnector(d, constraint.connector);
                        break;
                    case BoundaryConstraint.ConstraintMode.ExcludeConnector:
                        defaultColumn.GetSlot(new Vector3Int(0, y, 0)).ExcludeConnector(d, constraint.connector);
                        break;
                }
            }
        }
    }


    // Method to get all slots in the map
    public override IEnumerable<Slot> GetAllSlots()
    {
        return slots.Values;
    }

    // Method to get the default slot at a specific height
    public Slot GetDefaultSlot(int y)
    {
        return defaultColumn.GetSlot(Vector3Int.up * y);
    }

    // Method to check if a slot is initialized at a specific position
    public bool IsSlotInitialized(Vector3Int position)
    {
        return slots.ContainsKey(position);
    }

    private bool muteRangeLimitWarning = false;

    // Method to handle hitting the range limit
    public void OnHitRangeLimit(Vector3Int position, ModuleSet modulesToRemove)
    {
        // Check if the range limit warning should be muted
        if (muteRangeLimitWarning || position.y < 0 || position.y >= Height)
        {
            return;
        }

        // Get the names of modules to be removed
        var moduleNames = modulesToRemove.Select(module => module.name);
        // Log a warning message
        Debug.LogWarning("Hit range limit at " + position + ". Module(s) to be removed:\n" + string.Join("\n", moduleNames.ToArray()) + "\n");
        muteRangeLimitWarning = true;
    }

}