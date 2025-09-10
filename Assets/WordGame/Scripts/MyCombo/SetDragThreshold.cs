using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class SetDragThreshold : MonoBehaviour {
    public Canvas myCanvas;

    private void Start()
    {
        this.GetComponent<EventSystem>().pixelDragThreshold = (int)(10 * this.myCanvas.scaleFactor);
    }
}
