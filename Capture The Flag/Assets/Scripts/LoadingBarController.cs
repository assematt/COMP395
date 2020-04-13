using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Script used to handle the look of the loading bar
/// </summary>
public class LoadingBarController : MonoBehaviour
{
    public RectTransform emptyBar;
    public RectTransform fullBar;

    /// <summary>
    /// Reset the size of the loading bar at startup
    /// </summary>
    void Start()
    {
        fullBar.localScale = new Vector3(0, 1f, 1f);
    }

    /// <summary>
    /// Update the size of the bar to reflect the current loading progress
    /// </summary>
    /// <param name="progress"></param>
    public void UpdateBar(float progress)
    {
        fullBar.localScale = new Vector3(progress, 1f, 1f);
    }
}
