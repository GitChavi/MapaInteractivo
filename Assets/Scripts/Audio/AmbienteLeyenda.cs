using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Paisaje sonoro de una leyenda mientras cualquiera de sus pantallas está abierta.
///
/// H.U. 5.1.2–5.1.5: el territorio suena en "Olvido" hasta completar la leyenda; al completarla
/// el audio pasa a "Resonancia" con un fundido de 3 segundos, y se queda así en las siguientes visitas.
/// Para las leyendas sin estados (Silbón, Bachué) se deja vacío el clip de completado.
/// Mientras suena un video el ambiente baja para no taparlo.
/// </summary>
public sealed class AmbienteLeyenda : MonoBehaviour
{
    private static readonly List<AmbienteLeyenda> Todos = new List<AmbienteLeyenda>();

    /// <summary>Silencio global del ambiente (botones de sonido del mapa y de las páginas).</summary>
    public static bool Silenciado { get; private set; }
    public static event Action CambioSilencio;

    public static void AlternarSilencio()
    {
        Silenciado = !Silenciado;
        CambioSilencio?.Invoke();
    }

    /// <summary>Hay alguna leyenda abierta con ambiente sonando.</summary>
    public static bool AlgunoAbierto
    {
        get
        {
            foreach (AmbienteLeyenda a in Todos)
                if (a.abierto && a.ambienteInicial != null) return true;
            return false;
        }
    }

    [Tooltip("Id de la leyenda en ProgresoLeyendas (tunda, mohan, madremonte, bachue, silbon).")]
    [SerializeField] private string idLeyenda;
    [Tooltip("Estado de Olvido (o el ambiente normal si la leyenda no tiene estados).")]
    [SerializeField] private AudioClip ambienteInicial;
    [Tooltip("Estado de Resonancia, tras completar la leyenda. Vacío = se mantiene el inicial.")]
    [SerializeField] private AudioClip ambienteCompletado;
    [Tooltip("Pantallas de la leyenda: si alguna está visible, suena el ambiente.")]
    [SerializeField] private GameObject[] pantallas;

    [Range(0f, 1f)] [SerializeField] private float volumen = 0.35f;
    [Tooltip("Duración del paso de Olvido a Resonancia (mínimo 3 s según la H.U. 5.1.5).")]
    [SerializeField] private float duracionTransicion = 3f;
    [SerializeField] private float fundidoApertura = 0.8f;
    [Range(0f, 1f)] [SerializeField] private float volumenDuranteVideo = 0.12f;

    private AudioSource actual;
    private AudioSource saliente;
    private float pesoActual, pesoSaliente;
    private float velActual = 1f, velSaliente = 1f;
    private float factor = 1f;
    private bool abierto;

    private void Awake()
    {
        actual = CrearFuente("Ambiente A");
        saliente = CrearFuente("Ambiente B");
    }

    private void OnEnable() => Todos.Add(this);

    private void OnDisable()
    {
        Todos.Remove(this);
        abierto = false;
        pesoActual = pesoSaliente = 0f;
        if (actual != null) actual.Stop();
        if (saliente != null) saliente.Stop();
    }

    private void Update()
    {
        abierto = false;
        if (pantallas != null)
            foreach (GameObject p in pantallas)
                if (p != null && p.activeInHierarchy) { abierto = true; break; }

        float objetivoActual;
        if (!abierto || ambienteInicial == null)
        {
            objetivoActual = 0f;
            velActual = 1f / Mathf.Max(0.05f, fundidoApertura);
        }
        else
        {
            AudioClip deseado = Completada() && ambienteCompletado != null ? ambienteCompletado : ambienteInicial;
            if (actual.clip != deseado)
            {
                // Si ya sonaba otro estado, es la transición Olvido → Resonancia.
                float duracion = pesoActual > 0.01f ? Mathf.Max(3f, duracionTransicion) : fundidoApertura;
                (actual, saliente) = (saliente, actual);
                (pesoActual, pesoSaliente) = (0f, pesoActual);
                velSaliente = 1f / Mathf.Max(0.05f, duracion);
                velActual = velSaliente;
                actual.clip = deseado;
                actual.time = 0f;
                actual.Play();
            }
            else if (!actual.isPlaying)
            {
                actual.Play();
                velActual = 1f / Mathf.Max(0.05f, fundidoApertura);
            }
            objetivoActual = 1f;
        }

        float dt = Time.unscaledDeltaTime;
        pesoActual = Mathf.MoveTowards(pesoActual, objetivoActual, velActual * dt);
        pesoSaliente = Mathf.MoveTowards(pesoSaliente, 0f, velSaliente * dt);

        float factorObjetivo = Silenciado ? 0f : ReproductorVideoLeyenda.AlgunoReproduciendo ? volumenDuranteVideo : 1f;
        factor = Mathf.MoveTowards(factor, factorObjetivo, 2.5f * dt);

        Aplicar(actual, pesoActual);
        Aplicar(saliente, pesoSaliente);
    }

    private void Aplicar(AudioSource fuente, float peso)
    {
        // Curva de igual potencia para que el fundido cruzado no tenga un bache de volumen.
        fuente.volume = volumen * factor * Mathf.Sin(peso * Mathf.PI * 0.5f);
        if (peso <= 0f && fuente.isPlaying) fuente.Stop();
    }

    private bool Completada()
    {
        ProgresoLeyendas progreso = ProgresoLeyendas.Instancia;
        if (progreso == null || string.IsNullOrEmpty(idLeyenda)) return false;
        foreach (LeyendaInfo info in progreso.Leyendas)
            if (info.id == idLeyenda) return progreso.EstaCompletada(info);
        return false;
    }

    private AudioSource CrearFuente(string nombre)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(transform, false);
        AudioSource fuente = go.AddComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.loop = true;
        fuente.spatialBlend = 0f;
        fuente.volume = 0f;
        return fuente;
    }
}
