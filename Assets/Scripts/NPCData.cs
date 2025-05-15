using UnityEngine;

[CreateAssetMenu(fileName = "NPCData", menuName = "Scriptable Objects/NPCData")]
public class NPCData : ScriptableObject
{
    public string npcName;
    public Sprite npcSprite;

    public NPCClass nPCClass;

    public int npcSubClass; //1-3

    public RuntimeAnimatorController npcAnimatorController;

}

public enum NPCClass
{
    A,
    B,
    C,
    D,
    E,
    F,

}