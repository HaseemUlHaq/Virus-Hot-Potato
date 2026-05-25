using Fusion;
using UnityEngine;

// Plays looping ambient audio only on the master client to avoid doubling
// in collocated MR where multiple headsets share the same physical space.
public class AmbientAudio : NetworkBehaviour
{
    [SerializeField] private AudioSource audioSource;

    public override void Spawned()
    {
        if (Runner.IsSharedModeMasterClient)
            audioSource.Play();
        else
            audioSource.Stop();
    }
}
