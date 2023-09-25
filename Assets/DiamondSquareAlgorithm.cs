using UnityEngine;

public static class DiamondSquareAlgorithm
{
    public static float[,] GenerateHeightMap(int tamanio, float H)
    {
        float[,] datosTerreno = new float[tamanio + 1, tamanio + 1];

        //Altura de esquinas en 0
        datosTerreno[0, 0] = 0;
        datosTerreno[0, tamanio] = 0;
        datosTerreno[tamanio, 0] = 0;
        datosTerreno[tamanio, tamanio] = 0;

        DiamondSquare(datosTerreno, 0, 0, tamanio, H);

        return datosTerreno;
    }

    public static void DiamondSquare(float[,] datosTerreno, int inicioX, int inicioY, int tamanio, float H)
    {
        int mitadT = tamanio / 2;

        if (mitadT < 1)
            return;

        // Paso Diamond
        for (int i = inicioX + mitadT; i < inicioX + tamanio; i += mitadT)
        {
            for (int j = inicioY + mitadT; j < inicioY + tamanio; j += mitadT)
            {
                int x0 = i - mitadT;
                int x1 = i + mitadT;
                int y0 = j - mitadT;
                int y1 = j + mitadT;

                //Checkeo de los indices
                x0 = Mathf.Clamp(x0, 0, datosTerreno.GetLength(0) - 1);
                x1 = Mathf.Clamp(x1, 0, datosTerreno.GetLength(0) - 1);
                y0 = Mathf.Clamp(y0, 0, datosTerreno.GetLength(1) - 1);
                y1 = Mathf.Clamp(y1, 0, datosTerreno.GetLength(1) - 1);

                float promedio = (
                    datosTerreno[x0, y0] +
                    datosTerreno[x0, y1] +
                    datosTerreno[x1, y0] +
                    datosTerreno[x1, y1]
                ) / 4.0f;

                float RandH = Random.Range(-H, H);
                datosTerreno[i, j] = promedio + RandH;
            }
        }

        // Paso Square
        for (int i = inicioX; i < inicioX + tamanio; i += mitadT)
        {
            for (int j = inicioY; j < inicioY + tamanio; j += mitadT)
            {
                int x0 = i;
                int x1 = i + mitadT;
                int y0 = j;
                int y1 = j + mitadT;

                if (x1 >= datosTerreno.GetLength(0) || y1 >= datosTerreno.GetLength(1))
                    continue;

                float promedio = (
                    datosTerreno[x0, y0] +
                    datosTerreno[x0, y1] +
                    datosTerreno[x1, y0] +
                    datosTerreno[x1, y1]
                ) / 4.0f;

                float RandH = Random.Range(-H, H);
                datosTerreno[x0 + mitadT, y0 + mitadT] = promedio + RandH;
            }
        }

        //Recursividad
        DiamondSquare(datosTerreno, inicioX, inicioY, mitadT, H);
        DiamondSquare(datosTerreno, inicioX + mitadT, inicioY, mitadT, H);
        DiamondSquare(datosTerreno, inicioX, inicioY + mitadT, mitadT, H);
        DiamondSquare(datosTerreno, inicioX + mitadT, inicioY + mitadT, mitadT, H);
    }
}
