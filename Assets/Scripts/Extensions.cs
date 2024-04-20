using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class Extensions
{
    //Draws debug lines representing the edges of the bounds in the specified color.
    public static void Draw(this Bounds bounds, Color color)
    {
        var e = bounds.extents;
        // Draw lines connecting opposite corners of the bounds to represent edges
        Debug.DrawLine(bounds.center + new Vector3(+e.x, +e.y, +e.z), bounds.center + new Vector3(-e.x, +e.y, +e.z), color);
        Debug.DrawLine(bounds.center + new Vector3(+e.x, -e.y, +e.z), bounds.center + new Vector3(-e.x, -e.y, +e.z), color);
        Debug.DrawLine(bounds.center + new Vector3(+e.x, -e.y, -e.z), bounds.center + new Vector3(-e.x, -e.y, -e.z), color);
        Debug.DrawLine(bounds.center + new Vector3(+e.x, +e.y, -e.z), bounds.center + new Vector3(-e.x, +e.y, -e.z), color);

        Debug.DrawLine(bounds.center + new Vector3(+e.x, +e.y, +e.z), bounds.center + new Vector3(+e.x, -e.y, +e.z), color);
        Debug.DrawLine(bounds.center + new Vector3(-e.x, +e.y, +e.z), bounds.center + new Vector3(-e.x, -e.y, +e.z), color);
        Debug.DrawLine(bounds.center + new Vector3(-e.x, +e.y, -e.z), bounds.center + new Vector3(-e.x, -e.y, -e.z), color);
        Debug.DrawLine(bounds.center + new Vector3(+e.x, +e.y, -e.z), bounds.center + new Vector3(+e.x, -e.y, -e.z), color);

        Debug.DrawLine(bounds.center + new Vector3(+e.x, +e.y, +e.z), bounds.center + new Vector3(+e.x, +e.y, -e.z), color);
        Debug.DrawLine(bounds.center + new Vector3(+e.x, -e.y, +e.z), bounds.center + new Vector3(+e.x, -e.y, -e.z), color);
        Debug.DrawLine(bounds.center + new Vector3(-e.x, +e.y, +e.z), bounds.center + new Vector3(-e.x, +e.y, -e.z), color);
        Debug.DrawLine(bounds.center + new Vector3(-e.x, -e.y, +e.z), bounds.center + new Vector3(-e.x, -e.y, -e.z), color);
    }

    //Converts a Vector3Int to a Vector3
    public static Vector3 ToVector3(this Vector3Int vector)
    {
        return (Vector3)vector;
    }

    //Pick a random element from a collection
    public static T PickRandom<T>(this ICollection<T> collection)
    {
        int index = UnityEngine.Random.Range(0, collection.Count);
        return collection.ElementAt(index);
    }

    //Delete all children of a Transform
    public static void DeleteChildren(this Transform transform)
    {
        int c = 0;
        while (transform.childCount != 0)
        {
            if (Application.isPlaying)
            {
                GameObject.DestroyImmediate(transform.GetChild(0).gameObject);
            }
            else
            {
                GameObject.DestroyImmediate(transform.GetChild(0).gameObject);
            }
            if (c++ > 10000)
            {
                throw new Exception();
            }
        }
    }

    //Retrieve the item with the best value based on a specified property.
    public static T GetBest<T>(this IEnumerable<T> enumerable, Func<T, float> property)
    {
        float bestValue = float.NegativeInfinity;
        T bestItem = default;
        foreach (var item in enumerable)
        {
            float value = property.Invoke(item); // Get the property value using the provided function
            if (value > bestValue) // If the current value is greater than the current best value
            {
                bestValue = value; // Update the best value
                bestItem = item; // Update the best item
            }
        }

        return bestItem; // Return the item with the highest property value
    }

}
