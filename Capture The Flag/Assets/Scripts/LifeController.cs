using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Script used to display the health bar on the screen
/// </summary>
public class LifeController : MonoBehaviour
{
    private int _enemyCount;

    /// <summary>
    /// Cache a reference to how many enemies we have in the  scene
    /// </summary>
    private void Awake()
    {
        _enemyCount = FindObjectsOfType<EnemyController>().Length;
    }

    /// <summary>
    /// Update the UI to reflect the number of enemies on the scene
    /// Set up the response to the callback called bu the PlayerController when the
    /// user gets hit
    /// </summary>
    void Start()
    {
        UpdateMaxHitPoints();

        PlayerController.onPlayerHit += () =>
        {
            Destroy(transform.GetChild(0).gameObject);
        };
    }

    /// <summary>
    /// Update the number of Health icons in the UI
    /// </summary>
    void UpdateMaxHitPoints()
    {
        var icon = transform.GetChild(0).gameObject;

        foreach (Transform child in transform)
            Destroy(child.gameObject);

        while (_enemyCount-- > 0)
            Instantiate(icon, transform).GetComponent<RawImage>().enabled = true;
    }
}
