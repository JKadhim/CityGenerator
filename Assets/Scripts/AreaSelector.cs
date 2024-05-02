using UnityEditor;
using UnityEngine;

//Represents an area selector used for selecting an area in the map.
public class AreaSelector : MonoBehaviour
{
    // Reference to the mapBehaviour component
    public MapBehaviour MapBehaviour;

    // Property to get the start position of the area
    public Vector3Int StartPosition
    {
        get
        {
            var start = Vector3Int.RoundToInt((this.transform.position) / InfiniteMap.BLOCK_SIZE);
            if (start.y >= this.MapBehaviour.Map.Height)
            {
                start.y = this.MapBehaviour.Map.Height - 1;
            }
            if (start.y < 0)
            {
                start.y = 0;
            }
            return start;
        }
    }

    // Property to get the size of the area
    public Vector3Int Size
    {
        get
        {
            var start = this.StartPosition;
            var size = Vector3Int.RoundToInt(this.transform.localScale / InfiniteMap.BLOCK_SIZE);
            if (size.y + start.y >= this.MapBehaviour.Map.Height)
            {
                size.y = System.Math.Max(0, this.MapBehaviour.Map.Height - start.y);
            }
            return size;
        }
    }
}

