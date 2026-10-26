using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Control del audio ambiental con botón guardado en la escena.</summary>
public sealed class AmbientAudioTogglePlaceholder : MonoBehaviour
{
    private static readonly string[] PointNames = { "Tunda", "Mohan", "Bachue", "MadreMonte", "Silbon" };

    [SerializeField] private Button ambientButton;
    [SerializeField] private TMP_Text ambientLabel;

    private GameObject activePoint;
    private AudioSource activeSource;
    private bool muted;

    private void Awake()
    {
        if (ambientButton == null)
            ambientButton = GetComponent<Button>();
        if (ambientLabel == null)
            ambientLabel = GetComponentInChildren<TMP_Text>(true);

        if (ambientButton != null)
        {
            ambientButton.onClick.RemoveListener(ToggleAudio);
            ambientButton.onClick.AddListener(ToggleAudio);
        }
        RefreshLabel();
    }

    private void Update()
    {
        GameObject point = FindActivePoint();
        if (point == activePoint)
            return;

        if (activeSource != null)
            activeSource.Stop();

        activePoint = point;
        activeSource = point != null ? point.GetComponent<AudioSource>() : null;
        if (activeSource != null && activeSource.clip != null)
        {
            activeSource.playOnAwake = false;
            activeSource.loop = true;
            activeSource.spatialBlend = 0f;
            activeSource.mute = muted;
            activeSource.Play();
        }

        RefreshLabel();
    }

    private void ToggleAudio()
    {
        if (activeSource == null || activeSource.clip == null)
            return;

        muted = !muted;
        activeSource.mute = muted;
        RefreshLabel();
    }

    private void RefreshLabel()
    {
        bool available = activePoint != null && activeSource != null && activeSource.clip != null;
        if (ambientButton != null)
            ambientButton.interactable = available;
        if (ambientLabel != null)
        {
            ambientLabel.text = activePoint == null
                ? "ABRE UN PUNTO PARA OIR AMBIENTE"
                : !available ? "SIN AMBIENTE CONFIGURADO"
                : muted ? "ACTIVAR AMBIENTE" : "SILENCIAR AMBIENTE";
        }
    }

    private static GameObject FindActivePoint()
    {
        foreach (string pointName in PointNames)
        {
            GameObject point = GameObject.Find(pointName);
            if (point != null && point.activeInHierarchy)
                return point;
        }

        return null;
    }
}
