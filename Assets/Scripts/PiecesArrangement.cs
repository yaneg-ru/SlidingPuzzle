using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PieceInfo
{
    public int PieceNumber;
    public int Current1DCoord { get; set; } // текущая 1D координата плитки на доске
    public int Row { get; set; } // текущая строка плитки на доске
    public int Column { get; set; } // текущий столбец плитки на доске

}

// Класс содержащий виртуальную карту расположения плиток пазла 
// - перемешивание плиток пазла
// - пошаговая сборка пазла
public class PiecesArrangement
{
    // В массиве хранятся номера плиток пазла
    // 1-based indexing [row, column]
    // левый верхний угол - (1,1)
    // правый верхний угол - (1,N)
    // левый нижний угол - (N,1)
    // правый нижний угол - (N,N)
    public int[,] Arrangement;

    // Номер пустой плитки
    public int EmptyPieceNumber;

    // Текущая координата пустой плитки
    // левый верхний угол - 1
    // правый верхний угол - N
    // левый нижний угол - N*(N-1)+1
    // правый нижний угол - N*N
    public int EmptyPieceCoord;

    // Количество неправильно расположенных плиток
    // null если вариант перемешивания не приемлем, так как пазл становится решенным в ходе перемешивания
    public int? CountMisplacedPieces;

    // История ходов пустой плитки
    public List<string> EmptyPieceMoveHistory = new List<string>();

    Queue<int> recentEmptyPieceCoords = new Queue<int>(); // очередь для хранения недавних позиций пустой плитки
    int recentEmptyCoordsQueueMaxLength; // максимальная длина очереди для хранения недавних позиций пустой плитки

    int n; // размерность пазла N x N

    int currentBuildStep; // текущий шаг сборки пазла

    // Инициализируем генератор случайных чисел
    private static readonly System.Random rnd = new System.Random();

    public PiecesArrangement()
    {
        // Изначально все плитки на своих местах
        CountMisplacedPieces = 0;

        n = TemplateManagerScript.N;
        Arrangement = new int[n + 1, n + 1]; // 1-based indexing

        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= n; j++)
            {
                Arrangement[i, j] = (i - 1) * n + j;
            }
        }

        // выбираем случайный номер пустой плитки от 1 до N*N
        EmptyPieceNumber = rnd.Next(1, n * n + 1);
        EmptyPieceCoord = EmptyPieceNumber;

        // случайная длина очереди для хранения недавних позиций пустой плитки от 4 до 10 включительно
        recentEmptyCoordsQueueMaxLength = rnd.Next(4, 11);
        AddEmptyCoordToRecent(EmptyPieceCoord);
    }

    // Перемешивание пазла заданное количество раз
    // На каждом шаге выбирается случайное движение пустой плитки
    // Если на каком то шаге мы получаем ситуацию, когда CountMisplacedPieces == 0, 
    // тогда перемешивание прекращается и в переменную CountMisplacedPieces присваивается значение null
    public void Shuffle(int countSteps)
    {
        // сразу устанавливаем текущий шаг сборки пазла в общее количество шагов
        // при сборке пазла это значение будет уменьшаться
        currentBuildStep = countSteps;

        for (int stepIndex = 0; stepIndex < countSteps; stepIndex++)
        {
            string move = GetRandomMove();
            EmptyPieceMoveHistory.Add(move);

            int newEmptyCoord = GetEmptyPieceCoordByMove(move);

            // Координаты текущей и новой позиции пустой плитки (в 1D)
            int oldCoord = EmptyPieceCoord;
            EmptyPieceCoord = newEmptyCoord;

            // Переводим 1D координаты в 2D индексы (row, col)
            var oldRowCol = Coord1DHelper.Convert1DCoordToRowCol(oldCoord);
            int oldRow = oldRowCol[0];
            int oldCol = oldRowCol[1];

            var newRowCol = Coord1DHelper.Convert1DCoordToRowCol(newEmptyCoord);
            int newRow = newRowCol[0];
            int newCol = newRowCol[1];

            // Меняем местами значения в массиве Arrangement
            int temp = Arrangement[oldRow, oldCol];
            Arrangement[oldRow, oldCol] = Arrangement[newRow, newCol];
            Arrangement[newRow, newCol] = temp;

            // Обновляем очередь недавних позиций пустой плитки
            AddEmptyCoordToRecent(newEmptyCoord);

            // Пересчитываем количество неправильно расположенных плиток
            CalcCountMisplacedPieces();

            // Если головоломка решена — выходим и устанавливаем CountMisplacedPieces в null
            // Нас такой вариант не устраивает так как текущий вариант генерации ходов приводит пазл к решённому состоянию
            if (CountMisplacedPieces == 0)
            {
                CountMisplacedPieces = null;
                break;
            }
        }
        // After shuffle (including early break), align currentBuildStep to actual number of moves
        currentBuildStep = EmptyPieceMoveHistory.Count;
    }

    // Метод выполняет один шаг сборки пазла
    public void BuildStep()
    {
        if (currentBuildStep <= 0 || EmptyPieceMoveHistory.Count == 0)
        {
            return; // все шаги сборки уже выполнены или история пуста
        }

        // Safe index calculation to avoid out-of-range when history < currentBuildStep
        int idx = EmptyPieceMoveHistory.Count - currentBuildStep;
        if (idx < 0 || idx >= EmptyPieceMoveHistory.Count)
        {
            // Align and bail out if inconsistent; caller can invoke again next frame
            currentBuildStep = Mathf.Min(currentBuildStep, EmptyPieceMoveHistory.Count);
            return;
        }

        // получаем исходный ход пустой плитки для текущего шага сборки
        string recordedMove = EmptyPieceMoveHistory[idx];

        // для отмены шага применяем обратное движение
        string reverseMove = InvertMove(recordedMove);

        int newEmptyCoord = GetEmptyPieceCoordByMove(reverseMove);

        // Координаты текущей и новой позиции пустой плитки (в 1D)
        int oldCoord = EmptyPieceCoord;
        EmptyPieceCoord = newEmptyCoord;

        // Переводим 1D координаты в 2D индексы (row, col)
        var oldRowCol = Coord1DHelper.Convert1DCoordToRowCol(oldCoord);
        int oldRow = oldRowCol[0];
        int oldCol = oldRowCol[1];

        var newRowCol = Coord1DHelper.Convert1DCoordToRowCol(newEmptyCoord);
        int newRow = newRowCol[0];
        int newCol = newRowCol[1];

        // Меняем местами значения в массиве Arrangement
        int temp = Arrangement[oldRow, oldCol];
        Arrangement[oldRow, oldCol] = Arrangement[newRow, newCol];
        Arrangement[newRow, newCol] = temp;

        // Обновляем очередь недавних позиций пустой плитки
        AddEmptyCoordToRecent(newEmptyCoord);

        // Пересчитываем количество неправильно расположенных плиток
        CalcCountMisplacedPieces();

        // уменьшаем текущий шаг сборки пазла
        currentBuildStep--;
    }

    // Метод выполняет движение пустой плитки в заданном направлении
    // direction - "up", "down", "left", "right"
    // Используется для ручного перемещения пустой плитки для тестирования
    // анимации и эффектов при перемещении плиток пазла
    public void MoveEmptyPiece(string direction)
    {
        int newEmptyCoord = GetEmptyPieceCoordByMove(direction);

        // Координаты текущей и новой позиции пустой плитки (в 1D)
        int oldCoord = EmptyPieceCoord;
        EmptyPieceCoord = newEmptyCoord;

        // Переводим 1D координаты в 2D индексы (row, col)
        var oldRowCol = Coord1DHelper.Convert1DCoordToRowCol(oldCoord);
        int oldRow = oldRowCol[0];
        int oldCol = oldRowCol[1];

        var newRowCol = Coord1DHelper.Convert1DCoordToRowCol(newEmptyCoord);
        int newRow = newRowCol[0];
        int newCol = newRowCol[1];

        // Меняем местами плитки в массиве Arrangement
        int temp = Arrangement[oldRow, oldCol];
        Arrangement[oldRow, oldCol] = Arrangement[newRow, newCol];
        Arrangement[newRow, newCol] = temp;

        // Пересчитываем количество неправильно расположенных плиток
        CalcCountMisplacedPieces();
    }

    // Получение информации о плитке по её номеру
    public PieceInfo GetPieceInfoByNumber(int pieceNumber)
    {
        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= n; j++)
            {
                if (Arrangement[i, j] == pieceNumber)
                {
                    return new PieceInfo
                    {
                        PieceNumber = pieceNumber,
                        Current1DCoord = Coord1DHelper.RowColToCoord1D(i, j),
                        Row = i,
                        Column = j
                    };
                }
            }
        }
        return null; // плитка с таким номером не найдена
    }

    private void AddEmptyCoordToRecent(int coord)
    {
        if (!recentEmptyPieceCoords.Contains(coord))
        {
            recentEmptyPieceCoords.Enqueue(coord);
        }
        while (recentEmptyPieceCoords.Count > recentEmptyCoordsQueueMaxLength)
        {
            // удаляем самые старые (1 элемент из очереди) элементы, если превышена максимальная длина очереди
            recentEmptyPieceCoords.Dequeue();
        }
    }

    private int GetEmptyPieceCoordByMove(string move)
    {
        // текущие строка и столбец пустой плитки
        var emptyPieceRowCol = Coord1DHelper.Convert1DCoordToRowCol(EmptyPieceCoord);
        int row = emptyPieceRowCol[0];
        int column = emptyPieceRowCol[1];

        // рассчитываем новые строку и столбец пустой плитки в зависимости от направления движения c с учётом wrapping'а
        switch (move.ToLowerInvariant())
        {
            case "up":
                row = row == 1 ? n : row - 1;
                break;
            case "down":
                row = row == n ? 1 : row + 1;
                break;
            case "left":
                column = column == 1 ? n : column - 1;
                break;
            case "right":
                column = column == n ? 1 : column + 1;
                break;
        }

        // Гарантированно валидные координаты благодаря wrapping'у
        return Coord1DHelper.RowColToCoord1D(row, column);
    }

    private string GetRandomMove()
    {
        var allMoves = new List<string> { "up", "down", "left", "right" };
        var possibleMoves = new List<string>(allMoves);

        // исключаем движения, которые приведут к недавним позициям пустой плитки
        // RemoveAll — метод класса List<T>, который удаляет все элементы, удовлетворяющие предикату (условию)
        possibleMoves.RemoveAll(move => recentEmptyPieceCoords.Contains(GetEmptyPieceCoordByMove(move)));

        // выбираем случайное движение из оставшихся возможных
        if (possibleMoves.Count == 0)
        {
            // если нет возможных движений, возвращаем любое случайное движение
            return allMoves[rnd.Next(0, allMoves.Count)];
        }
        int randomIndex = rnd.Next(0, possibleMoves.Count);
        return possibleMoves[randomIndex];
    }

    private void CalcCountMisplacedPieces()
    {
        int count = 0;
        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= n; j++)
            {
                int expectedNumber = (i - 1) * n + j;
                if (Arrangement[i, j] != expectedNumber)
                {
                    count++;
                }
            }
        }
        CountMisplacedPieces = count;
    }

    private string InvertMove(string move)
    {
        switch (move.ToLowerInvariant())
        {
            case "up": return "down";
            case "down": return "up";
            case "left": return "right";
            case "right": return "left";
            default: throw new System.ArgumentException("Invalid move direction");
        }
    }

}

public class PuzzleShuffle
{

    // countVarieties - количество различных вариантов перемешивания пазла
    // countMovesForShuffle - количество ходов для перемешивания пазла
    // targetCountMisplacedPieces - целевое количество неправильно расположенных плиток (если нужно)
    // boardId - идентификатор доски пазла для логов
    // возвращает вариант перемешивания пазла с наибольшим количеством неправильно расположенных плиток 
    // или вариант у которого CountMisplacedPieces == targetCountMisplacedPieces
    public static PiecesArrangement GetBestVarietyShuffleOfPuzzle(
        int countVarieties,
        int countMovesForShuffle,
        string boardId,
        int? targetCountMisplacedPieces = null)
    {
        var varieties = new List<PiecesArrangement>();

        for (int i = 0; i < countVarieties; i++)
        {
            PiecesArrangement arrangement = new PiecesArrangement();
            arrangement.Shuffle(countMovesForShuffle); // перемешиваем пазл countMovesForShuffle ходов
            // Debug.Log($"Variety {i + 1}: Misplaced Pieces = {arrangement.CountMisplacedPieces}");
            varieties.Add(arrangement);
        }

        // находим вариант с наибольшим количеством неправильно расположенных плиток
        int? maxMisplacedPieces = -1;
        PiecesArrangement bestVariety = null;
        foreach (var variety in varieties)
        {
            if (variety.CountMisplacedPieces != null)
            {

                if (targetCountMisplacedPieces != null && variety.CountMisplacedPieces == targetCountMisplacedPieces)
                {
                    Debug.Log($"For {boardId} Best variety selected (target met): Misplaced Pieces = {variety.CountMisplacedPieces}");
                    return variety;
                }
                if (variety.CountMisplacedPieces >= maxMisplacedPieces)
                {
                    maxMisplacedPieces = variety.CountMisplacedPieces;
                    bestVariety = variety;
                }
            }
        }
        Debug.Log($"For {boardId} Best variety selected: Misplaced Pieces = {bestVariety?.CountMisplacedPieces}");
        return bestVariety;
    }

}
