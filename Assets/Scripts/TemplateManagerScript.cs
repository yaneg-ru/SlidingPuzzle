using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TemplateManagerScript : MonoBehaviour
{

    public static int N; // Размерность пазлов N x N
    public static float widthOfPiece; // Ширина одной плитки пазла, рассчитывается как 1 / N

    [SerializeField] private int NxN = 3;
    [ReadOnly, SerializeField] private float pieceWidth;

    public GameObject piecePrefabForPuzzleOne; // Префаб для плитки пазла #1
    public GameObject piecePrefabForPuzzleTwo; // Префаб для плитки пазла #2
    public GameObject puzzleOneBoard; // Доска пазла #1
    public GameObject puzzleTwoBoard; // Доска пазла #2
    public int countVarietiesShuffleOfPuzzle = 100; // Количество вариантов перемешивания пазла для выбора лучшего
    public int countMovesForOneVarietyPuzzleShuffle = 20; // Количество ходов для перемешивания пазла в одном варианте


    private PuzzleBoardScript puzzleOneBoardScript;
    private PuzzleBoardScript puzzleTwoBoardScript;


    void Awake()
    {
        N = NxN;
        widthOfPiece = 1f / N;
        pieceWidth = Mathf.Round(widthOfPiece * 100f) / 100f; // округление до 2 знаков после запятой;
    }

    void Start()
    {
        puzzleOneBoardScript = puzzleOneBoard.GetComponent<PuzzleBoardScript>();
        puzzleTwoBoardScript = puzzleTwoBoard.GetComponent<PuzzleBoardScript>();

        // Начальная расстановка плиток на досках пазлов
        puzzleOneBoardScript.InitialPlacePiecesOnBoard(
           piecePrefab: piecePrefabForPuzzleOne,
           boardId: "1 PuzzleBoard",
           isUpRendered: true,
           isDownRendered: true);
        puzzleTwoBoardScript.InitialPlacePiecesOnBoard(
            piecePrefab: piecePrefabForPuzzleTwo,
            boardId: "2 PuzzleBoard",
            isUpRendered: true,
            isDownRendered: false);

        // Создание вариантов перемешивания пазлов
        var misplacedPiecesCountPuzzleOne = puzzleOneBoardScript.ShufflePieces(countVarietiesShuffleOfPuzzle, countMovesForOneVarietyPuzzleShuffle);
        puzzleTwoBoardScript.ShufflePieces(countVarietiesShuffleOfPuzzle, countMovesForOneVarietyPuzzleShuffle, misplacedPiecesCountPuzzleOne);
    }

    void Update()
    {

    }
}
