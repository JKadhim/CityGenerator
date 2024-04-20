using System.Collections.Generic;
using UnityEngine;

//Represents a room within the map
public class Room
{
    public HashSet<Slot> Slots;

    public readonly Color Color;

    public bool Visible;
    public bool VisibilityOutdated;

    public List<Renderer> Renderers;

    public Room()
    {
        this.Slots = new HashSet<Slot>();
        this.Renderers = new List<Renderer>();
        this.Color = Color.HSVToRGB(Random.Range(0f, 1f), 1f, 1f);
        this.Visible = true;
    }


    //SetVisibility method sets the visibility of the room and updates associated renderers accordingly.
    public void SetVisibility(bool visible)
    {
        // If visibility is not outdated and the new visibility matches the current visibility, return early.
        if (!this.VisibilityOutdated && visible == this.Visible)
        {
            return;
        }

        // Update visibility status and mark visibility as not outdated.
        this.VisibilityOutdated = false;
        this.Visible = visible;

        // Enable or disable renderers based on the visibility status.
        foreach (var renderer in this.Renderers)
        {
            renderer.enabled = visible;
        }
    }

#if UNITY_EDITOR
    public void DrawGizmo(MapBehaviour map)
    {
        // Skip drawing if the room is not visible or if its visibility is outdated.
        if (!this.Visible || this.VisibilityOutdated)
        {
            return;
        }

        // Set Gizmos color to the room's color.
        Gizmos.color = this.Color;

        // Draw wireframe cubes representing the slots of the room.
        foreach (var slot in this.Slots)
        {
            Gizmos.DrawWireCube(map.GetWorldspacePosition(slot.position), Vector3.one * AbstractMap.BLOCK_SIZE);
        }
    }
#endif
}
