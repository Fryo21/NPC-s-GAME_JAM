using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InterludePanel : MonoBehaviour
{
    [SerializeField] private Button startNextRoundButton;


    public void StartNextRound()
    {
        // Start the next round
        RoundManager.Instance.StartNextRound();
    }

}