using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TestCustomScript : MonoBehaviour
{
    public Text text;

    [VRCSDK2.RPC]
    public void SayHello()
    {
        UnityEngine.Debug.Log("Hello World!!");
        text.text = "Hello World";
    }
}