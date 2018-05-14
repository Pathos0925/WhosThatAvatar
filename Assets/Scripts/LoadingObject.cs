using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoadingObject : MonoBehaviour
{
    private Text text;
    int index = 0;
    private float counter = 0f;
    //public bool ShowStaticMessage = false;
    public string StaticMessage = "";
	// Use this for initialization
	void Start ()
    {
        text = GetComponent<Text>();
        this.gameObject.SetActive(false);
    }
	
	// Update is called once per frame
	void Update ()
    {
        if (!string.IsNullOrEmpty(StaticMessage))
        {
            text.text = StaticMessage;
        }
        else
        {
            counter += Time.deltaTime;
            if (counter >= 0.25f)
            {
                counter = 0f;
                index++;
                text.text = "Loading";

                if (index == 3)
                {
                    index = 0;
                }

                for (int i = 0; i < index; i++)
                {
                    text.text += ".";
                }
            }
        }
        
    }
    
}
