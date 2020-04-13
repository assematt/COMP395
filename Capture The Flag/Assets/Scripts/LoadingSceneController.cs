using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// This script is used to control the loading of another scene
/// </summary>
public class LoadingSceneController : MonoBehaviour
{
    public string sceneToLoad;
    public LoadingBarController loadingBar;
    
    /// <summary>
    /// Function used byt the UI to call the loading of a scene
    /// </summary>
    public void LoadScene()
    {
        StartCoroutine(LoadingCoroutine());
    }

    /// <summary>
    /// This coroutine loads the game without blocking the main thread
    /// and allowing the UI animations to still work
    /// </summary>
    /// <returns></returns>
    private IEnumerator LoadingCoroutine()
    {
        var loadingStatus = SceneManager.LoadSceneAsync(sceneToLoad);

        while (!loadingStatus.isDone)
        {
            loadingBar.UpdateBar(loadingStatus.progress);
            yield return null;
        }
    }
}
