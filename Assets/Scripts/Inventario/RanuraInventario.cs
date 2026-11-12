using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Tarjeta de un objeto dentro del inventario (obtenido o bloqueado).</summary>
public sealed class RanuraInventario : MonoBehaviour
{
    [SerializeField] private Button boton;
    [SerializeField] private Image borde;
    [SerializeField] private Image anillo;
    [SerializeField] private Image icono;
    [SerializeField] private GameObject bloqueado;
    [SerializeField] private TMP_Text nombre;
    [SerializeField] private TMP_Text leyenda;
    [SerializeField] private Image fondoEstado;
    [SerializeField] private TMP_Text textoEstado;
    [SerializeField] private GameObject marcaNueva;
    [SerializeField] private GameObject seleccion;

    public Button Boton => boton;

    public void Mostrar(LeyendaInfo info, bool obtenido, bool nueva, InventarioPanel.Paleta paleta)
    {
        if (icono != null)
        {
            icono.sprite = info.icono;
            icono.gameObject.SetActive(obtenido && info.icono != null);
        }
        if (bloqueado != null) bloqueado.SetActive(!obtenido);
        if (anillo != null) anillo.color = obtenido ? paleta.dorado : paleta.apagado;
        if (borde != null) borde.color = obtenido ? paleta.dorado : paleta.apagado;
        if (nombre != null)
        {
            nombre.text = obtenido ? info.objeto : "? ? ?";
            nombre.color = obtenido ? paleta.crema : paleta.textoApagado;
        }
        if (leyenda != null) leyenda.text = info.nombreLeyenda + "\n<size=80%>" + "Región " + info.region + "</size>";
        if (fondoEstado != null) fondoEstado.color = obtenido ? paleta.verde : paleta.fondoBloqueado;
        if (textoEstado != null)
        {
            textoEstado.text = obtenido ? "OBTENIDO" : "BLOQUEADO";
            textoEstado.color = obtenido ? paleta.crema : paleta.textoApagado;
        }
        if (marcaNueva != null) marcaNueva.SetActive(obtenido && nueva);
    }

    public void Seleccionar(bool activo)
    {
        if (seleccion != null) seleccion.SetActive(activo);
    }
}
