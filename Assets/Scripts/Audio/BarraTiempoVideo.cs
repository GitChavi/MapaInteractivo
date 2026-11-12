using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>Barra de progreso del video: tocarla o arrastrar sobre ella salta a ese punto.</summary>
public sealed class BarraTiempoVideo : MonoBehaviour, IPointerDownHandler, IDragHandler
{
    [SerializeField] private ReproductorVideoLeyenda reproductor;

    public void OnPointerDown(PointerEventData e) => Saltar(e);
    public void OnDrag(PointerEventData e) => Saltar(e);

    private void Saltar(PointerEventData e)
    {
        RectTransform rect = (RectTransform)transform;
        if (reproductor == null ||
            !RectTransformUtility.ScreenPointToLocalPointInRectangle(rect, e.position, e.pressEventCamera, out Vector2 local))
            return;
        reproductor.Buscar((local.x - rect.rect.xMin) / rect.rect.width);
    }
}
