using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This script hides/show the Explanation screen at the start of the level
/// </summary>
public class ExplanationScreen : MonoBehaviour
{
    private static readonly int HideMenu = Animator.StringToHash("HideMenu");
    private static readonly int ShowMenu = Animator.StringToHash("ShowMenu");

    /// <summary>
    /// Show the menu and then calls the coroutine that waits for
    /// 5 seconds before closing the explanation screen
    /// </summary>
    void Start()
    {
        GetComponent<Animator>().SetTrigger(ShowMenu);

        StartCoroutine(HideScreen());
    }

    /// <summary>
    /// Hides the screen after 5 seconds
    /// </summary>
    /// <returns></returns>
    private IEnumerator HideScreen()
    {
        yield return new WaitForSeconds(5f);

        GetComponent<Animator>().SetTrigger(HideMenu);
    }
}
