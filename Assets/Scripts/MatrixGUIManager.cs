using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class MatrixGUIManager : MonoBehaviour
{
    public Grid grid;
    public int width = 8;

    public Button saveButton;
    public Button loadButton;
    public Button decreaseGridSizeButton;
    public Button increaseGridSizeButton;
    public Text matrixNameText;

    public GameObject borderPrefab;
    public GameObject linePrefab;
    public GameObject cellPrefab;
    public Transform bordersParent;
    public Transform linesParent;
    public Transform cellsParent;

    private float CenterOffset => width / 2f;

    private GameObject[] borders = new GameObject[4];
    private List<GameObject> lines = new List<GameObject>();
    private Queue<GameObject> additionalLines = new Queue<GameObject>();
    private List<GameObject[]> cells = new List<GameObject[]>();
    private List<bool[]> cellsBool = new List<bool[]>();
    private Queue<GameObject> additionalCells = new Queue<GameObject>();

    private bool dragTrigger = false;
    private List<Vector3Int> draggedCells = new List<Vector3Int>();

    private string matrixName;

    private float time = 0;

    private List<(float, Button)> disabledButtons = new List<(float, Button)>();

    public void Save()
    {
        try
        {
            if (string.IsNullOrEmpty(matrixNameText.text))
            {
                DisableButton(1, saveButton);
                return;
            }

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
            File.WriteAllText(path, GetMatrixData());
        }
        catch (Exception)
        {

        }
    }
    public void Load()
    {

    }
    public void Wipe()
    {
        for (int x = 0; x < cellsBool.Count; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                if (x < width)
                    SetCell(cells[x][y], false);
                cellsBool[x][y] = false;
            }
        }
    }
    public void Fill()
    {
        for (int x = 0; x < cellsBool.Count; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                if (x < width)
                    SetCell(cells[x][y], true);
                cellsBool[x][y] = true;
            }
        }
    }
    public void Invert()
    {
        for (int x = 0; x < cellsBool.Count; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                bool newState = !cellsBool[x][y];
                if (x < width)
                    SetCell(cells[x][y], newState);
                cellsBool[x][y] = newState;
            }
        }
    }
    public void DecreaseGridSize()
    {
        if (width > 1)
        {
            width--;
            UpdateGUI();
        }

        if (width == 1)
            decreaseGridSizeButton.interactable = false;
        else
        {
            increaseGridSizeButton.interactable = true;
            decreaseGridSizeButton.interactable = true;
        }
    }
    public void IncreaseGridSize()
    {
        if (width < 16)
        {
            width++;
            UpdateGUI();
        }

        if (width == 16)
            increaseGridSizeButton.interactable = false;
        else
        {
            increaseGridSizeButton.interactable = true;
            decreaseGridSizeButton.interactable = true;
        }
    }
    public void Quit()
    {
        Application.Quit();
    }

    private void Start()
    {
        UpdateGUI();
    }

    private void Update()
    {
        time += Time.deltaTime;
        CheckDisabledButtons();

        bool leftClickBegan = Input.GetMouseButtonDown(0);
        if (leftClickBegan || Input.GetMouseButton(0))
        {
            Vector3 mousePos = Input.mousePosition;
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(mousePos);
            Vector3Int cellPos = grid.WorldToCell(worldPos);
            if (CellPosInRange(cellPos))
            {
                if (leftClickBegan)
                {
                    dragTrigger = GetCellPos(cellPos);
                }
                if (!draggedCells.Contains(cellPos) && GetCellPos(cellPos) == dragTrigger)
                {
                    ToggleCellPos(cellPos);
                    draggedCells.Add(cellPos);
                }
            }
        }
        if (Input.GetMouseButtonUp(0))
        {
            draggedCells.Clear();
        }
    }

    private void UpdateGUI()
    {
        PlaceGrid();
        PlaceBorders();
        PlaceLines();
        PlaceCells();
    }

    private void PlaceGrid()
    {
        Vector3 gridPos = new Vector3(-CenterOffset, -4f);
        grid.transform.position = gridPos;
    }

    private void PlaceBorders()
    {
        bool bordersInitialized = borders[0] != null;

        Vector3 topBorderPos = new Vector3(0, 4);
        Quaternion topBorderRot = Quaternion.Euler(0, 0, -90);

        Vector3 rightBorderPos = new Vector3(CenterOffset, 0);
        Quaternion rightBorderRot = Quaternion.Euler(0, 0, 180);

        Vector3 bottomBorderPos = new Vector3(0, -4);
        Quaternion bottomBorderRot = Quaternion.Euler(0, 0, 90);

        Vector3 leftBorderPos = new Vector3(-CenterOffset, 0);
        Quaternion leftBorderRot = Quaternion.Euler(0, 0, 0);

        if (!bordersInitialized)
        {
            borders[0] = Instantiate(borderPrefab, topBorderPos, topBorderRot, bordersParent);
            borders[1] = Instantiate(borderPrefab, rightBorderPos, rightBorderRot, bordersParent);
            borders[2] = Instantiate(borderPrefab, bottomBorderPos, bottomBorderRot, bordersParent);
            borders[3] = Instantiate(borderPrefab, leftBorderPos, leftBorderRot, bordersParent);
        }
        else
        {
            borders[0].transform.position = topBorderPos;
            borders[1].transform.position = rightBorderPos;
            borders[2].transform.position = bottomBorderPos;
            borders[3].transform.position = leftBorderPos;
        }

        float borderYSca = GetBarrierScale();
        borders[0].transform.localScale = new Vector3(8, borderYSca, 1);
        borders[1].transform.localScale = new Vector3(8, 26.5f, 1);
        borders[2].transform.localScale = new Vector3(8, borderYSca, 1);
        borders[3].transform.localScale = new Vector3(8, 26.5f, 1);
    }

    private void PlaceLines()
    {
        foreach (GameObject line in lines) // Remove old lines
        {
            line.SetActive(false);
            additionalLines.Enqueue(line);
        }
        lines.Clear();

        // Place vertical lines
        for (int x = 1; x < width; x++)
        {
            GameObject line = GetLineGO();
            line.transform.position = new Vector3(x - CenterOffset, 0);
            line.transform.localEulerAngles = new Vector3(0, 0, 0);
            line.transform.localScale = new Vector3(4, 27, 1);
            lines.Add(line);
        }
        // Place horizontal
        for (int y = 1; y < 8; y++)
        {
            GameObject line = GetLineGO();
            line.transform.position = new Vector3(0, y - 4);
            line.transform.localEulerAngles = new Vector3(0, 0, 90);
            line.transform.localScale = new Vector3(4, GetBarrierScale(), 1);
            lines.Add(line);
        }
    }
    private GameObject GetLineGO()
    {
        if (additionalLines.Count > 0)
        {
            GameObject line = additionalLines.Dequeue();
            line.SetActive(true);
            return line;
        }
        else
            return Instantiate(linePrefab, linesParent);
    }

    private void PlaceCells()
    {
        foreach (GameObject[] cellColumn in cells)
        {
            foreach (GameObject cell in cellColumn)
            {
                cell.SetActive(false);
                additionalCells.Enqueue(cell);
            }
        }
        cells.Clear();
        if (cellsBool.Count > width)
        {
            // Adds wipe extra on resize feature
            //cellsBool.RemoveRange(width, cellsBool.Count - width);
        }
        else
        {
            while (cellsBool.Count < width)
            {
                cellsBool.Add(new bool[8]);
            }
        }

        for (int x = 0; x < width; x++)
        {
            GameObject[] cellColumn = new GameObject[8];
            for (int y = 0; y < 8; y++)
            {
                Vector3 cellPos = new Vector3((x - CenterOffset) + 0.5f, y - 3.5f);
                GameObject cell = GetCellGO();
                cell.transform.position = cellPos;
                SetCell(cell, GetCellPos(new Vector3Int(x, y, 0)));
                cellColumn[y] = cell;
            }
            cells.Add(cellColumn);
        }
        UpdateCells();
    }
    private GameObject GetCellGO()
    {
        if (additionalCells.Count > 0)
        {
            GameObject cell = additionalCells.Dequeue();
            cell.SetActive(true);
            return cell;
        }
        else
            return Instantiate(cellPrefab, cellsParent);
    }
    private void UpdateCells()
    {
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                SetCell(cells[x][y], GetCellPos(new Vector3Int(x, y, 0)));
            }
        }
    }

    private void SetCell(GameObject cell, bool state)
    {
        Material cellMat = cell.GetComponent<SpriteRenderer>().material;
        if (state)
            cellMat.color = new Color(1, 1, 1);
        else
            cellMat.color = new Color(0, 0, 0);
    }
    private void SetCellPos(Vector3Int pos, bool state)
    {
        SetCell(cells[pos.x][pos.y], state);
    }
    private void ToggleCellPos(Vector3Int pos)
    {
        bool newState = !cellsBool[pos.x][pos.y];
        cellsBool[pos.x][pos.y] = newState;
        SetCellPos(pos, newState);
    }
    private bool GetCellPos(Vector3Int pos)
    {
        return cellsBool[pos.x][pos.y];
    }

    private bool CellPosInRange(Vector3Int cellPos)
    {
        return (cellPos.x >= 0 && cellPos.x <= width - 1) && (cellPos.y >= 0 && cellPos.y <= 7);
    }

    private float GetBarrierScale()
    {
        switch (width)
        {
            case 1:
                return 5.125f;
            case 2:
                return 8.25f;
            case 3:
                return 11.37499f;
            case 4:
                return 14.5f;
            case 5:
                return 17.62499f;
            case 6:
                return 20.75f;
            case 7:
                return 23.9f;
            case 8:
                return 27f;
            case 9:
                return 30.125f;
            case 10:
                return 33.25f;
            case 11:
                return 36.375f;
            case 12:
                return 39.5f;
            case 13:
                return 42.62499f;
            case 14:
                return 45.75f;
            case 15:
                return 48.874999f;
            case 16:
                return 52f;
            default:
                return 0;
        }
    }

    private string GetMatrixData()
    {
        string[] lines = new string[8];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < 8; y++)
            {
                if (cellsBool[x][y])
                    lines[y] += "1";
                else
                    lines[y] += "0";
            }
        }

        string data = "";
        foreach (string line in lines)
        {
            data += line + "\n";
        }

        return data;
    }

    private void DisableButton(float disableTime, Button button)
    {
        float finishTime = time + disableTime;
        button.interactable = false;
        disabledButtons.Add((finishTime, button));
    }
    private void CheckDisabledButtons()
    {
        List<(float, Button)> tuplesToRemove = new List<(float, Button)>();
        foreach ((float, Button) finishTimeButtonTuple in disabledButtons)
        {
            if (time > finishTimeButtonTuple.Item1)
            {
                finishTimeButtonTuple.Item2.interactable = true;
                tuplesToRemove.Add(finishTimeButtonTuple);
            }
        }
        foreach ((float, Button) tupleToRemove in tuplesToRemove)
        {
            disabledButtons.Remove(tupleToRemove);
        }
    }
}
