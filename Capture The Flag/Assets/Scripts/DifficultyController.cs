using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Stores the game difficulty and handles the change in game difficulty
/// </summary>
public class DifficultyController : MonoBehaviour
{
    public enum Difficulty
    {
        Easy,
        Medium,
        Hard
    }

    public static Difficulty GameDifficulty { get; private set; } = Difficulty.Easy;

    // Start is called before the first frame update
    /// <summary>
    /// make sure that this component is preserved between loading new scenes
    /// </summary>
    void Start()
    {
        DontDestroyOnLoad(this);
    }

    /// <summary>
    /// Update the game difficulty
    /// </summary>
    /// <param name="difficulty">The new difficulty level</param>
    public void SetGameDifficulty(string difficulty)
    {
        if (Enum.TryParse(difficulty, true, out Difficulty newDifficulty))
        {
            GameDifficulty = newDifficulty;
        }
    }
}
