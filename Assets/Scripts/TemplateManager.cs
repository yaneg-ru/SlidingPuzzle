using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TemplateManager : MonoBehaviour
{

    public static int N; // Размерность пазлов N x N
    public static float widthOfPiece; // Ширина одной плитки пазла, рассчитывается как 1 / N
    [SerializeField] private int NxN = 3;
    [ReadOnly, SerializeField] private float pieceWidth;

    public GameObject piecePrefabForPuzzleOne; // Префаб для плитки пазла #1
    public GameObject piecePrefabForPuzzleTwo; // Префаб для плитки пазла #2
    public Transform puzzleOneBoard; // Трансформ для доски пазла #1
    public Transform puzzleTwoBoard; // Трансформ для доски пазла #2

    void Awake()
    {
        N = NxN;
        widthOfPiece = 1f / N;
        pieceWidth = Mathf.Round(widthOfPiece * 100f) / 100f; // округление до 2 знаков после запятой;
    }

    void Start()
    {
        for (int i = 1; i <= N * N; i++)
        {
            GameObject puzzlePiece1 = PuzzlePiece.AddPiece(prefab: piecePrefabForPuzzleOne, board: puzzleOneBoard, pieceNumber: i);
            GameObject puzzlePiece2 = PuzzlePiece.AddPiece(prefab: piecePrefabForPuzzleTwo, board: puzzleTwoBoard, pieceNumber: i);
        }

        //    if (numberOfPuzzle == 1)
        //     {
        //         puzzleBoard = GameObject.Find("PuzzleOneBoard").transform;
        //     }
        //     else if (numberOfPuzzle == 2)
        //     {
        //         puzzleBoard = GameObject.Find("PuzzleTwoBoard").transform;
        //     }        

    }

    void Update()
    {

    }
}
