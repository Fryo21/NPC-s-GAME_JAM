using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class TipManager : MonoBehaviour
{

    public static TipManager Instance { get; private set; }

    [SerializeField] private TMP_Text tipText;

    [SerializeField]
    private List<string> gameTips = new List<string>
    {
        "TIP: You can arrest suspects by clicking on them directly.",
        "TIP: Drones will automatically arrest suspects if you don't respond in time!",
        "TIP: Correct arrests earn money, but wrong arrests will cost you.",
        "TIP: The more drones you buy, the harder they become to manage.",
        "TIP: Each round requires you to meet your arrest quoota - 1/3 of the total wanted list.",
        "TIP: Check the wanted list carefully before confirming any arrests."
    };

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

        if (tipText == null)
        {
            tipText = GetComponent<TMP_Text>();
        }

    }

    public void SetTipTextToRandomTip()
    {
        if (tipText == null)
        {
            Debug.Log("Tip Text is not assigned.");
            return;
        }
        int randomIndex = Random.Range(0, gameTips.Count);
        string randomTip = gameTips[randomIndex];

        tipText.text = randomTip;
    }

}
