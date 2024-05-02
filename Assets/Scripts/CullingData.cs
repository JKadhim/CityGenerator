﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;

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

    public Vector3Int GetChunkAddress(Vector3Int position)
    {
        return Vector3Int.FloorToInt(position.ToVector3() / this.ChunkSize);
    }

    public Vector3 GetChunkCenter(Vector3Int chunkAddress)
    {
        return this.MapBehaviour.GetWorldspacePosition(chunkAddress * this.ChunkSize) + (this.ChunkSize - 1) * 0.5f * AbstractMap.BLOCK_SIZE * Vector3.one;
    }

    private Chunk getChunk(Vector3Int chunkAddress)
    {
        if (this.Chunks.ContainsKey(chunkAddress))
        {
            return this.Chunks[chunkAddress];
        }
        var chunk = new Chunk(new Bounds(this.GetChunkCenter(chunkAddress), Vector3.one * AbstractMap.BLOCK_SIZE * this.ChunkSize));
        this.Chunks[chunkAddress] = chunk;
        this.ChunksInRange.Add(chunk);
        return chunk;
    }

    public Chunk getChunkFromPosition(Vector3Int position)
    {
        return this.getChunk(this.GetChunkAddress(position));
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

    public void AddSlot(Slot slot)
    {
        if (!slot.Collapsed)
        {
            return;
        }
        var chunk = this.getChunkFromPosition(slot.Position);
        if (!slot.Module.Prototype.IsInterior)
        {
            chunk.AddBlock(slot);
            return;
        }

        Room room = null;
        for (int i = 0; i < 6; i++)
        {
            var face = slot.Module.GetFace(i);
            if (face.Connector == 1 || face.IsOcclusionPortal)
            {
                continue;
            }
            var neighbor = slot.GetNeighbor(i);
            if (neighbor == null)
            {
                continue;
            }
            if (neighbor.Collapsed && this.RoomsByPosition.ContainsKey(neighbor.Position) && !neighbor.Module.GetFace((i + 3) % 6).IsOcclusionPortal)
            {
                if (room == null)
                {
                    room = this.RoomsByPosition[neighbor.Position];
                }
                if (room != this.RoomsByPosition[neighbor.Position])
                {
                    room = this.mergeRooms(this.RoomsByPosition[neighbor.Position], room);
                }
            }
        }
        if (room == null)
        {
            room = new Room();
            chunk.Rooms.Add(room);
        }
        room.Slots.Add(slot);
        foreach (var renderer in slot.GameObject.GetComponentsInChildren<Renderer>())
        {
            room.Renderers.Add(renderer);
        }
        this.RoomsByPosition[slot.Position] = room;

        for (int i = 0; i < 6; i++)
        {
            var face = slot.Module.GetFace(i);
            if (face.Connector == 1)
            {
                continue;
            }
            var neighbor = slot.GetNeighbor(i);
        }
    }

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
            var slot = this.MapBehaviour.Map.GetSlot(position);
            if (slot == null || !slot.Collapsed)
            {
                continue;
            }
            this.AddSlot(slot);
        }
    }

    public void RemoveSlot(Slot slot)
    {
        var chunk = this.getChunkFromPosition(slot.Position);
        chunk.RemoveBlock(slot);

        if (this.RoomsByPosition.ContainsKey(slot.Position))
        {
            var room = this.RoomsByPosition[slot.Position];
            foreach (var roomSlot in room.Slots)
            {
                this.outdatedSlots.Add(roomSlot.Position);
                this.RoomsByPosition.Remove(roomSlot.Position);
            }
            this.removeRoom(room);
        }
        this.outdatedSlots.Remove(slot.Position);
    }

    private void removeRoom(Room room)
    {
        foreach (var slot in room.Slots)
        {
            var chunk = this.getChunkFromPosition(slot.Position);
            if (chunk.Rooms.Contains(room))
            {
                chunk.Rooms.Remove(room);
            }
        }
    }

    private Room mergeRooms(Room room1, Room room2)
    {
        foreach (var slot in room1.Slots)
        {
            this.RoomsByPosition[slot.Position] = room2;
            room2.Slots.Add(slot);
        }
        room2.Renderers.AddRange(room1.Renderers);
        room2.VisibilityOutdated = true;
        this.removeRoom(room1);
        return room2;
    }

#if UNITY_EDITOR
    [DrawGizmo(GizmoType.InSelectionHierarchy | GizmoType.NotInSelectionHierarchy)]
    static void DrawGizmos(CullingData cullingData, GizmoType gizmoType)
    {
        if (!cullingData.DrawGizmo || cullingData.ChunksInRange == null)
        {
            return;
        }
        foreach (var chunk in cullingData.ChunksInRange)
        {
            foreach (var room in chunk.Rooms)
            {
                room.DrawGizmo(cullingData.MapBehaviour);
            }
        }
    }
#endif
}
