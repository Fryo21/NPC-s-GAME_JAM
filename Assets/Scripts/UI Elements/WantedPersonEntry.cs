using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WantedPersonEntry : MonoBehaviour
{
    [SerializeField] private Image characterImage;
    [SerializeField] private TextMeshProUGUI nameText;

    public void Initialize(NPCData npcData)
    {
        if (npcData == null) return;

        // Set the character image
        if (characterImage != null && npcData.npcSprite != null)
        {
            characterImage.sprite = npcData.npcSprite;
            characterImage.preserveAspect = true;
        }

        // Set the name text
        if (nameText != null)
        {
            nameText.text = npcData.npcName;
        }
    }
}