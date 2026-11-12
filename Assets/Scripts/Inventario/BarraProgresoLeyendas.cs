using TMPro;
using UnityEngine;

/// <summary>
/// Barra que se llena según las leyendas completadas. El relleno es un RectTransform
/// anclado a la izquierda: su ancla derecha se anima de 0 a 1.
/// </summary>
public sealed class BarraProgresoLeyendas : MonoBehaviour
{
    [SerializeField] private ProgresoLeyendas progreso;
    [SerializeField] private RectTransform relleno;
    [SerializeField] private TMP_Text contador;
    [Tooltip("{0} = completadas, {1} = total")]
    [SerializeField] private string formatoContador = "{0} / {1}";
    [SerializeField] private float velocidadLlenado = 1.5f;
    [Tooltip("Objeto que se destaca brevemente al completar una leyenda (opcional).")]
    [SerializeField] private RectTransform destacarAlAvanzar;

    private float valorMostrado;
    private float pulso;

    private void OnEnable()
    {
        if (progreso == null) progreso = ProgresoLeyendas.Instancia;
        if (progreso == null) return;
        progreso.Cambio += Actualizar;
        progreso.LeyendaCompletada += AlCompletar;
        Actualizar();
    }

    private void OnDisable()
    {
        if (progreso == null) return;
        progreso.Cambio -= Actualizar;
        progreso.LeyendaCompletada -= AlCompletar;
    }

    private void Update()
    {
        if (progreso == null) return;

        valorMostrado = Mathf.MoveTowards(valorMostrado, progreso.Progreso, velocidadLlenado * Time.unscaledDeltaTime);
        if (relleno != null)
        {
            Vector2 max = relleno.anchorMax;
            max.x = valorMostrado;
            relleno.anchorMax = max;
            // Con el relleno vacío no se dibujan las esquinas redondeadas sueltas.
            relleno.gameObject.SetActive(valorMostrado > 0.001f);
        }

        if (destacarAlAvanzar != null && pulso > 0f)
        {
            pulso = Mathf.Max(0f, pulso - Time.unscaledDeltaTime * 1.6f);
            float s = 1f + 0.08f * Mathf.Sin(pulso * Mathf.PI);
            destacarAlAvanzar.localScale = new Vector3(s, s, 1f);
        }
    }

    private void Actualizar()
    {
        if (contador != null && progreso != null)
            contador.text = string.Format(formatoContador, progreso.Completadas, progreso.Total);
    }

    private void AlCompletar(LeyendaInfo _) => pulso = 1f;
}
