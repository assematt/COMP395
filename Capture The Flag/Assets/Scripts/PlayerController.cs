using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// This player handles the PlayerControll functionalities
/// </summary>
public class PlayerController : MonoBehaviour
{
    public static Action onPlayerDead = () => {};

    public static Action onPlayerHit = () => { };

    private int _hitpoints = 5;
    
    /// <summary>
    /// Initilize the number of the player hitpoints to be equal to the number of enemies in the scene
    /// </summary>
    void Start()
    {
        _hitpoints = FindObjectsOfType<EnemyController>().Length;
    }

    /// <summary>
    /// When the player gets hit it decrement the player's health
    /// and invoke the "onPlayerHit" callback
    /// </summary>
    public void RegisterHit()
    {
        if (--_hitpoints == 0)
        {
            Destroy(gameObject);
            onPlayerDead();
        }

        onPlayerHit();
    }

}
