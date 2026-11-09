
using UnityEngine;
using UnityEngine.EventSystems;

public class DragBasura : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform rectTransform;
    private Canvas canvas;

    private Transform posicionOriginal;
    private Vector2 posicionInicial;

    public CestoBasura cesto;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();

        posicionOriginal = transform.parent;
        posicionInicial = rectTransform.anchoredPosition;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        // Llevar la basura al frente mientras se arrastra
        transform.SetParent(canvas.transform);
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Mover la basura siguiendo el mouse
        rectTransform.anchoredPosition +=
            eventData.delta / canvas.scaleFactor;
    }



    public void OnEndDrag(PointerEventData eventData)
    {
        Debug.Log("Se soltó la basura: " + gameObject.name);

        if (cesto == null)
        {
            Debug.LogError("No se ha asignado el cesto a esta basura.");
            return;
        }

        if (cesto.EstaDentroDelCesto(rectTransform))
        {
            Debug.Log("¡Basura depositada correctamente!");

            cesto.RegistrarBasura();

            Destroy(gameObject);
        }
        else
        {
            Debug.Log("La basura se soltó fuera del cesto.");

            transform.SetParent(posicionOriginal);
            rectTransform.anchoredPosition = posicionInicial;
        }
    }


}
