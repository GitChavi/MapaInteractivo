using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// Reproductor de video de la pantalla de cada leyenda: portada con botón grande de reproducir,
/// barra de controles (reproducir/pausar, progreso que se puede tocar para saltar y tiempo).
/// Si no hay video asignado muestra un aviso en lugar de los controles.
/// </summary>
[RequireComponent(typeof(VideoPlayer), typeof(AudioSource))]
public sealed class ReproductorVideoLeyenda : MonoBehaviour
{
    private static readonly HashSet<ReproductorVideoLeyenda> Reproduciendo = new HashSet<ReproductorVideoLeyenda>();

    /// <summary>Algún video está sonando (el ambiente baja su volumen).</summary>
    public static bool AlgunoReproduciendo => Reproduciendo.Count > 0;

    [SerializeField] private VideoClip video;
    [SerializeField] private RawImage pantalla;
    [Tooltip("Capa con el botón grande de reproducir (antes de empezar y al terminar).")]
    [SerializeField] private GameObject portada;
    [Tooltip("Aviso que se muestra si no hay video asignado.")]
    [SerializeField] private GameObject sinVideo;
    [SerializeField] private GameObject controles;
    [SerializeField] private GameObject iconoReproducir;
    [SerializeField] private GameObject iconoPausa;
    [SerializeField] private RectTransform rellenoProgreso;
    [SerializeField] private TMP_Text tiempo;

    private VideoPlayer reproductor;
    private RenderTexture textura;
    private bool primerCuadro;

    private void Awake()
    {
        reproductor = GetComponent<VideoPlayer>();
        AudioSource audioVideo = GetComponent<AudioSource>();
        reproductor.playOnAwake = false;
        reproductor.isLooping = false;
        reproductor.renderMode = VideoRenderMode.RenderTexture;
        reproductor.audioOutputMode = VideoAudioOutputMode.AudioSource;
        reproductor.controlledAudioTrackCount = 1;
        reproductor.EnableAudioTrack(0, true);
        reproductor.SetTargetAudioSource(0, audioVideo);
        audioVideo.playOnAwake = false;
        reproductor.loopPointReached += AlTerminar;
    }

    private void OnEnable()
    {
        bool hayVideo = video != null;
        if (sinVideo != null) sinVideo.SetActive(!hayVideo);
        if (controles != null) controles.SetActive(hayVideo);
        if (portada != null) portada.SetActive(hayVideo);
        primerCuadro = false;
        if (pantalla != null)
        {
            pantalla.texture = null;
            pantalla.color = Color.black;
        }
        ActualizarControles(0.0);
    }

    private void OnDisable()
    {
        // Al salir de la pantalla el video se detiene y libera memoria.
        if (reproductor != null) reproductor.Stop();
        Reproduciendo.Remove(this);
        if (textura != null)
        {
            textura.Release();
            Destroy(textura);
            textura = null;
        }
    }

    private void Update()
    {
        if (video == null) return;

        bool sonando = reproductor.isPlaying;
        if (sonando) Reproduciendo.Add(this);
        else Reproduciendo.Remove(this);

        if (!primerCuadro && sonando && reproductor.frame > 0 && pantalla != null)
        {
            primerCuadro = true;
            pantalla.color = Color.white;
        }

        if (iconoReproducir != null) iconoReproducir.SetActive(!sonando);
        if (iconoPausa != null) iconoPausa.SetActive(sonando);
        ActualizarControles(reproductor.time);
    }

    public void AlternarReproduccion()
    {
        if (video == null) return;
        if (reproductor.isPlaying) reproductor.Pause();
        else Reproducir();
    }

    public void Reproducir()
    {
        if (video == null) return;
        if (textura == null)
        {
            textura = new RenderTexture((int)video.width, (int)video.height, 0) { name = "Video " + video.name };
            textura.Create();
        }
        reproductor.clip = video;
        reproductor.targetTexture = textura;
        if (pantalla != null) pantalla.texture = textura;
        if (portada != null) portada.SetActive(false);
        reproductor.Play();
    }

    /// <summary>Salta a una posición del video (0 = inicio, 1 = final).</summary>
    public void Buscar(float normalizado)
    {
        if (video == null) return;
        if (!reproductor.isPlaying && !reproductor.isPaused) Reproducir();
        reproductor.time = Mathf.Clamp01(normalizado) * video.length;
        ActualizarControles(reproductor.time);
    }

    private void AlTerminar(VideoPlayer _)
    {
        reproductor.Stop();
        Reproduciendo.Remove(this);
        if (portada != null) portada.SetActive(true);
        ActualizarControles(0.0);
    }

    private void ActualizarControles(double segundos)
    {
        double total = video != null ? video.length : 0.0;
        if (rellenoProgreso != null)
        {
            Vector2 max = rellenoProgreso.anchorMax;
            max.x = total > 0 ? Mathf.Clamp01((float)(segundos / total)) : 0f;
            rellenoProgreso.anchorMax = max;
        }
        if (tiempo != null) tiempo.text = Formato(segundos) + " / " + Formato(total);
    }

    private static string Formato(double segundos)
    {
        int s = Mathf.Max(0, Mathf.FloorToInt((float)segundos));
        return (s / 60) + ":" + (s % 60).ToString("00");
    }
}
