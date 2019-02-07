using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PauseButtonBehaviour : MonoBehaviour {
    Text buttonCaption;

	// Use this for initialization
	void Start ()
    {
        buttonCaption = gameObject.GetComponentInChildren<Text>();
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    public void PauseClick()
    {
        Debug.Log("PauseClick");
        GameBehaviour.instance.Pause();
        if(buttonCaption.text == "PAUSE")
        {
            buttonCaption.text = "UNPAUSE";
        }
        else
        {
            buttonCaption.text = "PAUSE";
        }
    }
}
