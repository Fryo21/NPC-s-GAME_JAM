using UnityEditor;
using UnityEngine;

public class WarningPopUp : MonoBehaviour
{
    [SerializeField] private GameObject helpTipPanel;
    [SerializeField] private float helpTipDuration = 3f;
    public void ShowHelpTip()
    {
        helpTipPanel.SetActive(true);
    }


}
