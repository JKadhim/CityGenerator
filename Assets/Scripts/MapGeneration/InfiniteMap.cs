using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;
using UnityEditor;

public class InfiniteMap : MapBase
{
    private readonly Dictionary<Vector3Int, Cell> cells;

    public readonly int height;

    public Vector3Int rangeLimitCentre;
    public int rangeLimit = 80;

    private readonly TileMap defaultMap;

    public InfiniteMap(int height) : base()
    {
        this.height = height;
        this.cells = new Dictionary<Vector3Int, Cell>();
        this.defaultMap = new TileMap(new Vector3Int(1, height, 1));

        if (ModuleData.current == null || ModuleData.current.Length == 0)
        {
            throw new InvalidOperationException("Module doesn't exist");
        }
    }

    public override Cell GetCell(Vector3Int position)
    {
        if (position.y >= this.height || position.y < 0)
        {
            return null;
        }

        if (this.cells.ContainsKey(position))
        {
            return this.cells[position];
        }

        if (this.IsOutOfRange(position))
        {
            return null;
        }

        this.cells[position] = new Cell(position, this, this.defaultMap.GetCell(position));
        return this.cells[position];
    }

    public bool IsOutOfRange(Vector3Int position)
    {
        return (position - this.rangeLimitCentre).magnitude > this.rangeLimit;
    }

    public override void ApplyConstraints(IEnumerable<Constraints> constraints)
    {
        foreach (var constraint in constraints)
        {
            int y = constraint.yLocal;
            if (y < 0)
            {
                y += this.height;
            }
            int[] directions = null;
            switch (constraint.direction)
            {
                case Constraints.ConstraintDirection.Up:
                    directions = new int[] { 4 };
                    break;
                
                case Constraints.ConstraintDirection.Down:
                    directions = new int[] { 1 };
                    break;
                
                case Constraints.ConstraintDirection.Horizontal:
                    directions = Directions.horizontal;
                    break;
            }

            foreach (int dir in directions)
            {
                switch (constraint.mode)
                {
                    case Constraints.ConstraintMode.EnforceConnector:
                        this.defaultMap.GetCell(new Vector3Int(0, y, 0)).EnforceConnector(dir, constraint.connector);
                        break;
                    case Constraints.ConstraintMode.ExcludeConnector:
                        this.defaultMap.GetCell(new Vector3Int(0, y, 0)).ExcludeConnector(dir, constraint.connector);
                        break;
                }
            }
        }
    }

    public override IEnumerable<Cell> GetAllCells()
    {
        return this.cells.Values;
    }

    public Cell GetDefaultCell(int y)
    {
        return this.defaultMap.GetCell(Vector3Int.up * y);
    }

    public bool IsCellInitialized(Vector3Int position)
    {
        return this.cells.ContainsKey(position);
    }

    private bool muteRangeLimitWarning = false;

    public void OnHitRangeLimit(Vector3Int position, ModuleSet modulesToRemove)
    {
        if (this.muteRangeLimitWarning || position.y < 0 || position.y >= this.height)
        {
            return;
        }

        var moduleNames = modulesToRemove.Select(module => module.name);
        Debug.LogWarning("Hit range limit at " + position + ". Module(s) to be removed:\n" + string.Join("\n", moduleNames.ToArray()) + "\n");
        this.muteRangeLimitWarning = true;
    }
}
