using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Random = System.Random;

public class GameBehaviour : MonoBehaviour
{
    internal const int SCORES_FOR_CHIP = 100;
    private const int BASE_CHIP_TYPES = 6;
    private const int TURNS_FOR_GAME = 20;
    private const int SCORES_TO_WIN = 4000;
    private const int TIME_BEFORE_HINT = 10;

    //min 4, max 9
    private const int MAX_ROWS = 8;

    //min 4, max 18
    private const int MAX_COLS = 8;

    internal static GameBehaviour instance;

    private readonly ChipBehaviour[,] chipArray = new ChipBehaviour[MAX_ROWS, MAX_COLS];

    private readonly Vector4 noMatchVector4 = new Vector4(-1, -1, -1, -1);
    private readonly float[,] previousPositionY = new float[MAX_ROWS, MAX_COLS];
    private AudioSource audioSource;

    private int[] bonusRow = new int[MAX_COLS];

    //how much chips we should wait before reenabling physics?
    internal int destroyWaiting;

    private GameObject field;
    internal float fieldHalfHeight;

    internal float fieldHalfWidth;
    internal bool isFieldActive;
    private bool isHintShowed;
    private bool isLose;

    private bool isMovingBack;
    private bool isMuted;
    private bool isPaused;

    private bool isShowRules;
    private bool isWaitingChipsFall;
    private bool isWin;

    private AudioClip matchSound;

    //we should get both chips moving end before processing next
    private int movingCounter;
    private GameObject pauseButton;

    private Random random;

    private int scorePoints;
    private ChipBehaviour secondChip;

    internal ChipBehaviour selectedChip;

    private float startTurnTime;
    private int turnsLeft;

    internal int ScorePoints
    {
        get { return scorePoints; }

        set
        {
            scorePoints = value;
            UpdateScore();
        }
    }

    private int TurnsLeft
    {
        get { return turnsLeft; }

        set
        {
            turnsLeft = value;
            UpdateScore();
        }
    }

    // Use this for initialization
    private void Start()
    {
        Debug.Log("It's Adventure Time!");

        instance = this;

        random = new Random();
        GenerateField();
        CenterField();

        audioSource = gameObject.AddComponent<AudioSource>();
        matchSound = (AudioClip) Resources.Load("Sounds/matchSound");

        TurnsLeft = TURNS_FOR_GAME;

        pauseButton = GameObject.Find("Pause Button");
        pauseButton.SetActive(false);
        Pause();

        isShowRules = true;
        isHintShowed = true;
    }

    //make/unmake field active
    internal void Pause()
    {
        isPaused = !isPaused;
        field.SetActive(!isPaused);
    }

    private void Update()
    {
        //some chips are still falling using physics?
        if (isWaitingChipsFall)
        {
            bool isSomethingFalling = false;
            for (int i = 0; i < MAX_ROWS; i++)
            for (int j = 0; j < MAX_COLS; j++)
            {
                //rigidbody.velocity not works there well; WARNING: can be destroyed between updates
                if (chipArray[i, j] && chipArray[i, j].rigidbody &&
                    Math.Abs(chipArray[i, j].rigidbody.transform.position.y - previousPositionY[i, j]) > 0.00001)
                    isSomethingFalling = true;
                if (chipArray[i, j] && chipArray[i, j].rigidbody)
                    previousPositionY[i, j] = chipArray[i, j].rigidbody.transform.position.y;
            }

            if (!isSomethingFalling)
            {
                isWaitingChipsFall = false;
                if (!FullMatchCheck())
                    CheckWinLose();
            }
        }
        else
        {
            if (!isHintShowed && Time.time - startTurnTime > TIME_BEFORE_HINT) ShowHint();
        }
    }

    //uses same halo as in check
    private void ShowHint()
    {
        isHintShowed = true;
        Debug.Log("Show hint");
        var hintCoords = GetAnyPossibleMove();
        chipArray[(int)hintCoords.x, (int)hintCoords.y].halo.enabled = true;
        chipArray[(int)hintCoords.z, (int)hintCoords.w].halo.enabled = true;
    }

    //three predefined points for chips, other place at random
    private void Shuffle()
    {
        selectedChip = null;
        SetPhysics(false);

        Debug.Log("Shuffle");
        var sameChips = GetAnySameChips(0);
        var excludes = new List<int>();

        //linear array is much better to randomize
        var linear = new ChipBehaviour[MAX_ROWS * MAX_COLS];

        //move 3 found close to each other to make sure of one turn
        linear[GetLinearIndex(1, 0)] = sameChips[0];
        excludes.Add(GetLinearIndex(1, 0));

        linear[GetLinearIndex(1, 1)] = sameChips[1];
        excludes.Add(GetLinearIndex(1, 1));

        linear[GetLinearIndex(2, 2)] = sameChips[2];
        excludes.Add(GetLinearIndex(2, 2));

        for (int i = 0; i < MAX_ROWS; i++)
        for (int j = 0; j < MAX_COLS; j++)
        {
            if (sameChips.Contains(chipArray[i, j])) continue;
            int randomPlace = GetRandomWithExcludes(MAX_ROWS * MAX_COLS, excludes);
            linear[randomPlace] = chipArray[i, j];
            excludes.Add(randomPlace);
        }

        //move
        for (int i = 0; i < linear.Length; i++)
        {
            linear[i].row = GetRowOfLinear(i);
            linear[i].col = GetColOfLinear(i);
            linear[i].MoveTo(chipArray[GetRowOfLinear(i), GetColOfLinear(i)].gameObject.transform.position);
        }

        //move linear array back to 2D
        for (int i = 0; i < linear.Length; i++) chipArray[GetRowOfLinear(i), GetColOfLinear(i)] = linear[i];
    }

    //2D to linear: row*MAX_ROWS+col
    private int GetLinearIndex(int row, int col)
    {
        return row * MAX_ROWS + col;
    }

    //linear to 2D: row = index / MAX_ROWS; col = index % MAX_ROWS
    private int GetRowOfLinear(int index)
    {
        return index / MAX_ROWS;
    }

    private int GetColOfLinear(int index)
    {
        return index % MAX_ROWS;
    }

    //looking for 3 same chips recursively
    private List<ChipBehaviour> GetAnySameChips(int currentType)
    {
        var sameChips = new List<ChipBehaviour>();
        for (int i = 0; i < MAX_ROWS; i++)
        for (int j = 0; j < MAX_COLS; j++)
            if (SafeGetType(i, j) == currentType)
            {
                sameChips.Add(chipArray[i, j]);
                if (sameChips.Count >= 3) return sameChips;
            }

        //did not find enough chips of this type, search next recursively
        return GetAnySameChips(currentType + 1);
    }

    //are we done yet?
    private void CheckWinLose()
    {
        if (scorePoints >= SCORES_TO_WIN)
            isWin = true;
        else if (turnsLeft <= 0)
            isLose = true;
        else
            EnablePlayerControl(false);
    }

    //check and collect chips
    private bool FullMatchCheck()
    {
        bool isNewCombinations = false;
        var line = new List<ChipBehaviour>();

        //uses to generate bonus for both horizontal and vertical line at once
        var horizontalLine = new List<ChipBehaviour>();

        int currentType = -2;

        bonusRow = new int[MAX_COLS];

        //horizontal check
        for (int i = 0; i < MAX_ROWS; i++)
        for (int j = 0; j < MAX_COLS; j++)
            //new line
            if (j == 0 || currentType < 0 || currentType != chipArray[i, j].Type)
            {
                if (line.Count >= 3)
                {
                    foreach (var iterChip in line) horizontalLine.Add(iterChip);
                    if (line.Count >= 4) bonusRow[j] = (int)ChipBehaviour.chipTypes.bomb;
                    isNewCombinations = true;
                    CollectLine(line);
                }

                line.Clear();
                currentType = chipArray[i, j].Type;
                line.Add(chipArray[i, j]);
            }
            else
            {
                line.Add(chipArray[i, j]);
            }

        //vertical check
        for (int j = 0; j < MAX_COLS; j++)
        for (int i = 0; i < MAX_ROWS; i++)
            //new line
            if (i == 0 || currentType < 0 || currentType != chipArray[i, j].Type)
            {
                if (line.Count >= 3)
                {
                    if (line.Count >= 4) bonusRow[j] = (int)ChipBehaviour.chipTypes.rocket;
                    foreach (var iterChip in horizontalLine)
                        if (line.Contains(iterChip))
                            bonusRow[j] = (int)ChipBehaviour.chipTypes.rainbow;
                    isNewCombinations = true;
                    CollectLine(line);
                }

                line.Clear();
                currentType = chipArray[i, j].Type;
                line.Add(chipArray[i, j]);
            }
            else
            {
                line.Add(chipArray[i, j]);
            }

        if (isNewCombinations) audioSource.PlayOneShot(matchSound);

        return isNewCombinations;
    }
    private void CollectLine(List<ChipBehaviour> line)
    {
        foreach (var iterChip in line) iterChip.StartDestroy();
    }

    internal void Mute()
    {
        var audioSource = GetComponent<AudioSource>();
        audioSource.mute = !audioSource.mute;
    }

    private void OnGUI()
    {
        //using non-transparent back for GUI
        var texture = (Texture2D) Resources.Load("windowColor");
        GUI.skin.window.normal.background = texture;
        GUI.skin.window.onFocused.background = texture;
        GUI.skin.window.onHover.background = texture;
        GUI.skin.window.onNormal.background = texture;
        if (isShowRules)
            GUI.ModalWindow(0, new Rect(Screen.width / 2 - 150, Screen.height / 2 - 75, 300, 100), showMission,
                "Mission");
        else if (isWin)
            GUI.ModalWindow(1, new Rect(Screen.width / 2 - 150, Screen.height / 2 - 75, 300, 100), showWin, "YOU WIN!");
        else if (isLose)
            GUI.ModalWindow(2, new Rect(Screen.width / 2 - 150, Screen.height / 2 - 75, 300, 100), showLose,
                "You lose");
    }

    private void showMission(int windowID)
    {
        GUI.Label(new Rect(65, 20, 200, 250),
            "Get " + SCORES_TO_WIN + " scores within " + TURNS_FOR_GAME + " turns. Good luck!");

        if (GUI.Button(new Rect(110, 60, 80, 30), "OK"))
        {
            isShowRules = false;
            Pause();
            EnablePlayerControl(true);
            pauseButton.SetActive(true);
        }
    }

    private void showWin(int windowID)
    {
        GUI.Label(new Rect(65, 20, 200, 250), "You beat this game, congratulation! Wanna try again?");
        if (GUI.Button(new Rect(110, 60, 80, 30), "Restart")) SceneManager.LoadScene("BaseScene");
    }

    private void showLose(int windowID)
    {
        GUI.Label(new Rect(65, 20, 200, 250), "You lose. Do you want another try?");
        if (GUI.Button(new Rect(110, 60, 80, 30), "Restart")) SceneManager.LoadScene("BaseScene");
    }

    //initial field generation. No autocollect at start but at least one possible move
    private void GenerateField()
    {
        if (MAX_ROWS < 4 || MAX_COLS < 4)
            throw new Exception("Too small field for this game. Generate at least 4*4 field using MAX_ROWS and MAX_COLS constants.");
        if (MAX_ROWS > 9 || MAX_COLS > 18)
            throw new Exception("Too big field for this game. Generate no more than 9*18 field using MAX_ROWS and MAX_COLS constants.");

        field = GameObject.Find("Game Field");
        for (int i = 0; i < MAX_ROWS; i++)
        {
            bool useGravity = i != 0;
            for (int j = 0; j < MAX_COLS; j++)
            {
                var chip = (GameObject) Instantiate(Resources.Load("ChipPrefab"));
                chip.transform.parent = field.transform;
                var chipBehaviour = chip.GetComponent<ChipBehaviour>();
                chipArray[i, j] = chipBehaviour;
                int type = GetRandomTypeForChip(i, j);
                chipBehaviour.Create(type, i, j, useGravity);
            }
        }

        if (GetAnyPossibleMove() != noMatchVector4)
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

    //immediate chip destroying, after animation
    internal void destroyChip(ChipBehaviour chip)
    {
        destroyWaiting--;

        for (int i = chip.row; i < MAX_ROWS - 1; i++)
        {
            chipArray[i, chip.col] = chipArray[i + 1, chip.col];
            //can be null for deleted chips
            if (chipArray[i, chip.col])
                chipArray[i, chip.col].row = i;
        }

        chipArray[MAX_ROWS - 1, chip.col] = null;

        Debug.Log("destroyChip, destroyWaiting: " + destroyWaiting);
        if (destroyWaiting <= 0)
        {
            Debug.Log("Turn physics on");
            FillNewChips();
            SetPhysics(true);
            for (int i = 0; i < MAX_ROWS; i++)
            for (int j = 0; j < MAX_COLS; j++)
                if (chipArray[i, j] && chipArray[i, j].rigidbody)
                    previousPositionY[i, j] = chipArray[i, j].rigidbody.transform.position.y;
            isWaitingChipsFall = true;
        }
    }

    private void FillNewChips()
    {
        for (int col = 0; col < MAX_COLS; col++)
        {
            int fillerForCol = 0;
            for (int row = 0; row < MAX_ROWS; row++)
                if (!chipArray[row, col])
                    fillerForCol++;
            if (fillerForCol > 0) FillCol(col, fillerForCol);
        }
    }

    private void FillCol(int col, int count)
    {
        for (int i = 0; i < count; i++)
        {
            var chip = (GameObject) Instantiate(Resources.Load("ChipPrefab"));
            chip.transform.parent = field.transform;
            var chipBehaviour = chip.GetComponent<ChipBehaviour>();
            chipArray[MAX_ROWS - count + i, col] = chipBehaviour;

            int type;
            if (i == 0 && bonusRow[col] > BASE_CHIP_TYPES)
                type = bonusRow[col];
            else
                type = random.Next(0, BASE_CHIP_TYPES);
            chipBehaviour.Create(type, MAX_ROWS + i, col, true);
            //set true coords
            chipBehaviour.row = MAX_ROWS - count + i;
            chip.transform.position = new Vector2(chip.transform.position.x - fieldHalfWidth,
                chip.transform.position.y - fieldHalfHeight);
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
        for (int i = 0; i < MAX_ROWS; i++)
        for (int j = 0; j < MAX_COLS; j++)
            if (chipArray[i, j])
                chipArray[i, j].halo.enabled = false;
        bonusRow = new int[MAX_COLS];
        isFieldActive = false;
        SetPhysics(false);
        this.secondChip = secondChip;
        movingCounter = 0;
        selectedChip.MoveTo(secondChip.gameObject.transform.position);
        secondChip.MoveTo(selectedChip.gameObject.transform.position);
    }

    //bad move, return chips back
    private void MoveChipsBack()
    {
        movingCounter = 0;
        isMovingBack = true;
        selectedChip.MoveTo(secondChip.gameObject.transform.position);
        secondChip.MoveTo(selectedChip.gameObject.transform.position);
    }

    //used both for direct and back moves
    internal void OnMovingEnd()
    {
        movingCounter++;
        if (movingCounter < 2) return;

        //shuffle end
        if (!selectedChip)
        {
            if (!FullMatchCheck()) EnablePlayerControl(true);
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

        if (isMovingBack)
        {
            isMovingBack = false;
            selectedChip = null;
            secondChip = null;
            EnablePlayerControl(true);
            return;
        }

        //evade lazy boolean evaluation
        bool firstSuccessful = СheckAndDestroyForChip(selectedChip, secondChip.Type);
        bool secondSuccessful = СheckAndDestroyForChip(secondChip, selectedChip.Type);
        if (firstSuccessful || secondSuccessful)
        {
            audioSource.PlayOneShot(matchSound);
            TurnsLeft--;
        }
        else
        {
            MoveChipsBack();
        }
    }

    //is there any lines with this chip? If so, collect it
    private bool СheckAndDestroyForChip(ChipBehaviour chip, int pairType)
    {
        if (chip.Type > BASE_CHIP_TYPES)
        {
            ActivateBonus(chip, pairType);
            return true;
        }

        var horizontalLine = new List<ChipBehaviour>();
        var verticalLine = new List<ChipBehaviour>();

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
            if (currentType == SafeGetType(row + 2, col)) verticalLine.Add(chipArray[row + 2, col]);
        }

        //DOWN
        if (currentType == SafeGetType(row - 1, col))
        {
            verticalLine.Add(chipArray[row - 1, col]);
            if (currentType == SafeGetType(row - 2, col)) verticalLine.Add(chipArray[row - 2, col]);
        }

        //LEFT
        if (currentType == SafeGetType(row, col - 1))
        {
            horizontalLine.Add(chipArray[row, col - 1]);
            if (currentType == SafeGetType(row, col - 2)) horizontalLine.Add(chipArray[row, col - 2]);
        }

        //RIGHT
        if (currentType == SafeGetType(row, col + 1))
        {
            horizontalLine.Add(chipArray[row, col + 1]);
            if (currentType == SafeGetType(row, col + 2)) horizontalLine.Add(chipArray[row, col + 2]);
        }

        if (horizontalLine.Count >= 3)
        {
            Debug.Log("It's horizontalLine line in (" + row + ";" + col + ")");
            if (horizontalLine.Count >= 4) bonusRow[col] = (int)ChipBehaviour.chipTypes.bomb;
            CollectLine(horizontalLine);
        }

        if (verticalLine.Count >= 3)
        {
            Debug.Log("It's vertical line in (" + row + ";" + col + ")");
            if (verticalLine.Count >= 4) bonusRow[col] = (int)ChipBehaviour.chipTypes.rocket;
            CollectLine(verticalLine);
        }

        if (verticalLine.Count >= 3 && horizontalLine.Count >= 3) bonusRow[col] = (int)ChipBehaviour.chipTypes.rainbow;

        return verticalLine.Count >= 3 || horizontalLine.Count >= 3;
    }

    //let 'em BOOM
    private void ActivateBonus(ChipBehaviour chip, int pairType)
    {
        //prevent duplicate activations
        if (chip.isDestroying) return;
        chip.StartDestroy();
        if (chip.Type == (int)ChipBehaviour.chipTypes.bomb)
            for (int i = -1; i < 2; i++)
            for (int j = -1; j < 2; j++)
                if (chip.row + i >= 0 && chip.col + j >= 0 && chip.row + i < MAX_ROWS && chip.col + j < MAX_COLS &&
                    chipArray[chip.row + i, chip.col + j])
                {
                    if (chipArray[chip.row + i, chip.col + j].Type > BASE_CHIP_TYPES)
                        ActivateBonus(chipArray[chip.row + i, chip.col + j], pairType);
                    chipArray[chip.row + i, chip.col + j].StartDestroy();
                }

        if (chip.Type == (int)ChipBehaviour.chipTypes.rainbow)
        {
            for (int i = 0; i < MAX_ROWS; i++)
            for (int j = 0; j < MAX_COLS; j++)
                if (chipArray[i, j] && chipArray[i, j].Type == pairType)
                    chipArray[i, j].StartDestroy();
            chip.StartDestroy();
        }

        if (chip.Type == (int)ChipBehaviour.chipTypes.rocket)
            for (int i = chip.row; i >= 0; i--)
            {
                chipArray[i, chip.col].StartDestroy();
                if (chipArray[i, chip.col].Type > BASE_CHIP_TYPES)
                    ActivateBonus(chipArray[i, chip.col], pairType);
                chipArray[i, chip.col].StartDestroy();
            }
    }

    //we don't need Unity physics in shuffle, chips exchange etc...
    private void SetPhysics(bool enabled)
    {
        for (int i = 0; i < MAX_ROWS; i++)
        for (int j = 0; j < MAX_COLS; j++)
            if (chipArray[i, j])
            {
                var rigidbody = chipArray[i, j].GetComponent<Rigidbody>();
                rigidbody.detectCollisions = enabled;
                rigidbody.useGravity = enabled;
                if (enabled) rigidbody.constraints = RigidbodyConstraints.FreezeRotation;
            }
    }

    //adjacent chips should be not the same at first generation
    private int GetRandomTypeForChip(int row, int col)
    {
        var excludes = new List<int>();
        if (row > 0)
            if (chipArray[row - 1, col])
                excludes.Add(chipArray[row - 1, col].Type);
        if (col > 0)
            if (chipArray[row, col - 1])
                excludes.Add(chipArray[row, col - 1].Type);
        return GetRandomWithExcludes(BASE_CHIP_TYPES, excludes);
    }

    //generate one of the random number [0, maxNumber) but except excludes, DON'T try to use it if excludes more than maxNumber or equal
    private int GetRandomWithExcludes(int maxNumber, List<int> excludes)
    {
        if (excludes.Count >= maxNumber)
            throw new Exception("GetRandomWithExcludes can't be done with so much excludes!");

        int answer = random.Next(0, maxNumber);
        for (int i = 0; i < excludes.Count; i++)
            if (excludes.Contains(answer))
            {
                answer++;
                if (answer >= maxNumber) answer = 0;
            }
            else
            {
                return answer;
            }

        return answer;
    }

    //first pair - first chip, second pair - second chip
    private Vector4 GetAnyPossibleMove()
    {
        var noMatchVector = new Vector2(-1, -1);
        for (int i = 0; i < MAX_ROWS; i++)
        for (int j = 0; j < MAX_COLS; j++)
        {
            var answerVector = GetPossibleMovesForChip(i, j);
            if (answerVector != noMatchVector) return new Vector4(i, j, answerVector.x, answerVector.y);
        }

        return new Vector4(-1, -1, -1, -1);
    }

    private Vector2 GetPossibleMovesForChip(int row, int col)
    {
        //try move up
        if (row + 1 < MAX_ROWS)
            if (VirtualMoveCheck(row + 1, col, row, col))
                return new Vector2(row + 1, col);

        //try move right
        if (col + 1 < MAX_COLS)
            if (VirtualMoveCheck(row, col + 1, row, col))
                return new Vector2(row, col + 1);

        //try move down
        if (row > 0)
            if (VirtualMoveCheck(row - 1, col, row, col))
                return new Vector2(row - 1, col);

        //try move left
        if (col > 0)
            if (VirtualMoveCheck(row, col - 1, row, col))
                return new Vector2(row, col - 1);

        return new Vector2(-1, -1);
    }

    //looking for a potential move
    private bool VirtualMoveCheck(int newRow, int newCol, int initRow, int initCol)
    {
        //HORIZONTAL CHECK
        if (newRow != initRow)
        {
            //CENTRAL HORIZONTAL
            if (SafeGetType(initRow, initCol) == SafeGetType(newRow, newCol - 1) &&
                SafeGetType(initRow, initCol) == SafeGetType(newRow, newCol + 1))
            {
                Debug.Log("Move (" + initRow + ";" + initCol + ") to (" + newRow + ";" + newCol +
                          ") for CENTRAL HORIZONTAL move");
                return true;
            }

            //LEFT HORIZONTAL
            if (SafeGetType(initRow, initCol) == SafeGetType(newRow, newCol - 1) &&
                SafeGetType(initRow, initCol) == SafeGetType(newRow, newCol - 2))
            {
                Debug.Log("Move (" + initRow + ";" + initCol + ") to (" + newRow + ";" + newCol +
                          ") for LEFT HORIZONTAL move");
                return true;
            }

            //RIGHT HORIZONTAL
            if (SafeGetType(initRow, initCol) == SafeGetType(newRow, newCol + 1) &&
                SafeGetType(initRow, initCol) == SafeGetType(newRow, newCol + 2))
            {
                Debug.Log("Move (" + initRow + ";" + initCol + ") to (" + newRow + ";" + newCol +
                          ") for RIGHT HORIZONTAL move");
                return true;
            }
        }

        //VERTICAL CHECK
        if (newCol != initCol)
        {
            //CENTRAL VERTICAL
            if (SafeGetType(initRow, initCol) == SafeGetType(newRow - 1, newCol) &&
                SafeGetType(initRow, initCol) == SafeGetType(newRow + 1, newCol))
            {
                Debug.Log("Move (" + initRow + ";" + initCol + ") to (" + newRow + ";" + newCol +
                          ") for CENTRAL VERTICAL move");
                return true;
            }

            //UP VERTICAL
            if (SafeGetType(initRow, initCol) == SafeGetType(newRow + 1, newCol) &&
                SafeGetType(initRow, initCol) == SafeGetType(newRow + 2, newCol))
            {
                Debug.Log("Move (" + initRow + ";" + initCol + ") to (" + newRow + ";" + newCol +
                          ") for UP VERTICAL move");
                return true;
            }

            //DOWN VERTICAL
            if (SafeGetType(initRow, initCol) == SafeGetType(newRow - 1, newCol) &&
                SafeGetType(initRow, initCol) == SafeGetType(newRow - 2, newCol))
            {
                Debug.Log("Move (" + initRow + ";" + initCol + ") to (" + newRow + ";" + newCol +
                          ") for DOWN VERTICAL move");
                return true;
            }
        }

        return false;
    }

    //return type of chip is available, -1 if not
    private int SafeGetType(int row, int col)
    {
        if (row < 0 || row >= MAX_ROWS || col < 0 || col >= MAX_COLS) return -1;
        if (chipArray[row, col]) return chipArray[row, col].Type;
        return -1;
    }

    private void UpdateScore()
    {
        var textFieldObject = GameObject.Find("Score Text");
        var textField = textFieldObject.GetComponent<Text>();
        textField.text = "Score: " + ScorePoints + "     Turns left: " + TurnsLeft;
    }

    private void EnablePlayerControl(bool skipCheck)
    {
        if (skipCheck || GetAnyPossibleMove() != noMatchVector4)
        {
            SetPhysics(true);
            isFieldActive = true;
            startTurnTime = Time.time;
            isHintShowed = false;
        }
        else
        {
            Shuffle();
        }
    }
}