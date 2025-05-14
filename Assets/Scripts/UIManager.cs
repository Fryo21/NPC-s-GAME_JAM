using Unity.VisualScripting;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    // UI Displays Criminal Legibility
    public void ShowConfirmation()
    {
        Debug.Log("UI Show Confirmation");
    }
    // UI Hides Criminal Legibility
    public void HideConfirmation()
    {
        Debug.Log("UI Hide Confirmation");
    }  
}
