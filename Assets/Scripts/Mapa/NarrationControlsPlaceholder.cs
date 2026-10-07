using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Creates the pause and repeat controls used by a point narration.</summary>
public sealed class NarrationControlsPlaceholder : MonoBehaviour
{
    private bool paused;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneHook()
    {
        SceneManager.sceneLoaded += AddControls;
    }

    private static void AddControls(Scene scene, LoadSceneMode mode)
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null || canvas.transform.Find("NarrationControlsPlaceholder") != null)
            return;

        GameObject group = new GameObject("NarrationControlsPlaceholder", typeof(RectTransform));
        RectTransform groupRect = group.GetComponent<RectTransform>();
        groupRect.SetParent(canvas.transform, false);
        groupRect.anchorMin = new Vector2(0.5f, 0f);
        groupRect.anchorMax = new Vector2(0.5f, 0f);
        groupRect.pivot = new Vector2(0.5f, 0f);
        groupRect.anchoredPosition = new Vector2(0f, 28f);
        groupRect.sizeDelta = new Vector2(420f, 58f);

        CreateButton(groupRect, "PauseNarration", "Pausar", new Vector2(0f, 0f), TogglePause);
        CreateButton(groupRect, "RepeatNarration", "Repetir", new Vector2(210f, 0f), Repeat);
    }

    private static void CreateButton(RectTransform parent, string name, string title, Vector2 position, Action action)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0f, 0.5f);
        rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(190f, 54f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.12f, 0.18f, 0.14f, 0.92f);
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => action());

        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.SetParent(rect, false);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(8f, 4f);
        labelRect.offsetMax = new Vector2(-8f, -4f);
        Text label = labelObject.GetComponent<Text>();
        label.text = title;
        label.alignment = TextAnchor.MiddleCenter;
        label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        label.fontSize = 22;
        label.color = Color.white;
        label.raycastTarget = false;
    }

    private static void TogglePause()
    {
        AudioSource source = FindNarrationSource();
        if (source == null)
        {
            Debug.Log("Placeholder: asignar el AudioSource de la narración del punto.");
            return;
        }

        NarrationControlsPlaceholder control = source.GetComponent<NarrationControlsPlaceholder>();
        if (control == null)
            control = source.gameObject.AddComponent<NarrationControlsPlaceholder>();

        control.paused = !control.paused;
        if (control.paused) source.Pause();
        else source.UnPause();
    }

    private static void Repeat()
    {
        AudioSource source = FindNarrationSource();
        if (source == null)
        {
            Debug.Log("Placeholder: asignar el AudioSource de la narración del punto.");
            return;
        }

        source.time = 0f;
        source.Play();
    }

    private static AudioSource FindNarrationSource()
    {
        foreach (AudioSource source in Object.FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
        {
            string name = source.gameObject.name;
            if (name.IndexOf("narr", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("relato", StringComparison.OrdinalIgnoreCase) >= 0 ||
                name.IndexOf("voz", StringComparison.OrdinalIgnoreCase) >= 0)
                return source;
        }

        return null;
    }
}
