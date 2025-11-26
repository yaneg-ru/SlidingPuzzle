using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PuzzlePiece : MonoBehaviour
{

    public int pieceID;
    public int numberOfPuzzle;

    private Transform puzzleBoard;


    // public static PuzzlePiece

    void Start()
    {
        if (numberOfPuzzle == 1)
        {
            puzzleBoard = GameObject.Find("PuzzleOneBoard").transform;
        }
        else if (numberOfPuzzle == 2)
        {
            puzzleBoard = GameObject.Find("PuzzleTwoBoard").transform;
        }
    }



    // Update is called once per frame
    void Update()
    {

    }
}
