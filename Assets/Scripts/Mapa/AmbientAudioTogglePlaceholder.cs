using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Visible mute/unmute scaffold for point ambience.</summary>
public sealed class AmbientAudioTogglePlaceholder : MonoBehaviour
{
    private Text label;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneHook()
    {
        SceneManager.sceneLoaded += AddControl;
    }

    private static void AddControl(Scene scene, LoadSceneMode mode)
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null || canvas.transform.Find("AmbientAudioTogglePlaceholder") != null)
            return;

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
        label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        label.fontSize = 20;
        label.color = Color.white;
        label.raycastTarget = false;

        AmbientAudioTogglePlaceholder control = buttonObject.GetComponent<AmbientAudioTogglePlaceholder>();
        control.label = label;
        control.RefreshLabel();
    }

    private void ToggleAudio()
    {
        AudioSource source = FindAmbientSource();
        if (source == null)
        {
            Debug.Log("Placeholder: asignar el AudioSource ambiental del punto.");
            return;
        }

        source.mute = !source.mute;
        RefreshLabel(source.mute);
    }

    private void RefreshLabel()
    {
        AudioSource source = FindAmbientSource();
        RefreshLabel(source != null && source.mute);
    }

    private void RefreshLabel(bool muted)
    {
        if (label != null)
            label.text = muted ? "Activar ambiente" : "Silenciar ambiente";
    }

    private static AudioSource FindAmbientSource()
    {
        foreach (AudioSource source in Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
        {
            string name = source.gameObject.name;
            if (name.IndexOf("ambient", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("ambiente", StringComparison.OrdinalIgnoreCase) >= 0)
                return source;
        }

        return null;
    }
}
