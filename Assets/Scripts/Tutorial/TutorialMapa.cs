using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Tutorial "¿Cómo jugar?" del mapa, en pasos. "Probar" oculta el panel y deja al usuario hacer la
/// acción de verdad (mover el mapa, hacer zoom, abrir una leyenda); al lograrlo avanza solo.
/// Aparece al llegar desde el menú inicial y se puede abrir con el botón AYUDA.
/// </summary>
public sealed class TutorialMapa : MonoBehaviour
{
    public enum Accion { Desplazar, Zoom, AbrirLeyenda }

    [Serializable]
    public sealed class Paso
    {
        public string titulo;
        [TextArea(2, 4)] public string texto;
        [Tooltip("Indicación que se muestra mientras el usuario lo prueba.")]
        public string pista;
        public Accion accion;
        [Tooltip("Ilustración del paso (se muestra solo la del paso actual).")]
        public GameObject ilustracion;
    }

    [SerializeField] private Paso[] pasos;

    [Header("Panel")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text titulo;
    [SerializeField] private TMP_Text texto;
    [SerializeField] private Button[] paginas;

    [Header("Mientras se prueba")]
    [SerializeField] private GameObject pista;
    [SerializeField] private TMP_Text textoPista;

    [Header("Mapa")]
    [SerializeField] private RectTransform mapa;
    [Tooltip("Fichas de las leyendas: abrir una completa el último paso.")]
    [SerializeField] private GameObject[] fichas;
    [SerializeField] private float distanciaDesplazamiento = 80f;
    [SerializeField] private float cambioZoom = 0.08f;

    [Header("Colores de la paginación")]
    [SerializeField] private Color paginaActivaFondo = new Color32(42, 52, 38, 255);
    [SerializeField] private Color paginaActivaTexto = new Color32(233, 220, 192, 255);
    [SerializeField] private Color paginaFondo = new Color32(233, 220, 192, 255);
    [SerializeField] private Color paginaTexto = new Color32(42, 52, 38, 255);

    private int actual;
    private bool probando;
    private Vector2 posicionInicial;
    private float escalaInicial;

    public bool Abierto => (panel != null && panel.activeSelf) || probando;

    private void Awake()
    {
        for (int i = 0; i < paginas.Length; i++)
        {
            int indice = i;
            if (paginas[i] != null) paginas[i].onClick.AddListener(() => Mostrar(indice));
        }
        Ocultar();
    }

    private void Start()
    {
        if (EstadoJuego.MostrarTutorial)
        {
            EstadoJuego.MostrarTutorial = false;
            Mostrar(0);
        }
    }

    /// <summary>Botón AYUDA del mapa.</summary>
    public void Abrir() => Mostrar(0);

    public void Mostrar(int indice)
    {
        if (pasos == null || pasos.Length == 0) return;
        StopAllCoroutines();
        actual = Mathf.Clamp(indice, 0, pasos.Length - 1);
        probando = false;
        if (pista != null) pista.SetActive(false);
        if (panel != null) panel.SetActive(true);

        Paso p = pasos[actual];
        if (titulo != null) titulo.text = p.titulo;
        if (texto != null) texto.text = p.texto;
        for (int i = 0; i < pasos.Length; i++)
            if (pasos[i].ilustracion != null) pasos[i].ilustracion.SetActive(i == actual);

        for (int i = 0; i < paginas.Length; i++)
        {
            if (paginas[i] == null) continue;
            bool activa = i == actual;
            Image fondo = paginas[i].GetComponent<Image>();
            if (fondo != null) fondo.color = activa ? paginaActivaFondo : paginaFondo;
            TMP_Text numero = paginas[i].GetComponentInChildren<TMP_Text>(true);
            if (numero != null) numero.color = activa ? paginaActivaTexto : paginaTexto;
            Transform aro = paginas[i].transform.Find("Borde");
            if (aro != null) aro.gameObject.SetActive(activa);
        }
    }

    /// <summary>Botón "Probar": cierra el panel y espera a que el usuario haga la acción.</summary>
    public void Probar()
    {
        if (pasos == null || pasos.Length == 0) return;
        if (panel != null) panel.SetActive(false);
        probando = true;
        if (mapa != null)
        {
            posicionInicial = mapa.anchoredPosition;
            escalaInicial = mapa.localScale.x;
        }
        if (pista != null) pista.SetActive(true);
        if (textoPista != null) textoPista.text = pasos[actual].pista;
    }

    /// <summary>Cerrar (X) o "Saltar tutorial".</summary>
    public void Cerrar() => Ocultar();

    private void Update()
    {
        if (!probando || !Logrado(pasos[actual].accion)) return;
        probando = false;
        StartCoroutine(Avanzar());
    }

    private bool Logrado(Accion accion)
    {
        switch (accion)
        {
            case Accion.Desplazar:
                return mapa != null && Vector2.Distance(mapa.anchoredPosition, posicionInicial) > distanciaDesplazamiento;
            case Accion.Zoom:
                return mapa != null && Mathf.Abs(mapa.localScale.x - escalaInicial) > cambioZoom;
            case Accion.AbrirLeyenda:
                if (fichas == null) return false;
                foreach (GameObject f in fichas)
                    if (f != null && f.activeInHierarchy) return true;
                return false;
        }
        return false;
    }

    private IEnumerator Avanzar()
    {
        if (textoPista != null) textoPista.text = "¡Muy bien!";
        yield return new WaitForSecondsRealtime(0.9f);
        if (actual + 1 < pasos.Length && pasos[actual].accion != Accion.AbrirLeyenda) Mostrar(actual + 1);
        else Ocultar(); // abrir una leyenda termina el tutorial y deja la ficha visible
    }

    private void Ocultar()
    {
        StopAllCoroutines();
        probando = false;
        if (panel != null) panel.SetActive(false);
        if (pista != null) pista.SetActive(false);
    }
}
