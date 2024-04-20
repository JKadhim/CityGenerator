using UnityEngine;

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

    // Shows the exterior of chunks based on frustum planes and direction mask.
    public void ShowOutside(Plane[] frustumPlanes)
    {
        // Iterate over each chunk in range
        foreach (var chunk in this.cullingData.ChunksInRange)
        {
            // Skip chunks that are outside of the frustum planes or camera frustum planes
            if ((frustumPlanes != null && !GeometryUtility.TestPlanesAABB(frustumPlanes, chunk.bounds)) ||
                !GeometryUtility.TestPlanesAABB(this.cameraFrustumPlanes, chunk.bounds))
            {
                continue;
            }

            // Set exterior visibility of the chunk to true
            chunk.SetExteriorVisibility(true);
        }
    }

    // Updates the visibility of rooms and chunks based on camera frustum and position.
    void Update()
    {
        // Calculate frustum planes of the camera
        this.cameraFrustumPlanes = GeometryUtility.CalculateFrustumPlanes(this.Camera);
        var cameraPosition = this.cullingData.MapBehaviour.GetMapPosition(this.Camera.transform.position);

        // Check if the camera is inside a room
        bool cameraIsInsideRoom = this.cullingData.RoomsByPosition.ContainsKey(cameraPosition);

        // Iterate over each chunk in range
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
        
        // If the camera is outside any room
        else
        {
            // Show outside of the camera frustum
            this.ShowOutside(null);
        }
    }

    // Disables the script and resets the visibility of chunks and rooms.
    void OnDisable()
    {
        // Iterate over each chunk in the culling data
        foreach (var chunk in this.cullingData.Chunks.Values)
        {
            // Set exterior visibility of the chunk to true
            chunk.SetExteriorVisibility(true);
            // Set room visibility of the chunk to true
            chunk.SetRoomVisibility(true);
        }
    }

}
