using UnityEngine;

public class MeshToTerrainBridge : MonoBehaviour
{
    [Header("Input Components")]
    public MeshCollider sourceMeshCollider;
    public Terrain terrain;

    [Header("Tuning")]
    public float bufferRadius = 3f; // World units
    public float blendFalloff = 1f; // Smoothness of transition

    void Start()
    {
        if (sourceMeshCollider == null || terrain == null)
        {
            Debug.LogError("MeshCollider or Terrain is not assigned.");
            return;
        }

        TerrainData terrainData = terrain.terrainData;
        int resolution = terrainData.heightmapResolution;
        float[,] heights = new float[resolution, resolution];

        Vector3 terrainPos = terrain.GetPosition();
        Vector3 terrainSize = terrainData.size;
        float unitPerStep = terrainSize.x / (resolution - 1);

        for (int z = 0; z < resolution; z++)
        {
            for (int x = 0; x < resolution; x++)
            {
                float worldX = terrainPos.x + ((float)x / (resolution - 1)) * terrainSize.x;
                float worldZ = terrainPos.z + ((float)z / (resolution - 1)) * terrainSize.z;

                Vector3 rayOrigin = new Vector3(worldX, terrainPos.y + terrainSize.y + 10f, worldZ);
                Ray ray = new Ray(rayOrigin, Vector3.down);

                if (sourceMeshCollider.Raycast(ray, out RaycastHit hit, terrainSize.y + 20f))
                {
                    float normalizedHeight = (hit.point.y - terrainPos.y) / terrainSize.y;
                    heights[z, x] = Mathf.Clamp01(normalizedHeight);
                }
                else
                {
                    // Check nearby grid points within buffer
                    float sum = 0f;
                    int count = 0;

                    int bufferSteps = Mathf.CeilToInt(bufferRadius / unitPerStep);
                    for (int dz = -bufferSteps; dz <= bufferSteps; dz++)
                    {
                        for (int dx = -bufferSteps; dx <= bufferSteps; dx++)
                        {
                            int nx = x + dx;
                            int nz = z + dz;

                            if (nx >= 0 && nx < resolution && nz >= 0 && nz < resolution)
                            {
                                float distance = Mathf.Sqrt(dx * dx + dz * dz) * unitPerStep;
                                if (distance <= bufferRadius)
                                {
                                    // Sample from nearby elevated points
                                    float neighborHeight = heights[nz, nx];
                                    float falloff = Mathf.Clamp01(1f - (distance / bufferRadius));
                                    sum += neighborHeight * Mathf.Pow(falloff, blendFalloff);
                                    count++;
                                }
                            }
                        }
                    }

                    if (count > 0)
                        heights[z, x] = sum / count;
                    else
                        heights[z, x] = 0f;
                }
            }
        }

        terrainData.SetHeights(0, 0, heights);
        Debug.Log("Terrain molded to mesh with soft edges.");
    }
}
