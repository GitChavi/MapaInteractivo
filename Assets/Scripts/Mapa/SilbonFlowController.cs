using System.Collections;
using UnityEngine;

/// <summary>
/// Minijuego de El Silbón dentro de MapaInteractivo.
///
/// Leyenda: si el silbido se oye LEJOS, el Silbón está CERCA; si se oye CERCA, está LEJOS.
/// El jugador toca un punto del llano, escucha el silbido "desde" ese punto (paneo izquierda/derecha
/// según su posición y volumen/filtro según qué tan cerca suena) y debe elegir el punto donde el
/// Silbón está más lejos, es decir, donde el silbido se escucha más cerca.
///
/// Todas las pantallas, popups y botones son objetos editables en la jerarquía
/// (se crean con Tools > Mapa interactivo > Configurar flujo de El Silbón).
/// </summary>
public sealed class SilbonFlowController : MonoBehaviour
{
    [Header("Pantallas editables de El Silbón")]
    [SerializeField] private GameObject pantallaMinijuego;
    [SerializeField] private GameObject pantallaRecompensa;
    [SerializeField] private GameObject popupEscucha;
    [SerializeField] private GameObject popupSalvado;
    [SerializeField] private GameObject popupAgarrado;
    [Tooltip("Pantalla del video (SilbonP2). Si se cierra desde fuera (Volver al mapa), el minijuego también se cierra.")]
    [SerializeField] private GameObject paginaVideo;

    [Header("Puntos del llano (de izquierda a derecha)")]
    [SerializeField] private RectTransform[] puntos;
    [SerializeField] private RectTransform marcadorSeleccion;

    [Header("Respuesta")]
    [Tooltip("Si está activo, en cada intento se reparte al azar dónde está el Silbón.")]
    [SerializeField] private bool aleatorizarCadaIntento = true;
    [Tooltip("Punto correcto (1-6, de izquierda a derecha) cuando no se aleatoriza.")]
    [Range(1, 6)] [SerializeField] private int puntoCorrectoFijo = 4;

    [Header("Audio")]
    [Tooltip("Sonido del silbido. Si se deja vacío se usa un silbido sintetizado de prueba.")]
    [SerializeField] private AudioClip silbido;
    [Tooltip("Tramos (inicio, fin en segundos) del archivo que contienen un silbido. En cada toque se usa uno al azar. " +
             "Vacío = se usa el archivo completo.")]
    [SerializeField] private Vector2[] tramosSilbido = new Vector2[0];
    [Tooltip("Sube el volumen de cada tramo hasta un pico uniforme (la grabación es muy baja).")]
    [SerializeField] private bool normalizarSilbido = true;
    [SerializeField] private AudioSource fuenteSilbido;
    [SerializeField] private AudioSource ambiente;
    [Tooltip("Audios de las pantallas anteriores que se silencian mientras se juega (p. ej. el de SilbonP2).")]
    [SerializeField] private AudioSource[] audiosASilenciar;

    [Header("Sensación de distancia del silbido")]
    [Range(0f, 1f)] [SerializeField] private float volumenSonidoCerca = 1f;
    [Range(0f, 1f)] [SerializeField] private float volumenSonidoLejos = 0.12f;
    [Tooltip("Frecuencia de corte cuando el silbido suena lejos (apagado / sin agudos).")]
    [SerializeField] private float corteSonidoLejos = 900f;
    [Tooltip("Qué tanto se desplaza el sonido a izquierda/derecha según la posición del punto (0-1).")]
    [Range(0f, 1f)] [SerializeField] private float intensidadPaneo = 0.9f;

    [Header("Popup de éxito")]
    [Tooltip("Segundos que se muestra '¡Te has salvado!' antes de pasar a la recompensa (también se puede tocar).")]
    [SerializeField] private float segundosPopupSalvado = 2.5f;

    // 1 = el silbido suena muy cerca (Silbón lejos), 0 = suena muy lejos (Silbón encima).
    private static readonly float[] NivelesIncorrectos = { 0f, 0.15f, 0.3f, 0.45f, 0.6f };

    private float[] cercaniaSilbido;
    private int puntoCorrecto;
    private int puntoSeleccionado = -1;
    private bool flujoActivo;
    private AudioLowPassFilter filtroDistancia;
    private AudioReverbFilter ecoDistancia;
    private AudioClip silbidoSintetico;
    private AudioClip[] silbidosPreparados;
    private Coroutine rutinaSalvado;
    private Coroutine rutinaMarcador;

    private void Awake()
    {
        if (fuenteSilbido == null)
        {
            GameObject go = new GameObject("Audio - Silbido");
            go.transform.SetParent(transform, false);
            fuenteSilbido = go.AddComponent<AudioSource>();
        }

        fuenteSilbido.playOnAwake = false;
        fuenteSilbido.loop = false;
        fuenteSilbido.spatialBlend = 0f;
        filtroDistancia = fuenteSilbido.GetComponent<AudioLowPassFilter>();
        if (filtroDistancia == null) filtroDistancia = fuenteSilbido.gameObject.AddComponent<AudioLowPassFilter>();
        ecoDistancia = fuenteSilbido.GetComponent<AudioReverbFilter>();
        if (ecoDistancia == null) ecoDistancia = fuenteSilbido.gameObject.AddComponent<AudioReverbFilter>();
        ecoDistancia.reverbPreset = AudioReverbPreset.User;
        ecoDistancia.decayTime = 2.4f;
    }

    private void OnEnable()
    {
        flujoActivo = false;
        MostrarSolo(null);
    }

    private void OnDisable()
    {
        DetenerAudio();
        RestaurarAudiosPrevios();
        flujoActivo = false;
    }

    private void Update()
    {
        // El botón general "Volver al mapa" cierra SilbonP2; cerrar también el minijuego.
        if (flujoActivo && paginaVideo != null && !paginaVideo.activeInHierarchy)
            CerrarFlujo();
    }

    // ---------- Navegación ----------

    /// <summary>Botón CONTINUAR de la pantalla del video.</summary>
    public void IniciarMinijuego()
    {
        flujoActivo = true;
        SilenciarAudiosPrevios();
        NuevoIntento();
        if (ambiente != null && ambiente.clip != null) ambiente.Play();
    }

    /// <summary>Botón "Volver atrás": regresa a la pantalla del video.</summary>
    public void VolverAtras()
    {
        CerrarFlujo();
        if (paginaVideo != null) paginaVideo.SetActive(true);
    }

    /// <summary>Botón CONTINUAR de la recompensa: vuelve al mapa.</summary>
    public void ContinuarDesdeRecompensa()
    {
        CerrarFlujo();
        if (paginaVideo != null) paginaVideo.SetActive(false);
    }

    // ---------- Minijuego ----------

    public void ElegirPunto(int indice)
    {
        if (puntos == null || indice < 0 || indice >= puntos.Length) return;
        puntoSeleccionado = indice;
        MoverMarcador(indice);
        if (popupEscucha != null) popupEscucha.SetActive(true);
        ReproducirSilbido();
    }

    /// <summary>Icono de altavoz del popup "Escucha con atención".</summary>
    public void RepetirSilbido() => ReproducirSilbido();

    /// <summary>X del popup "Escucha con atención": permite probar otro punto.</summary>
    public void CerrarPopupEscucha()
    {
        if (popupEscucha != null) popupEscucha.SetActive(false);
        if (fuenteSilbido != null) fuenteSilbido.Stop();
        puntoSeleccionado = -1;
        OcultarMarcador();
    }

    /// <summary>Botón "Comprobar zona".</summary>
    public void ComprobarZona()
    {
        if (puntoSeleccionado < 0) return;
        if (popupEscucha != null) popupEscucha.SetActive(false);
        if (fuenteSilbido != null) fuenteSilbido.Stop();

        if (puntoSeleccionado == puntoCorrecto)
        {
            if (popupSalvado != null)
            {
                popupSalvado.SetActive(true);
                if (rutinaSalvado != null) StopCoroutine(rutinaSalvado);
                rutinaSalvado = StartCoroutine(PasarARecompensa());
            }
            else
            {
                MostrarRecompensa();
            }
        }
        else if (popupAgarrado != null)
        {
            popupAgarrado.SetActive(true);
        }
    }

    /// <summary>Tocar el popup "¡Te has salvado!" pasa de inmediato a la recompensa.</summary>
    public void MostrarRecompensa()
    {
        if (rutinaSalvado != null) { StopCoroutine(rutinaSalvado); rutinaSalvado = null; }
        CerrarPopups();
        OcultarMarcador();
        if (ambiente != null) ambiente.Stop();
        MostrarSolo(pantallaRecompensa);
    }

    /// <summary>Botón REINTENTAR de "El Silbón te ha agarrado".</summary>
    public void Reintentar()
    {
        NuevoIntento();
    }

    // ---------- Lógica interna ----------

    private void NuevoIntento()
    {
        CerrarPopups();
        OcultarMarcador();
        puntoSeleccionado = -1;
        RepartirDistancias();
        MostrarSolo(pantallaMinijuego);
    }

    private void RepartirDistancias()
    {
        int total = puntos != null ? puntos.Length : 0;
        cercaniaSilbido = new float[total];
        if (total == 0) return;

        puntoCorrecto = aleatorizarCadaIntento ? Random.Range(0, total) : Mathf.Clamp(puntoCorrectoFijo - 1, 0, total - 1);

        // Niveles incorrectos barajados para que cada punto suene distinto.
        float[] niveles = (float[])NivelesIncorrectos.Clone();
        for (int i = niveles.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (niveles[i], niveles[j]) = (niveles[j], niveles[i]);
        }

        int n = 0;
        for (int i = 0; i < total; i++)
            cercaniaSilbido[i] = i == puntoCorrecto ? 1f : niveles[n++ % niveles.Length];
    }

    private void ReproducirSilbido()
    {
        if (fuenteSilbido == null || puntoSeleccionado < 0) return;

        AudioClip clip = ObtenerSilbido();
        float cercania = cercaniaSilbido != null && puntoSeleccionado < cercaniaSilbido.Length
            ? cercaniaSilbido[puntoSeleccionado] : 0.5f;

        // Paneo: el silbido viene del lado de la pantalla donde está el punto.
        fuenteSilbido.panStereo = Mathf.Clamp((PosicionHorizontal(puntos[puntoSeleccionado]) * 2f - 1f) * intensidadPaneo, -1f, 1f);
        // Distancia percibida: lejos = bajo volumen, sin agudos y con más eco.
        fuenteSilbido.volume = Mathf.Lerp(volumenSonidoLejos, volumenSonidoCerca, cercania);
        filtroDistancia.cutoffFrequency = Mathf.Exp(Mathf.Lerp(Mathf.Log(corteSonidoLejos), Mathf.Log(22000f), cercania));
        ecoDistancia.dryLevel = 0f;
        ecoDistancia.room = Mathf.Lerp(-400f, -4000f, cercania);
        ecoDistancia.reverbLevel = Mathf.Lerp(200f, -2500f, cercania);

        fuenteSilbido.Stop();
        fuenteSilbido.clip = clip;
        fuenteSilbido.Play();

        if (rutinaMarcador != null) StopCoroutine(rutinaMarcador);
        if (marcadorSeleccion != null) rutinaMarcador = StartCoroutine(PulsarMarcador());
    }

    private float PosicionHorizontal(RectTransform punto)
    {
        RectTransform area = punto != null ? punto.parent as RectTransform : null;
        if (area == null || area.rect.width <= 0f) return 0.5f;
        Vector3 local = area.InverseTransformPoint(punto.position);
        return Mathf.Clamp01((local.x - area.rect.xMin) / area.rect.width);
    }

    private IEnumerator PasarARecompensa()
    {
        yield return new WaitForSeconds(segundosPopupSalvado);
        rutinaSalvado = null;
        MostrarRecompensa();
    }

    private void MoverMarcador(int indice)
    {
        if (marcadorSeleccion == null) return;
        RectTransform punto = puntos[indice];
        marcadorSeleccion.SetParent(punto.parent, false);
        marcadorSeleccion.anchorMin = punto.anchorMin;
        marcadorSeleccion.anchorMax = punto.anchorMax;
        marcadorSeleccion.anchoredPosition = punto.anchoredPosition;
        marcadorSeleccion.sizeDelta = punto.sizeDelta;
        marcadorSeleccion.localScale = Vector3.one;
        marcadorSeleccion.SetSiblingIndex(punto.GetSiblingIndex());
        marcadorSeleccion.gameObject.SetActive(true);
    }

    private IEnumerator PulsarMarcador()
    {
        while (fuenteSilbido != null && fuenteSilbido.isPlaying && marcadorSeleccion != null)
        {
            float s = 1f + 0.12f * Mathf.Sin(Time.unscaledTime * 10f);
            marcadorSeleccion.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        if (marcadorSeleccion != null) marcadorSeleccion.localScale = Vector3.one;
        rutinaMarcador = null;
    }

    private void OcultarMarcador()
    {
        if (rutinaMarcador != null) { StopCoroutine(rutinaMarcador); rutinaMarcador = null; }
        if (marcadorSeleccion != null)
        {
            marcadorSeleccion.localScale = Vector3.one;
            marcadorSeleccion.gameObject.SetActive(false);
        }
    }

    private void CerrarPopups()
    {
        if (popupEscucha != null) popupEscucha.SetActive(false);
        if (popupSalvado != null) popupSalvado.SetActive(false);
        if (popupAgarrado != null) popupAgarrado.SetActive(false);
    }

    private void CerrarFlujo()
    {
        flujoActivo = false;
        if (rutinaSalvado != null) { StopCoroutine(rutinaSalvado); rutinaSalvado = null; }
        CerrarPopups();
        OcultarMarcador();
        MostrarSolo(null);
        DetenerAudio();
        RestaurarAudiosPrevios();
    }

    private void MostrarSolo(GameObject pantalla)
    {
        if (pantallaMinijuego != null) pantallaMinijuego.SetActive(pantalla == pantallaMinijuego);
        if (pantallaRecompensa != null) pantallaRecompensa.SetActive(pantalla == pantallaRecompensa);
        if (pantalla == null) CerrarPopups();
    }

    private void DetenerAudio()
    {
        if (fuenteSilbido != null) fuenteSilbido.Stop();
        if (ambiente != null) ambiente.Stop();
    }

    private void SilenciarAudiosPrevios()
    {
        if (audiosASilenciar == null) return;
        foreach (AudioSource a in audiosASilenciar)
            if (a != null) a.Pause();
    }

    private void RestaurarAudiosPrevios()
    {
        if (audiosASilenciar == null) return;
        foreach (AudioSource a in audiosASilenciar)
            if (a != null && a.isActiveAndEnabled && a.clip != null && a.time > 0f) a.UnPause();
    }

    /// <summary>Un silbido real (tramo al azar, normalizado) o el sintetizado si no hay audio.</summary>
    private AudioClip ObtenerSilbido()
    {
        if (silbido == null) return ObtenerSilbidoSintetico();
        if (silbidosPreparados == null) PrepararSilbidos();
        return silbidosPreparados.Length > 0 ? silbidosPreparados[Random.Range(0, silbidosPreparados.Length)] : silbido;
    }

    private void PrepararSilbidos()
    {
        silbidosPreparados = new AudioClip[0];
        int canales = silbido.channels;
        float[] datos = new float[silbido.samples * canales];
        // GetData solo funciona con clips "Decompress On Load"; si no, se usa el archivo tal cual.
        if (!silbido.GetData(datos, 0)) return;

        Vector2[] tramos = tramosSilbido != null && tramosSilbido.Length > 0
            ? tramosSilbido
            : new[] { new Vector2(0f, silbido.length) };
        var lista = new System.Collections.Generic.List<AudioClip>();
        foreach (Vector2 tramo in tramos)
        {
            int inicio = Mathf.Clamp(Mathf.RoundToInt(tramo.x * silbido.frequency), 0, silbido.samples - 1);
            int fin = Mathf.Clamp(Mathf.RoundToInt(tramo.y * silbido.frequency), inicio + 1, silbido.samples);
            int largo = fin - inicio;
            float[] parte = new float[largo * canales];
            System.Array.Copy(datos, inicio * canales, parte, 0, parte.Length);

            float pico = 0f;
            foreach (float v in parte) pico = Mathf.Max(pico, Mathf.Abs(v));
            float ganancia = normalizarSilbido && pico > 0.0001f ? Mathf.Min(0.9f / pico, 20f) : 1f;
            int fundido = Mathf.Min(largo / 4, silbido.frequency / 25); // 40 ms de entrada y salida
            for (int i = 0; i < largo; i++)
            {
                float env = Mathf.Min(1f, Mathf.Min(i, largo - 1 - i) / (float)Mathf.Max(1, fundido));
                for (int c = 0; c < canales; c++) parte[i * canales + c] *= ganancia * env;
            }

            AudioClip clip = AudioClip.Create(silbido.name + " " + (lista.Count + 1), largo, canales, silbido.frequency, false);
            clip.SetData(parte, 0);
            lista.Add(clip);
        }
        silbidosPreparados = lista.ToArray();
    }

    /// <summary>
    /// Silbido de prueba mientras no se asigne el audio real: las siete notas ascendentes
    /// (do-re-mi-fa-sol-la-si) con las que se suele describir el silbido del Silbón.
    /// </summary>
    private AudioClip ObtenerSilbidoSintetico()
    {
        if (silbidoSintetico != null) return silbidoSintetico;

        const int frecuenciaMuestreo = 44100;
        const float duracionNota = 0.3f;
        float[] notas = { 1046.5f, 1174.7f, 1318.5f, 1396.9f, 1568f, 1760f, 1975.5f };
        int muestrasNota = Mathf.RoundToInt(duracionNota * frecuenciaMuestreo);
        int totalMuestras = muestrasNota * notas.Length + frecuenciaMuestreo / 2;
        float[] datos = new float[totalMuestras];
        float fase = 0f;
        System.Random ruido = new System.Random(7);

        for (int i = 0; i < totalMuestras; i++)
        {
            int nota = Mathf.Min(i / muestrasNota, notas.Length - 1);
            float t = (float)i / frecuenciaMuestreo;
            float enNota = (float)(i % muestrasNota) / muestrasNota;
            // Pequeño glissando hacia la nota siguiente y vibrato, como un silbido humano.
            float destino = nota + 1 < notas.Length ? notas[nota + 1] : notas[nota];
            float freq = Mathf.Lerp(notas[nota], destino, Mathf.SmoothStep(0.85f, 1f, enNota) * 0.5f);
            freq *= 1f + 0.012f * Mathf.Sin(2f * Mathf.PI * 5.5f * t);
            fase += 2f * Mathf.PI * freq / frecuenciaMuestreo;

            float envolvente = Mathf.Clamp01(t / 0.08f);
            if (i >= muestrasNota * notas.Length)
                envolvente *= 1f - (float)(i - muestrasNota * notas.Length) / (frecuenciaMuestreo / 2f);
            else
                envolvente *= 0.75f + 0.25f * Mathf.Sin(Mathf.PI * enNota);

            float aire = ((float)ruido.NextDouble() * 2f - 1f) * 0.04f;
            datos[i] = (Mathf.Sin(fase) * 0.55f + aire) * envolvente;
        }

        silbidoSintetico = AudioClip.Create("Silbido sintetizado", totalMuestras, 1, frecuenciaMuestreo, false);
        silbidoSintetico.SetData(datos, 0);
        return silbidoSintetico;
    }
}
