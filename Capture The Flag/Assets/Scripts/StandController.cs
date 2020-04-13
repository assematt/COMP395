using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// The stand controller handles the stand that holds the flag
/// </summary>
public class StandController : MonoBehaviour
{
    private Animator _flagAnimator;
    private static readonly int Flag = Animator.StringToHash("CaptureFlag");

    private FlagsDatabase.FlagData _flagData;

    public static Action<string> OnFlagCaptured = (flag) => {};
    
    /// <summary>
    /// Cache the Animator controller
    /// </summary>
    void Start()
    {
        _flagAnimator = GetComponentInChildren<Animator>();
    }

    /// <summary>
    /// Action executed when the flag gets captured. Mainly execute an anitmation on the flag
    /// </summary>
    public void CaptureFlag()
    {
        _flagAnimator.SetTrigger(Flag);
    }

    /// <summary>
    /// Update the flag displayed on the stand
    /// </summary>
    /// <param name="flag">The flag data used to display the new texture</param>
    public void UpdateFlag(FlagsDatabase.FlagData flag)
    {
        _flagData = flag;

        var spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        spriteRenderer.sprite = _flagData.flagSprite;
    }

    /// <summary>
    /// When the player enters the stand area. It "captures" the flag
    /// </summary>
    /// <param name="other"></param>
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            CaptureFlag();

            StandController.OnFlagCaptured(_flagData.flagCountry);

            enabled = false;
        }
    }
}
