
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class FogReveal : MonoBehaviour,
    IPointerDownHandler, IDragHandler
{
    [Header("Elementos de interfaz")]
    public RawImage fogImage;
    public TextMeshProUGUI progressText;
    public GameObject completionPanel;

    [Header("Configuracion")]
    public int brushSize = 35;

    private Texture2D fogTexture;
    private Color32[] pixels;

    private int totalFogPixels;
    private int erasedPixels;

    private bool completed = false;

    void Start()
    {
        // Ocultar panel de finalizacion
        completionPanel.SetActive(false);

        // Crear copia editable de la textura original
        Texture2D originalTexture =
            (Texture2D)fogImage.texture;

        fogTexture = Instantiate(originalTexture);

        fogImage.texture = fogTexture;

        // Obtener los pixeles de la niebla
        pixels = fogTexture.GetPixels32();

        // Contar solamente los pixeles visibles
        totalFogPixels = 0;

        foreach (Color32 pixel in pixels)
        {
            if (pixel.a > 0)
            {
                totalFogPixels++;
            }
        }

        erasedPixels = 0;

        progressText.text = "0%";
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        EraseFog(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        EraseFog(eventData);
    }

    void EraseFog(PointerEventData eventData)
    {
        if (completed) return;

        Vector2 localPoint;

        RectTransform rect = fogImage.rectTransform;

        bool inside =
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                rect,
                eventData.position,
                eventData.pressEventCamera,
                out localPoint
            );

        if (!inside || !rect.rect.Contains(localPoint))
            return;

        // Convertir posicion del mouse a coordenadas de textura
        float normalizedX =
            (localPoint.x - rect.rect.xMin) / rect.rect.width;

        float normalizedY =
            (localPoint.y - rect.rect.yMin) / rect.rect.height;

        int centerX = Mathf.RoundToInt(
            normalizedX * (fogTexture.width - 1)
        );

        int centerY = Mathf.RoundToInt(
            normalizedY * (fogTexture.height - 1)
        );

        // Borrar en forma circular
        for (int x = -brushSize; x <= brushSize; x++)
        {
            for (int y = -brushSize; y <= brushSize; y++)
            {
                if (x * x + y * y > brushSize * brushSize)
                    continue;

                int px = centerX + x;
                int py = centerY + y;

                if (px < 0 || px >= fogTexture.width ||
                    py < 0 || py >= fogTexture.height)
                    continue;

                int index = py * fogTexture.width + px;

                if (pixels[index].a > 0)
                {
                    pixels[index].a = 0;
                    erasedPixels++;
                }
            }
        }

        // Aplicar cambios a la textura
        fogTexture.SetPixels32(pixels);
        fogTexture.Apply();

        UpdateProgress();
    }

    void UpdateProgress()
    {
        if (totalFogPixels <= 0) return;

        float percentage =
            (float)erasedPixels / totalFogPixels * 100f;

        percentage = Mathf.Clamp(percentage, 0, 100);

        int displayedPercentage = Mathf.FloorToInt(percentage);

        progressText.text = displayedPercentage + "%";

        // Comprobar si se ha eliminado toda la niebla
        if (percentage >= 99.9f)
        {
            CompleteInteraction();
        }
    }

    void CompleteInteraction()
    {
        if (completed) return;

        completed = true;

        progressText.text = "100%";

        completionPanel.SetActive(true);

        Debug.Log("¡Interacción completada!");
    }

    void OnDestroy()
    {
        if (fogTexture != null)
        {
            Destroy(fogTexture);
        }
    }
}

