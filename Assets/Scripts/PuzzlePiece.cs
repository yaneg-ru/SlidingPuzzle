using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class PuzzlePiece : MonoBehaviour
{

    public int pieceID;
    public int numberOfPuzzle;


    // статический фабричный метод для создания экземпляра префаба для плитки пазла
    public static GameObject AddPiece(GameObject prefab, Transform board, int pieceNumber)
    {
        GameObject instanceObject = GameObject.Instantiate(prefab, board);
        // масштабирование в зависимости от N
        float n = TemplateManager.N != 0 ? (float)TemplateManager.N : 1f;
        instanceObject.transform.localScale = new Vector3(1f / n, 1f / n, 1f / n);
        // присваиваем материал в зависимости от номера пазла
        MeshRenderer meshRenderer = instanceObject.GetComponent<MeshRenderer>();

        return instanceObject;
    }

    void Start()
    {

    }



    // Update is called once per frame
    void Update()
    {

    }
}
