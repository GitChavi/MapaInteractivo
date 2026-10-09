using UnityEngine;

/// <summary>Asigna al AudioSource raíz el clip ambiental de este punto.</summary>
public sealed class PointAmbientAudioSetup : MonoBehaviour
{
    [SerializeField] private AudioClip ambientClip;

    private void Awake()
    {
        if (ambientClip == null)
            return;

        AudioSource source = GetComponent<AudioSource>();
        if (source == null)
            source = gameObject.AddComponent<AudioSource>();

        source.clip = ambientClip;
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
    }
}
