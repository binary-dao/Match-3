using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChipBehaviour : MonoBehaviour {
    public const int ICON_WIDTH = 1;
    public const int ICON_HEIGHT = 1;

    //destroying animation
    private const float DESTROY_TIME = 0.3f;
    private SpriteRenderer spriteRenderer;
    private bool isDestroying;
    private float startTime;
    private Color finalColor;

    Component halo;

    public int row;
    public int col;

    //moving animation
    private const float MOVE_TIME = 0.3f;
    private float startX;
    private float startY;
    private float endX;
    private float endY;

    // Use this for initialization
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        finalColor = new Color(0.5f, 0, 1, 1);
        halo = gameObject.GetComponent("Halo");
        SetHalo(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (isDestroying)
        {
            float part = (Time.time - startTime) / DESTROY_TIME;
            if (part >= 1)
            {
                isDestroying = false;
                Destroy(gameObject);
            }
            else
            {
                spriteRenderer.color = Color.Lerp(Color.white, finalColor, part);
            }
        }
    }

    internal void Create(GameObject holder, int type, int row, int col, bool useGravity)
    {
        this.row = row;
        this.col = col;
        Texture2D texture = (Texture2D)Resources.Load("chip" + type);
        Sprite runtimeSprite = Sprite.Create(texture, new Rect(0.0f, 0.0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100.0f);
        gameObject.GetComponent<SpriteRenderer>().sprite = runtimeSprite;
        transform.position = new Vector2(col * ICON_WIDTH, row * ICON_HEIGHT);

        if (!useGravity)
        {
            Rigidbody rigidbody = gameObject.GetComponent<Rigidbody>();
            //gidbody.useGravity = false;
            //rigidbody.constraints = RigidbodyConstraints.FreezePositionY;
            rigidbody.constraints = RigidbodyConstraints.FreezeAll;
        }


    }

    private void OnMouseUp()
    {
        //uncheck chip
        if(GameBehaviour.selectedChip == this)
        {
            SetHalo(false);
            GameBehaviour.selectedChip = null;
        }
        //no previous checks
        else if (!GameBehaviour.selectedChip)
        { 
            SetHalo(true);
            GameBehaviour.selectedChip = this;
        }
        //try to change places?
        else if (IsChipsAdjacent())
        {
            SetHalo(false);
            GameBehaviour.selectedChip.SetHalo(false);
            GameBehaviour.TrySwipe(GameBehaviour.selectedChip, this);
        }
        //not adjacent, lets check another chip
        else
        {
            GameBehaviour.selectedChip.SetHalo(false);
            SetHalo(true);
            GameBehaviour.selectedChip = this;
        }
        
    }

    private bool IsChipsAdjacent()
    {
        int xRange = Math.Abs(col - GameBehaviour.selectedChip.col);
        int yRange = Math.Abs(row - GameBehaviour.selectedChip.row);
        return (xRange + yRange == 1);
    }

    public void SetHalo(bool enabled)
    {
        halo.GetType().GetProperty("enabled").SetValue(halo, enabled, null);
    }

    private void StartDestroy()
    {
        startTime = Time.time;
        isDestroying = true;
    }

}
