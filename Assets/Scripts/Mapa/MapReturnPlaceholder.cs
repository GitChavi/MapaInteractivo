using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>Visible return control scaffold for the immersive point experience.</summary>
public sealed class MapReturnPlaceholder : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneHook()
    {
        SceneManager.sceneLoaded += AddPlaceholder;
    }

    private static void AddPlaceholder(Scene scene, LoadSceneMode mode)
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null || canvas.transform.Find("ReturnToMapPlaceholder") != null)
            return;

        GameObject buttonObject = new GameObject("ReturnToMapPlaceholder", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.SetParent(canvas.transform, false);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(24f, -24f);
        rect.sizeDelta = new Vector2(210f, 56f);

        Image background = buttonObject.GetComponent<Image>();
        background.color = new Color(0.12f, 0.18f, 0.14f, 0.92f);
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = background;
        button.onClick.AddListener(ReturnToMap);

        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.SetParent(rect, false);
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(8f, 4f);
        labelRect.offsetMax = new Vector2(-8f, -4f);
        Text label = labelObject.GetComponent<Text>();
        label.text = "← Volver al mapa";
        label.alignment = TextAnchor.MiddleCenter;
        label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        label.fontSize = 22;
        label.color = Color.white;
        label.raycastTarget = false;
    }

    private static void ReturnToMap()
    {
        // Placeholder hook: future immersive point panels will be closed here.
        GameObject map = GameObject.Find("mapa");
        if (map != null)
            map.SetActive(true);

        Debug.Log("Placeholder: conectar este botón con el retorno al mapa y conservar el progreso.");
    }
}
