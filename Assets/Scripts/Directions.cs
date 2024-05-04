using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public static class Directions
{
    public const int LEFT = 0;
    public const int DOWN = 1;
    public const int BACK = 2;
    public const int RIGHT = 3;
    public const int UP = 4;
    public const int FORWARD = 5;

    private static Quaternion[] rotations;
    private static Vector3[] vectors;
    private static Vector3Int[] directions;

    public static Quaternion[] Rotations
    {
        get
        {
            if (Directions.rotations == null)
            {
                Directions.Initialize();
            }
            return Directions.rotations;
        }
    }

    public static Vector3Int[] Direction
    {
        get
        {
            if (Directions.directions == null)
            {
                Directions.Initialize();
            }
            return Directions.directions;
        }
    }

    private static void Initialize()
    {
        Directions.vectors = new Vector3[] {
            Vector3.left,
            Vector3.down,
            Vector3.back,
            Vector3.right,
            Vector3.up,
            Vector3.forward
        };

        Directions.rotations = Directions.vectors.Select(vector => Quaternion.LookRotation(vector)).ToArray();
        Directions.directions = Directions.vectors.Select(vector => Vector3Int.RoundToInt(vector)).ToArray();
    }

    public static readonly int[] horizontal = { 0, 2, 3, 5 };

    public static int Rotate(int direction, int amount)
    {
        if (direction == 1 || direction == 4)
        {
            return direction;
        }
        return horizontal[(Array.IndexOf(horizontal, direction) + amount) % 4];
    }

    public static bool IsHorizontal(int direction)
    {
        return direction != 1 && direction != 4;
    }

    public static int GetIndex(Vector3 direction)
    {
        if (direction.x < 0)
        {
            return 0;
        }
        else if (direction.y < 0)
        {
            return 1;
        }
        else if (direction.z < 0)
        {
            return 2;
        }
        else if (direction.x > 0)
        {
            return 3;
        }
        else if (direction.y > 0)
        {
            return 4;
        }
        else
        {
            return 5;
        }
    }
}
