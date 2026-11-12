using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Crea una sola vez la jerarquía editable del minijuego de El Silbón dentro de MapaInteractivo
/// (Silbon_todo/SilbonMinijuego) y conecta el botón CONTINUAR de SilbonP2.
/// Después se puede mover / ajustar todo a mano en la escena.
/// </summary>
internal static class SilbonPanelSetup
{
    private const string ScenePath = "Assets/Scenes/MapaInteractivo.unity";
    private const string ArtPath = "Assets/Mapa/popUps/Silbon/";
    private const string AmbientPath = "Assets/Mapa/popUps/sounds/Sonido el silbon - solo ambiente.wav";
    private const string FlowName = "SilbonMinijuego";

    private const string ArteMinijuego = "Silbon vista2.png";
    private const string ArteRecompensa = "Silbón recompensa.png";
    private const string ArteEscucha = "Pop Up Buena.png";
    private const string ArteSalvado = "Pop Up salvado.png";
    private const string ArteAgarrado = "Pop Up incorrecto.png";

    private static readonly Color Granate = new Color32(43, 16, 20, 255);
    private static readonly Color Transparente = new Color(1f, 1f, 1f, 0f);

    // Centro y tamaño normalizados de los 6 puntos dibujados en "Silbon vista2.png" (izquierda a derecha).
    // El área tocable es más grande que el círculo dibujado para que sea fácil de pulsar.
    private static readonly Vector2[] PuntosCentro =
    {
        new Vector2(0.093f, 0.393f),
        new Vector2(0.278f, 0.445f),
        new Vector2(0.473f, 0.359f),
        new Vector2(0.568f, 0.087f),
        new Vector2(0.801f, 0.462f),
        new Vector2(0.930f, 0.364f),
    };
    private static readonly float[] PuntosDiametro = { 0.059f, 0.028f, 0.069f, 0.119f, 0.022f, 0.062f };

    [MenuItem("Tools/Mapa interactivo/Configurar flujo de El Silbón")]
    private static void Configurar() => Construir(false);

    [MenuItem("Tools/Mapa interactivo/Reconstruir flujo de El Silbón (borra ajustes manuales)")]
    private static void Reconstruir() => Construir(true);

    private static void Construir(bool reconstruir)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        Scene scene = EditorSceneManager.GetSceneByPath(ScenePath);
        bool openedHere = !scene.IsValid() || !scene.isLoaded;
        if (openedHere) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);
        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogError("No se pudo abrir MapaInteractivo para configurar El Silbón.");
            return;
        }

        try
        {
            Transform popup = FindInScene(scene, "Silbon_todo");
            Transform video = popup != null ? popup.Find("SilbonP2") : null;
            Button continuar = video != null ? FindButton(video, "Button") : null;
            if (popup == null || video == null || continuar == null)
            {
                Debug.LogError("No se encontró Silbon_todo/SilbonP2/Button en MapaInteractivo.");
                return;
            }

            foreach (string arte in new[] { ArteMinijuego, ArteRecompensa, ArteEscucha, ArteSalvado, ArteAgarrado })
            {
                if (!PrepararTextura(ArtPath + arte))
                {
                    Debug.LogError("Falta el recurso de El Silbón: " + ArtPath + arte);
                    return;
                }
            }

            Transform existente = popup.Find(FlowName);
            if (existente != null)
            {
                if (!reconstruir)
                {
                    Debug.LogWarning("SilbonMinijuego ya existe; no se modificó para conservar los ajustes manuales. " +
                                     "Usa 'Reconstruir flujo de El Silbón' si quieres crearlo de nuevo.");
                    return;
                }
                Object.DestroyImmediate(existente.gameObject);
            }

            SilbonFlowController controller = CrearFlujo(popup, video.GetComponent<RectTransform>());

            // CONTINUAR del video abre el minijuego.
            QuitarListeners(continuar);
            UnityEventTools.AddPersistentListener(continuar.onClick, controller.IniciarMinijuego);
            EditorUtility.SetDirty(continuar);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Minijuego de El Silbón creado en Silbon_todo/SilbonMinijuego. " +
                      "Asigna el audio del silbido en el campo 'Silbido' del SilbonFlowController.");
        }
        finally
        {
            if (openedHere) EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static SilbonFlowController CrearFlujo(Transform popup, RectTransform referencia)
    {
        GameObject root = new GameObject(FlowName, typeof(RectTransform));
        root.transform.SetParent(popup, false);
        RectTransform rootRect = root.GetComponent<RectTransform>();
        CopyRectTransform(referencia, rootRect);
        Canvas canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = 100;
        root.AddComponent<GraphicRaycaster>();

        // Audio
        GameObject silbidoGo = new GameObject("Audio - Silbido");
        silbidoGo.transform.SetParent(root.transform, false);
        AudioSource fuenteSilbido = silbidoGo.AddComponent<AudioSource>();
        fuenteSilbido.playOnAwake = false;
        silbidoGo.AddComponent<AudioLowPassFilter>();
        silbidoGo.AddComponent<AudioReverbFilter>().reverbPreset = AudioReverbPreset.User;

        GameObject ambienteGo = new GameObject("Audio ambiente del llano");
        ambienteGo.transform.SetParent(root.transform, false);
        AudioSource ambiente = ambienteGo.AddComponent<AudioSource>();
        ambiente.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(AmbientPath);
        ambiente.loop = true;
        ambiente.playOnAwake = false;
        ambiente.volume = 0.3f;

        // Tamaño de las páginas: el arte (4:3) ajustado al área de SilbonP2.
        Vector2 area = referencia.rect.size;
        Vector2 tamPagina = Ajustar(TamanoFuente(ArtPath + ArteMinijuego), area);
        float escala = tamPagina.x / TamanoFuente(ArtPath + ArteMinijuego).x;

        // 02 - Minijuego
        GameObject juego = CrearPagina(root.transform, "02 - Escucha el silbido", ArteMinijuego, tamPagina, out Transform arteJuego);
        Button volver = CrearBoton(arteJuego, "Volver atrás", new Vector2(0.10f, 0.932f), new Vector2(0.19f, 0.09f));

        Sprite circulo = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        GameObject marcador = new GameObject("Marcador de selección", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        marcador.transform.SetParent(arteJuego, false);
        Image marcadorImg = marcador.GetComponent<Image>();
        marcadorImg.sprite = circulo;
        marcadorImg.color = new Color32(255, 186, 120, 170);
        marcadorImg.raycastTarget = false;
        marcador.SetActive(false);

        Button[] puntos = new Button[PuntosCentro.Length];
        for (int i = 0; i < puntos.Length; i++)
        {
            float d = PuntosDiametro[i];
            float lado = Mathf.Max(d * 1.6f, 0.07f);            // ancho normalizado del área tocable
            Vector2 tam = new Vector2(lado, lado * tamPagina.x / tamPagina.y);
            puntos[i] = CrearBoton(arteJuego, "Punto " + (i + 1), PuntosCentro[i], tam);
        }
        // El marcador se muestra con el tamaño del círculo dibujado, no del área tocable.
        SetNormalizedRect(marcador.GetComponent<RectTransform>(), PuntosCentro[0], new Vector2(PuntosDiametro[0], PuntosDiametro[0] * tamPagina.x / tamPagina.y));

        // 03 - Recompensa
        GameObject recompensa = CrearPagina(root.transform, "03 - Recompensa", ArteRecompensa, tamPagina, out Transform arteRecompensa);
        Button continuarRecompensa = CrearBoton(arteRecompensa, "Continuar al mapa", new Vector2(0.789f, 0.144f), new Vector2(0.19f, 0.08f));

        // Popups (se ven al tamaño del diseño, escalados igual que la página)
        GameObject escucha = CrearPopup(root.transform, "Popup - Escucha con atención", ArteEscucha, escala * 1.25f, 0.25f, out Transform ventanaEscucha);
        Button cerrarEscucha = CrearBoton(ventanaEscucha, "Cerrar", new Vector2(0.893f, 0.914f), new Vector2(0.16f, 0.17f));
        Button repetir = CrearBoton(ventanaEscucha, "Repetir silbido", new Vector2(0.5f, 0.553f), new Vector2(0.45f, 0.16f));
        Button comprobar = CrearBoton(ventanaEscucha, "Comprobar zona", new Vector2(0.495f, 0.182f), new Vector2(0.58f, 0.13f));

        GameObject salvado = CrearPopup(root.transform, "Popup - Te has salvado", ArteSalvado, escala * 0.7f, 0.45f, out Transform ventanaSalvado);
        Button tocarSalvado = CrearBoton(ventanaSalvado, "Continuar a la recompensa", new Vector2(0.5f, 0.5f), Vector2.one);

        GameObject agarrado = CrearPopup(root.transform, "Popup - El Silbón te ha agarrado", ArteAgarrado, escala * 0.7f, 0.45f, out Transform ventanaAgarrado);
        Button reintentar = CrearBoton(ventanaAgarrado, "Reintentar", new Vector2(0.5f, 0.208f), new Vector2(0.31f, 0.15f));

        juego.SetActive(false);
        recompensa.SetActive(false);

        // Controlador
        SilbonFlowController controller = root.AddComponent<SilbonFlowController>();
        SerializedObject data = new SerializedObject(controller);
        data.FindProperty("pantallaMinijuego").objectReferenceValue = juego;
        data.FindProperty("pantallaRecompensa").objectReferenceValue = recompensa;
        data.FindProperty("popupEscucha").objectReferenceValue = escucha;
        data.FindProperty("popupSalvado").objectReferenceValue = salvado;
        data.FindProperty("popupAgarrado").objectReferenceValue = agarrado;
        data.FindProperty("paginaVideo").objectReferenceValue = referencia.gameObject;
        data.FindProperty("marcadorSeleccion").objectReferenceValue = marcador.GetComponent<RectTransform>();
        data.FindProperty("fuenteSilbido").objectReferenceValue = fuenteSilbido;
        data.FindProperty("ambiente").objectReferenceValue = ambiente;
        SerializedProperty listaPuntos = data.FindProperty("puntos");
        listaPuntos.arraySize = puntos.Length;
        for (int i = 0; i < puntos.Length; i++)
            listaPuntos.GetArrayElementAtIndex(i).objectReferenceValue = puntos[i].GetComponent<RectTransform>();
        SerializedProperty silenciar = data.FindProperty("audiosASilenciar");
        AudioSource audioVideo = referencia.GetComponent<AudioSource>();
        silenciar.arraySize = audioVideo != null ? 1 : 0;
        if (audioVideo != null) silenciar.GetArrayElementAtIndex(0).objectReferenceValue = audioVideo;
        data.ApplyModifiedPropertiesWithoutUndo();

        Wire(volver, controller.VolverAtras);
        for (int i = 0; i < puntos.Length; i++)
        {
            UnityEventTools.AddIntPersistentListener(puntos[i].onClick, controller.ElegirPunto, i);
            EditorUtility.SetDirty(puntos[i]);
        }
        Wire(cerrarEscucha, controller.CerrarPopupEscucha);
        Wire(repetir, controller.RepetirSilbido);
        Wire(comprobar, controller.ComprobarZona);
        Wire(tocarSalvado, controller.MostrarRecompensa);
        Wire(reintentar, controller.Reintentar);
        Wire(continuarRecompensa, controller.ContinuarDesdeRecompensa);
        return controller;
    }

    // ---------- Constructores ----------

    private static GameObject CrearPagina(Transform parent, string nombre, string arte, Vector2 tam, out Transform contenidoArte)
    {
        GameObject pagina = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        pagina.transform.SetParent(parent, false);
        RectTransform rect = pagina.GetComponent<RectTransform>();
        Stretch(rect);
        // Fondo granate a pantalla completa del panel para que no se vea la página del video detrás.
        Image fondo = pagina.GetComponent<Image>();
        fondo.color = Granate;
        fondo.raycastTarget = true;

        // Contenedor con la proporción exacta del arte: los botones se colocan sobre él.
        GameObject contenido = new GameObject("Arte y botones", typeof(RectTransform));
        contenido.transform.SetParent(pagina.transform, false);
        RectTransform contenidoRect = contenido.GetComponent<RectTransform>();
        contenidoRect.anchorMin = contenidoRect.anchorMax = new Vector2(0.5f, 0.5f);
        contenidoRect.sizeDelta = tam;
        CrearArte(contenido.transform, arte);
        contenidoArte = contenido.transform;
        return pagina;
    }

    private static GameObject CrearPopup(Transform parent, string nombre, string arte, float escala, float oscuridad, out Transform ventana)
    {
        GameObject popup = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        popup.transform.SetParent(parent, false);
        Stretch(popup.GetComponent<RectTransform>());
        Image bloqueo = popup.GetComponent<Image>();
        bloqueo.color = new Color(0f, 0f, 0f, oscuridad);
        bloqueo.raycastTarget = true; // evita tocar los puntos mientras el popup está abierto

        GameObject v = new GameObject("Ventana", typeof(RectTransform));
        v.transform.SetParent(popup.transform, false);
        RectTransform vRect = v.GetComponent<RectTransform>();
        vRect.anchorMin = vRect.anchorMax = new Vector2(0.5f, 0.5f);
        vRect.sizeDelta = TamanoFuente(ArtPath + arte) * escala;
        CrearArte(v.transform, arte);
        ventana = v.transform;
        popup.SetActive(false);
        return popup;
    }

    private static void CrearArte(Transform parent, string arte)
    {
        GameObject go = new GameObject("Arte - " + arte, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
        go.transform.SetParent(parent, false);
        Stretch(go.GetComponent<RectTransform>());
        RawImage img = go.GetComponent<RawImage>();
        img.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(ArtPath + arte);
        img.raycastTarget = false;
    }

    private static Button CrearBoton(Transform parent, string nombre, Vector2 centro, Vector2 tam)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        SetNormalizedRect(go.GetComponent<RectTransform>(), centro, tam);
        Image img = go.GetComponent<Image>();
        img.color = Transparente;
        img.raycastTarget = true;
        Button b = go.GetComponent<Button>();
        b.targetGraphic = img;
        b.transition = Selectable.Transition.None;
        b.navigation = new Navigation { mode = Navigation.Mode.None };
        return b;
    }

    // ---------- Utilidades ----------

    private static bool PrepararTextura(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return false;
        // Sin reescalado a potencia de 2 ni mipmaps: el arte se ve nítido y con su proporción real.
        if (importer.npotScale != TextureImporterNPOTScale.None || importer.mipmapEnabled || !importer.alphaIsTransparency)
        {
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path) != null;
    }

    private static Vector2 TamanoFuente(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return new Vector2(1024, 768);
        importer.GetSourceTextureWidthAndHeight(out int w, out int h);
        return new Vector2(w, h);
    }

    private static Vector2 Ajustar(Vector2 fuente, Vector2 area)
    {
        float s = Mathf.Min(area.x / fuente.x, area.y / fuente.y);
        return fuente * s;
    }

    private static void Wire(Button button, UnityAction action)
    {
        UnityEventTools.AddPersistentListener(button.onClick, action);
        EditorUtility.SetDirty(button);
    }

    private static void QuitarListeners(Button button)
    {
        for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
            UnityEventTools.RemovePersistentListener(button.onClick, i);
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    private static void SetNormalizedRect(RectTransform rect, Vector2 centro, Vector2 tam)
    {
        Vector2 half = tam * 0.5f;
        rect.anchorMin = centro - half;
        rect.anchorMax = centro + half;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    private static void CopyRectTransform(RectTransform source, RectTransform target)
    {
        target.anchorMin = source.anchorMin;
        target.anchorMax = source.anchorMax;
        target.pivot = source.pivot;
        target.anchoredPosition = source.anchoredPosition;
        target.sizeDelta = source.sizeDelta;
        target.localScale = source.localScale;
        target.localRotation = source.localRotation;
    }

    private static Button FindButton(Transform root, string name)
    {
        Transform t = root.Find(name);
        return t != null ? t.GetComponent<Button>() : null;
    }

    private static Transform FindInScene(Scene scene, string name)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == name) return t;
        return null;
    }
}
