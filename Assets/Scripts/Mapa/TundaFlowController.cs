using UnityEngine;

/// <summary>
/// Minijuego de La Tunda dentro de MapaInteractivo.
///
/// Flujo: ficha (Tunda) → JUGAR → video (TundaP2) → CONTINUAR → decisión auditiva → búsqueda de
/// sombra → recompensa → mapa. Todas las pantallas y botones son objetos editables en la jerarquía
/// (Tools > Mapa interactivo > Configurar flujo de La Tunda los ajusta).
/// </summary>
public sealed class TundaFlowController : MonoBehaviour
{
    [Header("Páginas originales del popup")]
    [Tooltip("Ficha del mapa (Tunda), con el botón JUGAR.")]
    [SerializeField] private GameObject paginaFicha;
    [Tooltip("Pantalla del video (TundaP2), con el botón CONTINUAR.")]
    [SerializeField] private GameObject paginaVideo;
    [Tooltip("Raíz del popup en el mapa (Tunda_todo). Si se deja vacío se usa el padre de este objeto.")]
    [SerializeField] private GameObject popupRaiz;

    [Header("Pantallas del minijuego")]
    [SerializeField] private GameObject pantallaDecision;
    [SerializeField] private GameObject pantallaSombra;
    [SerializeField] private GameObject pantallaRecompensa;
    [Tooltip("Fondo a pantalla completa detrás de las pantallas del minijuego.")]
    [SerializeField] private GameObject fondoPantallas;

    [Header("Popups")]
    [SerializeField] private GameObject popupElegirVoz;
    [SerializeField] private GameObject popupIncorrecto;
    [SerializeField] private GameObject popupSombraIncorrecta;
    [Tooltip("Capa oscura que se muestra detrás de cualquier popup abierto.")]
    [SerializeField] private GameObject oscurecerPopups;

    [Header("Respuestas correctas (se pueden cambiar en Inspector)")]
    [Tooltip("Voz que debe elegirse. 1 = Voz 01, 2 = Voz 02.")]
    [Range(1, 2)] [SerializeField] private int vozTrampa = 1;
    [Tooltip("Sombra correcta de izquierda a derecha. La correcta es la tercera (central).")]
    [Range(1, 4)] [SerializeField] private int sombraTunda = 3;

    [Header("Audio")]
    [SerializeField] private AudioSource ambiente;
    [SerializeField] private AudioSource voz01;
    [SerializeField] private AudioSource voz02;
    [SerializeField] private GameObject feedbackVoz01;
    [SerializeField] private GameObject feedbackVoz02;
    [Tooltip("Audios de las páginas anteriores que se pausan mientras se juega (p. ej. el de TundaP2).")]
    [SerializeField] private AudioSource[] audiosASilenciar;

    private bool flujoActivo;

    private void Awake()
    {
        if (popupRaiz == null && transform.parent != null) popupRaiz = transform.parent.gameObject;
    }

    // Cada vez que se abre el punto de la Tunda en el mapa, empieza desde la ficha,
    // aunque se haya cerrado a mitad del flujo con "Volver al mapa".
    private void OnEnable()
    {
        flujoActivo = false;
        MostrarPantalla(null);
        if (paginaVideo != null) paginaVideo.SetActive(false);
        if (paginaFicha != null) paginaFicha.SetActive(true);
    }

    private void OnDisable()
    {
        flujoActivo = false;
        DetenerAudio();
        RestaurarAudiosPrevios();
    }

    private void Update()
    {
        // El botón general "Volver al mapa" cierra el popup; cerrar también el minijuego.
        if (flujoActivo && paginaVideo != null && !paginaVideo.activeInHierarchy)
            CerrarFlujo();

        if (oscurecerPopups != null)
        {
            bool hayPopup = Activo(popupElegirVoz) || Activo(popupIncorrecto) || Activo(popupSombraIncorrecta);
            if (oscurecerPopups.activeSelf != hayPopup) oscurecerPopups.SetActive(hayPopup);
        }
    }

    // ---------- Navegación ----------

    /// <summary>Botón CONTINUAR de la pantalla del video (TundaP2).</summary>
    public void IniciarMinijuego()
    {
        flujoActivo = true;
        SilenciarAudiosPrevios();
        ActualizarFeedbackVoz(0);
        MostrarPantalla(pantallaDecision);
        if (ambiente != null && ambiente.clip != null)
        {
            ambiente.mute = false;
            ambiente.Play();
        }
    }

    public void ContinuarDesdeRecompensa()
    {
        CerrarFlujo();
        // Cierra el popup completo para volver al mapa; al reabrirlo se reinicia en la ficha.
        if (popupRaiz != null) popupRaiz.SetActive(false);
        else if (paginaVideo != null) paginaVideo.SetActive(false);
    }

    // ---------- Decisión auditiva ----------

    public void ReproducirVoz01()
    {
        ActualizarFeedbackVoz(1);
        ReproducirPista(voz01);
    }

    public void ReproducirVoz02()
    {
        ActualizarFeedbackVoz(2);
        ReproducirPista(voz02);
    }

    public void AbrirSelectorVoz()
    {
        DetenerVoces();
        if (popupElegirVoz != null) popupElegirVoz.SetActive(true);
    }

    public void CancelarSelectorVoz()
    {
        if (popupElegirVoz != null) popupElegirVoz.SetActive(false);
    }

    public void ElegirVoz01() => ValidarVoz(1);
    public void ElegirVoz02() => ValidarVoz(2);

    // ---------- Búsqueda de sombra ----------

    public void ElegirSombra01() => ValidarSombra(1);
    public void ElegirSombra02() => ValidarSombra(2);
    public void ElegirSombra03() => ValidarSombra(3);
    public void ElegirSombra04() => ValidarSombra(4);

    public void Reintentar()
    {
        CerrarPopups();
    }

    // ---------- Lógica interna ----------

    private void ValidarVoz(int opcion)
    {
        if (popupElegirVoz != null) popupElegirVoz.SetActive(false);
        if (opcion == vozTrampa)
        {
            CerrarPopups();
            DetenerVoces();
            MostrarPantalla(pantallaSombra);
        }
        else if (popupIncorrecto != null)
        {
            popupIncorrecto.SetActive(true);
        }
    }

    private void ValidarSombra(int opcion)
    {
        if (opcion == sombraTunda)
        {
            CerrarPopups();
            MostrarPantalla(pantallaRecompensa);
        }
        else if (popupSombraIncorrecta != null)
        {
            popupSombraIncorrecta.SetActive(true);
        }
    }

    private void ReproducirPista(AudioSource pista)
    {
        DetenerVoces();
        if (pista != null && pista.clip != null) pista.Play();
    }

    private void DetenerVoces()
    {
        if (voz01 != null) voz01.Stop();
        if (voz02 != null) voz02.Stop();
    }

    private void DetenerAudio()
    {
        DetenerVoces();
        if (ambiente != null) ambiente.Stop();
    }

    private void ActualizarFeedbackVoz(int seleccion)
    {
        if (feedbackVoz01 != null) feedbackVoz01.SetActive(seleccion == 1);
        if (feedbackVoz02 != null) feedbackVoz02.SetActive(seleccion == 2);
    }

    private void CerrarPopups()
    {
        if (popupElegirVoz != null) popupElegirVoz.SetActive(false);
        if (popupIncorrecto != null) popupIncorrecto.SetActive(false);
        if (popupSombraIncorrecta != null) popupSombraIncorrecta.SetActive(false);
        if (oscurecerPopups != null) oscurecerPopups.SetActive(false);
    }

    private void CerrarFlujo()
    {
        flujoActivo = false;
        MostrarPantalla(null);
        DetenerAudio();
        RestaurarAudiosPrevios();
    }

    private void MostrarPantalla(GameObject pantalla)
    {
        CerrarPopups();
        if (pantallaDecision != null) pantallaDecision.SetActive(pantalla == pantallaDecision);
        if (pantallaSombra != null) pantallaSombra.SetActive(pantalla == pantallaSombra);
        if (pantallaRecompensa != null) pantallaRecompensa.SetActive(pantalla == pantallaRecompensa);
        if (fondoPantallas != null) fondoPantallas.SetActive(pantalla != null);
        if (pantalla != pantallaDecision) ActualizarFeedbackVoz(0);
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

    private static bool Activo(GameObject go) => go != null && go.activeSelf;
}
