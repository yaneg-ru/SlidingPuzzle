using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PuzzleBoardScript : MonoBehaviour
{
    [ReadOnly] public int N;
    [ReadOnly] public float WidthOfPiece;

    public bool ManualMoveEmptyPieceEnabled = false;

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
        WidthOfPiece = TemplateManagerScript.WidthOfPiece;
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
    public int CreateShuffledPiecesArrangement(int countVarieties, int countMovesForShuffle, int? targetCountMisplacedPieces = null)
    {
        piecesArrangement = PuzzleShuffle.GetBestVarietyShuffleOfPuzzle(
            countVarieties,
            countMovesForShuffle,
            boardId,
            targetCountMisplacedPieces);
        return piecesArrangement.CountMisplacedPieces ?? 0;
    }

    // Мгновенное применение расположения плиток пазла из piecesArrangement к реальным плиткам на доске
    public void ApplyPiecesArrangementToBoard()
    {
        foreach (GameObject piece in piecesOnBoard)
        {
            PuzzlePieceScript pieceScript = piece.GetComponent<PuzzlePieceScript>();
            pieceScript.UpdateLocalPositionByPiecesArrangement(piecesArrangement);
        }
    }

    void Start()
    {

    }

    void Update()
    {
        if (ManualMoveEmptyPieceEnabled)
        {
            HandleArrowInput();
        }
    }

    /// Слушает нажатия стрелок и двигает пустую плитку в piecesArrangement.
    private void HandleArrowInput()
    {
        if (piecesArrangement == null) return;

        string direction = null;

        if (Input.GetKeyDown(KeyCode.UpArrow)) direction = "up";
        else if (Input.GetKeyDown(KeyCode.DownArrow)) direction = "down";
        else if (Input.GetKeyDown(KeyCode.LeftArrow)) direction = "left";
        else if (Input.GetKeyDown(KeyCode.RightArrow)) direction = "right";

        if (direction != null)
        {
            piecesArrangement.MoveEmptyPiece(direction); // calls PiecesArrangement.MoveEmptyPiece
            ApplyPiecesArrangementToBoard();             // обновляем позиции визуальных плиток
        }
    }
}
