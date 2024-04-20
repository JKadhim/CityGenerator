using System.Collections.Generic;
using System.Linq;
using UnityEngine;

//Define a class named CullingData derived from MonoBehaviour
public class CullingData : MonoBehaviour
{
    [HideInInspector]
    public MapBehaviour MapBehaviour;

    public Dictionary<Vector3Int, Room> RoomsByPosition;

    private HashSet<Vector3Int> outdatedSlots;

    public Dictionary<Vector3Int, Chunk> Chunks;
    public List<Chunk> ChunksInRange;

    public int ChunkSize = 3;

    public bool DrawGizmo = false;

    public void Initialize()
    {
        this.MapBehaviour = this.GetComponent<MapBehaviour>();
        this.RoomsByPosition = new Dictionary<Vector3Int, Room>();
        this.outdatedSlots = new HashSet<Vector3Int>();
        this.Chunks = new Dictionary<Vector3Int, Chunk>();
        this.ChunksInRange = new List<Chunk>();
    }

    // Method to get the chunk address based on a position
    public Vector3Int GetChunkAddress(Vector3Int position)
    {
        return Vector3Int.FloorToInt(position.ToVector3() / this.ChunkSize);
    }

    // Method to get the center of a chunk based on its address
    public Vector3 GetChunkCenter(Vector3Int chunkAddress)
    {
        return this.MapBehaviour.GetWorldspacePosition(chunkAddress * this.ChunkSize) + (this.ChunkSize - 1) * 0.5f * AbstractMap.BLOCK_SIZE * Vector3.one;
    }

    // Method to retrieve a chunk based on its address
    private Chunk GetChunk(Vector3Int chunkAddress)
    {
        if (this.Chunks.ContainsKey(chunkAddress))
        {
            return this.Chunks[chunkAddress];
        }
        var chunk = new Chunk(new Bounds(this.GetChunkCenter(chunkAddress), AbstractMap.BLOCK_SIZE * this.ChunkSize * Vector3.one));
        this.Chunks[chunkAddress] = chunk;
        this.ChunksInRange.Add(chunk);
        return chunk;
    }

    // Method to retrieve a chunk based on its position
    public Chunk GetChunkFromPosition(Vector3Int position)
    {
        return this.GetChunk(this.GetChunkAddress(position));
    }

    public Room GetRoom(Vector3Int position)
    {
        if (this.RoomsByPosition.ContainsKey(position))
        {
            return this.RoomsByPosition[position];
        }
        else
        {
            return null;
        }
    }

    // Method to add a slot to the culling data
    public void AddSlot(Slot slot)
    {
        // If the slot is not collapsed, exit the method
        if (!slot.Collapsed)
        {
            return;
        }

        var chunk = this.GetChunkFromPosition(slot.position);

        // If the slot is not marked as interior
        if (!slot.module.prefab.isInterior)
        {
            // Add the slot to the chunk
            chunk.AddBlock(slot);
            return;
        }

        Room room = null;

        // Iterate over the faces of the slot
        for (int i = 0; i < 4; i++)
        {
            var face = slot.module.GetFace(i);
            // If the face is a connector or an occlusion portal, continue to the next iteration
            if (face.connector == 1 || face.isOcclusionPortal)
            {
                continue;
            }

            var neighbor = slot.GetNeighbour(i);
            // If the neighbor slot is null, continue to the next iteration
            if (neighbor == null)
            {
                continue;
            }

            // If the neighbor slot is collapsed and exists in the RoomsByPosition dictionary
            if (neighbor.Collapsed && this.RoomsByPosition.ContainsKey(neighbor.position) && !neighbor.module.GetFace((i + 2) % 4).isOcclusionPortal)
            {
                // If room is null, set it to the room associated with the neighbor position; otherwise, merge the rooms
                room ??= this.RoomsByPosition[neighbor.position];
                if (room != this.RoomsByPosition[neighbor.position])
                {
                    room = this.MergeRooms(this.RoomsByPosition[neighbor.position], room);
                }
            }
        }

        // If room is still null, create a new room and add it to the chunk's list of rooms
        if (room == null)
        {
            room = new Room();
            chunk.rooms.Add(room);
        }

        // Add the slot to the room and update its renderers
        room.Slots.Add(slot);
        foreach (var renderer in slot.gameObject.GetComponentsInChildren<Renderer>())
        {
            room.Renderers.Add(renderer);
        }
        // Update RoomsByPosition dictionary with the slot's position and associated room
        this.RoomsByPosition[slot.position] = room;
    }

    //clear outdated slots and update them
    public void ClearOutdatedSlots()
    {
        if (!this.outdatedSlots.Any())
        {
            return;
        }
        var items = this.outdatedSlots.ToArray();
        this.outdatedSlots.Clear();
        foreach (var position in items)
        {
            var slot = this.MapBehaviour.map.GetSlot(position);
            if (slot == null || !slot.Collapsed)
            {
                continue;
            }
            this.AddSlot(slot);
        }
    }

    //remove a slot and associated rooms and portals
    public void RemoveSlot(Slot slot)
    {
        var chunk = this.GetChunkFromPosition(slot.position);
        chunk.RemoveBlock(slot);

        if (this.RoomsByPosition.ContainsKey(slot.position))
        {
            var room = this.RoomsByPosition[slot.position];
            foreach (var roomSlot in room.Slots)
            {
                this.outdatedSlots.Add(roomSlot.position);
                this.RoomsByPosition.Remove(roomSlot.position);
            }
            this.RemoveRoom(room);
        }
        this.outdatedSlots.Remove(slot.position);
    }

    //remove a room
    private void RemoveRoom(Room room)
    {
        foreach (var slot in room.Slots)
        {
            var chunk = this.GetChunkFromPosition(slot.position);
            if (chunk.rooms.Contains(room))
            {
                chunk.rooms.Remove(room);
            }
        }
    }

    // Method to merge two rooms
    private Room MergeRooms(Room room1, Room room2)
    {
        foreach (var slot in room1.Slots)
        {
            this.RoomsByPosition[slot.position] = room2;
            room2.Slots.Add(slot);
        }
        room2.Renderers.AddRange(room1.Renderers);
        room2.VisibilityOutdated = true;
        this.RemoveRoom(room1);
        return room2;
    }
}
