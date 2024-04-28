using UnityEditor;
using UnityEngine;

public class ModulePrefab : MonoBehaviour
{
    [System.Serializable]
    // Abstract class representing details of a face
    public abstract class FaceDetails
    {
        public bool walkable;
        public int connector;

        // Reset connector to default value
        public virtual void ResetConnector()
        {
            this.connector = 0;
        }

        public ModulePrefab[] excludedNeighbours;
        public bool enforceWalkableNeighbor = false;
    }

    // Serializable class representing horizontal face details
    [System.Serializable]
    public class HorizontalFaceDetails : FaceDetails
    {
        public bool symmetric;
        public bool flipped;

        // Convert face details to string representation
        public override string ToString()
        {
            string temp = flipped ? "F" : "";
            return this.connector.ToString() + (this.symmetric ? "s" : temp);
        }

        // Reset connector and additional details
        public override void ResetConnector()
        {
            base.ResetConnector();
            this.symmetric = false;
            this.flipped = false;
        }
    }

    public float probability = 1.0f;
    public bool spawn = true;
    public bool isInterior = false;
    public bool isVertical = false;

    public HorizontalFaceDetails Left;
    public HorizontalFaceDetails Back;
    public HorizontalFaceDetails Right;
    public HorizontalFaceDetails Forward;

    // Get an array of all face details
    public FaceDetails[] Faces
    {
        get
        {
            return new FaceDetails[] {
                this.Forward,
                this.Left,
                this.Back,
                this.Right
            };
        }
    }


    // Method to get the mesh of the GameObject
    public Mesh GetMesh(bool createEmptyFallbackMesh = true)
    {
        var meshFilter = this.GetComponent<MeshFilter>();
        // If mesh filter exists and has a shared mesh, return it
        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            return meshFilter.sharedMesh;
        }
        // If createEmptyFallbackMesh is true, create and return an empty mesh
        if (createEmptyFallbackMesh)
        {
            var mesh = new Mesh();
            return mesh;
        }
        // If createEmptyFallbackMesh is false and no mesh found, return null
        return null;
    }


#if UNITY_EDITOR
    private static ModulePrefabEditorData editorData;
    private static GUIStyle style;

    [DrawGizmo(GizmoType.InSelectionHierarchy | GizmoType.NotInSelectionHierarchy)]
    static void DrawGizmo(ModulePrefab modulePrefab, GizmoType gizmoType)
    {
        var transform = modulePrefab.transform;
        Vector3 position = transform.position;
        var rotation = transform.rotation;

        if (ModulePrefab.editorData == null || ModulePrefab.editorData.modulePrefab != modulePrefab)
        {
            ModulePrefab.editorData = new ModulePrefabEditorData(modulePrefab);
        }

        Gizmos.color = new Color(1f, 1f, 1f, 0.3f);
        if ((gizmoType & GizmoType.Selected) != 0)
        {
            for (int i = 0; i < 4; i++)
            {
                var hint = ModulePrefab.editorData.GetConnectorHint(i);
                if (hint.mesh != null)
                {
                    Vector3 scaleTemp = new Vector3(2, 1, 2);
                    Gizmos.DrawMesh(hint.mesh,
                        position + rotation * Directions.Direction[i].ToVector3() * Map.BLOCK_SIZE,
                        rotation * Quaternion.Euler(90f * hint.rotation * Vector3.up), scaleTemp);
                }
            }
        }
        for (int i = 0; i < 4; i++)
        {
            if (modulePrefab.Faces[i].walkable)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(position + Vector3.down * 0.1f, position + rotation * Directions.Rotations[i] * Vector3.forward * Map.BLOCK_SIZE * 0.5f + Vector3.down * 0.1f);
            }
        }

        ModulePrefab.style ??= new GUIStyle
            {
                alignment = TextAnchor.MiddleCenter
            };

        ModulePrefab.style.normal.textColor = Color.black;
        for (int i = 0; i < 4; i++)
        {
            var face = modulePrefab.Faces[i];
            Handles.Label(position + rotation * Directions.Rotations[i] * Vector3.forward * InfiniteMap.BLOCK_SIZE / 2f, face.ToString(), ModulePrefab.style);
        }
    }
#endif

    // Method to compare rotated variants of the module
    public bool CompareRotatedVariants(int r1, int r2)
    {
        for (int i = 0; i < 4; i++)
        {
            // Get faces for rotation r1 and r2
            var face1 = this.Faces[Directions.Rotate(Directions.PossibleDirections[i], r1)] as HorizontalFaceDetails;
            var face2 = this.Faces[Directions.Rotate(Directions.PossibleDirections[i], r2)] as HorizontalFaceDetails;

            // Compare connectors
            if (face1.connector != face2.connector)
            {
                return false;
            }

            // If both faces are not symmetric and flipped state is different, return false
            if (!face1.symmetric && !face2.symmetric && face1.flipped != face2.flipped)
            {
                return false;
            }
        }

        return true;
    }

    // Method to reset the module details
    void Reset()
    {
        // InitializeMap face details
        this.Forward = new HorizontalFaceDetails();
        this.Back = new HorizontalFaceDetails();
        this.Right = new HorizontalFaceDetails();
        this.Left = new HorizontalFaceDetails();

        // Reset excluded neighbours for all faces
        foreach (var face in this.Faces)
        {
            face.excludedNeighbours = new ModulePrefab[] { };
        }
    }
}