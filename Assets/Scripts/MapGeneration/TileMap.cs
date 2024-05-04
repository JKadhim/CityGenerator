using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class TileMap : MapBase
{
    public readonly Vector3Int size;

    private readonly Cell[,,] cells;

    public TileMap(Vector3Int size) : base()
    {
        this.size = size;
        this.cells = new Cell[size.x, size.y, size.z];

        for (int x = 0; x < size.x; x++)
        {
            for (int y = 0; y < size.y; y++)
            {
                for (int z = 0; z < size.z; z++)
                {
                    this.cells[x, y, z] = new Cell(new Vector3Int(x, y, z), this);
                }
            }
        }
    }

    public override Cell GetCell(Vector3Int position)
    {
        if (position.y < 0 || position.y >= this.size.y)
        {
            return null;
        }
        return this.cells[
            position.x % this.size.x + (position.x % this.size.x < 0 ? this.size.x : 0),
            position.y,
            position.z % this.size.z + (position.z % this.size.z < 0 ? this.size.z : 0)];
    }

    public override IEnumerable<Cell> GetAllCells()
    {
        for (int x = 0; x < this.size.x; x++)
        {
            for (int y = 0; y < this.size.y; y++)
            {
                for (int z = 0; z < this.size.z; z++)
                {
                    yield return this.cells[x, y, z];
                }
            }
        }
    }

    public override void ApplyConstraints(IEnumerable<Constraints> constraints)
    {
        foreach (var constraint in constraints)
        {
            switch (constraint.direction)
            {
                case Constraints.ConstraintDirection.Up:
                    for (int x = 0; x < this.size.x; x++)
                    {
                        for (int z = 0; z < this.size.z; z++)
                        {
                            if (constraint.mode == Constraints.ConstraintMode.EnforceConnector)
                            {
                                this.GetCell(new Vector3Int(x, this.size.y - 1, z)).EnforceConnector(4, constraint.connector);
                            }
                            else
                            {
                                this.GetCell(new Vector3Int(x, this.size.y - 1, z)).ExcludeConnector(4, constraint.connector);
                            }
                        }
                    }
                    break;
                case Constraints.ConstraintDirection.Down:
                    for (int x = 0; x < this.size.x; x++)
                    {
                        for (int z = 0; z < this.size.z; z++)
                        {
                            if (constraint.mode == Constraints.ConstraintMode.EnforceConnector)
                            {
                                this.GetCell(new Vector3Int(x, 0, z)).EnforceConnector(1, constraint.connector);
                            }
                            else
                            {
                                this.GetCell(new Vector3Int(x, 0, z)).ExcludeConnector(1, constraint.connector);
                            }
                        }
                    }
                    break;
            }
        }
    }
}
