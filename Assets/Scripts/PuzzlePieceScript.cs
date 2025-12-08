using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PieceMoveInfo
{
    // Флаг активного перемещения
    public bool IsActive { get; set; }

    // 1D координаты
    public int From1D { get; set; }
    public int Target1DCoord { get; set; }

    // Накопленное время с начала анимации
    public float Elapsed { get; set; }

    public bool MoveWithoutWrapping { get; set; }

    public PieceMoveInfo(int from1D, int to1D)
    {
        IsActive = true;
        Target1DCoord = to1D;
        Elapsed = 0f;
        MoveWithoutWrapping = Coord1DHelper.IsPossibleMoveWithoutWrapping(from1D, to1D);
    }

    public void MoveDone()
    {
        IsActive = false;
        Elapsed = 0f;
    }

}

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
        float widthOfPiece = TemplateManagerScript.WidthOfPiece;

        piece.transform.localScale = new Vector3(widthOfPiece, widthOfPiece, 0);
        GameObject pieceUP = piece.transform.Find("UP").gameObject;
        pieceUP.transform.localScale = new Vector3(pieceScript.ScaleOfUpPiece, pieceScript.ScaleOfUpPiece, 1f);

        // настройка видимости верхней и нижней части плитки пазла
        pieceUP.GetComponent<MeshRenderer>().enabled = isUpRendered;
        GameObject pieceDOWN = piece.transform.Find("DOWN").gameObject;
        pieceDOWN.GetComponent<MeshRenderer>().enabled = isDownRendered;

        // Apply clip uniforms via MaterialPropertyBlock (matrix + bounds)
        var upRenderer = pieceUP.GetComponent<MeshRenderer>();
        var downRenderer = pieceDOWN.GetComponent<MeshRenderer>();

        var mpbUp = new MaterialPropertyBlock();
        var mpbDown = new MaterialPropertyBlock();

        // Мировые координаты доски
        var worldToParent = board.transform.worldToLocalMatrix;

        // Границы доски в локальном пространстве родителя
        mpbUp.SetMatrix("_WorldToParent", worldToParent);
        mpbUp.SetFloat("_MinX", -0.5f);
        mpbUp.SetFloat("_MaxX", 0.5f);
        mpbUp.SetFloat("_MinY", -0.5f);
        mpbUp.SetFloat("_MaxY", 0.5f);
        mpbUp.SetFloat("_EnableClip", 1f);

        mpbDown.SetMatrix("_WorldToParent", worldToParent);
        mpbDown.SetFloat("_MinX", -0.5f);
        mpbDown.SetFloat("_MaxX", 0.5f);
        mpbDown.SetFloat("_MinY", -0.5f);
        mpbDown.SetFloat("_MaxY", 0.5f);
        mpbDown.SetFloat("_EnableClip", 1f);

        upRenderer.SetPropertyBlock(mpbUp);
        downRenderer.SetPropertyBlock(mpbDown);

        // обновляем локальную позицию плитки на доске
        pieceScript.updateLocalPositionByRowAndColum();

        // Настраиваем UV‑координаты для текстуры плитки пазла
        Mesh mesh = pieceUP.GetComponent<MeshFilter>().mesh;
        Vector2[] uv = new Vector2[4];

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

    // Обновление локальной позиции плитки на доске по текущим значениям строки и столбца
    private void updateLocalPositionByRowAndColum()
    {
        float pieceSize = TemplateManagerScript.WidthOfPiece;
        float x = -0.5f + (pieceSize * (CurrentColumnOnBoard - 1)) + pieceSize / 2f;
        float y = +0.5f - (pieceSize * (CurrentRowOnBoard - 1)) - pieceSize / 2f;
        this.transform.localPosition = new Vector3(x, y, 0f);
    }

    // Конвертация (row, col) -> локальная позиция Vector3
    private static Vector3 RowColToLocalPos(int row, int col)
    {
        float pieceSize = TemplateManagerScript.WidthOfPiece;
        float x = -0.5f + (pieceSize * (col - 1)) + pieceSize / 2f;
        float y = +0.5f - (pieceSize * (row - 1)) - pieceSize / 2f;
        return new Vector3(x, y, 0f);
    }

    void Start()
    {

    }



    // Update is called once per frame
    void Update()
    {

    }
}
