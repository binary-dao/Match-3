﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChipBehaviour : MonoBehaviour {
    internal const int ICON_WIDTH = 1;
    internal const int ICON_HEIGHT = 1;

    //destroying animation
    private const float DESTROY_TIME = 0.3f;
    private SpriteRenderer spriteRenderer;
    private bool isDestroying;
    private float startTime;
    private Color finalColor;

    //moving animation
    private const float MOVE_TIME = 0.5f;
    private bool isMoving;
    private Vector2 startPosition;
    private Vector2 endPosition;

    //swipe support
    private static ChipBehaviour startSwipeChip;
    private static ChipBehaviour lastMouseEnterSprite;

    //selected halo
    Component halo;

    internal int row;
    internal int col;
    private int type;

    internal int Type
    {
        get
        {
            return type;
        }
        set
        {
            ChangeType(value);
        }
    }

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
        else if(isMoving)
        {
            float part = (Time.time - startTime) / MOVE_TIME;
            if (part >=1 )
            {
                isMoving = false;
                GameBehaviour.instance.OnMovingEnd();
            }
            else
            {
                transform.position = Vector2.Lerp(startPosition, endPosition, part);
            }
        }
    }

    internal void Create(int type, int row, int col, bool useGravity)
    {
        this.row = row;
        this.col = col;
        ChangeType(type);
        
        if (!useGravity)
        {
            Rigidbody rigidbody = gameObject.GetComponent<Rigidbody>();
            //gidbody.useGravity = false;
            //rigidbody.constraints = RigidbodyConstraints.FreezePositionY;
            rigidbody.constraints = RigidbodyConstraints.FreezeAll;
        }
    }

    private void ChangeType(int type)
    {
        this.type = type;
        Texture2D texture = (Texture2D)Resources.Load("chip" + type);
        Sprite runtimeSprite = Sprite.Create(texture, new Rect(0.0f, 0.0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100.0f);
        gameObject.GetComponent<SpriteRenderer>().sprite = runtimeSprite;
        transform.position = new Vector2(col * ICON_WIDTH, row * ICON_HEIGHT);
    }

    internal void MoveTo(Vector2 newPosition)
    {
        startTime = Time.time;
        startPosition = transform.position;
        endPosition = newPosition;
        isMoving = true;
    }

    private void OnMouseDown()
    {
        startSwipeChip = this;
    }

    private void OnMouseEnter()
    {
        lastMouseEnterSprite = this;
    }

    private void OnMouseUp()
    {
        if (startSwipeChip != lastMouseEnterSprite)
        {
            GameBehaviour.instance.selectedChip = startSwipeChip;
            if(IsChipsAdjacent(startSwipeChip, lastMouseEnterSprite))
            {
                GameBehaviour.instance.TrySwipeWith(lastMouseEnterSprite);
            }
            return;
        }
        
        //uncheck chip
        if(GameBehaviour.instance.selectedChip == this)
        {
            startSwipeChip = null;
            SetHalo(false);
            GameBehaviour.instance.selectedChip = null;
        }
        //no previous checks
        else if (!GameBehaviour.instance.selectedChip)
        { 
            SetHalo(true);
            GameBehaviour.instance.selectedChip = this;
        }
        //try to change places?
        else if (IsChipsAdjacent(GameBehaviour.instance.selectedChip, this))
        {
            SetHalo(false);
            GameBehaviour.instance.selectedChip.SetHalo(false);
            GameBehaviour.instance.TrySwipeWith(this);
        }
        //not adjacent, lets check another chip
        else
        {
            GameBehaviour.instance.selectedChip.SetHalo(false);
            SetHalo(true);
            GameBehaviour.instance.selectedChip = this;
        }
        
    }

    private bool IsChipsAdjacent(ChipBehaviour firstChip, ChipBehaviour secondChip)
    {
        int xRange = Math.Abs(firstChip.col - secondChip.col);
        int yRange = Math.Abs(firstChip.row - secondChip.row);
        return (xRange + yRange == 1);
    }

    internal void SetHalo(bool enabled)
    {
        halo.GetType().GetProperty("enabled").SetValue(halo, enabled, null);
    }

    internal void StartDestroy()
    {
        if(isDestroying)
        {
            return;
        }
        GameBehaviour.instance.scorePoints += GameBehaviour.SCORES_FOR_CHIP;
        startTime = Time.time;
        isDestroying = true;
    }

}
