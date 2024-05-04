using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

[RequireComponent(typeof(CullingData))]
public class DistanceCulling : MonoBehaviour
{

    private CullingData cullingData;

    public Camera camera_;

    public float range = 40;

    private Vector3Int chunkAddress;

    public void OnEnable()
    {
        this.cullingData = this.GetComponent<CullingData>();
        if (this.cullingData.chunksInRange != null)
        {
            this.chunkAddress = this.cullingData.GetChunkAddress(this.cullingData.mapBehaviour.GetMapPosition(this.camera_.transform.position));
            this.UpdateChunks();
        }
    }

    public void OnDisable()
    {
        this.cullingData.chunksInRange = this.cullingData.chunks.Values.ToList();
        foreach (var chunk in this.cullingData.chunksInRange)
        {
            chunk.SetInRange(true);
        }
    }

    void Update()
    {
        var address = this.cullingData.GetChunkAddress(this.cullingData.mapBehaviour.GetMapPosition(this.camera_.transform.position));
        if (address == this.chunkAddress)
        {
            return;
        }
        this.chunkAddress = address;
        this.UpdateChunks();
    }

    float GetDistance(Vector3 a, Vector3 b)
    {
        return Mathf.Sqrt(Mathf.Pow(a.x - b.x, 2) + Mathf.Pow(a.z - b.z, 2));
    }

    void UpdateChunks()
    {
        var chunksInRange = this.cullingData.chunksInRange;
        for (int i = 0; i < chunksInRange.Count; i++)
        {
            if (GetDistance(chunksInRange[i].bounds.center, this.camera_.transform.position) > this.range)
            {
                chunksInRange[i].SetInRange(false);
                chunksInRange.RemoveAt(i);
                i--;
            }
        }

        int chunkCount = (int)(this.range / (MapBase.BLOCK_SIZE * this.cullingData.chunkSize));
        for (int x = this.chunkAddress.x - chunkCount; x <= this.chunkAddress.x + chunkCount; x++)
        {
            for (int y = 0; y < Mathf.CeilToInt((float)this.cullingData.mapBehaviour.mapHeight / this.cullingData.chunkSize); y++)
            {
                for (int z = this.chunkAddress.z - chunkCount; z <= this.chunkAddress.z + chunkCount; z++)
                {
                    var address = new Vector3Int(x, y, z);
                    if (Vector3.Distance(this.camera_.transform.position, this.cullingData.GetChunkCenter(address)) > this.range)
                    {
                        continue;
                    }
                    if (!this.cullingData.chunks.ContainsKey(address))
                    {
                        continue;
                    }
                    var chunk = this.cullingData.chunks[address];
                    if (chunk.InRenderRange)
                    {
                        continue;
                    }
                    chunk.SetInRange(true);
                    chunksInRange.Add(chunk);
                }
            }
        }
    }
}
