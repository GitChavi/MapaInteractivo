using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Botón de sonido de las páginas de cada leyenda (ícono de altavoz). Silencia o activa el
/// ambiente y muestra el ícono que corresponde al estado actual.
/// </summary>
public sealed class IconoSonidoPagina : MonoBehaviour
{
    [Tooltip("Ícono visible cuando el sonido está activo.")]
    [SerializeField] private GameObject activado;
    [Tooltip("Ícono visible cuando el sonido está silenciado.")]
    [SerializeField] private GameObject desactivado;

    private void Awake()
    {
        foreach (GameObject icono in new[] { activado, desactivado })
        {
            Button b = icono != null ? icono.GetComponent<Button>() : null;
            if (b != null) b.onClick.AddListener(AmbienteLeyenda.AlternarSilencio);
        }
    }

    private void OnEnable() => Actualizar();

    private void Update() => Actualizar();

    private void Actualizar()
    {
        bool silenciado = AmbienteLeyenda.Silenciado;
        if (activado != null && activado.activeSelf == silenciado) activado.SetActive(!silenciado);
        if (desactivado != null && desactivado.activeSelf != silenciado) desactivado.SetActive(silenciado);
    }
}
