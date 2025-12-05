using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PuzzleBoardScript : MonoBehaviour
{
    [ReadOnly] public int N;
    [ReadOnly] public float widthOfPiece;
    private List<GameObject> piecesOnBoard = new List<GameObject>();

    private PiecesArrangement piecesArrangement;
    private string boardId;

    void Awake()
    {
    }

    public void InitialPlacePiecesOnBoard(
        GameObject piecePrefab,
        string boardId,
        bool isUpRendered = true,
        bool isDownRendered = true)
    {
        this.boardId = boardId;
        N = TemplateManagerScript.N;
        widthOfPiece = TemplateManagerScript.widthOfPiece;
        for (int i = 1; i <= N * N; i++)
        {
            GameObject puzzlePiece = PuzzlePieceScript.AddPiece(
                prefab: piecePrefab,
                board: this.gameObject,
                pieceNumber: i,
                isUpRendered: isUpRendered,
                isDownRendered: isDownRendered);
            piecesOnBoard.Add(puzzlePiece);
        }
    }

    // Создание вариантов перемешивания пазла
    // Возвращает количество неправильно расположенных плиток в лучшем найденном варианте перемешивания
    public int ShufflePieces(int countVarieties, int countMovesForShuffle, int? targetCountMisplacedPieces = null)
    {
        piecesArrangement = PuzzleShuffle.GetBestVarietyShuffleOfPuzzle(
            countVarieties,
            countMovesForShuffle,
            boardId,
            targetCountMisplacedPieces);
        return piecesArrangement.CountMisplacedPieces ?? 0;
    }

    void Start()
    {

    }

    void Update()
    {

    }
}
