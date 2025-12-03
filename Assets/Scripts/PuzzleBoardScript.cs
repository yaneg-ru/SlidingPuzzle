using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PuzzleBoardScript : MonoBehaviour
{
    [ReadOnly] public int N;
    [ReadOnly] public float widthOfPiece;
    private List<GameObject> piecesOnBoard = new List<GameObject>();

    void Awake()
    {
    }

    public void InitialPlacePiecesOnBoard(GameObject piecePrefab)
    {
        N = TemplateManagerScript.N;
        widthOfPiece = TemplateManagerScript.widthOfPiece;
        for (int i = 1; i <= N * N; i++)
        {
            GameObject puzzlePiece = PuzzlePieceScript.AddPiece(prefab: piecePrefab, board: this.gameObject, pieceNumber: i);
            piecesOnBoard.Add(puzzlePiece);
        }
    }

    void Start()
    {

    }

    void Update()
    {

    }
}
