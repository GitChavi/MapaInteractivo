using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Ajusta el flujo de La Tunda que ya existe en MapaInteractivo (Tunda_todo/TundaMinijuego):
/// usa TundaP2 como pantalla de video (igual que El Silbón), agrega fondo y capa oscura para los
/// popups, cambia el resaltado de las voces por uno redondeado y vuelve a conectar los botones.
/// Se puede ejecutar varias veces; solo se corre desde el menú.
/// </summary>
internal static class TundaPanelSetup
{
    private const string ScenePath = "Assets/Scenes/MapaInteractivo.unity";
    private const string SpritePath = "Assets/Mapa/popUps/Tunda/UI/Seleccion redondeada.png";
    private const string FondoSolidoPath = "Assets/Mapa/popUps/Tunda/UI/Fondo redondeado.png";
    private const int SpriteSize = 128;
    private const int SpriteRadius = 40;
    private const int SpriteOutline = 4;

    private static readonly Color Ink = new Color32(25, 31, 26, 255);
    private static readonly Color Dorado = new Color32(224, 191, 113, 255);

    // Tarjetas VOZ 01 / VOZ 02 medidas sobre "La Tunda — 02 · Decisión auditiva.png" (1103x847).
    private static readonly Vector2 TarjetaVoz01 = new Vector2(0.2847f, 0.474f);
    private static readonly Vector2 TarjetaVoz02 = new Vector2(0.7171f, 0.474f);
    private static readonly Vector2 TamTarjeta = new Vector2(0.4025f, 0.3247f);

    [MenuItem("Tools/Mapa interactivo/Configurar flujo de La Tunda")]
    private static void Configurar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        Sprite seleccion = AsegurarSpriteRedondeado();

        Scene scene = EditorSceneManager.GetSceneByPath(ScenePath);
        bool openedHere = !scene.IsValid() || !scene.isLoaded;
        if (openedHere) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

        try
        {
            Transform popup = FindInScene(scene, "Tunda_todo");
            Transform ficha = popup != null ? popup.Find("Tunda") : null;
            Transform video = popup != null ? popup.Find("TundaP2") : null;
            Transform flow = popup != null ? popup.Find("TundaMinijuego") : null;
            TundaFlowController controller = flow != null ? flow.GetComponent<TundaFlowController>() : null;
            if (ficha == null || video == null || controller == null)
            {
                Debug.LogError("No se encontró Tunda_todo con Tunda, TundaP2 y TundaMinijuego en MapaInteractivo.");
                return;
            }

            Transform decision = flow.Find("02 - Decisión auditiva");
            Transform sombra = flow.Find("03 - Búsqueda de sombra");
            Transform recompensa = flow.Find("04 - Recompensa");
            Transform selector = flow.Find("Selector de voz");
            Transform vozMal = flow.Find("Respuesta de voz incorrecta");
            Transform sombraMal = flow.Find("Sombra incorrecta");
            if (decision == null || sombra == null || recompensa == null || selector == null || vozMal == null || sombraMal == null)
            {
                Debug.LogError("Faltan pantallas o popups dentro de TundaMinijuego.");
                return;
            }

            // 1. Quitar lo que se veía mal o ya no se usa.
            Destruir(flow.Find("01 - Introducción"));          // intro hecha a código; ahora se usa TundaP2
            Destruir(decision.Find("Arte modular - Decision auditiva"));
            Destruir(decision.Find("Estado de pista"));         // texto que se montaba sobre el botón
            Destruir(decision.Find("Retroalimentación - Voz 01"));
            Destruir(decision.Find("Retroalimentación - Voz 02"));
            Destruir(selector.Find("Fondo opaco"));             // recuadro negro de esquinas rectas

            // El arte del selector es semitransparente: fondo sólido con su misma forma redondeada.
            Sprite solido = AsegurarSpriteRedondeado(FondoSolidoPath, 1f);
            Transform fondoSelector = selector.Find("Fondo redondeado");
            GameObject fondoSel = fondoSelector != null
                ? fondoSelector.gameObject
                : new GameObject("Fondo redondeado", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            fondoSel.transform.SetParent(selector, false);
            fondoSel.transform.SetAsFirstSibling();
            SetNormalizedRect(fondoSel.GetComponent<RectTransform>(), new Vector2(0.484f, 0.498f), new Vector2(0.85f, 0.805f));
            Image fondoSelImg = fondoSel.GetComponent<Image>();
            fondoSelImg.sprite = solido;
            fondoSelImg.type = Image.Type.Sliced;
            fondoSelImg.pixelsPerUnitMultiplier = 1.25f;
            fondoSelImg.color = Ink;
            fondoSelImg.raycastTarget = true;

            // 2. Fondo detrás de las pantallas y capa oscura para los popups.
            // Orden fijo (de atrás hacia delante): fondo, pantallas, capa oscura, popups.
            GameObject fondo = AsegurarCapa(flow, "Fondo de pantallas", Ink);
            GameObject oscurecer = AsegurarCapa(flow, "Oscurecer popups", new Color(0f, 0f, 0f, 0.6f));
            fondo.transform.SetAsFirstSibling();
            foreach (Transform t in new[] { decision, sombra, recompensa, oscurecer.transform, selector, vozMal, sombraMal })
                t.SetAsLastSibling();
            fondo.SetActive(false);
            oscurecer.SetActive(false);

            // 3. Resaltado redondeado sobre la tarjeta de la voz que suena.
            // Justo encima del arte y debajo de los botones.
            GameObject sel01 = CrearSeleccion(decision, "Selección - Voz 01", seleccion, TarjetaVoz01);
            GameObject sel02 = CrearSeleccion(decision, "Selección - Voz 02", seleccion, TarjetaVoz02);
            Transform arteDecision = null;
            foreach (Transform hijo in decision)
                if (hijo.name.StartsWith("Arte - ")) { arteDecision = hijo; break; }
            if (arteDecision != null)
            {
                sel01.transform.SetSiblingIndex(arteDecision.GetSiblingIndex() + 1);
                sel02.transform.SetSiblingIndex(sel01.transform.GetSiblingIndex() + 1);
            }
            PosicionarBoton(decision, "Reproducir voz 01", TarjetaVoz01, TamTarjeta);
            PosicionarBoton(decision, "Reproducir voz 02", TarjetaVoz02, TamTarjeta);

            foreach (Transform t in new[] { decision, sombra, recompensa }) t.gameObject.SetActive(false);
            foreach (Transform t in new[] { selector, vozMal, sombraMal }) t.gameObject.SetActive(false);

            // 4. Referencias del controlador.
            SerializedObject data = new SerializedObject(controller);
            data.FindProperty("paginaFicha").objectReferenceValue = ficha.gameObject;
            data.FindProperty("paginaVideo").objectReferenceValue = video.gameObject;
            data.FindProperty("popupRaiz").objectReferenceValue = popup.gameObject;
            data.FindProperty("pantallaDecision").objectReferenceValue = decision.gameObject;
            data.FindProperty("pantallaSombra").objectReferenceValue = sombra.gameObject;
            data.FindProperty("pantallaRecompensa").objectReferenceValue = recompensa.gameObject;
            data.FindProperty("fondoPantallas").objectReferenceValue = fondo;
            data.FindProperty("popupElegirVoz").objectReferenceValue = selector.gameObject;
            data.FindProperty("popupIncorrecto").objectReferenceValue = vozMal.gameObject;
            data.FindProperty("popupSombraIncorrecta").objectReferenceValue = sombraMal.gameObject;
            data.FindProperty("oscurecerPopups").objectReferenceValue = oscurecer;
            data.FindProperty("feedbackVoz01").objectReferenceValue = sel01;
            data.FindProperty("feedbackVoz02").objectReferenceValue = sel02;
            data.FindProperty("ambiente").objectReferenceValue = flow.Find("Audio ambiente del Pacífico")?.GetComponent<AudioSource>();
            data.FindProperty("voz01").objectReferenceValue = decision.Find("Audio - Voz 01")?.GetComponent<AudioSource>();
            data.FindProperty("voz02").objectReferenceValue = decision.Find("Audio - Voz 02")?.GetComponent<AudioSource>();
            SerializedProperty silenciar = data.FindProperty("audiosASilenciar");
            AudioSource audioVideo = video.GetComponent<AudioSource>();
            silenciar.arraySize = audioVideo != null ? 1 : 0;
            if (audioVideo != null) silenciar.GetArrayElementAtIndex(0).objectReferenceValue = audioVideo;
            data.ApplyModifiedPropertiesWithoutUndo();

            // 5. Botones. JUGAR abre el video (igual que en las demás leyendas) y CONTINUAR inicia el minijuego.
            Button jugar = Boton(ficha, "Button");
            if (jugar != null)
            {
                LimpiarListeners(jugar);
                UnityEventTools.AddBoolPersistentListener(jugar.onClick, video.gameObject.SetActive, true);
                UnityEventTools.AddBoolPersistentListener(jugar.onClick, ficha.gameObject.SetActive, false);
                EditorUtility.SetDirty(jugar);
            }
            Conectar(Boton(video, "Button"), controller.IniciarMinijuego);
            Conectar(Boton(decision, "Reproducir voz 01"), controller.ReproducirVoz01);
            Conectar(Boton(decision, "Reproducir voz 02"), controller.ReproducirVoz02);
            Conectar(Boton(decision, "Botón reproducir voz 01"), controller.ReproducirVoz01);
            Conectar(Boton(decision, "Botón reproducir voz 02"), controller.ReproducirVoz02);
            Conectar(Boton(decision, "Elegir voz"), controller.AbrirSelectorVoz);
            Conectar(Boton(selector, "Elegir voz 01"), controller.ElegirVoz01);
            Conectar(Boton(selector, "Elegir voz 02"), controller.ElegirVoz02);
            Conectar(Boton(selector, "Cerrar selector"), controller.CancelarSelectorVoz);
            Conectar(Boton(vozMal, "Reintentar voz"), controller.Reintentar);
            Conectar(Boton(vozMal, "Cerrar popup"), controller.Reintentar);
            Conectar(Boton(sombra, "Sombra extrema izquierda"), controller.ElegirSombra01);
            Conectar(Boton(sombra, "Sombra izquierda"), controller.ElegirSombra02);
            Conectar(Boton(sombra, "Sombra central"), controller.ElegirSombra03);
            Conectar(Boton(sombra, "Sombra derecha"), controller.ElegirSombra04);
            Conectar(Boton(sombraMal, "Reintentar sombra"), controller.Reintentar);
            Conectar(Boton(sombraMal, "Cerrar popup"), controller.Reintentar);
            Conectar(Boton(recompensa, "Continuar al mapa"), controller.ContinuarDesdeRecompensa);

            // El punto queda cerrado hasta que se toque en el mapa, empezando por la ficha.
            ficha.gameObject.SetActive(true);
            video.gameObject.SetActive(false);
            popup.gameObject.SetActive(false);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Flujo de La Tunda ajustado: JUGAR → video (TundaP2) → CONTINUAR → minijuego.");
        }
        finally
        {
            if (openedHere) EditorSceneManager.CloseScene(scene, true);
        }
    }

    // ---------- Construcción ----------

    private static GameObject AsegurarCapa(Transform parent, string nombre, Color color)
    {
        Transform existente = parent.Find(nombre);
        GameObject go = existente != null
            ? existente.gameObject
            : new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        Image img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = true; // bloquea los toques a lo que hay detrás
        return go;
    }

    private static GameObject CrearSeleccion(Transform parent, string nombre, Sprite sprite, Vector2 centro)
    {
        Transform existente = parent.Find(nombre);
        GameObject go = existente != null
            ? existente.gameObject
            : new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        SetNormalizedRect(go.GetComponent<RectTransform>(), centro, TamTarjeta);
        Image img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = 1.05f; // esquinas del mismo radio que las tarjetas del arte
        img.color = Dorado;
        img.raycastTarget = false;
        go.SetActive(false);
        return go;
    }

    private static void PosicionarBoton(Transform parent, string nombre, Vector2 centro, Vector2 tam)
    {
        Transform t = parent.Find(nombre);
        if (t != null) SetNormalizedRect(t.GetComponent<RectTransform>(), centro, tam);
    }

    /// <summary>Sprite blanco redondeado (9-slice): borde opaco y relleno suave, para teñir.</summary>
    private static Sprite AsegurarSpriteRedondeado() => AsegurarSpriteRedondeado(SpritePath, 0.22f);

    private static Sprite AsegurarSpriteRedondeado(string ruta, float alphaInterior)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
        if (sprite != null) return sprite;

        Directory.CreateDirectory(Path.GetDirectoryName(ruta));
        Texture2D tex = new Texture2D(SpriteSize, SpriteSize, TextureFormat.RGBA32, false);
        for (int y = 0; y < SpriteSize; y++)
        {
            for (int x = 0; x < SpriteSize; x++)
            {
                float dx = Mathf.Max(SpriteRadius - (x + 0.5f), 0f, (x + 0.5f) - (SpriteSize - SpriteRadius));
                float dy = Mathf.Max(SpriteRadius - (y + 0.5f), 0f, (y + 0.5f) - (SpriteSize - SpriteRadius));
                // Distancia con signo al borde del rectángulo redondeado (positiva hacia dentro).
                float dentro = SpriteRadius - Mathf.Sqrt(dx * dx + dy * dy);
                if (dx <= 0f && dy <= 0f)
                    dentro = Mathf.Min(Mathf.Min(x + 0.5f, SpriteSize - x - 0.5f), Mathf.Min(y + 0.5f, SpriteSize - y - 0.5f));
                float forma = Mathf.Clamp01(dentro + 0.5f);
                float borde = Mathf.Clamp01(SpriteOutline + 0.5f - dentro);
                float alpha = forma * Mathf.Lerp(alphaInterior, 1f, borde);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();
        File.WriteAllBytes(ruta, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(ruta);

        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(ruta);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.alphaIsTransparency = true;
        importer.mipmapEnabled = false;
        importer.spriteBorder = new Vector4(SpriteRadius + 4, SpriteRadius + 4, SpriteRadius + 4, SpriteRadius + 4);
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
    }

    // ---------- Utilidades ----------

    private static void Destruir(Transform t)
    {
        if (t != null) Object.DestroyImmediate(t.gameObject);
    }

    private static Button Boton(Transform root, string nombre)
    {
        Transform t = root.Find(nombre);
        return t != null ? t.GetComponent<Button>() : null;
    }

    private static void Conectar(Button button, UnityAction action)
    {
        if (button == null) return;
        LimpiarListeners(button);
        UnityEventTools.AddPersistentListener(button.onClick, action);
        button.transition = Selectable.Transition.None;
        EditorUtility.SetDirty(button);
    }

    private static void LimpiarListeners(Button button)
    {
        for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
            UnityEventTools.RemovePersistentListener(button.onClick, i);
    }

    private static void SetNormalizedRect(RectTransform rect, Vector2 centro, Vector2 tam)
    {
        Vector2 half = tam * 0.5f;
        rect.anchorMin = centro - half;
        rect.anchorMax = centro + half;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    private static Transform FindInScene(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
        return null;
    }
}
