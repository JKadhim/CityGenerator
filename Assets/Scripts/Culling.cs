using UnityEngine;

[RequireComponent(typeof(MapBehaviour))]
[RequireComponent(typeof(CullingData))]
public class Culling : MonoBehaviour
{
    public Camera camera_;

    private CullingData cullingData;

    public void OnEnable()
    {
        this.cullingData = this.GetComponent<CullingData>();
    }

    void Update()
    {
        foreach (var chunk in this.cullingData.chunksInRange)
        {
            chunk.SetVisibility(true);
        }
    }

    void OnDisable()
    {
        foreach (var chunk in this.cullingData.chunks.Values)
        {
            chunk.SetVisibility(true);
        }
    }
}
