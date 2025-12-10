using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PuzzleBoardScript : MonoBehaviour
{
    [ReadOnly] public int N;
    [ReadOnly] public float WidthOfPiece;

    public bool ManualMoveEmptyPieceEnabled = false;

    // Словарь игровых плиток на доске, ключ - номер плитки
    private Dictionary<int, GameObject> piecesOnBoard = new Dictionary<int, GameObject>();

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
        piecesOnBoard.Clear();
        for (int i = 1; i <= N * N; i++)
        {
            GameObject puzzlePiece = PuzzlePieceScript.AddPiece(
                prefab: piecePrefab,
                board: this.gameObject,
                pieceNumber: i,
                isUpRendered: isUpRendered,
                isDownRendered: isDownRendered);
            piecesOnBoard.Add(i, puzzlePiece);
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
        foreach (var kv in piecesOnBoard)
        {
            PuzzlePieceScript pieceScript = kv.Value.GetComponent<PuzzlePieceScript>();
            pieceScript.UpdateLocalPositionByPiecesArrangement(piecesArrangement);
        }
    }

    // Получить игровую плитку по её номеру (ключу)
    public GameObject GetPieceOnBoard(int pieceNumber)
    {
        piecesOnBoard.TryGetValue(pieceNumber, out var piece);
        return piece;
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
