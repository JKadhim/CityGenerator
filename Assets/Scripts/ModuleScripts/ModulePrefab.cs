using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System.Linq;
using System;

public class ModulePrefab : MonoBehaviour
{
    [System.Serializable]
    public abstract class FaceDetails
    {
        public bool walkable;
        public int connector;

        public virtual void ResetConnector()
        {
            this.connector = 0;
        }

        public ModulePrefab[] excludedNeighbours;

        public bool enforceWalkableNeighbor = false;
    }

    [System.Serializable]
    public class Horizontal : FaceDetails
    {
        public bool symmetric;
        public bool flipped;

        public override string ToString()
        {
            var temp = this.flipped ? "F" : "";
            return this.connector.ToString() + (this.symmetric ? "S" : (temp));
        }

        public override void ResetConnector()
        {
            base.ResetConnector();
            this.symmetric = false;
            this.flipped = false;
        }
    }

    [System.Serializable]
    public class Vertical : FaceDetails
    {
        public bool @fixed;
        public int rotation;

        public override string ToString()
        {
            var temp = this.rotation != 0 ? "_BCD".ElementAt(this.rotation).ToString() : "";
            return this.connector.ToString() + (this.@fixed ? "I" : temp);
        }

        public override void ResetConnector()
        {
            base.ResetConnector();
            this.@fixed = false;
            this.rotation = 0;
        }
    }

    public float probability = 1.0f;
    public bool spawn = true;

    public Horizontal Left;
    public Vertical Down;
    public Horizontal Back;
    public Horizontal Right;
    public Vertical Up;
    public Horizontal Forward;

    public FaceDetails[] Faces
    {
        get
        {
            return new FaceDetails[] {
                this.Left,
                this.Down,
                this.Back,
                this.Right,
                this.Up,
                this.Forward
            };
        }
    }

    public Mesh GetMesh(bool createFallback = true)
    {
        var filter = this.GetComponent<MeshFilter>();
        if (filter != null && filter.sharedMesh != null)
        {
            return filter.sharedMesh;
        }
        if (createFallback)
        {
            var mesh = new Mesh();
            return mesh;
        }
        return null;
    }

#if UNITY_EDITOR
    private static PrefabEditorData editorData;
    private static GUIStyle style;

    [DrawGizmo(GizmoType.InSelectionHierarchy | GizmoType.NotInSelectionHierarchy)]
    static void DrawGizmo(ModulePrefab modulePrefab, GizmoType gizmoType)
    {
        var transform = modulePrefab.transform;
        Vector3 position = transform.position;
        var rotation = transform.rotation;

        if (ModulePrefab.editorData == null || ModulePrefab.editorData.ModulePrefab != modulePrefab)
        {
            ModulePrefab.editorData = new PrefabEditorData(modulePrefab);
        }

        Gizmos.color = new Color(1f, 1f, 1f, 0.3f);
        if ((gizmoType & GizmoType.Selected) != 0)
        {
            for (int i = 0; i < 6; i++)
            {
                var hint = ModulePrefab.editorData.GetConnectorHint(i);
                if (hint.mesh != null)
                {
                    Gizmos.DrawMesh(hint.mesh,
                        position + rotation * Directions.Direction[i].ToVector3() * MapBase.BLOCK_SIZE,
                        rotation * Quaternion.Euler(90f * hint.rotation * Vector3.up));
                }
            }
        }
        for (int i = 0; i < 6; i++)
        {
            if (modulePrefab.Faces[i].walkable)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(position + Vector3.down * 0.1f, position + rotation * Directions.Rotations[i] * Vector3.forward * MapBase.BLOCK_SIZE * 0.5f + Vector3.down * 0.1f);
            }
        }

        ModulePrefab.style ??= new GUIStyle
            {
                alignment = TextAnchor.MiddleCenter
            };

        ModulePrefab.style.normal.textColor = Color.black;
        for (int i = 0; i < 6; i++)
        {
            var face = modulePrefab.Faces[i];
            Handles.Label(position + rotation * Directions.Rotations[i] * Vector3.forward * InfiniteMap.BLOCK_SIZE / 2f, face.ToString(), ModulePrefab.style);
        }
    }
#endif

    public bool CompareRotated(int r1, int r2)
    {
        if (!(this.Faces[Directions.UP] as Vertical).@fixed || !(this.Faces[Directions.DOWN] as Vertical).@fixed)
        {
            return false;
        }

        for (int i = 0; i < 4; i++)
        {
            var face1 = this.Faces[Directions.Rotate(Directions.horizontal[i], r1)] as Horizontal;
            var face2 = this.Faces[Directions.Rotate(Directions.horizontal[i], r2)] as Horizontal;

            if (face1.connector != face2.connector)
            {
                return false;
            }

            if (!face1.symmetric && !face2.symmetric && face1.flipped != face2.flipped)
            {
                return false;
            }
        }

        return true;
    }

    void Reset()
    {
        this.Up = new Vertical();
        this.Down = new Vertical();
        this.Right = new Horizontal();
        this.Left = new Horizontal();
        this.Forward = new Horizontal();
        this.Back = new Horizontal();

        foreach (var face in this.Faces)
        {
            face.excludedNeighbours = new ModulePrefab[] { };
        }
    }
}
