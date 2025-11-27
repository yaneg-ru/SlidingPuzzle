using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PuzzlePiece : MonoBehaviour
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
    public static GameObject AddPiece(GameObject prefab, Transform board, int pieceNumber)
    {
        GameObject piece = GameObject.Instantiate(prefab, board);
        piece.name = $"{pieceNumber}";

        // сохраняем значения номера плитки в скрипте PuzzlePiece
        PuzzlePiece pieceScript = piece.GetComponent<PuzzlePiece>();
        pieceScript.pieceNumber = pieceNumber;
        pieceScript.currentNumberOnBoard = pieceNumber;

        // рассчитываем и сохраняем текущие строку и столбец плитки на доске
        pieceScript.calcRowAndColumnFromCurrentNumberOnBoard();

        // масштабирование
        piece.transform.localScale = new Vector3(TemplateManager.widthOfPiece, TemplateManager.widthOfPiece, 0);
        GameObject pieceUP = piece.transform.Find("UP").gameObject;
        pieceUP.transform.localScale = new Vector3(pieceScript.scaleOfUpPiece, pieceScript.scaleOfUpPiece, 1f);

        // обновляем локальную позицию плитки на доске
        pieceScript.updateLocalPosition();


        return piece;
    }

    // расчёт в 1‑базовой системе: строка и столбец начинаются с 1
    // левый верхний угол — (1, 1) 
    // правый нижний угол — (N, N)
    private void calcRowAndColumnFromCurrentNumberOnBoard()
    {
        int N = TemplateManager.N;
        currentRowOnBoard = ((currentNumberOnBoard - 1) / N) + 1;
        currentColumnOnBoard = ((currentNumberOnBoard - 1) % N) + 1;
    }

    private void updateLocalPosition()
    {
        float pieceSize = TemplateManager.widthOfPiece;
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
