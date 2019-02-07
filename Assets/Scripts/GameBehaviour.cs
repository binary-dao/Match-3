using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameBehaviour : MonoBehaviour {
    internal const int SCORES_FOR_CHIP = 100;
    const int BASE_CHIP_TYPES = 6;
    const int TURNS_FOR_GAME = 20;
    const int SCORES_TO_WIN = 10000;

    //min 3, max 9
    const int MAX_ROWS = 9;

    //min 3, max 18
    const int MAX_COLS = 18;

    private GameObject field;
    private GameObject pauseButton;

    private ChipBehaviour[,] chipArray = new ChipBehaviour[MAX_ROWS, MAX_COLS];
    private float[,] previousPositionY = new float[MAX_ROWS, MAX_COLS];

    private System.Random random;

    internal ChipBehaviour selectedChip;
    private ChipBehaviour secondChip;

    internal static GameBehaviour instance;

    //we should get both chips moving end before processing next
    private int movingCounter;

    private int scorePoints = 0;
    private int turnsLeft;

    //how much chips we should wait before reenabling physics?
    internal int destroyWaiting;

    internal float fieldHalfWidth;
    internal float fieldHalfHeight;

    bool isShowRules;
    bool isPaused;
    bool isMuted;
    bool isWaitingChipsFall;
    internal bool isFieldActive;

    bool isMovingBack;

    private AudioClip matchSound;
    private AudioSource audioSource;

    // Use this for initialization
    void Start () {
        Debug.Log("It's Adventure Time!");

        instance = this;

        random = new System.Random();
        GenerateField();
        CenterField();

        audioSource = gameObject.AddComponent<AudioSource>();
        matchSound = (AudioClip)Resources.Load("Sounds/matchSound");

        TurnsLeft = TURNS_FOR_GAME;

        pauseButton = GameObject.Find("Pause Button");
        pauseButton.SetActive(false);
        Pause();

        isShowRules = true;
    }

    

    // Update is called once per frame
    void Update ()
    {
        //some chips are still falling using physics?
        if (isWaitingChipsFall)
        {
            bool isSomethingFalling = false;
            for (int i = 0; i < MAX_ROWS; i++)
            {
                for (int j = 0; j < MAX_COLS; j++)
                {
                    //rigidbody.velocity not works there well
                    if (Math.Abs(chipArray[i, j].rigidbody.transform.position.y - previousPositionY[i, j]) > 0.00001 )
                        isSomethingFalling = true;
                    previousPositionY[i, j] = chipArray[i, j].rigidbody.transform.position.y;
                }
            }
            if(!isSomethingFalling)
            {
                isWaitingChipsFall = false;
                isFieldActive = true;
            }
        }
	}

    internal void Pause()
    {
        isPaused = !isPaused;
        field.SetActive(!isPaused);
    }

    internal void Mute()
    {
        //isMuted = !isMuted;
        AudioSource audioSource = GetComponent<AudioSource>();
        audioSource.mute = !audioSource.mute;
    }

    void OnGUI()
    {
        Texture2D texture = (Texture2D)Resources.Load("windowColor");
        GUI.skin.window.normal.background = texture;
        GUI.skin.window.onFocused.background = texture;
        GUI.skin.window.onHover.background = texture;
        GUI.skin.window.onNormal.background = texture;
        if (isShowRules)
        {
            GUI.ModalWindow(0, new Rect((Screen.width / 2) - 150, (Screen.height / 2) - 75, 300, 100), showMission, "Mission");
        }
    }

    void showMission(int windowID)
    {
        GUI.Label(new Rect(65, 20, 200, 250), "Get " + SCORES_TO_WIN + " scores within " + TURNS_FOR_GAME + " turns. Good luck!");

        if (GUI.Button(new Rect(110, 60, 80, 30), "OK"))
        {
            isShowRules = false;
            Pause();
            pauseButton.SetActive(true);
            isFieldActive = true;
            /*SceneManager.UnloadSceneAsync("EmptyScene");
            SceneManager.LoadSceneAsync("CheckerScene");*/
        }

    }

    private void GenerateField()
    {
        if(MAX_ROWS < 3 || MAX_COLS < 3)
        {
            throw new Exception("Too small field for this game. Generate at least 3*3 field using MAX_ROWS and MAX_COLS constants.");
        }
        else if (MAX_ROWS > 9 || MAX_COLS > 18)
        {
            throw new Exception("Too big field for this game. Generate no more than 9*18 field using MAX_ROWS and MAX_COLS constants.");
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
        if (IsAnyPossibleMoves())
        {
            Debug.Log("There ARE possible moves.");
        }
        else
        {
            //ok, there is no turns at start, so we generate a one turn as an exception
            Debug.Log("There are NO possible moves. Generating patch.");
            GeneratePatchForField();
        }
    }

    internal void destroyChip(ChipBehaviour chip)
    {
        destroyWaiting--;
        
        for (int i = chip.row; i<MAX_ROWS-1; i++)
        {
            chipArray[i, chip.col] = chipArray[i + 1, chip.col];
            //can be null for deleted chips
            if(chipArray[i, chip.col])
                chipArray[i, chip.col].row = i;
        }
        chipArray[MAX_ROWS-1, chip.col] = null;

        Debug.Log("destroyChip, destroyWaiting: " + destroyWaiting);
        if(destroyWaiting <=0 )
        {
            Debug.Log("Turn physics on");
            FillNewChips();
            SetPhysics(true);
            for(int i = 0; i < MAX_ROWS; i++)
                for (int j = 0; j < MAX_COLS; j++)
                    previousPositionY[i, j] = chipArray[i, j].rigidbody.transform.position.y;
            isWaitingChipsFall = true;
        }
    }

    private void FillNewChips()
    {
        for(int col = 0; col<MAX_COLS; col++)
        {
            int fillerForCol = 0;
            for (int row = 0 ; row < MAX_ROWS; row++)
            {
                if(!chipArray[row,col])
                {
                    fillerForCol++;
                }
            }
            if(fillerForCol>0)
            {
                FillCol(col, fillerForCol);
                Debug.Log("col:" + col + "; fillerForCol:"+fillerForCol);
            }
        }
    }

    private void FillCol(int col, int count)
    {
        for (int i = 0; i < count; i++)
        {
            GameObject chip = (GameObject)Instantiate(Resources.Load("ChipPrefab"));
            chip.transform.parent = field.transform;
            ChipBehaviour chipBehaviour = chip.GetComponent<ChipBehaviour>();
            chipArray[MAX_ROWS - count + i, col] = chipBehaviour;
            int type = random.Next(0, BASE_CHIP_TYPES);
            chipBehaviour.Create(type, MAX_ROWS + i, col, true);
            //задать истинные координаты
            chipBehaviour.row = MAX_COLS - count + i;
            chip.transform.position = new Vector2(chip.transform.position.x - fieldHalfWidth, chip.transform.position.y - fieldHalfHeight);
        }
    }

    //set another type for two adjacent chips to generate one move in case of no moves
    private void GeneratePatchForField()
    {
        chipArray[0, 2].Type = chipArray[0, 0].Type;
        chipArray[1, 1].Type = chipArray[0, 0].Type;
    }

    private void CenterField()
    {
        fieldHalfWidth = (MAX_COLS - 1) * ChipBehaviour.ICON_WIDTH / 2.0f;
        fieldHalfHeight = (MAX_ROWS - 1) * ChipBehaviour.ICON_HEIGHT / 2.0f;
        field.transform.position = new Vector2(-fieldHalfWidth, -fieldHalfHeight);
    }

    internal void TrySwipeWith(ChipBehaviour secondChip)
    {
        Debug.Log("TrySwipe");
        isFieldActive = false;
        SetPhysics(false);
        this.secondChip = secondChip;
        movingCounter = 0;
        selectedChip.MoveTo(secondChip.gameObject.transform.position);
        secondChip.MoveTo(selectedChip.gameObject.transform.position);
    }

    private void MoveChipsBack()
    {
        movingCounter = 0;
        isMovingBack = true;
        selectedChip.MoveTo(secondChip.gameObject.transform.position);
        secondChip.MoveTo(selectedChip.gameObject.transform.position);
    }

    internal void OnMovingEnd()
    {
        movingCounter++;
        if (movingCounter < 2)
        {
            //waiting for the second chip
            return;
        }
        
        int transitRow = selectedChip.row;
        int transitCol = selectedChip.col;
        selectedChip.row = secondChip.row;
        selectedChip.col = secondChip.col;
        secondChip.row = transitRow;
        secondChip.col = transitCol;

        chipArray[selectedChip.row, selectedChip.col] = selectedChip;
        chipArray[secondChip.row, secondChip.col] = secondChip;

        if(isMovingBack)
        {
            isMovingBack = false;
            selectedChip = null;
            secondChip = null;
            SetPhysics(true);
            isFieldActive = true;
            return;
        }

        //evade lazy boolean evaluation
        bool firstSuccessfull = СheckAndDestroyForChip(selectedChip);
        bool secondSuccessfull = СheckAndDestroyForChip(secondChip);
        if(firstSuccessfull||secondSuccessfull)
        {
            audioSource.PlayOneShot(matchSound);
            TurnsLeft--;
        }
        else
        {
            MoveChipsBack();
        }
    }

    private bool СheckAndDestroyForChip(ChipBehaviour chip)
    {
        List<ChipBehaviour> horizontalLine = new List<ChipBehaviour>();
        List<ChipBehaviour> verticalLine = new List<ChipBehaviour>();

        //first chip is it itself
        horizontalLine.Add(chip);
        verticalLine.Add(chip);

        int row = chip.row;
        int col = chip.col;

        Debug.Log("row:" + row + "; col:" + col);


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

    private void SetPhysics(bool enabled)
    {
        for (int i = 0; i < MAX_ROWS; i++)
        {
            for (int j = 0; j < MAX_COLS; j++)
            {
                if(chipArray[i,j])
                {
                    Rigidbody rigidbody = chipArray[i, j].GetComponent<Rigidbody>();
                    rigidbody.detectCollisions = enabled;
                    rigidbody.useGravity = enabled;
                    if(enabled)
                    {
                        rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
                    }
                }
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

    internal int ScorePoints
    {
        get
        {
            return scorePoints;
        }

        set
        {
            scorePoints = value;
            UpdateScore();
        }
    }

    private int TurnsLeft
    {
        get
        {
            return turnsLeft;
        }

        set
        {
            turnsLeft = value;
            UpdateScore();
        }
    }

    private void UpdateScore()
    {
        GameObject textFieldObject = GameObject.Find("Score Text");
        Text textField = textFieldObject.GetComponent<Text>();
        textField.text = "Score: " + ScorePoints + "     Turns left: " + TurnsLeft;
    }
}
