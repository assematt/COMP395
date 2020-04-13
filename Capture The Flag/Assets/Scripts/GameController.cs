using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Invector.vCharacterController;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

/// <summary>
/// The game controller is used to control the main functionalities of "Capture the Flag" game-mode
/// </summary>
public class GameController : MonoBehaviour
{
    public FlagsDatabase flagsDatabase;

    public FlagsController flagsController;

    public RectTransform gameOverScreen;

    public TextMeshProUGUI scoreText;

    public TimerController timerController;

    private int _score = 0;
    
    private List<FlagsDatabase.FlagData> _flagsToSpawn = new List<FlagsDatabase.FlagData>(5);
    private EnemyController[] _enemies;
    private static int FlagsToCapture = 3;
    private static readonly int ShowMenu = Animator.StringToHash("ShowMenu");

    public UnityEvent onGameWon;
    public UnityEvent onGameLost;

    /// <summary>
    /// Cache a reference to all the EnemyControllers in the scene
    /// </summary>
    private void Awake()
    {
        _enemies = FindObjectsOfType<EnemyController>();
    }
    
    /// <summary>
    /// Set the initial level values and attach a callback to the events that:
    ///     - end the game
    ///     - control the scoring
    /// </summary>
    void Start()
    {
        FlagsToCapture = 3;
        _score = 0;

        _flagsToSpawn = flagsDatabase.flags.OrderBy(x => Random.value).Take(5).ToList();

        UpdateFlagsUI();
        UpdateStands();

        Time.timeScale = 1f;

        TimerController.TimesEnded += () => EndGame("You Lost!");
        PlayerController.onPlayerDead += () => EndGame("You Lost!");
        PlayerController.onPlayerHit += () =>
        {
            _score -= 50;
            scoreText.text = $"Score: {_score}";
        };

        StandController.OnFlagCaptured += (flag) =>
        {
            var flagIndex = _flagsToSpawn.FindIndex(match => match.flagCountry == flag);

            if (flagIndex < 3)
            {
                FlagsToCapture--;

                if (FlagsToCapture == 0)
                    EndGame("You Won!");

                _score += 100;
            }
            else
            {
                if (timerController.TimerStarted)
                    timerController.HalfTimer();
                else
                {
                    foreach (var enemy in _enemies)
                    {
                        enemy.FollowPlayer();
                    }
                    timerController.StartTimer();
                }

                _score -= 25;
            }

            scoreText.text = $"Score: {_score}";
        };
    }

    /// <summary>
    /// This function is called when the player either wins or loses the game.
    /// It display the end game screen, the final result and score
    /// </summary>
    /// <param name="result">
    /// The final result, either:
    ///     - "You Won!" or
    ///     - "You Lost!"
    /// </param>
    void EndGame(string result)
    {
        gameOverScreen.GetComponent<Animator>().SetTrigger(ShowMenu);

        gameOverScreen.Find("Result").GetComponent<TextMeshProUGUI>().text = result;
        gameOverScreen.Find("FinalScore").GetComponent<TextMeshProUGUI>().text = $"Final score: {_score}";

        if (result == "You Won!")
            onGameWon.Invoke();
        else
            onGameLost.Invoke();
    }

    /// <summary>
    /// Update the UI that displays the list of flags to search
    /// </summary>
    void UpdateFlagsUI()
    {
        var flagsToSearch = new string[3];

        for (int index = 0; index < flagsToSearch.Length; index++)
        {
            flagsToSearch[index] = _flagsToSpawn[index].flagCountry;
        }

        flagsController.SetFlagsText(flagsToSearch);
    }

    /// <summary>
    /// Attach to all the stands in the scene the FlagData generated at the start of the level
    /// </summary>
    void UpdateStands()
    {
        var stands = FindObjectsOfType<StandController>();

        for (int index = 0; index < stands.Length; index++)
        {
            stands[index].UpdateFlag(_flagsToSpawn[index]);
        }
    }
    
    /// <summary>
    /// Handy function used by the UI to exit the application
    /// </summary>
    public void ExitToDesktop()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
         Application.Quit();
#endif
    }
}
