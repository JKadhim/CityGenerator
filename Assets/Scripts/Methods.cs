using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class Methods
{
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
                throw new System.Exception();
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
