using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public Grid grid;

    public GameObject cellPrefab;

    public Text matrixNameText;

    private bool[,] cellsBool = new bool[8, 8];
    private GameObject[,] cellsGo = new GameObject[8, 8];

    private string matrixName = "Unamed Matrix";

    public void Save()
    {
        try
        {
            matrixName = matrixNameText.text;

            string folderPath = Directory.GetParent(Application.dataPath) + @"\Matricies\";
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);
            string path = folderPath + matrixName + ".txt";
            int collisionIndex = 1;
            while (File.Exists(path))
            {
                path = folderPath + matrixName + $" {collisionIndex}.txt";
                collisionIndex++;
            }
            File.WriteAllText(path, GetCellsString());
        }
        catch (Exception)
        {

        }
    }

    public void Quit()
    {
        Application.Quit();
    }

    public void Wipe()
    {
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                cellsGo[x, y].GetComponent<SpriteRenderer>().material.color = new Color(0, 0, 0);
                cellsBool[x, y] = false;
            }
        }
    }

    private void Start()
    {
        for (int y = 0; y < 8; y++)
        {
            for (int x = 0; x < 8; x++)
            {
                Vector3 worldPos = grid.CellToWorld(new Vector3Int(x, y, 0));
                worldPos.x += 0.5f;
                worldPos.y += 0.5f;
                GameObject cell = Instantiate(cellPrefab, worldPos, Quaternion.identity);
                cell.GetComponent<SpriteRenderer>().material.color = new Color(0, 0, 0);
                cellsGo[x, y] = cell;
                cellsBool[x, y] = false;
            }
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 mousePos = Input.mousePosition;
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
            Vector3Int cellPos = grid.WorldToCell(worldPos);
            cellPos.z = 0;
            if (CellPosValid(cellPos))
            {
                cellsBool[cellPos.x, cellPos.y] = !cellsBool[cellPos.x, cellPos.y];
                if (cellsBool[cellPos.x, cellPos.y])
                {
                    cellsGo[cellPos.x, cellPos.y].GetComponent<SpriteRenderer>().material.color = new Color(1, 1, 1);
                }
                else
                {
                    cellsGo[cellPos.x, cellPos.y].GetComponent<SpriteRenderer>().material.color = new Color(0, 0, 0);
                }
            }
            //Debug.Log(GetCellsString());
        }
    }
    private bool CellPosValid(Vector3Int cellPos)
    {
        return (cellPos.x >= 0 && cellPos.x <= 7) && (cellPos.y >= 0 && cellPos.y <= 7);
    }

    private string GetCellsString()
    {
        string output = "";
        for (int y = 7; y >= 0; y--)
        {
            for (int x = 0; x < 8; x++)
            {
                if (cellsBool[x, y])
                    output += "1";
                else
                    output += "0";
            }
            output += "\n";
        }
        return output;
    }
}
