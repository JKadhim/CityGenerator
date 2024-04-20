using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[RequireComponent(typeof(CullingData))]

//Manages distance-based culling of chunks.
public class DistanceCulling : MonoBehaviour
{
    private CullingData cullingData;

    public Camera Camera;

    public float range = 40;

    private Vector3Int chunkAddress;

    public void OnEnable()
    {
        this.cullingData = this.GetComponent<CullingData>();
        if (this.cullingData.ChunksInRange != null)
        {
            this.chunkAddress = this.cullingData.GetChunkAddress(this.cullingData.MapBehaviour.GetMapPosition(this.Camera.transform.position));
            this.UpdateChunks();
        }
    }

    public void OnDisable()
    {
        this.cullingData.ChunksInRange = this.cullingData.Chunks.Values.ToList();
        foreach (var chunk in this.cullingData.ChunksInRange)
        {
            chunk.SetInRenderRange(true);
        }
    }

    void Update()
    {
        var newChunkAddress = this.cullingData.GetChunkAddress(this.cullingData.MapBehaviour.GetMapPosition(this.Camera.transform.position));
        if (newChunkAddress == this.chunkAddress)
        {
            return;
        }
        this.chunkAddress = newChunkAddress;
        this.UpdateChunks();
    }

    // Calculates the horizontal distance between two points
    float GetHorizontalDistance(Vector3 a, Vector3 b)
    {
        return Mathf.Sqrt(Mathf.Pow(a.x - b.x, 2) + Mathf.Pow(a.z - b.z, 2));
    }

    // Updates the visibility of chunks based on the distance from the camera
    void UpdateChunks()
    {
        var chunksInRange = this.cullingData.ChunksInRange;

        // Remove chunks that are out of range
        for (int i = 0; i < chunksInRange.Count; i++)
        {
            if (GetHorizontalDistance(chunksInRange[i].bounds.center, this.Camera.transform.position) > this.range)
            {
                chunksInRange[i].SetInRenderRange(false);
                chunksInRange.RemoveAt(i);
                i--;
            }
        }

        // Calculate the number of chunks around the camera within the rendering range
        int chunkCount = (int)(this.range / (AbstractMap.BLOCK_SIZE * this.cullingData.ChunkSize));

        // Loop through the chunks surrounding the camera
        for (int x = this.chunkAddress.x - chunkCount; x <= this.chunkAddress.x + chunkCount; x++)
        {
            for (int y = 0; y < Mathf.CeilToInt((float)this.cullingData.MapBehaviour.mapHeight / this.cullingData.ChunkSize); y++)
            {
                for (int z = this.chunkAddress.z - chunkCount; z <= this.chunkAddress.z + chunkCount; z++)
                {
                    var address = new Vector3Int(x, y, z);

                    // Check if the chunk is within the rendering range
                    if (Vector3.Distance(this.Camera.transform.position, this.cullingData.GetChunkCenter(address)) > this.range)
                    {
                        continue;
                    }

                    // Check if the chunk exists in the dictionary of chunks
                    if (!this.cullingData.Chunks.ContainsKey(address))
                    {
                        continue;
                    }

                    var chunk = this.cullingData.Chunks[address];

                    // Skip if the chunk is already in the rendering range
                    if (chunk.InRenderRange)
                    {
                        continue;
                    }

                    // Set the chunk to be in render range and add it to the list
                    chunk.SetInRenderRange(true);
                    chunksInRange.Add(chunk);
                }
            }
        }
    }

}

