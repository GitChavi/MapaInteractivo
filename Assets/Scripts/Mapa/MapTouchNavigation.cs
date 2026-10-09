using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;

/// <summary>
/// Permite explorar el mapa mediante arrastre táctil o con el botón izquierdo
/// del mouse, y acercar/alejar con pellizco o rueda del mouse. Suaviza los
/// movimientos para evitar saltos y limita el zoom al rango configurado.
/// </summary>
public sealed class MapTouchNavigation : MonoBehaviour
{
    [SerializeField] private float minimumZoom = 1f;
    [SerializeField] private float maximumZoom = 3.5f;
    [SerializeField] private float dragSmoothing = 20f;
    [SerializeField] private float mouseZoomStep = 1.15f;

    private RectTransform map;
    private Vector2 targetPosition;
    private float targetScale = 1f;
    private float previousPinchDistance;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void RegisterSceneHook()
    {
        SceneManager.sceneLoaded += AttachToMap;
    }

    private static void AttachToMap(Scene scene, LoadSceneMode mode)
    {
        GameObject mapObject = GameObject.Find("mapa");
        if (mapObject == null || mapObject.GetComponent<MapTouchNavigation>() != null)
            return;

        mapObject.AddComponent<MapTouchNavigation>();
    }

    private void Awake()
    {
        map = transform as RectTransform;
        if (map == null)
        {
            enabled = false;
            return;
        }

        targetPosition = map.anchoredPosition;
        targetScale = map.localScale.x;
    }

    private void Update()
    {
        Touchscreen screen = Touchscreen.current;
        int activeTouches = 0;
        TouchControl first = null;
        TouchControl second = null;
        if (screen != null)
        {
            foreach (TouchControl touch in screen.touches)
            {
                if (!touch.press.isPressed)
                    continue;

                if (activeTouches == 0) first = touch;
                else if (activeTouches == 1) second = touch;
                activeTouches++;
            }
        }

        if (activeTouches == 1 && first != null)
        {
            targetPosition += first.delta.ReadValue() / CanvasScale();
            previousPinchDistance = 0f;
        }
        else if (activeTouches >= 2 && first != null && second != null)
        {
            float distance = Vector2.Distance(first.position.ReadValue(), second.position.ReadValue());
            if (previousPinchDistance > 0f)
                targetScale = Mathf.Clamp(targetScale * distance / previousPinchDistance, minimumZoom, maximumZoom);
            previousPinchDistance = distance;
        }
        else
        {
            previousPinchDistance = 0f;
        }

        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            Vector2 scroll = mouse.scroll.ReadValue();
            if (!Mathf.Approximately(scroll.y, 0f))
                targetScale = Mathf.Clamp(targetScale * Mathf.Pow(mouseZoomStep, scroll.y / 120f), minimumZoom, maximumZoom);

            if (mouse.leftButton.isPressed)
                targetPosition += mouse.delta.ReadValue() / CanvasScale();
        }

        float blend = 1f - Mathf.Exp(-dragSmoothing * Time.unscaledDeltaTime);
        map.anchoredPosition = Vector2.Lerp(map.anchoredPosition, targetPosition, blend);
        float scale = Mathf.Lerp(map.localScale.x, targetScale, blend);
        map.localScale = new Vector3(scale, scale, 1f);
    }

    private float CanvasScale()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        return canvas != null ? Mathf.Max(canvas.scaleFactor, 0.01f) : 1f;
    }
}
