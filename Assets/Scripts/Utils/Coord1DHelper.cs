using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Coord1DHelper
{
    // Конвертация 1D -> (row, col), оба в диапазоне 1..N
    public static int[] Convert1DCoordToRowCol(int coord)
    {
        var N = TemplateManagerScript.N;
        int row = (coord - 1) / N + 1;
        int col = (coord - 1) % N + 1;
        return new[] { row, col };
    }

    // Конвертация (row, col) -> 1D, оба в диапазоне 1..N
    public static int RowColToCoord1D(int row, int col)
    {
        var N = TemplateManagerScript.N;
        return (row - 1) * N + col;
    }
}
