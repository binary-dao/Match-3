using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameBehaviour : MonoBehaviour {
    const int MAX_ROWS = 6;
    const int MAX_COLS = 6;

    GameObject field;
    private ChipBehaviour[,] chipArray = new ChipBehaviour[MAX_ROWS, MAX_COLS];

    public static ChipBehaviour selectedChip;

	// Use this for initialization
	void Start () {
        Debug.Log("It's Adventure Time!");
        

        GenerateField();
        CenterField();
    }

    // Update is called once per frame
    void Update () {
		
	}

    private void GenerateField()
    {
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
                chipBehaviour.Create(field, i, i, j, useGravity);
            }
        }
    }

    private void CenterField()
    {
        float halfWidth = MAX_COLS * ChipBehaviour.ICON_WIDTH / 2.0f;
        float halfHeight = MAX_ROWS * ChipBehaviour.ICON_HEIGHT / 2.0f;
        field.transform.position = new Vector2(-halfWidth, -halfHeight);
        
    }

    internal static void TrySwipe(ChipBehaviour firstChip, ChipBehaviour secondChip)
    {
        selectedChip = null;
        DisablePhysics();
    }

    private static void DisablePhysics()
    {
        for (int i = 0; i < MAX_ROWS; i++)
        {
            bool useGravity = (i != 0);
            for (int j = 0; j < MAX_COLS; j++)
            {
                //chipArray[i, j].GetComponent<RigidBody>().enable = false
            }
        }
    }
}
