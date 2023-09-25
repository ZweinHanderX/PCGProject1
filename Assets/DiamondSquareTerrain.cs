using UnityEngine;

public class DiamondSquareTerrain : MonoBehaviour
{
    public int grillaT = 128; 
    public float H = 0.5f; 

    public Terrain terreno;

    private void Start()
    {
        terreno = GetComponent<Terrain>();
        terreno.terrainData = GenerateTerrain(terreno.terrainData);
    }

    private TerrainData GenerateTerrain(TerrainData terrainData)
    {
        terrainData.heightmapResolution = grillaT + 1;
        terrainData.size = new Vector3(grillaT, 10, grillaT);


        float[,] heightmap = DiamondSquareAlgorithm.GenerateHeightMap(grillaT, H);
        terrainData.SetHeights(0, 0, heightmap);

        return terrainData;
    }
}
    