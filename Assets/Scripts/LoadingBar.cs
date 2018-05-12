using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LoadingBar : MonoBehaviour {

    public Image loadingBarImage;
    public Text loadingBarStatusText;
    private float maxWidth;
    private float currentProgress = 1f;
	// Use this for initialization
	void Start ()
    {
        maxWidth = this.gameObject.GetComponent<RectTransform>().sizeDelta.x;
        SetProgress(0f);
    }
	
    public void SetProgress(float newProgress, string newStatus = "")
    {
        var newWidth = maxWidth - (newProgress * maxWidth);

        if (loadingBarStatusText)
            loadingBarStatusText.text = newStatus;

        if (this.gameObject.activeSelf)
            StartCoroutine(Delayedset(newWidth));        
    }

    private IEnumerator Delayedset(float newWidth)
    {
        yield return new WaitForEndOfFrame();

        if (this.gameObject.activeSelf)
            loadingBarImage.rectTransform.offsetMax = new Vector2(-newWidth, loadingBarImage.rectTransform.offsetMax.y);        
    }
}
