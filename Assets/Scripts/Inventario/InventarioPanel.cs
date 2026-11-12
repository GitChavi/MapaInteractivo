using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Inventario de objetos recuperados en las leyendas. Muestra una tarjeta por leyenda
/// (obtenida o bloqueada) y el detalle del objeto seleccionado.
/// </summary>
public sealed class InventarioPanel : MonoBehaviour
{
    [Serializable]
    public struct Paleta
    {
        public Color dorado;
        public Color crema;
        public Color apagado;
        public Color textoApagado;
        public Color verde;
        public Color fondoBloqueado;
    }

    [SerializeField] private ProgresoLeyendas progreso;
    [Tooltip("Raíz del panel modal (se activa y desactiva).")]
    [SerializeField] private GameObject panel;
    [SerializeField] private RanuraInventario[] ranuras;

    [Header("Botón del mapa")]
    [SerializeField] private Button botonAbrir;
    [SerializeField] private GameObject indicadorNuevos;
    [SerializeField] private TMP_Text numeroNuevos;

    [Header("Detalle del objeto")]
    [SerializeField] private Image detalleIcono;
    [SerializeField] private GameObject detalleBloqueado;
    [SerializeField] private Image detalleAnillo;
    [SerializeField] private TMP_Text detalleTitulo;
    [SerializeField] private TMP_Text detalleTexto;

    [SerializeField] private Paleta paleta = new Paleta
    {
        dorado = new Color32(216, 191, 120, 255),
        crema = new Color32(237, 227, 204, 255),
        apagado = new Color32(74, 64, 54, 255),
        textoApagado = new Color32(140, 128, 108, 255),
        verde = new Color32(90, 110, 88, 255),
        fondoBloqueado = new Color32(52, 45, 38, 255),
    };

    private readonly HashSet<string> nuevosAlAbrir = new HashSet<string>();
    private int seleccionada = -1;

    public bool Abierto => panel != null && panel.activeSelf;

    private void Awake()
    {
        if (progreso == null) progreso = ProgresoLeyendas.Instancia;
        for (int i = 0; i < ranuras.Length; i++)
        {
            int indice = i;
            if (ranuras[i] != null && ranuras[i].Boton != null)
                ranuras[i].Boton.onClick.AddListener(() => Seleccionar(indice));
        }
        if (panel != null) panel.SetActive(false);
    }

    private void OnEnable()
    {
        if (progreso == null) return;
        progreso.Cambio += Refrescar;
        Refrescar();
    }

    private void OnDisable()
    {
        if (progreso != null) progreso.Cambio -= Refrescar;
    }

    public void Abrir()
    {
        if (panel == null || progreso == null) return;
        nuevosAlAbrir.Clear();
        foreach (LeyendaInfo info in progreso.Leyendas)
            if (progreso.EsNueva(info)) nuevosAlAbrir.Add(info.id);

        panel.SetActive(true);
        // Al abrir se muestra el objeto más reciente, o el primero obtenido.
        seleccionada = PrimerIndice(info => nuevosAlAbrir.Contains(info.id));
        if (seleccionada < 0) seleccionada = PrimerIndice(progreso.EstaCompletada);
        progreso.MarcarVistos();
        Refrescar();
    }

    public void Cerrar()
    {
        if (panel != null) panel.SetActive(false);
    }

    public void Alternar()
    {
        if (Abierto) Cerrar();
        else Abrir();
    }

    public void Seleccionar(int indice)
    {
        seleccionada = indice;
        Refrescar();
    }

    private void Refrescar()
    {
        if (progreso == null) return;

        IReadOnlyList<LeyendaInfo> leyendas = progreso.Leyendas;
        for (int i = 0; i < ranuras.Length; i++)
        {
            if (ranuras[i] == null) continue;
            bool existe = i < leyendas.Count;
            ranuras[i].gameObject.SetActive(existe);
            if (!existe) continue;
            LeyendaInfo info = leyendas[i];
            ranuras[i].Mostrar(info, progreso.EstaCompletada(info), nuevosAlAbrir.Contains(info.id), paleta);
            ranuras[i].Seleccionar(i == seleccionada);
        }

        MostrarDetalle(seleccionada >= 0 && seleccionada < leyendas.Count ? leyendas[seleccionada] : null);

        int nuevos = progreso.NuevosSinVer;
        if (indicadorNuevos != null) indicadorNuevos.SetActive(nuevos > 0);
        if (numeroNuevos != null) numeroNuevos.text = nuevos.ToString();
    }

    private void MostrarDetalle(LeyendaInfo info)
    {
        bool obtenido = info != null && progreso.EstaCompletada(info);
        if (detalleIcono != null)
        {
            detalleIcono.sprite = info?.icono;
            detalleIcono.gameObject.SetActive(obtenido && info.icono != null);
        }
        if (detalleBloqueado != null) detalleBloqueado.SetActive(!obtenido);
        if (detalleAnillo != null) detalleAnillo.color = obtenido ? paleta.dorado : paleta.apagado;

        if (info == null)
        {
            Escribir(detalleTitulo, "Tu colección");
            Escribir(detalleTexto, "Completa las leyendas del mapa para recuperar sus objetos. Toca una tarjeta para ver su historia.");
        }
        else if (obtenido)
        {
            Escribir(detalleTitulo, info.objeto);
            Escribir(detalleTexto, info.descripcion + "\n<color=#A89C86><size=85%>Recuperado en " + info.nombreLeyenda +
                                   " · Región " + info.region + "</size></color>");
        }
        else
        {
            Escribir(detalleTitulo, "Objeto por descubrir");
            Escribir(detalleTexto, "Completa la leyenda de " + info.nombreLeyenda + " en la región " + info.region +
                                   " para recuperar este objeto.");
        }
    }

    private int PrimerIndice(Predicate<LeyendaInfo> condicion)
    {
        IReadOnlyList<LeyendaInfo> leyendas = progreso.Leyendas;
        for (int i = 0; i < leyendas.Count; i++)
            if (condicion(leyendas[i])) return i;
        return -1;
    }

    private static void Escribir(TMP_Text texto, string valor)
    {
        if (texto != null) texto.text = valor;
    }
}
