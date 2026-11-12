using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>Datos de una leyenda y del objeto que se recupera al completarla.</summary>
[Serializable]
public sealed class LeyendaInfo
{
    [Tooltip("Identificador interno (sin espacios). Se usa para guardar el progreso.")]
    public string id;
    public string nombreLeyenda;
    public string region;
    public string objeto;
    [TextArea(2, 4)] public string descripcion;
    public Sprite icono;
    [Tooltip("Pantalla de recompensa de la leyenda. Cuando se muestra, la leyenda cuenta como completada.")]
    public GameObject pantallaRecompensa;
}

/// <summary>
/// Lleva el registro de las leyendas completadas y de los objetos del inventario.
/// No hace falta tocar los minijuegos: una leyenda se completa sola cuando su pantalla de
/// recompensa aparece en pantalla.
/// </summary>
public sealed class ProgresoLeyendas : MonoBehaviour
{
    private const string ClaveGuardado = "MapaInteractivo.LeyendasCompletadas";

    public static ProgresoLeyendas Instancia { get; private set; }

    [SerializeField] private LeyendaInfo[] leyendas = Array.Empty<LeyendaInfo>();
    [Tooltip("Si está activo, el progreso se conserva al cerrar la aplicación. " +
             "Desactivado = cada visitante empieza de cero.")]
    [SerializeField] private bool guardarEntreSesiones;

    private readonly HashSet<string> completadas = new HashSet<string>();
    private readonly HashSet<string> sinVer = new HashSet<string>();

    /// <summary>Se dispara cuando cambia cualquier dato del progreso.</summary>
    public event Action Cambio;
    /// <summary>Se dispara al completar una leyenda nueva.</summary>
    public event Action<LeyendaInfo> LeyendaCompletada;

    public IReadOnlyList<LeyendaInfo> Leyendas => leyendas;
    public int Total => leyendas.Length;
    public int Completadas => completadas.Count;
    public float Progreso => Total == 0 ? 0f : (float)Completadas / Total;
    public int NuevosSinVer => sinVer.Count;

    public bool EstaCompletada(LeyendaInfo leyenda) => leyenda != null && completadas.Contains(leyenda.id);
    public bool EsNueva(LeyendaInfo leyenda) => leyenda != null && sinVer.Contains(leyenda.id);

    private void Awake()
    {
        Instancia = this;
        if (guardarEntreSesiones) Cargar();
    }

    private void OnDestroy()
    {
        if (Instancia == this) Instancia = null;
    }

    private void Update()
    {
        foreach (LeyendaInfo leyenda in leyendas)
        {
            if (leyenda.pantallaRecompensa != null && leyenda.pantallaRecompensa.activeInHierarchy && !EstaCompletada(leyenda))
                Completar(leyenda.id);
        }
    }

    /// <summary>Marca una leyenda como completada (también se puede llamar desde un botón).</summary>
    public void Completar(string id)
    {
        LeyendaInfo leyenda = Array.Find(leyendas, l => l.id == id);
        if (leyenda == null || !completadas.Add(id)) return;
        sinVer.Add(id);
        if (guardarEntreSesiones) Guardar();
        LeyendaCompletada?.Invoke(leyenda);
        Cambio?.Invoke();
    }

    /// <summary>Al abrir el inventario los objetos dejan de marcarse como nuevos.</summary>
    public void MarcarVistos()
    {
        if (sinVer.Count == 0) return;
        sinVer.Clear();
        Cambio?.Invoke();
    }

    [ContextMenu("Reiniciar progreso")]
    public void Reiniciar()
    {
        completadas.Clear();
        sinVer.Clear();
        PlayerPrefs.DeleteKey(ClaveGuardado);
        Cambio?.Invoke();
    }

    [ContextMenu("Completar todas (prueba)")]
    private void CompletarTodas()
    {
        foreach (LeyendaInfo leyenda in leyendas) Completar(leyenda.id);
    }

    private void Guardar()
    {
        PlayerPrefs.SetString(ClaveGuardado, string.Join(",", completadas));
        PlayerPrefs.Save();
    }

    private void Cargar()
    {
        string guardado = PlayerPrefs.GetString(ClaveGuardado, string.Empty);
        foreach (string id in guardado.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            if (Array.Exists(leyendas, l => l.id == id)) completadas.Add(id);
    }
}
