using System.Collections.Generic;
using UnityEngine;

public class TreePlacement : MonoBehaviour
{
    // Referencia al objeto terreno y el objeto de �rbol en el Inspector.
    public Terrain terrainObject;
    public GameObject treeObject;

    // Distancia m�nima entre �rboles y n�mero m�ximo de iteraciones.
    public float minimumDistance = 5.0f;
    public int maximumIterations = 30;

    // Almacena las alturas del terreno y los �rboles generados.
    private float[,] terrainHeights;
    private List<GameObject> spawnedTrees = new List<GameObject>();

    private void Start()
    {
        // Precalcula las alturas del terreno.
        PrecomputeTerrainHeights();

        // Genera los �rboles.
        GenerateTrees();
    }

    private void PrecomputeTerrainHeights()
    {
        if (terrainObject == null)
        {
            Debug.LogError("Falta la referencia al terreno. Asigna el objeto terreno en el Inspector.");
            return;
        }

        // Obtiene la resoluci�n del mapa de altura del terreno.
        int width = terrainObject.terrainData.heightmapResolution;
        int height = terrainObject.terrainData.heightmapResolution;

        // Inicializa el arreglo de alturas.
        terrainHeights = new float[width, height];

        // Calcula y almacena las alturas del terreno.
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                float normalizedX = (float)x / (width - 1);
                float normalizedY = (float)y / (height - 1);
                float worldX = normalizedX * terrainObject.terrainData.size.x;
                float worldZ = normalizedY * terrainObject.terrainData.size.z;
                terrainHeights[x, y] = terrainObject.SampleHeight(new Vector3(worldX, 0f, worldZ));
            }
        }
    }

    private void GenerateTrees()
    {
        // Limpia los �rboles existentes.
        foreach (var tree in spawnedTrees)
        {
            Destroy(tree);
        }
        spawnedTrees.Clear();

        if (terrainObject == null || treeObject == null)
        {
            Debug.LogError("Falta el terreno o el objeto de �rbol. Asigna ambos en el Inspector.");
            return;
        }

        // Define el �rea donde se generan los �rboles.
        Vector2 bottomLeft = Vector2.zero;
        Vector2 topRight = new Vector2(terrainObject.terrainData.size.x, terrainObject.terrainData.size.z);

        // Genera posiciones de �rboles usando el algoritmo de Poisson.
        List<Vector2> treePositions = FastPoissonDiskSampling.GenerateSamples(bottomLeft, topRight, minimumDistance, maximumIterations);

        foreach (Vector2 position in treePositions)
        {
            float x = position.x;
            float y = position.y;

            // Obtiene la altura del terreno en la posici�n del �rbol.
            float terrainHeight = GetTerrainHeightAtPosition(new Vector3(x, 0f, y));

            // Calcula la posici�n final del �rbol.
            Vector3 treePosition = new Vector3(x, terrainHeight, y);

            // Instancia el �rbol y lo agrega a la lista de �rboles generados.
            GameObject newTree = Instantiate(treeObject, treePosition, Quaternion.identity);
            spawnedTrees.Add(newTree);
        }
    }

    private float GetTerrainHeightAtPosition(Vector3 position)
    {
        if (terrainObject == null)
        {
            Debug.LogError("La referencia al terreno es nula. Asigna el objeto terreno en el Inspector.");
            return 0f;
        }

        int width = terrainObject.terrainData.heightmapResolution;
        int height = terrainObject.terrainData.heightmapResolution;

        // Normaliza la posici�n en relaci�n con el tama�o del terreno.
        float normalizedX = Mathf.Clamp01(position.x / terrainObject.terrainData.size.x);
        float normalizedZ = Mathf.Clamp01(position.z / terrainObject.terrainData.size.z);

        // Convierte las coordenadas normalizadas a �ndices del mapa de altura.
        int x = Mathf.FloorToInt(normalizedX * (width - 1));
        int z = Mathf.FloorToInt(normalizedZ * (height - 1));

        // Devuelve la altura del terreno en la posici�n dada.
        return terrainHeights[x, z];
    }

    // Esta funci�n se llama cuando se modifican variables en el Inspector.
    private void OnValidate()
    {
        // Regenera los �rboles cuando se modifican las variables en el Inspector.
        GenerateTrees();
    }
}


public static class FastPoissonDiskSampling
{
    // Constantes para el algoritmo de muestreo de Poisson.
    public const float InvertRootTwo = 0.70710678118f;
    public const int DefaultIterationPerPoint = 30;

    // Clase interna para configuraci�n.
    private class Settings
    {
        public Vector2 BottomLeft;
        public Vector2 TopRight;
        public Vector2 Center;
        public Rect Dimension;

        public float MinimumDistance;
        public int IterationPerPoint;

        public float CellSize;
        public int GridWidth;
        public int GridHeight;
    }

    // Clase interna para almacenar datos temporales.
    private class Bags
    {
        public Vector2?[,] Grid;
        public List<Vector2> SamplePoints;
        public List<Vector2> ActivePoints;
    }

    // M�todo principal para generar muestras de Poisson.
    public static List<Vector2> GenerateSamples(Vector2 bottomLeft, Vector2 topRight, float minimumDistance, int iterationPerPoint)
    {
        // Configura los par�metros del algoritmo.
        var settings = GetSettings(bottomLeft, topRight, minimumDistance, iterationPerPoint <= 0 ? DefaultIterationPerPoint : iterationPerPoint);

        // Inicializa estructuras de datos temporales.
        var bags = new Bags()
        {
            Grid = new Vector2?[settings.GridWidth + 1, settings.GridHeight + 1],
            SamplePoints = new List<Vector2>(),
            ActivePoints = new List<Vector2>()
        };

        // Genera el primer punto y lo agrega a la lista.
        GetFirstPoint(settings, bags);

        do
        {
            // Escoge un punto activo al azar.
            var index = Random.Range(0, bags.ActivePoints.Count);
            var point = bags.ActivePoints[index];

            var found = false;
            for (var k = 0; k < settings.IterationPerPoint; k++)
            {
                // Intenta encontrar un nuevo punto basado en el punto activo.
                found = found | GetNextPoint(point, settings, bags);
            }

            if (found == false)
            {
                // Si no se encontraron m�s puntos, elimina este punto activo.
                bags.ActivePoints.RemoveAt(index);
            }
        }
        while (bags.ActivePoints.Count > 0);

        return bags.SamplePoints;
    }

    // Intenta encontrar un nuevo punto basado en el punto actual.
    private static bool GetNextPoint(Vector2 point, Settings set, Bags bags)
    {
        var found = false;
        var p = GetRandPosInCircle(set.MinimumDistance, 2f * set.MinimumDistance) + point;

        if (set.Dimension.Contains(p) == false)
        {
            return false;
        }

        var minimum = set.MinimumDistance * set.MinimumDistance;
        var index = GetGridIndex(p, set);
        var drop = false;

        var around = 2;
        var fieldMin = new Vector2Int(Mathf.Max(0, index.x - around), Mathf.Max(0, index.y - around));
        var fieldMax = new Vector2Int(Mathf.Min(set.GridWidth, index.x + around), Mathf.Min(set.GridHeight, index.y + around));

        for (var i = fieldMin.x; i <= fieldMax.x && drop == false; i++)
        {
            for (var j = fieldMin.y; j <= fieldMax.y && drop == false; j++)
            {
                var q = bags.Grid[i, j];
                if (q.HasValue == true && (q.Value - p).sqrMagnitude <= minimum)
                {
                    drop = true;
                }
            }
        }

        if (drop == false)
        {
            found = true;

            // Agrega el nuevo punto a las listas y la cuadr�cula.
            bags.SamplePoints.Add(p);
            bags.ActivePoints.Add(p);
            bags.Grid[index.x, index.y] = p;
        }

        return found;
    }

    // Genera el primer punto aleatorio y lo agrega a las listas.
    private static void GetFirstPoint(Settings set, Bags bags)
    {
        var first = new Vector2(
            Random.Range(set.BottomLeft.x, set.TopRight.x),
            Random.Range(set.BottomLeft.y, set.TopRight.y)
        );

        var index = GetGridIndex(first, set);

        bags.Grid[index.x, index.y] = first;
        bags.SamplePoints.Add(first);
        bags.ActivePoints.Add(first);
    }

    // Convierte una posici�n en coordenadas locales a �ndices de cuadr�cula.
    private static Vector2Int GetGridIndex(Vector2 point, Settings set)
    {
        return new Vector2Int(
            Mathf.FloorToInt((point.x - set.BottomLeft.x) / set.CellSize),
            Mathf.FloorToInt((point.y - set.BottomLeft.y) / set.CellSize)
        );
    }

    // Configura los par�metros del algoritmo de Poisson.
    private static Settings GetSettings(Vector2 bl, Vector2 tr, float min, int iteration)
    {
        var dimension = (tr - bl);
        var cell = min * InvertRootTwo;

        return new Settings()
        {
            BottomLeft = bl,
            TopRight = tr,
            Center = (bl + tr) * 0.5f,
            Dimension = new Rect(new Vector2(bl.x, bl.y), new Vector2(dimension.x, dimension.y)),

            MinimumDistance = min,
            IterationPerPoint = iteration,

            CellSize = cell,
            GridWidth = Mathf.CeilToInt(dimension.x / cell),
            GridHeight = Mathf.CeilToInt(dimension.y / cell)
        };
    }

    // Genera una posici�n aleatoria dentro de un c�rculo.
    private static Vector2 GetRandPosInCircle(float fieldMin, float fieldMax)
    {
        var theta = Random.Range(0f, Mathf.PI * 2f);
        var radius = Mathf.Sqrt(Random.Range(fieldMin * fieldMin, fieldMax * fieldMax));

        return new Vector2(radius * Mathf.Cos(theta), radius * Mathf.Sin(theta));
    }
}