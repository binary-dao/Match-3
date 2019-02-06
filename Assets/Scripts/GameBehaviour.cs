﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameBehaviour : MonoBehaviour {
    internal const int SCORES_FOR_CHIP = 100;
    const int BASE_CHIP_TYPES = 6;

    //min 3, max 10
    const int MAX_ROWS = 5;

    //min 3, max 20
    const int MAX_COLS = 5;

    GameObject field;
    private ChipBehaviour[,] chipArray = new ChipBehaviour[MAX_ROWS, MAX_COLS];

    private System.Random random;

    internal ChipBehaviour selectedChip;
    private ChipBehaviour secondChip;

    internal static GameBehaviour instance;

    //we should get both chips moving end before processing next
    private int movingCounter;

    public int scorePoints = 0;

    // Use this for initialization
    void Start () {
        Debug.Log("It's Adventure Time!");

        instance = this;

        random = new System.Random();
        GenerateField();
        CenterField();
    }

    // Update is called once per frame
    void Update () {
		
	}

    private void GenerateField()
    {
        if(MAX_ROWS < 3 || MAX_COLS < 3)
        {
            throw new Exception("Too small field for this game. Generate at least 3*3 field using MAX_ROWS and MAX_COLS constants.");
        }
        else if (MAX_ROWS > 10 || MAX_COLS > 20)
        {
            throw new Exception("Too big field for this game. Generate no more than 20*10 field using MAX_ROWS and MAX_COLS constants.");
        }

        field = GameObject.Find("Game Field");
        for (int i = 0; i< MAX_ROWS; i++)
        {
            bool useGravity = (i != 0);
            for (int j=0; j< MAX_COLS; j++)
            {
                GameObject chip = (GameObject)Instantiate(Resources.Load("ChipPrefab"));
                chip.transform.parent = field.transform;
                ChipBehaviour chipBehaviour = chip.GetComponent<ChipBehaviour>();
                chipArray[i, j] = chipBehaviour;
                int type = GetRandomTypeForChip(i, j);
                chipBehaviour.Create(type, i, j, useGravity);
            }
        }
        GeneratePatchForField();
        /*if (IsAnyPossibleMoves())
        {
            Debug.Log("There ARE possible moves.");
        }
        else
        {
            //ok, there is no turns at start, so we generate a one turn as an exception
            Debug.Log("There are NO possible moves. Generating patch.");
            GeneratePatchForField();
        }*/
    }

    //set another type for two adjacent chips to generate one move in case of no moves
    private void GeneratePatchForField()
    {
        chipArray[0, 2].Type = chipArray[0, 0].Type;
        chipArray[1, 1].Type = chipArray[0, 0].Type;
    }

    private void CenterField()
    {
        float halfWidth = (MAX_COLS - 1) * ChipBehaviour.ICON_WIDTH / 2.0f ;
        float halfHeight = (MAX_ROWS - 1) * ChipBehaviour.ICON_HEIGHT / 2.0f;
        field.transform.position = new Vector2(-halfWidth, -halfHeight);
    }

    internal void TrySwipeWith(ChipBehaviour secondChip)
    {
        Debug.Log("TrySwipe");
        DisablePhysics();
        this.secondChip = secondChip;
        movingCounter = 0;
        selectedChip.MoveTo(secondChip.gameObject.transform.position);
        secondChip.MoveTo(selectedChip.gameObject.transform.position);
    //    selectedChip = null;
    }

    internal void OnMovingEnd()
    {
        movingCounter++;
        if (movingCounter < 2)
        {
            //waiting for the second chip
            return;
        }

        Debug.Log("OnMovingEnd");
        Debug.Log("selecteChip.row: " + selectedChip.row);
        Debug.Log("selecteChip.col: " + selectedChip.col);
        Debug.Log("secondChip.row: " + secondChip.row);
        Debug.Log("secondChip.col: " + secondChip.col);

        int transitRow = selectedChip.row;
        int transitCol = selectedChip.col;
        selectedChip.row = secondChip.row;
        selectedChip.col = secondChip.col;
        secondChip.row = transitRow;
        secondChip.col = transitCol;

        Debug.Log("After exchange");
        Debug.Log("selectedChip.row: " + selectedChip.row);
        Debug.Log("selectedChip.col: " + selectedChip.col);
        Debug.Log("secondChip.row: " + secondChip.row);
        Debug.Log("secondChip.col: " + secondChip.col);

        chipArray[selectedChip.row, selectedChip.col] = selectedChip;
        chipArray[secondChip.row, secondChip.col] = secondChip;

        СheckAndDestroyForChip(selectedChip);
        СheckAndDestroyForChip(secondChip);
    }

    private bool СheckAndDestroyForChip(ChipBehaviour chip)
    {
        
        //int horizontalLine = 1;
        //int verticalLine = 1;
        List<ChipBehaviour> horizontalLine = new List<ChipBehaviour>();
        List<ChipBehaviour> verticalLine = new List<ChipBehaviour>();

        //first chip is it itself
        horizontalLine.Add(chip);
        verticalLine.Add(chip);

        int row = chip.row;
        int col = chip.col;

        Debug.Log("row:" + row + "; col:" + col);

        /*int current = SafeGetType(row, col);
        int up = SafeGetType(row + 1, col);
        int down = SafeGetType(row - 1, col);
        int left = SafeGetType(row, col - 1);
        int right = SafeGetType(row, col + 1);*/

        //Debug.Log("row:"+ row + " col:" + col + " current:" + current + " up:" + up + " down:" + down + " left:" + left + " right:" + right);

        int currentType = SafeGetType(chip.row, chip.col);

        //UP
        if (currentType == SafeGetType(row + 1, col))
        {
            verticalLine.Add(chipArray[row + 1, col]);
            if (currentType == SafeGetType(row + 2, col))
            {
                verticalLine.Add(chipArray[row + 2, col]);
            }
        }
        //DOWN
        if (currentType == SafeGetType(row - 1, col))
        {
            verticalLine.Add(chipArray[row - 1, col]);
            if (currentType == SafeGetType(row - 2, col))
            {
                verticalLine.Add(chipArray[row - 2, col]);
            }
        }

        //LEFT
        if (currentType == SafeGetType(row, col - 1))
        {
            horizontalLine.Add(chipArray[row, col - 1]);
            if (currentType == SafeGetType(row, col - 2))
            {
                horizontalLine.Add(chipArray[row, col - 2]);
            }
        }
        //RIGHT
        if (currentType == SafeGetType(row, col + 1))
        {
            horizontalLine.Add(chipArray[row, col + 1]);
            if (currentType == SafeGetType(row, col + 2))
            {
                horizontalLine.Add(chipArray[row, col + 2]);
            }
        }

        if(horizontalLine.Count >= 3)
        {
            Debug.Log("It's horizontalLine line in (" + row + ";" + col + ")");
            foreach (ChipBehaviour iterChip in horizontalLine)
            {
                Debug.Log("(" + iterChip.row + ";" + iterChip.col + ")");
                iterChip.StartDestroy();
            }
        }

        if (verticalLine.Count >= 3)
        {
            Debug.Log("It's vertical line in (" + row + ";" + col + ")");
            foreach (ChipBehaviour iterChip in verticalLine)
            {
                Debug.Log("(" + iterChip.row + ";" + iterChip.col + ")");
                iterChip.StartDestroy();
            }
        }

        return (verticalLine.Count >= 3 || horizontalLine.Count >= 3);
    }

    private void DisablePhysics()
    {
        for (int i = 0; i < MAX_ROWS; i++)
        {
            for (int j = 0; j < MAX_COLS; j++)
            {
                chipArray[i, j].GetComponent<Rigidbody>().isKinematic = false;
                chipArray[i, j].GetComponent<Rigidbody>().detectCollisions = false;
                chipArray[i, j].GetComponent<Rigidbody>().useGravity = false;
            }
        }
    }

    //adjacent chips should be not the same at first generation
    private int GetRandomTypeForChip(int row, int col)
    {
        List<int> excludes = new List<int>();
        if(row > 0)
        {
            if (chipArray[row-1, col])
            {
                excludes.Add(chipArray[row - 1, col].Type);
            }
        }
        if (col > 0)
        {
            if (chipArray[row, col - 1])
            {
                excludes.Add(chipArray[row, col - 1].Type);
            }
        }
        return GetRandomWithExcludes(BASE_CHIP_TYPES, excludes);
    }

    //generate one of the random number [0, maxNumber) but except excludes, DON'T try to use it if excludes more than maxNumber or equal
    private int GetRandomWithExcludes(int maxNumber, List<int> excludes)
    {
        if (excludes.Count >= maxNumber)
        {
            throw new Exception("GetRandomWithExcludes can't be done with so much excludes!");
        }
        
        int answer = random.Next(0, maxNumber);
        if(excludes.Count == 0)
        {
            return answer;
        }
            for(int i=0; i<excludes.Count; i++)
            {
                if(answer == excludes[i])
                {
                    answer++;
                    if (answer>= maxNumber)
                    {
                        answer = 0;
                    }
                }
            }
        return answer;
    }

    private bool IsAnyPossibleMoves()
    {
        bool isPossible = false;
        for (int i = 0; i < MAX_ROWS; i++)
        {
            for (int j = 0; j < MAX_COLS; j++)
            {
                if(IsPossibleMovesForChip(i, j))
                {
                    isPossible = true;
                }
            }
        }
        return isPossible;
    }

    private bool IsPossibleMovesForChip(int row, int col)
    {
        //try move up
        if(row + 1 < MAX_ROWS)
        {
            if (VirtualMoveCheck(row + 1, col, row, col))
                return true;
        }

        //try move right
        if(col + 1 < MAX_COLS)
        {
            if (VirtualMoveCheck(row, col + 1, row, col))
                return true;
        }

        //try move down
        if (row > 0)
        {
            if (VirtualMoveCheck(row - 1, col, row, col))
                return true;
        }

        //try move left
        if (col > 0)
        {
            if (VirtualMoveCheck(row, col - 1, row, col))
                return true;
        }

        return false;
    }    

    //looking for a potential move
    private bool VirtualMoveCheck(int newRow, int newCol, int initRow, int initCol)
    {
        //HORIZONTAL CHECK
        if(newRow != initRow)
        {
            //CENTRAL HORIZONTAL
            if(SafeGetType(initRow, initCol) == SafeGetType(newRow, newCol - 1) && SafeGetType(initRow, initCol) == SafeGetType(newRow, newCol + 1))
            {
                Debug.Log("Move (" + initRow + ";" + initCol + ") to (" + newRow + ";" + newCol + ") for CENTRAL HORIZONTAL move");
                return true;
            }
            //LEFT HORIZONTAL
            if (SafeGetType(initRow, initCol) == SafeGetType(newRow, newCol - 1) && SafeGetType(initRow, initCol) == SafeGetType(newRow, newCol - 2))
            {
                Debug.Log("Move (" + initRow + ";" + initCol + ") to (" + newRow + ";" + newCol + ") for LEFT HORIZONTAL move");
                return true;
            }
            //RIGHT HORIZONTAL
            if (SafeGetType(initRow, initCol) == SafeGetType(newRow, newCol + 1) && SafeGetType(initRow, initCol) == SafeGetType(newRow, newCol + 2))
            {
                Debug.Log("Move (" + initRow + ";" + initCol + ") to (" + newRow + ";" + newCol + ") for RIGHT HORIZONTAL move");
                return true;
            }
        }

        //VERTICAL CHECK
        if (newCol != initCol)
        {
            //CENTRAL VERTICAL
            if (SafeGetType(initRow, initCol) == SafeGetType(newRow - 1, newCol) && SafeGetType(initRow, initCol) == SafeGetType(newRow + 1, newCol))
            {
                Debug.Log("Move (" + initRow + ";" + initCol + ") to (" + newRow + ";" + newCol + ") for CENTRAL VERTICAL move");
                return true;
            }
            //UP VERTICAL
            if (SafeGetType(initRow, initCol) == SafeGetType(newRow + 1, newCol) && SafeGetType(initRow, initCol) == SafeGetType(newRow + 2, newCol))
            {
                Debug.Log("Move (" + initRow + ";" + initCol + ") to (" + newRow + ";" + newCol + ") for UP VERTICAL move");
                return true;
            }
            //DOWN VERTICAL
            if (SafeGetType(initRow, initCol) == SafeGetType(newRow - 1, newCol) && SafeGetType(initRow, initCol) == SafeGetType(newRow - 2, newCol))
            {
                Debug.Log("Move (" + initRow + ";" + initCol + ") to (" + newRow + ";" + newCol + ") for DOWN VERTICAL move");
                return true;
            }
        }

        return false;
    }

    private int SafeGetType(int row, int col)
    {
        if(row < 0 || row >= MAX_ROWS || col < 0 || col >= MAX_COLS)
        {
            return -1;
        }
        if(chipArray[row, col])
        {
            return chipArray[row, col].Type;
        }
        return -1;
    }
}
