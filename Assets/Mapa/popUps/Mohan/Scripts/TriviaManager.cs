
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TriviaManager : MonoBehaviour
{
    [Header("Panel actual")]
    public GameObject panelActual;

    [Header("Botones")]
    public Button botonA;
    public Button botonB;
    public Button botonC;
    public Button botonD;

    [Header("Respuesta correcta")]
    public string respuestaCorrecta = "B";

    [Header("Colores")]
    public Color colorNormal = Color.white;
    public Color colorCorrecto = Color.green;
    public Color colorIncorrecto = Color.red;

    [Header("Sonidos")]
    public AudioSource reproductor;
    public AudioClip sonidoAcierto;
    public AudioClip sonidoDesacierto;

    [Header("Panel de respuesta correcta")]
    public GameObject panelVictoria;

    [Header("Animación")]
    public float intensidadSacudida = 10f;
    public float duracionSacudida = 0.3f;

    private bool respondido = false;

    void Start()
    {
        botonA.onClick.AddListener(() => RevisarRespuesta("A", botonA));
        botonB.onClick.AddListener(() => RevisarRespuesta("B", botonB));
        botonC.onClick.AddListener(() => RevisarRespuesta("C", botonC));
        botonD.onClick.AddListener(() => RevisarRespuesta("D", botonD));

        if (panelVictoria != null)
        {
            panelVictoria.SetActive(false);
        }
    }

    void RevisarRespuesta(string respuesta, Button boton)
    {
        if (respondido) return;

        respondido = true;

        if (respuesta == respuestaCorrecta)
        {
            boton.image.color = colorCorrecto;

            if (reproductor != null && sonidoAcierto != null)
            {
                reproductor.PlayOneShot(sonidoAcierto);
            }

            StartCoroutine(RespuestaCorrecta());
        }
        else
        {
            boton.image.color = colorIncorrecto;

            if (reproductor != null && sonidoDesacierto != null)
            {
                reproductor.PlayOneShot(sonidoDesacierto);
            }

            StartCoroutine(SacudirBoton(boton.transform));
            StartCoroutine(RespuestaIncorrecta());
        }

        botonA.interactable = false;
        botonB.interactable = false;
        botonC.interactable = false;
        botonD.interactable = false;
    }

    IEnumerator RespuestaCorrecta()
    {
        yield return new WaitForSeconds(3f);

        // Desactivar el panel actual
        if (panelActual != null)
        {
            panelActual.SetActive(false);
        }

        // Activar el panel de victoria
        if (panelVictoria != null)
        {
            panelVictoria.SetActive(true);
        }
    }

    IEnumerator RespuestaIncorrecta()
    {
        yield return new WaitForSeconds(3f);

        // Restaurar colores
        botonA.image.color = colorNormal;
        botonB.image.color = colorNormal;
        botonC.image.color = colorNormal;
        botonD.image.color = colorNormal;

        // Reactivar botones
        botonA.interactable = true;
        botonB.interactable = true;
        botonC.interactable = true;
        botonD.interactable = true;

        respondido = false;
    }

    IEnumerator SacudirBoton(Transform boton)
    {
        Vector3 posicionInicial = boton.localPosition;
        float tiempo = 0f;

        while (tiempo < duracionSacudida)
        {
            float desplazamiento = Random.Range(
                -intensidadSacudida,
                intensidadSacudida
            );

            boton.localPosition = posicionInicial +
                new Vector3(desplazamiento, 0, 0);

            tiempo += Time.deltaTime;
            yield return null;
        }

        boton.localPosition = posicionInicial;
    }
}
