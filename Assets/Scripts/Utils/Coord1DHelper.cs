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

    // Проверка возможности перемещения из current в target без "обёртывания"
    public static bool IsPossibleMoveWithoutWrapping(int current, int target)
    {
        var N = TemplateManagerScript.N;
        var currentRowCol = Convert1DCoordToRowCol(current);
        var targetRowCol = Convert1DCoordToRowCol(target);
        int currentRow = currentRowCol[0];
        int currentCol = currentRowCol[1];
        int targetRow = targetRowCol[0];
        int targetCol = targetRowCol[1];

        // Проверяем, что цель находится на расстоянии 1 по строке или столбцу
        if (currentRow == targetRow && Mathf.Abs(currentCol - targetCol) == 1)
        {
            return true; // Горизонтальное перемещение
        }
        if (currentCol == targetCol && Mathf.Abs(currentRow - targetRow) == 1)
        {
            return true; // Вертикальное перемещение
        }
        return false; //  Перемещение невозможно без обёртывания
    }
}
