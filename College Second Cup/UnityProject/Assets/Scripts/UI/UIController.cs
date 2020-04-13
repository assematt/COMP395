using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class UIController : MonoBehaviour
{
    public void ToggleControl(RectTransform control)
    {
        var sizeDelta = control.sizeDelta;
        sizeDelta = new Vector2(sizeDelta.x, Math.Abs(sizeDelta.y - 315f) < 0.1f ? 60f : 315f);
        control.sizeDelta = sizeDelta;
    }

    public void FlipButton(RectTransform control)
    {
        control.localScale.Scale(new Vector3(1f, -1f, 1f));
    }
}
