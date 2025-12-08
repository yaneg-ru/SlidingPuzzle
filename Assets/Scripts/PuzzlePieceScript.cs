using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PuzzlePieceScript : MonoBehaviour
{

    [ReadOnly] public int PieceNumber;

    // Текущий 1D координата плитки на доске (меняется при перемещении)
    // левый верхний угол - 1
    // правый верхний угол - N
    // левый нижний угол - N*(N-1)+1
    // правый нижний угол - N*N
    [ReadOnly] public int Current1DCoordOnBoard;


    // Текущие строка и столбец плитки на доске (1-based indexing)
    // левый верхний угол - (1,1)
    // правый верхний угол - (1,N)
    // левый нижний угол - (N,1)
    // правый нижний угол - (N,N)
    [ReadOnly] public int CurrentRowOnBoard;
    [ReadOnly] public int CurrentColumnOnBoard;

    // Коэффициент масштаба для верхнего слоя плитки
    [ReadOnly] public float ScaleOfUpPiece;

    void Awake()
    {
        ScaleOfUpPiece = 0.95f;
    }

    // статический фабричный метод для создания экземпляра префаба для плитки пазла
    public static GameObject AddPiece(
        GameObject prefab,
        GameObject board,
        int pieceNumber,
        bool isUpRendered = true,
        bool isDownRendered = true)
    {
        GameObject piece = GameObject.Instantiate(prefab, board.GetComponent<Transform>());
        piece.name = $"{pieceNumber}";

        // сохраняем значения номера плитки в скрипте PuzzlePiece
        PuzzlePieceScript pieceScript = piece.GetComponent<PuzzlePieceScript>();
        pieceScript.PieceNumber = pieceNumber;
        pieceScript.Current1DCoordOnBoard = pieceNumber;

        // рассчитываем и сохраняем текущие строку и столбец плитки на доске
        pieceScript.calcRowAndColumnFromCurrentNumberOnBoard();

        // масштабирование
        piece.transform.localScale = new Vector3(TemplateManagerScript.widthOfPiece, TemplateManagerScript.widthOfPiece, 0);
        GameObject pieceUP = piece.transform.Find("UP").gameObject;
        pieceUP.transform.localScale = new Vector3(pieceScript.ScaleOfUpPiece, pieceScript.ScaleOfUpPiece, 1f);

        // настройка видимости верхней и нижней части плитки пазла
        pieceUP.GetComponent<MeshRenderer>().enabled = isUpRendered;
        GameObject pieceDOWN = piece.transform.Find("DOWN").gameObject;
        pieceDOWN.GetComponent<MeshRenderer>().enabled = isDownRendered;

        // обновляем локальную позицию плитки на доске
        pieceScript.updateLocalPositionByRowAndColum();

        // Настраиваем UV‑координаты для текстуры плитки пазла
        Mesh mesh = pieceUP.GetComponent<MeshFilter>().mesh;
        Vector2[] uv = new Vector2[4];

        float widthOfPiece = TemplateManagerScript.widthOfPiece;

        // Используем рассчитанные строку и столбец плитки (1‑based)
        int col = pieceScript.CurrentColumnOnBoard;
        int row = pieceScript.CurrentRowOnBoard;

        // UV в диапазоне [0,1): смещение по сетке NxN
        float uvX = (col - 1) * widthOfPiece;
        float uvY = (row - 1) * widthOfPiece;

        // Назначаем UV‑координаты четырём вершинам квадрата плитки
        uv[0] = new Vector2(uvX, 1f - (uvY + widthOfPiece)); // левый нижний угол
        uv[1] = new Vector2(uvX + widthOfPiece, 1f - (uvY + widthOfPiece)); // правый нижний угол
        uv[2] = new Vector2(uvX, 1f - uvY); // левый верхний угол
        uv[3] = new Vector2(uvX + widthOfPiece, 1f - uvY); // правый верхний угол

        mesh.uv = uv;

        return piece;
    }

    // Метод, который обновляет позицию плитки пазла 
    // в соответствии переданной виртуальной картой расположения плиток пазла
    // Если pieceNumber совпадает с EmptyPieceNumber из piecesArrangement 
    // тогда отключаем у плитки рендеринг UP объекта
    public void UpdateLocalPositionByPiecesArrangement(PiecesArrangement piecesArrangement)
    {
        int N = TemplateManagerScript.N;

        // Ищем позицию текущей плитки в массиве Arrangement
        for (int row = 1; row <= N; row++)
        {
            for (int col = 1; col <= N; col++)
            {
                if (piecesArrangement.Arrangement[row, col] == PieceNumber)
                {
                    // Обновляем текущую 1D координату плитки на доске
                    Current1DCoordOnBoard = (row - 1) * N + col;

                    // Пересчитываем строку и столбец из новой координаты
                    calcRowAndColumnFromCurrentNumberOnBoard();

                    // Обновляем локальную позицию плитки на доске
                    updateLocalPositionByRowAndColum();

                    // Если это пустая плитка, отключаем рендеринг UP объекта
                    if (PieceNumber == piecesArrangement.EmptyPieceNumber)
                    {
                        GameObject pieceUP = this.transform.Find("UP").gameObject;
                        pieceUP.GetComponent<MeshRenderer>().enabled = false;
                    }

                    return;
                }
            }
        }
    }

    // расчёт в 1‑базовой системе: строка и столбец начинаются с 1
    // левый верхний угол — (1, 1) 
    // правый нижний угол — (N, N)
    private void calcRowAndColumnFromCurrentNumberOnBoard()
    {
        var currentRowCol = Coord1DHelper.Convert1DCoordToRowCol(Current1DCoordOnBoard);
        CurrentRowOnBoard = currentRowCol[0];
        CurrentColumnOnBoard = currentRowCol[1];
    }

    private void updateLocalPositionByRowAndColum()
    {
        float pieceSize = TemplateManagerScript.widthOfPiece;
        float x = -0.5f + (pieceSize * (CurrentColumnOnBoard - 1)) + pieceSize / 2f;
        float y = +0.5f - (pieceSize * (CurrentRowOnBoard - 1)) - pieceSize / 2f;
        this.transform.localPosition = new Vector3(x, y, 0f);
    }



    void Start()
    {

    }



    // Update is called once per frame
    void Update()
    {

    }
}
