using System;
using UnityEngine;
using System.Collections;
using TMPro;

[Serializable]
public class CounterLine
{
    public Transform counterTransform;
    public ClerkController assignedClerk;
    private int _customerInLine;

    [HideInInspector]
    public int customerInLine
    {
        get => _customerInLine;
        set
        {
            _customerInLine = value;
            displayedText.text = value.ToString();
        }
    }

    public TextMeshProUGUI displayedText;
}
