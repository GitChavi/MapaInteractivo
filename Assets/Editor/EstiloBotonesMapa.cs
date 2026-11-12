using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Cambia los botones genéricos de Unity del mapa (volver, ambiente, zoom y narración)
/// por el estilo del proyecto: tarjeta oscura redondeada, borde dorado y texto Space Grotesk.
/// Se puede ejecutar varias veces. Tools > Mapa interactivo > Aplicar estilo a botones del mapa.
/// </summary>
internal static class EstiloBotonesMapa
{
    private const string ScenePath = "Assets/Scenes/MapaInteractivo.unity";
    private const string SpriteFondo = "Assets/Mapa/popUps/Inventario/UI/Redondeado.png";
    private const string SpriteBorde = "Assets/Mapa/popUps/Inventario/UI/Borde redondeado.png";
    private const string Fuente = "Assets/Fonts/SpaceGrotesk-VariableFont_wght SDF.asset";
    private const float RadioSprite = 40f;
    private const float Radio = 16f;

    private static readonly Color Oscuro = new Color(30 / 255f, 26 / 255f, 22 / 255f, 0.94f);
    private static readonly Color Dorado = new Color32(216, 191, 120, 255);
    private static readonly Color Crema = new Color32(237, 227, 204, 255);

    [MenuItem("Tools/Mapa interactivo/Aplicar estilo a botones del mapa")]
    private static void Aplicar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        Sprite fondo = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteFondo);
        Sprite borde = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteBorde);
        TMP_FontAsset fuente = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Fuente);
        if (fondo == null || borde == null)
        {
            Debug.LogError("Faltan los sprites redondeados. Ejecuta primero 'Crear inventario y barra de progreso'.");
            return;
        }

        Scene scene = EditorSceneManager.GetSceneByPath(ScenePath);
        bool openedHere = !scene.IsValid() || !scene.isLoaded;
        if (openedHere) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

        try
        {
            Transform canvas = null;
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == "Canvas") canvas = root.transform;
            if (canvas == null) { Debug.LogError("No se encontró el Canvas."); return; }

            int cambiados = 0;
            foreach (string ruta in new[] { "ReturnToMapButton", "AmbientAudioToggleButton",
                         "NarrationControls/PauseNarration", "NarrationControls/RepeatNarration" })
            {
                Button b = canvas.Find(ruta)?.GetComponent<Button>();
                if (b == null) continue;
                Estilizar(b, fondo, borde);
                EstilizarTexto(b.GetComponentInChildren<TMP_Text>(true), fuente);
                cambiados++;
            }

            foreach (string ruta in new[] { "MapZoomInButton", "MapZoomOutButton" })
            {
                Button b = canvas.Find(ruta)?.GetComponent<Button>();
                if (b == null) continue;
                Estilizar(b, fondo, borde);
                RectTransform r = (RectTransform)b.transform;
                r.sizeDelta = new Vector2(58, 58);
                // Dibuja + / − con barras: nítido y sin depender de los glifos de la fuente.
                TMP_Text etiqueta = b.GetComponentInChildren<TMP_Text>(true);
                if (etiqueta != null) etiqueta.gameObject.SetActive(false);
                Barra(b.transform, "Horizontal", fondo, new Vector2(22, 4));
                if (ruta == "MapZoomInButton") Barra(b.transform, "Vertical", fondo, new Vector2(4, 22));
                cambiados++;
            }

            // Separación entre los botones de zoom acorde al nuevo tamaño.
            RectTransform zoomIn = canvas.Find("MapZoomInButton") as RectTransform;
            RectTransform zoomOut = canvas.Find("MapZoomOutButton") as RectTransform;
            if (zoomOut != null) zoomOut.anchoredPosition = new Vector2(-28, 108);
            if (zoomIn != null) zoomIn.anchoredPosition = new Vector2(-28, 174);
            RectTransform volver = canvas.Find("ReturnToMapButton") as RectTransform;
            if (volver != null) { volver.anchoredPosition = new Vector2(28, 28); volver.sizeDelta = new Vector2(250, 58); }
            RectTransform ambiente = canvas.Find("AmbientAudioToggleButton") as RectTransform;
            if (ambiente != null) { ambiente.anchoredPosition = new Vector2(-28, 28); ambiente.sizeDelta = new Vector2(280, 58); }
            foreach (string ruta in new[] { "NarrationControls/PauseNarration", "NarrationControls/RepeatNarration" })
                if (canvas.Find(ruta) is RectTransform n) n.sizeDelta = new Vector2(190, 52);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Estilo aplicado a " + cambiados + " botones del mapa.");
        }
        finally
        {
            if (openedHere) EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static void Estilizar(Button boton, Sprite fondo, Sprite borde)
    {
        Image img = boton.GetComponent<Image>();
        img.sprite = fondo;
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = RadioSprite / Radio;
        img.color = Oscuro;

        Transform existente = boton.transform.Find("Borde");
        GameObject go = existente != null
            ? existente.gameObject
            : new GameObject("Borde", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(boton.transform, false);
        go.transform.SetAsFirstSibling();
        RectTransform r = (RectTransform)go.transform;
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
        Image linea = go.GetComponent<Image>();
        linea.sprite = borde;
        linea.type = Image.Type.Sliced;
        linea.pixelsPerUnitMultiplier = RadioSprite / Radio;
        linea.color = new Color(Dorado.r, Dorado.g, Dorado.b, 0.6f);
        linea.raycastTarget = false;

        boton.transition = Selectable.Transition.ColorTint;
        ColorBlock colores = ColorBlock.defaultColorBlock;
        colores.normalColor = Color.white;
        colores.highlightedColor = Color.white;
        colores.pressedColor = new Color(0.75f, 0.72f, 0.66f, 1f);
        colores.selectedColor = Color.white;
        colores.disabledColor = new Color(1f, 1f, 1f, 0.55f);
        colores.fadeDuration = 0.12f;
        boton.colors = colores;
        boton.navigation = new Navigation { mode = Navigation.Mode.None };
        EditorUtility.SetDirty(boton);
    }

    private static void EstilizarTexto(TMP_Text texto, TMP_FontAsset fuente)
    {
        if (texto == null) return;
        if (fuente != null) texto.font = fuente;
        texto.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
        texto.color = Crema;
        texto.characterSpacing = 4f;
        texto.alignment = TextAlignmentOptions.Center;
        // Los scripts cambian el texto en ejecución ("ABRE UN PUNTO PARA OIR AMBIENTE"...): que siempre quepa.
        texto.enableAutoSizing = true;
        texto.fontSizeMin = 10f;
        texto.fontSizeMax = 15f;
        texto.textWrappingMode = TextWrappingModes.Normal;
        texto.margin = new Vector4(14f, 4f, 14f, 4f);
        texto.raycastTarget = false;
        RectTransform r = texto.rectTransform;
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
        EditorUtility.SetDirty(texto);
    }

    private static void Barra(Transform parent, string nombre, Sprite sprite, Vector2 tam)
    {
        Transform existente = parent.Find(nombre);
        GameObject go = existente != null
            ? existente.gameObject
            : new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform r = (RectTransform)go.transform;
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = Vector2.zero;
        r.sizeDelta = tam;
        Image img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = RadioSprite / 2f;
        img.color = Crema;
        img.raycastTarget = false;
    }
}
