using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

/// <summary>
/// Handle the Timer functionality used in the game-play
/// </summary>
public class TimerController : MonoBehaviour
{
    private float _prevTimeLeft = 60f;
    private float _timeLeft = 60f;

    public bool TimerStarted { get; private set; } = false;

    public TextMeshProUGUI timerText;

    public static Action TimesEnded = () => { };

    
    /// <summary>
    /// Reset the values used by the Timer
    /// </summary>
    void Start()
    {
        _prevTimeLeft = 60f;
        _timeLeft = 60f;
        TimerStarted = false;

        gameObject.SetActive(false);
    }

    /// <summary>
    /// Once the timer it started, it begins counting time
    /// once the timer reaches 0, we run out of the time
    /// and the game should end
    /// </summary>
    void Update()
    {
        if (TimerStarted)
        {
            _timeLeft -= Time.deltaTime;
            
            timerText.text = FormatTime(_timeLeft);

            if (_timeLeft <= 0f)
            {
                TimesEnded();
                TimerStarted = false;
            }
        }
    }

    /// <summary>
    /// Starts the timer
    /// </summary>
    public void StartTimer()
    {
        TimerStarted = true;
        gameObject.SetActive(true);
    }

    /// <summary>
    /// Half the amount of time avaliable
    /// </summary>
    public void HalfTimer() => _timeLeft /= 2f;

    /// <summary>
    /// Helper function used to display the time stored as a float
    /// In the mm:ss format
    /// </summary>
    /// <param name="time">The time to format in seconds</param>
    /// <returns>A formatted string in mm:ss format</returns>
    public string FormatTime(float time)
    {
        var ts = TimeSpan.FromSeconds(_timeLeft);
        return $"{ts.Minutes:00}:{ts.Seconds:00}";
    }
}
