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

    void Awake()
    {
        N = NxN;
        widthOfPiece = 1f / N;
        pieceWidth = Mathf.Round(widthOfPiece * 100f) / 100f; // округление до 2 знаков после запятой;
    }

    void Start()
    {
        puzzleOneBoard.GetComponent<PuzzleBoardScript>().PutInitialPieces(piecePrefabForPuzzleOne);
        puzzleTwoBoard.GetComponent<PuzzleBoardScript>().PutInitialPieces(piecePrefabForPuzzleTwo);
    }

    void Update()
    {

    }
}
