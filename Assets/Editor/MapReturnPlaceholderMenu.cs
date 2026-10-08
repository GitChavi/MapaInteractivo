using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class MapReturnPlaceholderMenu
{
    [MenuItem("Tools/Mapa Interactivo/Agregar botón de volver (HU-1.4.1)")]
    private static void AddReturnButton()
    {
        Canvas canvas = Object.FindAnyObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("Abre la escena MapaInteractivo antes de agregar el placeholder.");
            return;
        }

        Transform existing = canvas.transform.Find("ReturnToMapPlaceholder");
        if (existing != null)
        {
            Selection.activeGameObject = existing.gameObject;
            Debug.Log("El placeholder de volver ya está en la jerarquía.");
            return;
        }

        GameObject buttonObject = new GameObject(
            "ReturnToMapPlaceholder",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Button),
            typeof(MapReturnPlaceholder));
        Undo.RegisterCreatedObjectUndo(buttonObject, "Agregar botón placeholder de volver");

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.SetParent(canvas.transform, false);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(24f, -24f);
        rect.sizeDelta = new Vector2(240f, 64f);

        Image background = buttonObject.GetComponent<Image>();
        background.color = new Color(0.12f, 0.18f, 0.14f, 0.94f);
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = background;

        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(CanvasRenderer), typeof(Text));
        Undo.RegisterCreatedObjectUndo(labelObject, "Agregar texto al botón placeholder");
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

        Selection.activeGameObject = buttonObject;
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Debug.Log("Placeholder agregado a Canvas. Guarda la escena para conservarlo.");
    }
}
