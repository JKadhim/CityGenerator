using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CullingData : MonoBehaviour
{
    [HideInInspector]
    public MapBehaviour mapBehaviour;



    private HashSet<Vector3Int> oldCells;

    public Dictionary<Vector3Int, Chunk> chunks;
    public List<Chunk> chunksInRange;

    public int chunkSize = 3;

    public void Initialize()
    {
        this.mapBehaviour = this.GetComponent<MapBehaviour>();
        this.oldCells = new HashSet<Vector3Int>();
        this.chunks = new Dictionary<Vector3Int, Chunk>();
        this.chunksInRange = new List<Chunk>();
    }

    public Vector3Int GetChunkAddress(Vector3Int position)
    {
        return Vector3Int.FloorToInt(position.ToVector3() / this.chunkSize);
    }

    public Vector3 GetChunkCenter(Vector3Int chunkAddress)
    {
        return this.mapBehaviour.GetWorldPosition(chunkAddress * this.chunkSize) + (this.chunkSize - 1) * 0.5f * MapBase.BLOCK_SIZE * Vector3.one;
    }

    private Chunk GetChunk(Vector3Int chunkAddress)
    {
        if (this.chunks.ContainsKey(chunkAddress))
        {
            return this.chunks[chunkAddress];
        }
        var chunk = new Chunk(new Bounds(this.GetChunkCenter(chunkAddress), MapBase.BLOCK_SIZE * this.chunkSize * Vector3.one));
        this.chunks[chunkAddress] = chunk;
        this.chunksInRange.Add(chunk);
        return chunk;
    }

    public Chunk GetChunkFromPosition(Vector3Int position)
    {
        return this.GetChunk(this.GetChunkAddress(position));
    }

    public void AddCell(Cell cell)
    {
        if (!cell.Collapsed)
        {
            return;
        }
        var chunk = this.GetChunkFromPosition(cell.position);
        chunk.AddBlock(cell);

    }

    public void ClearOldCells()
    {
        if (!this.oldCells.Any())
        {
            return;
        }
        var items = this.oldCells.ToArray();
        this.oldCells.Clear();
        foreach (var position in items)
        {
            var cell = this.mapBehaviour.map.GetCell(position);
            if (cell == null || !cell.Collapsed)
            {
                continue;
            }
            this.AddCell(cell);
        }
    }

    public void RemoveCell(Cell cell)
    {
        var chunk = this.GetChunkFromPosition(cell.position);
        chunk.RemoveBlock(cell);
        this.oldCells.Remove(cell.position);
    }
}
