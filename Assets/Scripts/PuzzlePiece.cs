using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PuzzlePiece : MonoBehaviour
{

    [ReadOnly] public int pieceNumber;
    [ReadOnly] public int currentNumberOnBoard;
    [ReadOnly, SerializeField] float gapThickness = 0.01f;

    // статический фабричный метод для создания экземпляра префаба для плитки пазла
    public static GameObject AddPiece(GameObject prefab, Transform board, int pieceNumber)
    {
        GameObject piece = GameObject.Instantiate(prefab, board);
        piece.name = $"{pieceNumber}";

        // сохраняем значения номера плитки в скрипте PuzzlePiece
        PuzzlePiece pieceScript = piece.GetComponent<PuzzlePiece>();
        pieceScript.pieceNumber = pieceNumber;
        pieceScript.currentNumberOnBoard = pieceNumber;

        // масштабирование в зависимости от N
        float n = TemplateManager.N != 0 ? (float)TemplateManager.N : 1f;
        piece.transform.localScale = new Vector3(1f / n, 1f / n, 0);

        // рассчитываем и присваиваем координаты плитки в зависимости от номера плитки и размерности пазла N


        return piece;
    }

    void Start()
    {

    }



    // Update is called once per frame
    void Update()
    {

    }
}
