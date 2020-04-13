using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// This script controls the Flag list UI element
/// </summary>
public class FlagsController : MonoBehaviour
{
    private TextMeshProUGUI[] _flagsText;

    /// <summary>
    /// Cache the reference to the list of Text components used to display
    /// the flags to search
    /// </summary>
    private void Awake()
    {
        _flagsText = GetComponentsInChildren<TextMeshProUGUI>();
    }

    /// <summary>
    /// Attach a function to the callback "StandController.OnFlagCaptured"
    /// that checks which flag was captured and, if found, remove the Text
    /// element from the list
    /// </summary>
    void Start()
    {
        StandController.OnFlagCaptured += flag =>
        {
            for (int index = 0; index < _flagsText.Length; index++)
            {
                if (_flagsText[index].text == flag)
                {
                    Destroy(_flagsText[index].gameObject);
                    return;
                }
            }
        };
    }

    /// <summary>
    /// Update the UI element that shows the list of flags to search
    /// </summary>
    /// <param name="flagsName">An array with the flags name to display</param>
    public void SetFlagsText(string[] flagsName)
    {
        for (int index = 0; index < flagsName.Length; index++)
        {
            _flagsText[index].text = flagsName[index];
        }
    }
}
