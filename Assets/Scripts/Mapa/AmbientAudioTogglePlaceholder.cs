using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Control visible para silenciar y reactivar el ambiente del punto abierto.
/// Busca los puntos narrativos conocidos, inicia su AudioSource en bucle y
/// crea un ambiente sintético de prueba si el punto todavía no tiene un clip.
/// </summary>
public sealed class AmbientAudioTogglePlaceholder : MonoBehaviour
{
    private Text label;
    private TMP_Text tmpLabel;
    private GameObject activePoint;
    private AudioSource activeSource;
    private bool muted;
    private static AudioClip generatedAmbient;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneHook()
    {
        SceneManager.sceneLoaded += AddControl;
    }

    private static void AddControl(Scene scene, LoadSceneMode mode)
    {
        Canvas canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
            return;

        Transform existing = canvas.transform.Find("AmbientAudioTogglePlaceholder");
        if (existing != null)
        {
            AmbientAudioTogglePlaceholder existingControl = existing.GetComponent<AmbientAudioTogglePlaceholder>();
            if (existingControl == null)
                existingControl = existing.gameObject.AddComponent<AmbientAudioTogglePlaceholder>();
            existingControl.label = existing.GetComponentInChildren<Text>(true);
            existingControl.tmpLabel = existing.GetComponentInChildren<TMP_Text>(true);
            Button existingButton = existing.GetComponent<Button>();
            if (existingButton != null)
            {
                existingButton.onClick.RemoveListener(existingControl.ToggleAudio);
                existingButton.onClick.AddListener(existingControl.ToggleAudio);
            }
            existingControl.RefreshLabel();
            return;
        }

        GameObject buttonObject = new GameObject("AmbientAudioTogglePlaceholder", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(AmbientAudioTogglePlaceholder));
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.SetParent(canvas.transform, false);
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-24f, -24f);
        rect.sizeDelta = new Vector2(230f, 56f);

        Image background = buttonObject.GetComponent<Image>();
        background.color = new Color(0.12f, 0.18f, 0.14f, 0.92f);
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = background;
        button.onClick.AddListener(buttonObject.GetComponent<AmbientAudioTogglePlaceholder>().ToggleAudio);

        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.SetParent(rect, false);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(8f, 4f);
        labelRect.offsetMax = new Vector2(-8f, -4f);
        Text label = labelObject.GetComponent<Text>();
        label.text = "Silenciar ambiente";
        label.alignment = TextAnchor.MiddleCenter;
        label.font = Resources.GetBuiltinResource<Font>("Roboto.ttf");
        label.fontSize = 20;
        label.color = Color.white;
        label.raycastTarget = false;

        AmbientAudioTogglePlaceholder control = buttonObject.GetComponent<AmbientAudioTogglePlaceholder>();
        control.label = label;
        control.RefreshLabel();
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
        if (point != null)
        {
            if (activeSource == null)
                activeSource = point.AddComponent<AudioSource>();

            if (activeSource.clip == null)
                activeSource.clip = GetGeneratedAmbient();

            activeSource.playOnAwake = false;
            activeSource.loop = true;
            activeSource.spatialBlend = 0f;
            activeSource.volume = 0.18f;
            activeSource.mute = muted;
            activeSource.Play();
        }

        RefreshLabel();
    }

    private void ToggleAudio()
    {
        muted = !muted;
        if (activeSource != null)
            activeSource.mute = muted;
        RefreshLabel();
    }

    private void RefreshLabel()
    {
        string text = activePoint == null
            ? "Ambiente desactivado"
            : muted ? "Activar ambiente" : "Silenciar ambiente";
        if (label != null) label.text = text;
        if (tmpLabel != null) tmpLabel.text = text;
    }

    private static GameObject FindActivePoint()
    {
        string[] pointNames = { "Tunda", "Mohan", "Bachue", "MadreMonte", "Silbon" };
        foreach (string pointName in pointNames)
        {
            GameObject point = GameObject.Find(pointName);
            if (point != null && point.activeInHierarchy)
                return point;
        }

        return null;
    }

    private static AudioClip GetGeneratedAmbient()
    {
        if (generatedAmbient != null)
            return generatedAmbient;

        const int sampleRate = 22050;
        const int durationSeconds = 8;
        int sampleCount = sampleRate * durationSeconds;
        float[] samples = new float[sampleCount];
        System.Random random = new System.Random(31031);
        float filteredNoise = 0f;

        for (int i = 0; i < samples.Length; i++)
        {
            float whiteNoise = (float)(random.NextDouble() * 2.0 - 1.0);
            filteredNoise = filteredNoise * 0.985f + whiteNoise * 0.015f;
            float time = (float)i / sampleRate;
            float swell = 0.65f + 0.35f * Mathf.Sin(time * Mathf.PI * 2f / durationSeconds);
            samples[i] = filteredNoise * swell * 0.35f;
        }

        generatedAmbient = AudioClip.Create("Ambiente de prueba", sampleCount, 1, sampleRate, false);
        generatedAmbient.SetData(samples, 0);
        return generatedAmbient;
    }
}
