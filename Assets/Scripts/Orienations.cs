using System;
using System.Linq;
using UnityEngine;

//Manages orientation-related operations and constants.
public static class Directions
{
    public const int UP = 0;
    public const int LEFT = 1;
    public const int DOWN = 2;
    public const int RIGHT = 3;


    private static Quaternion[] rotations;
    private static Vector3[] vectors;
    private static Vector3Int[] directions;

    public static Quaternion[] Rotations
    {
        get
        {
            if (rotations == null)
            {
                Initialize();
            }
            return rotations;
        }
    }

    public static Vector3Int[] Direction
    {
        get
        {
            if (directions == null)
            {
                Initialize();
            }
            return directions;
        }
    }

    private static void Initialize()
    {
        vectors = new Vector3[] {
            Vector3.forward,
            Vector3.left,
            Vector3.back,
            Vector3.right
        };

        rotations = vectors.Select(vector => Quaternion.LookRotation(vector)).ToArray();
        directions = vectors.Select(vector => Vector3Int.RoundToInt(vector)).ToArray();
    }

    public static readonly int[] PossibleDirections = { 0, 1, 2, 3 };

    //Rotates the given direction index by the specified amount.
    public static int Rotate(int direction, int amount)
    {
        return PossibleDirections[(Array.IndexOf(PossibleDirections, direction) + amount) % 4];
    }

    //Determines the index of the direction vector based on its components
    public static int GetIndex(Vector3 direction)
    {
        if (direction.z > 0)
        {
            return 0;
        }
        else if (direction.x > 0)
        {
            return 1;
        }
        else if (direction.z < 0)
        {
            return 2;
        }
        else
        {
            return 3;
        }
    }
}
