using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// If the trigger collider is touched by the player, it will load the next scene (from the build settings)
/// </summary>
public class EndLevel : MonoBehaviour
{   
    [SerializeField,Tooltip("The Loading Screen")]
    private GameObject loadingScreen;
    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent(out Character character))
        {
            if (character == PlayerController.instance.currentCharacter)
            {
                int total = SceneManager.sceneCountInBuildSettings;
                int currentIndex = SceneManager.GetActiveScene().buildIndex;
                int nextIndex = currentIndex + 1;
                // If we don't have any next level, it will go back to the scene 0 which should be the main menu
                if (nextIndex >= total || nextIndex < 0)
                {
                    nextIndex = 0;
                }
                StartCoroutine(LoadLevelCoroutine(nextIndex));
            }
        }
    }

    /// <summary>
    /// Loads level with coroutine
    /// </summary>
    /// <param name="sceneIndex">the index of the scene to load</param>
    private IEnumerator LoadLevelCoroutine(int sceneIndex)
    {
        Time.timeScale = 0.0f;
        if (loadingScreen != null)
        {
            loadingScreen.SetActive(true);
        }
        else
        {
            Debug.LogWarning("Loading Screen is null");
        }
        AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(sceneIndex);
        asyncOperation.allowSceneActivation = false;
        while (!asyncOperation.isDone)
        {
            if (asyncOperation.progress >= 0.9f)
            {
                Time.timeScale = 1.0f;
                asyncOperation.allowSceneActivation = true;
            }
            yield return null;
        }
    }
}
