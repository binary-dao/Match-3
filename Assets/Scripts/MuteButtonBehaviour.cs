using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MuteButtonBehaviour : MonoBehaviour {
    Text buttonCaption;

    // Use this for initialization
    void Start ()
    {
        buttonCaption = gameObject.GetComponentInChildren<Text>();
    }
	
	// Update is called once per frame
	void Update () {
		
	}

    public void MuteClick()
    {
        Debug.Log("Mute click");
        GameBehaviour.instance.Mute();
        if (buttonCaption.text == "MUTE")
        {
            buttonCaption.text = "UNMUTE";
        }
        else
        {
            buttonCaption.text = "MUTE";
        }
    }
}
