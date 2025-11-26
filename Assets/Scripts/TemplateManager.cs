using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TemplateManager : MonoBehaviour
{

    public static int N; // Размерность пазлов N x N
    [SerializeField]
    private int inspectorN = 3;

    public GameObject piecePrefabForPuzzleOne; // Префаб для плитки пазла #1
    public GameObject piecePrefabForPuzzleTwo; // Префаб для плитки пазла #2
    public Transform puzzleOneBoard; // Трансформ для доски пазла #1
    public Transform puzzleTwoBoard; // Трансформ для доски пазла #2

    void Awake()
    {
        N = inspectorN;
    }

    void Start()
    {
        GameObject puzzlePiece1 = PuzzlePiece.AddPiece(prefab: piecePrefabForPuzzleOne, board: puzzleOneBoard, pieceNumber: 1);
        GameObject puzzlePiece2 = PuzzlePiece.AddPiece(prefab: piecePrefabForPuzzleTwo, board: puzzleTwoBoard, pieceNumber: 2);

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
