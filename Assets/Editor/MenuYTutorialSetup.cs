using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Crea la escena "MenuInicial" (logo, INICIAR y pantalla de carga) y el tutorial "¿Cómo jugar?"
/// dentro de MapaInteractivo, y deja ambas escenas en Build Settings (menú primero).
/// Tools > Mapa interactivo > Crear menú inicial y tutorial.
/// </summary>
internal static class MenuYTutorialSetup
{
    private const string EscenaMenu = "Assets/Scenes/MenuInicial.unity";
    private const string EscenaMapa = "Assets/Scenes/MapaInteractivo.unity";
    private const string UI = "Assets/Mapa/popUps/Inventario/UI/";
    private const string CarpetaMenu = "Assets/Mapa/Menu/";
    private const float RadioSprite = 40f;

    // Paleta "Tunda" de la guía de estilo (Personajes-tipografia.png).
    private static readonly Color VerdeNoche = Hex("1B1E18");
    private static readonly Color VerdeMusgo = Hex("3A4535");
    private static readonly Color VerdeOliva = Hex("5D6B4D");
    private static readonly Color Salvia = Hex("A5A485");
    private static readonly Color Crema = Hex("EFEDDB");
    private static readonly Color Arena = Hex("D9C4A3");
    private static readonly Color CremaTitulo = Hex("E9DCC0");
    private static readonly Color VerdeTinta = Hex("2A3426");
    private static readonly Color Dorado = new Color32(216, 191, 120, 255);
    private static readonly Color Oscuro = new Color32(30, 26, 22, 255);

    private static Sprite redondeado, borde, circulo, anillo, triangulo;
    private static TMP_FontAsset cinzel, grotesk;

    [MenuItem("Tools/Mapa interactivo/Crear menú inicial y tutorial")]
    private static void Crear()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        redondeado = AssetDatabase.LoadAssetAtPath<Sprite>(UI + "Redondeado.png");
        borde = AssetDatabase.LoadAssetAtPath<Sprite>(UI + "Borde redondeado.png");
        circulo = AssetDatabase.LoadAssetAtPath<Sprite>(UI + "Circulo.png");
        anillo = AssetDatabase.LoadAssetAtPath<Sprite>(UI + "Anillo.png");
        triangulo = AssetDatabase.LoadAssetAtPath<Sprite>(UI + "Triangulo.png");
        cinzel = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Cinzel-VariableFont_wght SDF.asset");
        grotesk = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/SpaceGrotesk-VariableFont_wght SDF.asset");
        if (redondeado == null || triangulo == null)
        {
            Debug.LogError("Faltan sprites de UI: ejecuta antes 'Crear inventario...' y 'Conectar audios y videos'.");
            return;
        }

        string escenaAbierta = SceneManager.GetActiveScene().path;
        CrearMenu();
        CrearTutorial();
        ConfigurarBuild();
        if (!string.IsNullOrEmpty(escenaAbierta)) EditorSceneManager.OpenScene(escenaAbierta);
        Debug.Log("Menú inicial y tutorial creados. Build Settings: MenuInicial (0), MapaInteractivo (1).");
    }

    // ===================== MENÚ INICIAL =====================

    private static void CrearMenu()
    {
        Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        GameObject cam = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        cam.tag = "MainCamera";
        Camera c = cam.GetComponent<Camera>();
        c.clearFlags = CameraClearFlags.SolidColor;
        c.backgroundColor = VerdeNoche;
        c.orthographic = true;
        System.Type urp = System.Type.GetType("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData, Unity.RenderPipelines.Universal.Runtime");
        if (urp != null) cam.AddComponent(urp);

        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        GameObject canvasGo = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        Transform canvas = canvasGo.transform;

        // Fondo con degradado vertical
        GameObject fondo = Nodo("Fondo", canvas, typeof(CanvasRenderer), typeof(RawImage));
        Estirar(fondo);
        fondo.GetComponent<RawImage>().texture = TexturaDegradado();
        fondo.GetComponent<RawImage>().raycastTarget = false;

        // Logo (compartido por el menú y la carga)
        TMP_Text logo = Txt("Logo", canvas, "CRÓNICAS\n<size=62%>De</size>\nColombia", cinzel, 118, Color.white,
            TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0, 215), new Vector2(1000, 330), FontStyles.Bold);
        logo.lineSpacing = -22f;
        logo.gameObject.AddComponent<TextoDegradado>();

        // Grupo del menú
        GameObject menu = Nodo("Menú", canvas, typeof(CanvasGroup));
        Estirar(menu);
        Txt("Subtítulo", menu.transform, "Mapa interactivo para promover las\nleyendas del patrimonio cultural", grotesk, 36, Crema,
            TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(0, -50), new Vector2(1000, 110));
        GameObject iniciar = Img("Iniciar", menu.transform, redondeado, new Color(VerdeMusgo.r, VerdeMusgo.g, VerdeMusgo.b, 0.85f), 14);
        Anclar(iniciar, new Vector2(0.5f, 0.5f), new Vector2(0, -250), new Vector2(330, 92));
        Shadow sombra = iniciar.AddComponent<Shadow>();
        sombra.effectColor = new Color(0, 0, 0, 0.35f);
        sombra.effectDistance = new Vector2(0, -8);
        Borde(iniciar.transform, new Color(Crema.r, Crema.g, Crema.b, 0.45f), 14);
        Txt("Texto", iniciar.transform, "INICIAR", grotesk, 32, Crema, TextAlignmentOptions.Center,
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(330, 92), FontStyles.Bold, 2f);
        Button botonIniciar = BotonDe(iniciar);

        // Grupo de carga
        GameObject carga = Nodo("Carga", canvas, typeof(CanvasGroup));
        Estirar(carga);
        GameObject pildora = Img("Píldora", carga.transform, redondeado, new Color(VerdeMusgo.r, VerdeMusgo.g, VerdeMusgo.b, 0.85f), 12);
        Anclar(pildora, new Vector2(0.5f, 0.5f), new Vector2(0, -120), new Vector2(310, 60));
        Borde(pildora.transform, new Color(Crema.r, Crema.g, Crema.b, 0.35f), 12);
        List<Graphic> puntos = new List<Graphic>();
        for (int i = 0; i < 3; i++)
        {
            GameObject p = Img("Punto " + (i + 1), pildora.transform, circulo, Salvia);
            RectTransform r = (RectTransform)p.transform;
            r.anchorMin = r.anchorMax = new Vector2(0, 0.5f);
            r.anchoredPosition = new Vector2(30 + i * 24, 0);
            r.sizeDelta = new Vector2(13, 13);
            puntos.Add(p.GetComponent<Image>());
        }
        Txt("Texto", pildora.transform, "Cargando...", grotesk, 26, Crema, TextAlignmentOptions.Left,
            new Vector2(0, 0.5f), new Vector2(108, 0), new Vector2(190, 60), FontStyles.Bold, 1f, new Vector2(0, 0.5f));

        // Cortina para los fundidos
        GameObject cortina = Img("Cortina", canvas, null, VerdeNoche);
        Estirar(cortina);
        cortina.AddComponent<CanvasGroup>();

        MenuInicial menuInicial = canvasGo.AddComponent<MenuInicial>();
        SerializedObject d = new SerializedObject(menuInicial);
        d.FindProperty("grupoMenu").objectReferenceValue = menu.GetComponent<CanvasGroup>();
        d.FindProperty("grupoCarga").objectReferenceValue = carga.GetComponent<CanvasGroup>();
        d.FindProperty("cortina").objectReferenceValue = cortina.GetComponent<CanvasGroup>();
        d.FindProperty("botonIniciar").objectReferenceValue = botonIniciar;
        SerializedProperty lista = d.FindProperty("puntos");
        lista.arraySize = puntos.Count;
        for (int i = 0; i < puntos.Count; i++) lista.GetArrayElementAtIndex(i).objectReferenceValue = puntos[i];
        d.ApplyModifiedPropertiesWithoutUndo();
        carga.GetComponent<CanvasGroup>().alpha = 0f;

        EditorSceneManager.SaveScene(escena, EscenaMenu);
    }

    private static Texture2D TexturaDegradado()
    {
        string ruta = CarpetaMenu + "Fondo degradado.png";
        if (!File.Exists(ruta))
        {
            Directory.CreateDirectory(CarpetaMenu);
            Texture2D tex = new Texture2D(4, 512, TextureFormat.RGBA32, false);
            for (int y = 0; y < 512; y++)
            {
                Color c = Color.Lerp(Hex("3B4636"), VerdeNoche, Mathf.SmoothStep(0f, 1f, y / 511f));
                for (int x = 0; x < 4; x++) tex.SetPixel(x, y, c);
            }
            tex.Apply();
            File.WriteAllBytes(ruta, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(ruta);
            TextureImporter imp = (TextureImporter)AssetImporter.GetAtPath(ruta);
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.mipmapEnabled = false;
            imp.textureCompression = TextureImporterCompression.Uncompressed; // sin bandas
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(ruta);
    }

    // ===================== TUTORIAL EN EL MAPA =====================

    private static void CrearTutorial()
    {
        Scene escena = EditorSceneManager.OpenScene(EscenaMapa, OpenSceneMode.Single);
        Transform canvas = null;
        foreach (GameObject r in escena.GetRootGameObjects()) if (r.name == "Canvas") canvas = r.transform;
        if (canvas == null) { Debug.LogError("No se encontró el Canvas del mapa."); return; }

        Transform previo = canvas.Find("Tutorial");
        if (previo != null) Object.DestroyImmediate(previo.gameObject);

        GameObject raiz = Nodo("Tutorial", canvas);
        Estirar(raiz);
        raiz.transform.SetAsLastSibling();
        TutorialMapa tutorial = raiz.AddComponent<TutorialMapa>();

        // ---- Botón AYUDA (se oculta con el HUD cuando hay una pantalla grande abierta)
        GameObject grupoAyuda = Nodo("Botón ayuda", raiz.transform, typeof(CanvasGroup));
        Estirar(grupoAyuda);
        HudMapaVisibilidad visHud = canvas.Find("Inventario y progreso/HUD del mapa")?.GetComponent<HudMapaVisibilidad>();
        HudMapaVisibilidad visAyuda = grupoAyuda.AddComponent<HudMapaVisibilidad>();
        if (visHud != null)
        {
            SerializedObject origen = new SerializedObject(visHud);
            SerializedObject destino = new SerializedObject(visAyuda);
            destino.CopyFromSerializedProperty(origen.FindProperty("ocultarSiActivo"));
            destino.ApplyModifiedPropertiesWithoutUndo();
        }
        GameObject ayuda = Img("Ayuda", grupoAyuda.transform, redondeado, new Color(Oscuro.r, Oscuro.g, Oscuro.b, 0.94f), 16);
        Anclar(ayuda, new Vector2(1, 1), new Vector2(-614, -45), new Vector2(140, 58), new Vector2(1, 1));
        Borde(ayuda.transform, new Color(Dorado.r, Dorado.g, Dorado.b, 0.6f), 16);
        Txt("Texto", ayuda.transform, "AYUDA", grotesk, 15, Hex("EDE3CC"), TextAlignmentOptions.Center,
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(140, 58), FontStyles.Bold, 4f);
        UnityEventTools.AddPersistentListener(BotonDe(ayuda).onClick, tutorial.Abrir);

        // ---- Panel
        GameObject panel = Nodo("Panel tutorial", raiz.transform);
        Estirar(panel);
        GameObject dim = Img("Oscurecer", panel.transform, null, new Color(0, 0, 0, 0.35f));
        Estirar(dim);
        GameObject vidrio = Img("Vidrio", panel.transform, redondeado, new Color(1, 1, 1, 0.16f), 30);
        Anclar(vidrio, new Vector2(0.5f, 0.5f), new Vector2(0, -10), new Vector2(1280, 720));
        vidrio.GetComponent<Image>().raycastTarget = false;
        Borde(vidrio.transform, new Color(1, 1, 1, 0.35f), 30);

        GameObject ventana = Img("Ventana", panel.transform, redondeado, VerdeMusgo, 18);
        Anclar(ventana, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1100, 520));
        Shadow sombra = ventana.AddComponent<Shadow>();
        sombra.effectColor = new Color(0, 0, 0, 0.35f);
        sombra.effectDistance = new Vector2(0, -10);
        Borde(ventana.transform, new Color(Salvia.r, Salvia.g, Salvia.b, 0.45f), 18);
        Transform v = ventana.transform;

        GameObject icono = Img("Ícono", v, circulo, CremaTitulo);
        Anclar(icono, new Vector2(0, 1), new Vector2(52, -52), new Vector2(48, 48), new Vector2(0, 1));
        Txt("?", icono.transform, "?", cinzel, 30, VerdeTinta, TextAlignmentOptions.Center,
            new Vector2(0.5f, 0.5f), new Vector2(0, -1), new Vector2(48, 48), FontStyles.Bold);
        Txt("Etiqueta", v, "¿Cómo jugar?", grotesk, 24, CremaTitulo, TextAlignmentOptions.Left,
            new Vector2(0, 1), new Vector2(112, -76), new Vector2(400, 40), FontStyles.Normal, 0f, new Vector2(0, 0.5f));
        TMP_Text titulo = Txt("Título", v, "Desplazamiento", grotesk, 40, CremaTitulo, TextAlignmentOptions.Left,
            new Vector2(0, 1), new Vector2(52, -150), new Vector2(520, 56), FontStyles.Bold, 0f, new Vector2(0, 0.5f));
        TMP_Text texto = Txt("Texto", v, "", grotesk, 23, Crema, TextAlignmentOptions.TopLeft,
            new Vector2(0, 1), new Vector2(52, -190), new Vector2(500, 150), FontStyles.Normal, 0f, new Vector2(0, 1));
        texto.lineSpacing = 6f;

        GameObject probar = Img("Probar", v, redondeado, Arena, 14);
        Anclar(probar, new Vector2(0, 1), new Vector2(52, -352), new Vector2(460, 58), new Vector2(0, 1));
        Txt("Texto", probar.transform, "Probar", grotesk, 23, VerdeTinta, TextAlignmentOptions.Center,
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(460, 58), FontStyles.Bold);
        UnityEventTools.AddPersistentListener(BotonDe(probar).onClick, tutorial.Probar);

        Button[] paginas = new Button[3];
        for (int i = 0; i < 3; i++)
        {
            GameObject p = Img("Página " + (i + 1), v, circulo, CremaTitulo);
            Anclar(p, new Vector2(0, 1), new Vector2(52 + i * 60, -448), new Vector2(46, 46), new Vector2(0, 0.5f));
            GameObject aro = Img("Borde", p.transform, anillo, CremaTitulo);
            Estirar(aro);
            aro.GetComponent<Image>().raycastTarget = false;
            Txt("Número", p.transform, (i + 1).ToString(), grotesk, 20, VerdeTinta, TextAlignmentOptions.Center,
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(46, 46), FontStyles.Bold);
            paginas[i] = BotonDe(p);
        }

        GameObject cerrar = Img("Cerrar", v, null, new Color(1, 1, 1, 0));
        Anclar(cerrar, new Vector2(1, 1), new Vector2(-24, -24), new Vector2(52, 52), new Vector2(1, 1));
        foreach (float a in new[] { 45f, -45f })
        {
            GameObject barra = Img("Aspa", cerrar.transform, redondeado, CremaTitulo, 1.5f);
            Anclar(barra, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(40, 3));
            barra.transform.localEulerAngles = new Vector3(0, 0, a);
            barra.GetComponent<Image>().raycastTarget = false;
        }
        UnityEventTools.AddPersistentListener(BotonDe(cerrar).onClick, tutorial.Cerrar);

        // Ilustración: dos tarjetas giradas, la de delante con el contenido del paso
        GameObject trasera = Img("Tarjeta trasera", v, redondeado, Salvia, 30);
        Anclar(trasera, new Vector2(0.5f, 0.5f), new Vector2(280, 30), new Vector2(400, 290));
        trasera.transform.localEulerAngles = new Vector3(0, 0, -9f);
        trasera.GetComponent<Image>().raycastTarget = false;
        GameObject frente = Img("Tarjeta", v, redondeado, VerdeTinta, 30);
        Anclar(frente, new Vector2(0.5f, 0.5f), new Vector2(270, -10), new Vector2(400, 290));
        frente.transform.localEulerAngles = new Vector3(0, 0, 7f);
        frente.GetComponent<Image>().raycastTarget = false;
        frente.AddComponent<Mask>().showMaskGraphic = true;

        Texture mapaTex = canvas.Find("mapa")?.GetComponent<Image>()?.sprite?.texture;
        GameObject ilu1 = IlustracionDesplazar(frente.transform, mapaTex);
        GameObject ilu2 = IlustracionZoom(frente.transform, mapaTex);
        GameObject ilu3 = IlustracionLeyendas(frente.transform, mapaTex, canvas);
        Borde(frente.transform, CremaTitulo, 30).transform.SetAsLastSibling();

        // ---- Pista mientras se prueba
        GameObject pista = Img("Pista", raiz.transform, redondeado, new Color(VerdeMusgo.r, VerdeMusgo.g, VerdeMusgo.b, 0.96f), 18);
        Anclar(pista, new Vector2(0.5f, 0), new Vector2(0, 150), new Vector2(700, 72), new Vector2(0.5f, 0));
        Borde(pista.transform, new Color(CremaTitulo.r, CremaTitulo.g, CremaTitulo.b, 0.6f), 18);
        pista.GetComponent<Image>().raycastTarget = false;
        GameObject mano = Img("Ícono", pista.transform, circulo, CremaTitulo);
        Anclar(mano, new Vector2(0, 0.5f), new Vector2(22, 0), new Vector2(36, 36), new Vector2(0, 0.5f));
        Txt("?", mano.transform, "?", cinzel, 22, VerdeTinta, TextAlignmentOptions.Center,
            new Vector2(0.5f, 0.5f), new Vector2(0, -1), new Vector2(36, 36), FontStyles.Bold);
        TMP_Text textoPista = Txt("Texto", pista.transform, "", grotesk, 23, CremaTitulo, TextAlignmentOptions.Left,
            new Vector2(0, 0.5f), new Vector2(74, 0), new Vector2(440, 72), FontStyles.Bold, 0f, new Vector2(0, 0.5f));
        GameObject saltar = Img("Saltar", pista.transform, redondeado, new Color(1, 1, 1, 0), 12);
        Anclar(saltar, new Vector2(1, 0.5f), new Vector2(-16, 0), new Vector2(150, 44), new Vector2(1, 0.5f));
        Borde(saltar.transform, new Color(CremaTitulo.r, CremaTitulo.g, CremaTitulo.b, 0.6f), 12);
        Txt("Texto", saltar.transform, "SALTAR", grotesk, 15, CremaTitulo, TextAlignmentOptions.Center,
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(150, 44), FontStyles.Bold, 4f);
        UnityEventTools.AddPersistentListener(BotonDe(saltar).onClick, tutorial.Cerrar);

        // ---- Datos
        SerializedObject d = new SerializedObject(tutorial);
        SerializedProperty pasos = d.FindProperty("pasos");
        pasos.arraySize = 3;
        Paso(pasos.GetArrayElementAtIndex(0), "Desplazamiento",
            "Arrastra el mapa con el dedo (o con el mouse) para recorrer Colombia y descubrir sus regiones.",
            "Arrastra el mapa para moverte", TutorialMapa.Accion.Desplazar, ilu1);
        Paso(pasos.GetArrayElementAtIndex(1), "Acercar y alejar",
            "Pellizca con dos dedos, usa la rueda del mouse o los botones + y - para ver de cerca cada región.",
            "Acerca o aleja el mapa", TutorialMapa.Accion.Zoom, ilu2);
        Paso(pasos.GetArrayElementAtIndex(2), "Descubre las leyendas",
            "Toca un marcador ? para abrir una leyenda. Supera su reto para ganar un objeto y llenar tu barra de progreso.",
            "Toca un marcador ? del mapa", TutorialMapa.Accion.AbrirLeyenda, ilu3);
        d.FindProperty("panel").objectReferenceValue = panel;
        d.FindProperty("titulo").objectReferenceValue = titulo;
        d.FindProperty("texto").objectReferenceValue = texto;
        SerializedProperty listaPaginas = d.FindProperty("paginas");
        listaPaginas.arraySize = paginas.Length;
        for (int i = 0; i < paginas.Length; i++) listaPaginas.GetArrayElementAtIndex(i).objectReferenceValue = paginas[i];
        d.FindProperty("pista").objectReferenceValue = pista;
        d.FindProperty("textoPista").objectReferenceValue = textoPista;
        d.FindProperty("mapa").objectReferenceValue = canvas.Find("mapa") as RectTransform;
        List<GameObject> fichas = new List<GameObject>();
        foreach (string f in new[] { "Tunda_todo/Tunda", "Mohan_todo/Mohan", "Bachue_todo/Bachue", "MadreMonte_Todo/MadreMonte", "Silbon_todo/Silbon" })
            if (canvas.Find(f) != null) fichas.Add(canvas.Find(f).gameObject);
        SerializedProperty listaFichas = d.FindProperty("fichas");
        listaFichas.arraySize = fichas.Count;
        for (int i = 0; i < fichas.Count; i++) listaFichas.GetArrayElementAtIndex(i).objectReferenceValue = fichas[i];
        d.ApplyModifiedPropertiesWithoutUndo();

        MapTouchNavigation nav = canvas.Find("mapa")?.GetComponent<MapTouchNavigation>();
        if (nav != null)
        {
            SerializedObject dn = new SerializedObject(nav);
            dn.FindProperty("tutorialPanel").objectReferenceValue = panel;
            dn.ApplyModifiedPropertiesWithoutUndo();
        }

        panel.SetActive(false);
        pista.SetActive(false);
        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);
    }

    private static void Paso(SerializedProperty p, string titulo, string texto, string pista, TutorialMapa.Accion accion, GameObject ilustracion)
    {
        p.FindPropertyRelative("titulo").stringValue = titulo;
        p.FindPropertyRelative("texto").stringValue = texto;
        p.FindPropertyRelative("pista").stringValue = pista;
        p.FindPropertyRelative("accion").enumValueIndex = (int)accion;
        p.FindPropertyRelative("ilustracion").objectReferenceValue = ilustracion;
    }

    // ---- Ilustraciones (trozo del mapa + íconos dibujados)

    private static GameObject BaseIlustracion(Transform parent, string nombre, Texture mapa, Rect uv)
    {
        GameObject ilu = Nodo(nombre, parent);
        Estirar(ilu);
        GameObject fondo = Nodo("Mapa", ilu.transform, typeof(CanvasRenderer), typeof(RawImage));
        RectTransform r = (RectTransform)fondo.transform;
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
        r.sizeDelta = new Vector2(470, 352); // más grande que la tarjeta: sin esquinas vacías al girarla
        RawImage raw = fondo.GetComponent<RawImage>();
        raw.texture = mapa;
        raw.uvRect = uv;
        raw.color = new Color(0.62f, 0.66f, 0.6f, 1f); // apagado para que resalten los íconos
        raw.raycastTarget = false;
        fondo.transform.localEulerAngles = new Vector3(0, 0, -7f);
        return ilu;
    }

    private static GameObject IlustracionDesplazar(Transform parent, Texture mapa)
    {
        GameObject ilu = BaseIlustracion(parent, "Ilustración desplazar", mapa, new Rect(0.28f, 0.22f, 0.45f, 0.45f));
        GameObject dedo = Img("Dedo", ilu.transform, circulo, new Color(CremaTitulo.r, CremaTitulo.g, CremaTitulo.b, 0.95f));
        Anclar(dedo, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(70, 70));
        GameObject centro = Img("Centro", dedo.transform, circulo, VerdeTinta);
        Anclar(centro, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(26, 26));
        for (int i = 0; i < 4; i++)
        {
            float ang = i * 90f;
            Vector2 dir = Quaternion.Euler(0, 0, ang) * Vector2.right;
            GameObject flecha = Img("Flecha", ilu.transform, triangulo, CremaTitulo);
            Anclar(flecha, new Vector2(0.5f, 0.5f), dir * 82f, new Vector2(26, 30));
            flecha.transform.localEulerAngles = new Vector3(0, 0, ang);
        }
        return ilu;
    }

    private static GameObject IlustracionZoom(Transform parent, Texture mapa)
    {
        GameObject ilu = BaseIlustracion(parent, "Ilustración zoom", mapa, new Rect(0.45f, 0.42f, 0.22f, 0.22f));
        string[] signos = { "+", "-" };
        for (int i = 0; i < 2; i++)
        {
            GameObject b = Img(i == 0 ? "Más" : "Menos", ilu.transform, redondeado, new Color(Oscuro.r, Oscuro.g, Oscuro.b, 0.94f), 14);
            Anclar(b, new Vector2(0.5f, 0.5f), new Vector2(0, 42 - i * 84), new Vector2(68, 68));
            Borde(b.transform, new Color(Dorado.r, Dorado.g, Dorado.b, 0.7f), 14);
            GameObject h = Img("Horizontal", b.transform, redondeado, Hex("EDE3CC"), 2);
            Anclar(h, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(26, 5));
            if (signos[i] == "+")
            {
                GameObject vv = Img("Vertical", b.transform, redondeado, Hex("EDE3CC"), 2);
                Anclar(vv, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(5, 26));
            }
        }
        return ilu;
    }

    private static GameObject IlustracionLeyendas(Transform parent, Texture mapa, Transform canvas)
    {
        GameObject ilu = BaseIlustracion(parent, "Ilustración leyendas", mapa, new Rect(0.3f, 0.3f, 0.42f, 0.42f));
        string[] marcadores = { "mapa/Tu", "mapa/Ba", "mapa/Si" };
        Vector2[] pos = { new Vector2(-95, 20), new Vector2(10, 60), new Vector2(100, -10) };
        for (int i = 0; i < marcadores.Length; i++)
        {
            Texture t = canvas.Find(marcadores[i])?.GetComponent<RawImage>()?.texture;
            GameObject m = Nodo("Marcador", ilu.transform, typeof(CanvasRenderer), typeof(RawImage));
            Anclar(m, new Vector2(0.5f, 0.5f), pos[i], new Vector2(76, 76));
            m.GetComponent<RawImage>().texture = t;
            m.GetComponent<RawImage>().raycastTarget = false;
            if (i == 1)
            {
                GameObject toque = Img("Toque", m.transform, anillo, CremaTitulo);
                Anclar(toque, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(118, 118));
                GameObject dedo = Img("Dedo", m.transform, circulo, new Color(CremaTitulo.r, CremaTitulo.g, CremaTitulo.b, 0.85f));
                Anclar(dedo, new Vector2(0.5f, 0.5f), new Vector2(26, -30), new Vector2(40, 40));
            }
        }
        return ilu;
    }

    // ===================== BUILD SETTINGS =====================

    private static void ConfigurarBuild()
    {
        List<EditorBuildSettingsScene> escenas = new List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(EscenaMenu, true),
            new EditorBuildSettingsScene(EscenaMapa, true),
        };
        foreach (EditorBuildSettingsScene e in EditorBuildSettings.scenes)
            if (e.path != EscenaMenu && e.path != EscenaMapa)
                escenas.Add(new EditorBuildSettingsScene(e.path, false)); // el resto se conserva, desactivado
        EditorBuildSettings.scenes = escenas.ToArray();
    }

    // ===================== UTILIDADES UI =====================

    private static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out Color c);
        return c;
    }

    private static GameObject Nodo(string nombre, Transform parent, params System.Type[] extra)
    {
        List<System.Type> tipos = new List<System.Type> { typeof(RectTransform) };
        tipos.AddRange(extra);
        GameObject go = new GameObject(nombre, tipos.ToArray());
        go.transform.SetParent(parent, false);
        return go;
    }

    private static GameObject Img(string nombre, Transform parent, Sprite sprite, Color color, float radio = 0f)
    {
        GameObject go = Nodo(nombre, parent, typeof(CanvasRenderer), typeof(Image));
        Image img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        if (sprite != null && sprite.border != Vector4.zero)
        {
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = radio > 0f ? RadioSprite / radio : 1f;
        }
        else if (sprite != null) img.preserveAspect = true;
        return go;
    }

    private static Image Borde(Transform parent, Color color, float radio)
    {
        GameObject go = Img("Borde", parent, borde, color, radio);
        Estirar(go);
        Image img = go.GetComponent<Image>();
        img.raycastTarget = false;
        return img;
    }

    private static Button BotonDe(GameObject go)
    {
        Button b = go.AddComponent<Button>();
        Image img = go.GetComponent<Image>();
        img.raycastTarget = true;
        b.targetGraphic = img;
        ColorBlock colores = ColorBlock.defaultColorBlock;
        colores.highlightedColor = Color.white;
        colores.selectedColor = Color.white;
        colores.pressedColor = new Color(0.8f, 0.8f, 0.78f, 1f);
        colores.fadeDuration = 0.1f;
        b.colors = colores;
        b.navigation = new Navigation { mode = Navigation.Mode.None };
        return b;
    }

    private static TMP_Text Txt(string nombre, Transform parent, string valor, TMP_FontAsset fuente, float tam, Color color,
        TextAlignmentOptions alineacion, Vector2 ancla, Vector2 pos, Vector2 caja,
        FontStyles estilo = FontStyles.Normal, float espaciado = 0f, Vector2? pivote = null)
    {
        GameObject go = Nodo(nombre, parent, typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform r = (RectTransform)go.transform;
        r.anchorMin = r.anchorMax = ancla;
        r.pivot = pivote ?? new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos;
        r.sizeDelta = caja;
        TextMeshProUGUI t = go.GetComponent<TextMeshProUGUI>();
        t.font = fuente;
        t.text = valor;
        t.fontSize = tam;
        t.color = color;
        t.alignment = alineacion;
        t.fontStyle = estilo;
        t.characterSpacing = espaciado;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.raycastTarget = false;
        return t;
    }

    private static void Estirar(GameObject go)
    {
        RectTransform r = (RectTransform)go.transform;
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
    }

    private static void Anclar(GameObject go, Vector2 ancla, Vector2 pos, Vector2 tam, Vector2? pivote = null)
    {
        RectTransform r = (RectTransform)go.transform;
        r.anchorMin = r.anchorMax = ancla;
        r.pivot = pivote ?? new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos;
        r.sizeDelta = tam;
    }
}
