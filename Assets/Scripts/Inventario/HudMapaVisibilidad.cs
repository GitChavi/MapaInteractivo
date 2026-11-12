using UnityEngine;

/// <summary>
/// Muestra el HUD (barra de progreso e inventario) solo mientras se ve el mapa:
/// se desvanece cuando hay abierta alguna pantalla grande de una leyenda.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public sealed class HudMapaVisibilidad : MonoBehaviour
{
    [Tooltip("Pantallas de las leyendas que tapan el mapa (videos, minijuegos, recompensas).")]
    [SerializeField] private GameObject[] ocultarSiActivo;
    [SerializeField] private float velocidad = 6f;

    private CanvasGroup grupo;

    private void Awake()
    {
        grupo = GetComponent<CanvasGroup>();
    }

    private void Update()
    {
        bool visible = true;
        if (ocultarSiActivo != null)
        {
            foreach (GameObject pantalla in ocultarSiActivo)
            {
                if (pantalla != null && pantalla.activeInHierarchy)
                {
                    visible = false;
                    break;
                }
            }
        }

        grupo.alpha = Mathf.MoveTowards(grupo.alpha, visible ? 1f : 0f, velocidad * Time.unscaledDeltaTime);
        grupo.interactable = grupo.blocksRaycasts = visible;
    }
}
