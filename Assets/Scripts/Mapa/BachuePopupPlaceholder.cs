using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Construye una ficha para Bachué con la misma composición que los demás popups.</summary>
public sealed class BachuePopupPlaceholder : MonoBehaviour
{
    private void Awake()
    {
        if (transform.Find("BachueInfoPanel") != null)
            return;

        Sprite roundedSprite = CreateRoundedSprite();
        RectTransform panel = CreateImage("BachueInfoPanel", transform, roundedSprite,
            new Color(1f, 0.94f, 0.78f, 1f), new Vector2(1008f, 620f),
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero);

        CreateImage("PortraitPlaceholder", panel, roundedSprite,
            new Color(0.61f, 0.62f, 0.57f, 1f), new Vector2(296f, 476f),
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(38f, 0f));
        CreateLabel(panel, "PortraitLabel", "IMAGEN", 36f, new Color(0.14f, 0.14f, 0.14f),
            new Vector2(38f, 0f), new Vector2(296f, 476f), TextAlignmentOptions.Center,
            new Vector2(0f, 0.5f), new Vector2(0f, 0.5f));

        CreateLabel(panel, "Title", "BACHUÉ", 39f, new Color(0.31f, 0.23f, 0.16f),
            new Vector2(370f, -34f), new Vector2(570f, 68f), TextAlignmentOptions.Left,
            new Vector2(0f, 1f), new Vector2(0f, 1f));
        CreateLabel(panel, "Description",
            "Madre de la humanidad en el mito muisca, asociada a la laguna de Iguaque, en Boyacá.",
            25f, new Color(0.12f, 0.12f, 0.12f),
            new Vector2(370f, -108f), new Vector2(570f, 112f), TextAlignmentOptions.TopLeft,
            new Vector2(0f, 1f), new Vector2(0f, 1f));

        CreateInfoRow(panel, roundedSprite, "REGIÓN", "Andina", -236f);
        CreateInfoRow(panel, roundedSprite, "TIPO DE TERRITORIO", "Laguna de Iguaque (Boyacá)", -348f);
        CreateInfoRow(panel, roundedSprite, "FIGURA ASOCIADA", "Bachué", -460f);

        GameObject closeObject = new GameObject(
            "CloseBachuePopup", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        RectTransform closeRect = closeObject.GetComponent<RectTransform>();
        closeRect.SetParent(panel, false);
        closeRect.anchorMin = closeRect.anchorMax = new Vector2(1f, 1f);
        closeRect.pivot = new Vector2(1f, 1f);
        closeRect.anchoredPosition = new Vector2(-12f, -8f);
        closeRect.sizeDelta = new Vector2(60f, 60f);
        Image closeBackground = closeObject.GetComponent<Image>();
        closeBackground.color = new Color(1f, 1f, 1f, 0f);
        Button closeButton = closeObject.GetComponent<Button>();
        closeButton.targetGraphic = closeBackground;
        closeButton.onClick.AddListener(() => gameObject.SetActive(false));
        CreateLabel(panel, "CloseIcon", "×", 54f, Color.black,
            new Vector2(-15f, -8f), new Vector2(60f, 60f), TextAlignmentOptions.Center,
            new Vector2(1f, 1f), new Vector2(1f, 1f));
    }

    private static void CreateInfoRow(RectTransform parent, Sprite sprite, string heading, string value, float y)
    {
        RectTransform row = CreateImage("Info_" + heading.Replace(" ", ""), parent, sprite,
            new Color(0.91f, 0.85f, 0.67f, 0.88f), new Vector2(570f, 86f),
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(370f, y));
        TMP_Text text = CreateLabel(row, "Text", $"<b>{heading}</b>\n{value}", 22f,
            new Color(0.2f, 0.18f, 0.15f), Vector2.zero, Vector2.zero,
            TextAlignmentOptions.Left, Vector2.zero, Vector2.zero, stretch: true);
        text.margin = new Vector4(58f, 7f, 12f, 5f);
    }

    private static RectTransform CreateImage(
        string objectName, Transform parent, Sprite sprite, Color color, Vector2 size,
        Vector2 anchor, Vector2 pivot, Vector2 position)
    {
        GameObject item = new GameObject(objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        RectTransform rect = item.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Image image = item.GetComponent<Image>();
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
        image.color = color;
        return rect;
    }

    private static TMP_Text CreateLabel(
        Transform parent, string objectName, string value, float fontSize, Color color,
        Vector2 position, Vector2 size, TextAlignmentOptions alignment,
        Vector2 anchor, Vector2 pivot, bool stretch = false)
    {
        GameObject labelObject = new GameObject(
            objectName, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform rect = labelObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        if (stretch)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(8f, 4f);
            rect.offsetMax = new Vector2(-8f, -4f);
        }
        else
        {
            rect.anchorMin = rect.anchorMax = anchor;
            rect.pivot = pivot;
            rect.anchoredPosition = position;
            rect.sizeDelta = size;
        }

        TMP_Text text = labelObject.GetComponent<TMP_Text>();
        text.font = TMP_Settings.defaultFontAsset;
        text.text = value;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.enableAutoSizing = true;
        text.fontSizeMin = fontSize * 0.75f;
        text.fontSizeMax = fontSize;
        text.raycastTarget = false;
        return text;
    }

    private static Sprite CreateRoundedSprite()
    {
        const int size = 64;
        const float radius = 14f;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "BachueRoundedUI";
        texture.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(radius - x, 0f, x - (size - 1 - radius));
                float dy = Mathf.Max(radius - y, 0f, y - (size - 1 - radius));
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(radius + 0.5f - distance);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f),
            100f, 0u, SpriteMeshType.FullRect, new Vector4(20f, 20f, 20f, 20f));
    }
}
