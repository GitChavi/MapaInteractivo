using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Menú inicial: botón INICIAR → pantalla de carga con puntos animados mientras el mapa se carga
/// en segundo plano → fundido → mapa (con el tutorial "¿Cómo jugar?").
/// </summary>
public sealed class MenuInicial : MonoBehaviour
{
    [SerializeField] private string escenaMapa = "MapaInteractivo";
    [SerializeField] private CanvasGroup grupoMenu;
    [SerializeField] private CanvasGroup grupoCarga;
    [Tooltip("Capa a pantalla completa para el fundido final antes de cambiar de escena.")]
    [SerializeField] private CanvasGroup cortina;
    [SerializeField] private Button botonIniciar;
    [Tooltip("Puntos de la píldora de carga (se iluminan en secuencia).")]
    [SerializeField] private Graphic[] puntos;
    [Tooltip("Tiempo mínimo en pantalla de carga, para que no sea un parpadeo.")]
    [SerializeField] private float tiempoMinimoCarga = 1.6f;

    private bool iniciando;

    private void Awake()
    {
        if (botonIniciar != null) botonIniciar.onClick.AddListener(Iniciar);
        Fijar(grupoMenu, 1f, true);
        Fijar(grupoCarga, 0f, false);
        Fijar(cortina, 1f, false);
    }

    private IEnumerator Start()
    {
        // Entrada suave desde el color de fondo.
        yield return Fundir(cortina, 1f, 0f, 0.6f);
    }

    private void Update()
    {
        if (puntos == null || grupoCarga == null || grupoCarga.alpha <= 0f) return;
        for (int i = 0; i < puntos.Length; i++)
        {
            if (puntos[i] == null) continue;
            // Onda que recorre los tres puntos.
            float fase = Mathf.Repeat(Time.unscaledTime * 1.4f - i * 0.22f, 1f);
            float brillo = Mathf.Clamp01(1f - Mathf.Abs(fase - 0.25f) * 4f);
            Color c = puntos[i].color;
            c.a = Mathf.Lerp(0.3f, 1f, brillo);
            puntos[i].color = c;
            puntos[i].rectTransform.localScale = Vector3.one * Mathf.Lerp(1f, 1.25f, brillo);
        }
    }

    public void Iniciar()
    {
        if (iniciando) return;
        iniciando = true;
        StartCoroutine(Cargar());
    }

    private IEnumerator Cargar()
    {
        if (grupoMenu != null) grupoMenu.interactable = false;
        yield return Fundir(grupoMenu, 1f, 0f, 0.35f);
        yield return Fundir(grupoCarga, 0f, 1f, 0.35f);

        float inicio = Time.unscaledTime;
        AsyncOperation carga = SceneManager.LoadSceneAsync(escenaMapa);
        if (carga == null)
        {
            Debug.LogError("No se pudo cargar '" + escenaMapa + "'. ¿Está en Build Settings?");
            yield break;
        }
        carga.allowSceneActivation = false;
        while (carga.progress < 0.9f || Time.unscaledTime - inicio < tiempoMinimoCarga)
            yield return null;

        yield return Fundir(cortina, 0f, 1f, 0.4f);
        EstadoJuego.MostrarTutorial = true;
        carga.allowSceneActivation = true;
    }

    private static IEnumerator Fundir(CanvasGroup grupo, float desde, float hasta, float duracion)
    {
        if (grupo == null) yield break;
        grupo.blocksRaycasts = hasta > 0f;
        for (float t = 0f; t < duracion; t += Time.unscaledDeltaTime)
        {
            grupo.alpha = Mathf.Lerp(desde, hasta, Mathf.SmoothStep(0f, 1f, t / duracion));
            yield return null;
        }
        grupo.alpha = hasta;
    }

    private static void Fijar(CanvasGroup grupo, float alpha, bool interactivo)
    {
        if (grupo == null) return;
        grupo.alpha = alpha;
        grupo.interactable = grupo.blocksRaycasts = interactivo;
    }
}
