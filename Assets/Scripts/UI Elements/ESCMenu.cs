using UnityEngine;

public class ESCMenu : MonoBehaviour
{

    private void Start()
    {
        // Ensure the ESC menu is hidden at the start
        gameObject.SetActive(false);
    }

    public void MainMenuButtonPressed()
    {
        // Load the main menu scene
        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    public void QuitButtonPressed()
    {
        // Quit the application
        Application.Quit();

        // If running in the editor, stop playing
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    public void SettingsButtonPressed()
    {
        // Open the settings menu
        Debug.Log("Settings button pressed - implement settings logic here");
        // This could open a settings panel or load a settings scene
    }
}
