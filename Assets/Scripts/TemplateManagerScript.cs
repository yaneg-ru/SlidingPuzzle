using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TemplateManagerScript : MonoBehaviour
{

    public static int N; // Размерность пазлов N x N
    public static float WidthOfPiece; // Ширина одной плитки пазла, рассчитывается как 1 / N

    [SerializeField] private int NxN = 3;
    [ReadOnly, SerializeField] private float pieceWidth;

    public GameObject piecePrefabForPuzzleOne; // Префаб для плитки пазла #1
    public GameObject piecePrefabForPuzzleTwo; // Префаб для плитки пазла #2
    public GameObject puzzleOneBoard; // Доска пазла #1
    public GameObject puzzleTwoBoard; // Доска пазла #2
    public int countVarietiesShuffleOfPuzzle = 100; // Количество вариантов перемешивания пазла для выбора лучшего
    public int maxCountMovesForOneVarietyPuzzleShuffle = 20; // Максимальное количество ходов для перемешивания пазла в одном варианте


    private PuzzleBoardScript puzzleOneBoardScript;
    private PuzzleBoardScript puzzleTwoBoardScript;


    void Awake()
    {
        N = NxN;
        WidthOfPiece = 1f / N;
        pieceWidth = Mathf.Round(WidthOfPiece * 100f) / 100f; // округление до 2 знаков после запятой;
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


        // Перемешивание обоих пазлов с учётом случайного порядка перемешивания

        // Определение, какой пазл будет первым перемешиваться
        int firstPuzzleWillBe = Random.Range(1, 3);
        // Определяем на сколько ходов один из пазлов будет перемешан раньше другого 
        int delta = Random.Range(1, 5);

        if (firstPuzzleWillBe == 1)
        {
            // Первым перемешивается пазл #1
            var misplacedPiecesCountPuzzleOne = puzzleOneBoardScript.CreateShuffledPiecesArrangement(
                countVarieties: countVarietiesShuffleOfPuzzle,
                countMovesForShuffle: maxCountMovesForOneVarietyPuzzleShuffle,
                targetCountMisplacedPieces: null);
            // Вторым перемешивается пазл #2 на delta ходов меньше (т.е. он будет собран раньше)
            puzzleTwoBoardScript.CreateShuffledPiecesArrangement(
                countVarieties: countVarietiesShuffleOfPuzzle,
                countMovesForShuffle: maxCountMovesForOneVarietyPuzzleShuffle - delta,
                targetCountMisplacedPieces: misplacedPiecesCountPuzzleOne);
        }
        else
        {
            // Первым перемешивается пазл #2
            var misplacedPiecesCountPuzzleTwo = puzzleTwoBoardScript.CreateShuffledPiecesArrangement(
                countVarieties: countVarietiesShuffleOfPuzzle,
                countMovesForShuffle: maxCountMovesForOneVarietyPuzzleShuffle,
                targetCountMisplacedPieces: null);
            // Вторым перемешивается пазл #1 на delta ходов меньше (т.е. он будет собран раньше)
            puzzleOneBoardScript.CreateShuffledPiecesArrangement(
                countVarieties: countVarietiesShuffleOfPuzzle,
                countMovesForShuffle: maxCountMovesForOneVarietyPuzzleShuffle - delta,
                targetCountMisplacedPieces: misplacedPiecesCountPuzzleTwo);
        }


        // Применение перемешанного расположения плиток пазлов к реальным плиткам на досках
        puzzleOneBoardScript.ApplyPiecesArrangementToBoard();
        puzzleTwoBoardScript.ApplyPiecesArrangementToBoard();
    }

    void Update()
    {

    }
}
