using UnityEngine;
using UnityEngine.SceneManagement;

namespace VisionsOfGenesis.UI
{
    public class SceneNavigator : MonoBehaviour
    {
        [Header("Scene names (must match Build Settings exactly)")]
        public string mainMenuScene = "MainMenu";
        public string gameScene     = "Game";
        public string creditsScene  = "Credits";
        public string homeScene     = "Home";

        public void PlayGame()
        {
            SceneManager.LoadScene(gameScene);
        }

        public void GoToHome()
        {
            SceneManager.LoadScene(homeScene);
        }

        public void RestartGame()
        {
            SceneManager.LoadScene(gameScene);
        }

        public void GoToCredits()
        {
            SceneManager.LoadScene(creditsScene);
        }

        public void GoToMainMenu()
        {
            SceneManager.LoadScene(mainMenuScene);
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
