using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;

[RequireComponent(typeof(MapBehaviour))]
[RequireComponent(typeof(CullingData))]
public class OcclusionCulling : MonoBehaviour
{
    public Camera Camera;

    private CullingData cullingData;
    private Plane[] cameraFrustumPlanes;

    public void OnEnable()
    {
        this.cullingData = this.GetComponent<CullingData>();
    }

    void Update()
    {
        this.cameraFrustumPlanes = GeometryUtility.CalculateFrustumPlanes(this.Camera);
        var cameraPosition = this.cullingData.MapBehaviour.GetMapPosition(this.Camera.transform.position);

        bool cameraIsInsideRoom = this.cullingData.RoomsByPosition.ContainsKey(cameraPosition);
        foreach (var chunk in this.cullingData.ChunksInRange)
        {
            chunk.SetRoomVisibility(false);
            chunk.SetExteriorVisibility(!cameraIsInsideRoom);
        }

        if (cameraIsInsideRoom)
        {
            var cameraRoom = this.cullingData.RoomsByPosition[cameraPosition];
            cameraRoom.SetVisibility(true);
        }
    }

    void OnDisable()
    {
        foreach (var chunk in this.cullingData.Chunks.Values)
        {
            chunk.SetExteriorVisibility(true);
            chunk.SetRoomVisibility(true);
        }
    }
}
