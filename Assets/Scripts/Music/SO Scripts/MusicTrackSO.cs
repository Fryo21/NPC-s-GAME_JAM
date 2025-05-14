using UnityEngine;

[CreateAssetMenu(fileName = "MusicTrackData", menuName = "ScriptableObjects/MusicTrackSO", order = 1)]
public class MusicTrackSO : ScriptableObject
{
    public AudioClip softIntensityClip;
    public AudioClip mediumIntensityClip;
    public AudioClip hardIntensityClip;
}
