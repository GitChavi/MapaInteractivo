
using UnityEngine;

public class CestoBasura : MonoBehaviour
{
    private RectTransform rectTransform;

    [Header("Contador de basuras")]
    public int basurasNecesarias = 4;
    private int basurasCompletadas = 0;

    [Header("Panel final")]
    public GameObject panelFinal;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();

        if (panelFinal != null)
        {
            panelFinal.SetActive(false);
        }
    }

    public bool EstaDentroDelCesto(RectTransform basura)
    {
        Vector3[] esquinas = new Vector3[4];
        rectTransform.GetWorldCorners(esquinas);

        Rect areaCesto = new Rect(
            esquinas[0].x,
            esquinas[0].y,
            esquinas[2].x - esquinas[0].x,
            esquinas[2].y - esquinas[0].y
        );

        return areaCesto.Contains(basura.position);
    }


    public void RegistrarBasura()
    {
        basurasCompletadas++;

        Debug.Log("Basuras depositadas: " + basurasCompletadas + "/" + basurasNecesarias);

        if (basurasCompletadas >= basurasNecesarias)
        {
            Debug.Log("¡Completaste las 4 basuras!");

            if (panelFinal != null)
            {
                panelFinal.SetActive(true);
            }
            else
            {
                Debug.LogError("No has asignado el panel final.");
            }
        }
    }

}
