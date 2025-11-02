using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// If the trigger collider is touched by the player, it will load the next scene (from the build settings)
/// </summary>
public class EndLevel : MonoBehaviour
{
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
                SceneManager.LoadScene(nextIndex);
            }
        }
    }
}
