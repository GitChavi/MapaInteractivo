using TMPro;
using UnityEngine;

/// <summary>
/// Pinta un texto TMP con un degradado horizontal continuo por línea (amarillo, azul y rojo de la
/// bandera para el logo "Crónicas de Colombia"). El degradado atraviesa las letras, no va letra a letra.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(TMP_Text))]
public sealed class TextoDegradado : MonoBehaviour
{
    [SerializeField] private Gradient degradado = Bandera();

    private TMP_Text texto;
    private bool aplicando;

    private void OnEnable()
    {
        texto = GetComponent<TMP_Text>();
        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(AlCambiar);
        Aplicar();
    }

    private void OnDisable()
    {
        TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(AlCambiar);
        if (texto != null) texto.ForceMeshUpdate();
    }

    private void OnValidate()
    {
        if (isActiveAndEnabled) Aplicar();
    }

    private void AlCambiar(Object obj)
    {
        if (obj == texto) Aplicar();
    }

    private void Aplicar()
    {
        // ForceMeshUpdate vuelve a lanzar TEXT_CHANGED_EVENT: evitar la recursión.
        if (texto == null || aplicando) return;
        aplicando = true;
        try { AplicarColores(); }
        finally { aplicando = false; }
    }

    private void AplicarColores()
    {
        texto.ForceMeshUpdate();
        TMP_TextInfo info = texto.textInfo;

        for (int l = 0; l < info.lineCount; l++)
        {
            TMP_LineInfo linea = info.lineInfo[l];
            if (linea.characterCount == 0) continue;

            // Bordes reales de los glifos visibles de la línea.
            float min = float.MaxValue, max = float.MinValue;
            for (int i = linea.firstCharacterIndex; i <= linea.lastCharacterIndex; i++)
            {
                TMP_CharacterInfo c = info.characterInfo[i];
                if (!c.isVisible) continue;
                min = Mathf.Min(min, c.bottomLeft.x);
                max = Mathf.Max(max, c.topRight.x);
            }
            if (max <= min) continue;

            for (int i = linea.firstCharacterIndex; i <= linea.lastCharacterIndex; i++)
            {
                TMP_CharacterInfo c = info.characterInfo[i];
                if (!c.isVisible) continue;
                Color32[] colores = info.meshInfo[c.materialReferenceIndex].colors32;
                Vector3[] vertices = info.meshInfo[c.materialReferenceIndex].vertices;
                for (int v = 0; v < 4; v++)
                {
                    int idx = c.vertexIndex + v;
                    Color col = degradado.Evaluate(Mathf.InverseLerp(min, max, vertices[idx].x));
                    col.a *= c.color.a / 255f;
                    colores[idx] = col;
                }
            }
        }
        texto.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }

    private static Gradient Bandera()
    {
        Color amarillo = new Color32(252, 209, 22, 255);
        Color azul = new Color32(0, 125, 245, 255);
        Color rojo = new Color32(206, 17, 38, 255);
        Gradient g = new Gradient();
        g.SetKeys(
            new[]
            {
                new GradientColorKey(amarillo, 0f), new GradientColorKey(amarillo, 0.22f),
                new GradientColorKey(azul, 0.42f), new GradientColorKey(azul, 0.55f),
                new GradientColorKey(rojo, 0.72f), new GradientColorKey(rojo, 1f),
            },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
        return g;
    }
}
