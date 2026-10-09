using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Añade un botón visible para cerrar el punto narrativo activo y mostrar de
/// nuevo el mapa, conservando el estado que el mapa tenía antes de abrirlo.
/// El botón se conecta al Canvas cuando se carga la escena.
/// </summary>
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
        if (canvas == null)
            return;

        Transform existing = canvas.transform.Find("ReturnToMapPlaceholder");
        if (existing != null)
        {
            Button existingButton = existing.GetComponent<Button>();
            if (existingButton != null)
            {
                existingButton.onClick.RemoveListener(ReturnToMap);
                existingButton.onClick.AddListener(ReturnToMap);
            }
            return;
        }

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
        label.text = "Volver al mapa";
        label.alignment = TextAnchor.MiddleCenter;
        label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        label.fontSize = 22;
        label.color = Color.white;
        label.raycastTarget = false;
    }

    private static void ReturnToMap()
    {
        GameObject map = GameObject.Find("mapa");
        if (map != null)
            map.SetActive(true);

        string[] popupNames = { "Tunda", "Mohan", "Bachue", "MadreMonte", "Silbon" };
        foreach (string popupName in popupNames)
        {
            GameObject popup = GameObject.Find(popupName);
            if (popup != null)
                popup.SetActive(false);
        }
    }
}
