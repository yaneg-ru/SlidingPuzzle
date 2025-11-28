using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PuzzlePieceScript : MonoBehaviour
{

    [ReadOnly] public int pieceNumber;
    [ReadOnly] public int currentNumberOnBoard;
    [ReadOnly] public int currentColumnOnBoard;
    [ReadOnly] public int currentRowOnBoard;
    [ReadOnly] public float scaleOfUpPiece;

    void Awake()
    {
        scaleOfUpPiece = 0.95f;
    }

    // статический фабричный метод для создания экземпляра префаба для плитки пазла
    public static GameObject AddPiece(GameObject prefab, GameObject board, int pieceNumber)
    {
        GameObject piece = GameObject.Instantiate(prefab, board.GetComponent<Transform>());
        piece.name = $"{pieceNumber}";

        // сохраняем значения номера плитки в скрипте PuzzlePiece
        PuzzlePieceScript pieceScript = piece.GetComponent<PuzzlePieceScript>();
        pieceScript.pieceNumber = pieceNumber;
        pieceScript.currentNumberOnBoard = pieceNumber;

        // рассчитываем и сохраняем текущие строку и столбец плитки на доске
        pieceScript.calcRowAndColumnFromCurrentNumberOnBoard();

        // масштабирование
        piece.transform.localScale = new Vector3(TemplateManagerScript.widthOfPiece, TemplateManagerScript.widthOfPiece, 0);
        GameObject pieceUP = piece.transform.Find("UP").gameObject;
        pieceUP.transform.localScale = new Vector3(pieceScript.scaleOfUpPiece, pieceScript.scaleOfUpPiece, 1f);

        // обновляем локальную позицию плитки на доске
        pieceScript.updateLocalPosition();

        // Настраиваем UV‑координаты для текстуры плитки пазла
        Mesh mesh = pieceUP.GetComponent<MeshFilter>().mesh;
        Vector2[] uv = new Vector2[4];

        float widthOfPiece = TemplateManagerScript.widthOfPiece;

        // Используем рассчитанные строку и столбец плитки (1‑based)
        int col = pieceScript.currentColumnOnBoard;
        int row = pieceScript.currentRowOnBoard;

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

    // расчёт в 1‑базовой системе: строка и столбец начинаются с 1
    // левый верхний угол — (1, 1) 
    // правый нижний угол — (N, N)
    private void calcRowAndColumnFromCurrentNumberOnBoard()
    {
        int N = TemplateManagerScript.N;
        currentRowOnBoard = ((currentNumberOnBoard - 1) / N) + 1;
        currentColumnOnBoard = ((currentNumberOnBoard - 1) % N) + 1;
    }

    private void updateLocalPosition()
    {
        float pieceSize = TemplateManagerScript.widthOfPiece;
        float x = -0.5f + (pieceSize * (currentColumnOnBoard - 1)) + pieceSize / 2f;
        float y = +0.5f - (pieceSize * (currentRowOnBoard - 1)) - pieceSize / 2f;
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
