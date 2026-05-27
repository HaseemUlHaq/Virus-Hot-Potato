using UnityEngine;

public class AmbientAudio : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;

    private void Start()
    {
        audioSource.Play();
    }
}
