using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// Conecta los audios y videos de "Assets/Mapa/Contenido CP" a cada leyenda:
/// reproductor de video en la pantalla de video (P2), paisaje sonoro Olvido/Resonancia,
/// botones de sonido funcionales y el silbido real del minijuego de El Silbón.
/// Se puede ejecutar de nuevo (p. ej. al agregar un video): busca el primer VideoClip de la carpeta de cada leyenda.
/// Tools > Mapa interactivo > Conectar audios y videos.
/// </summary>
internal static class MediosSetup
{
    private const string ScenePath = "Assets/Scenes/MapaInteractivo.unity";
    private const string Contenido = "Assets/Mapa/Contenido CP/";
    private const string UI = "Assets/Mapa/popUps/Inventario/UI/";
    private const float RadioSprite = 40f;

    private static readonly Color Crema = new Color32(237, 227, 204, 255);
    private static readonly Color Oscuro = new Color32(30, 26, 22, 255);

    private struct Config
    {
        public string id, nombre, raiz, ficha, video, carpeta, ambienteInicial, ambienteCompletado;
    }

    private static readonly Config[] Leyendas =
    {
        new Config { id = "tunda", nombre = "La Tunda", raiz = "Tunda_todo", ficha = "Tunda", video = "TundaP2",
            carpeta = "Región Pacífica - la Tunda/", ambienteInicial = "Estado olvido.mp3", ambienteCompletado = "Estado de Resonancia.mp3" },
        new Config { id = "mohan", nombre = "El Mohán", raiz = "Mohan_todo", ficha = "Mohan", video = "MohanP2",
            carpeta = "Región Caribe - El Mohan/", ambienteInicial = "Estado olvido.mp3", ambienteCompletado = "Estado de Resonancia.mp3" },
        new Config { id = "madremonte", nombre = "La Madremonte", raiz = "MadreMonte_Todo", ficha = "MadreMonte", video = "MadreMonteP2",
            carpeta = "Región Amazónica - Madremonte/", ambienteInicial = "Estado olvido.mp3", ambienteCompletado = "Estado de Resonancia.mp3" },
        new Config { id = "silbon", nombre = "El Silbón", raiz = "Silbon_todo", ficha = "Silbon", video = "SilbonP2",
            carpeta = "Llanos Orientales - El silbon/", ambienteInicial = "Sonido el silbon - solo ambiente.wav" },
        new Config { id = "bachue", nombre = "Bachué", raiz = "Bachue_todo", ficha = "Bachue", video = "BachueP2",
            carpeta = "Region Andina - Bachue/", ambienteInicial = "Sonido ambiente Bachue.wav" },
    };

    // Tramos del archivo "solo silbido" donde suena cada silbido (medidos sobre la forma de onda).
    private static readonly Vector2[] TramosSilbido =
    {
        new Vector2(0.3f, 3.6f), new Vector2(9.5f, 11.5f), new Vector2(18.6f, 21.7f),
        new Vector2(27.1f, 29.1f), new Vector2(36.3f, 39.35f),
    };

    private static Sprite redondeado, borde, circulo, triangulo;
    private static TMP_FontAsset titulo, texto;

    [MenuItem("Tools/Mapa interactivo/Conectar audios y videos")]
    private static void Conectar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        redondeado = AssetDatabase.LoadAssetAtPath<Sprite>(UI + "Redondeado.png");
        borde = AssetDatabase.LoadAssetAtPath<Sprite>(UI + "Borde redondeado.png");
        circulo = AssetDatabase.LoadAssetAtPath<Sprite>(UI + "Circulo.png");
        triangulo = SpriteTriangulo(UI + "Triangulo.png");
        titulo = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/Cinzel-VariableFont_wght SDF.asset");
        texto = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/SpaceGrotesk-VariableFont_wght SDF.asset");
        if (redondeado == null || circulo == null)
        {
            Debug.LogError("Faltan los sprites de UI. Ejecuta primero 'Crear inventario y barra de progreso'.");
            return;
        }

        AjustarImportacion();

        Scene scene = EditorSceneManager.GetSceneByPath(ScenePath);
        bool openedHere = !scene.IsValid() || !scene.isLoaded;
        if (openedHere) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

        try
        {
            Transform canvas = null;
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.name == "Canvas") canvas = root.transform;
            if (canvas == null) { Debug.LogError("No se encontró el Canvas."); return; }

            List<string> resumen = new List<string>();
            foreach (Config l in Leyendas)
            {
                Transform raiz = canvas.Find(l.raiz);
                if (raiz == null) { Debug.LogWarning("No se encontró " + l.raiz); continue; }

                QuitarAmbientesViejos(raiz);
                CrearAmbiente(raiz, l);
                ConectarBotonesSonido(raiz);

                Transform paginaVideo = raiz.Find(l.video);
                VideoClip clip = BuscarVideo(l.carpeta);
                if (paginaVideo != null) CrearReproductor(paginaVideo, clip, l.nombre);
                resumen.Add(l.nombre + ": " + (clip != null ? "video " + clip.name : "SIN VIDEO (falta un H.264 en " + l.carpeta + ")"));
            }

            ConectarSilbido(canvas);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Audios y videos conectados.\n" + string.Join("\n", resumen));
        }
        finally
        {
            if (openedHere) EditorSceneManager.CloseScene(scene, true);
        }
    }

    // ================= Audio =================

    private static void AjustarImportacion()
    {
        // El ambiente de Bachué dura 2:42: se transmite desde disco en vez de descomprimirse entero en memoria.
        AudioImporter bachue = AssetImporter.GetAtPath(Contenido + "Region Andina - Bachue/Sonido ambiente Bachue.wav") as AudioImporter;
        if (bachue != null && bachue.defaultSampleSettings.loadType != AudioClipLoadType.Streaming)
        {
            AudioImporterSampleSettings s = bachue.defaultSampleSettings;
            s.loadType = AudioClipLoadType.Streaming;
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = 0.7f;
            bachue.defaultSampleSettings = s;
            bachue.SaveAndReimport();
        }

        // El silbido se recorta por tramos en ejecución: necesita "Decompress On Load".
        AudioImporter silbido = AssetImporter.GetAtPath(Contenido + "Llanos Orientales - El silbon/Sonido el silbon - solo silbido.wav") as AudioImporter;
        if (silbido != null && silbido.defaultSampleSettings.loadType != AudioClipLoadType.DecompressOnLoad)
        {
            AudioImporterSampleSettings s = silbido.defaultSampleSettings;
            s.loadType = AudioClipLoadType.DecompressOnLoad;
            silbido.defaultSampleSettings = s;
            silbido.SaveAndReimport();
        }
    }

    /// <summary>Las páginas tenían cada una su AudioSource con ambientes de prueba (casi todas el de Bachué).</summary>
    private static void QuitarAmbientesViejos(Transform raiz)
    {
        foreach (Transform pagina in raiz)
        {
            AudioSource fuente = pagina.GetComponent<AudioSource>();
            if (fuente != null)
            {
                fuente.Stop();
                fuente.clip = null;
                fuente.playOnAwake = false;
                EditorUtility.SetDirty(fuente);
            }
        }

        foreach (string nombre in new[] { "Audio ambiente del Pacífico", "Audio ambiente del llano" })
        {
            foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
            {
                if (t.name != nombre) continue;
                AudioSource fuente = t.GetComponent<AudioSource>();
                if (fuente != null) { fuente.clip = null; EditorUtility.SetDirty(fuente); }
            }
        }

        // Los minijuegos ya no manejan su propio ambiente: lo hace AmbienteLeyenda.
        foreach (MonoBehaviour mb in raiz.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (!(mb is TundaFlowController) && !(mb is SilbonFlowController)) continue;
            SerializedObject d = new SerializedObject(mb);
            d.FindProperty("ambiente").objectReferenceValue = null;
            d.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void CrearAmbiente(Transform raiz, Config l)
    {
        Transform previo = raiz.Find("Ambiente de la leyenda");
        if (previo != null) Object.DestroyImmediate(previo.gameObject);

        GameObject go = new GameObject("Ambiente de la leyenda");
        go.transform.SetParent(raiz, false);
        go.transform.SetAsFirstSibling();
        AmbienteLeyenda ambiente = go.AddComponent<AmbienteLeyenda>();

        SerializedObject d = new SerializedObject(ambiente);
        d.FindProperty("idLeyenda").stringValue = l.id;
        d.FindProperty("ambienteInicial").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(Contenido + l.carpeta + l.ambienteInicial);
        d.FindProperty("ambienteCompletado").objectReferenceValue = string.IsNullOrEmpty(l.ambienteCompletado)
            ? null : AssetDatabase.LoadAssetAtPath<AudioClip>(Contenido + l.carpeta + l.ambienteCompletado);
        List<GameObject> pantallas = Pantallas(raiz, l.ficha);
        SerializedProperty lista = d.FindProperty("pantallas");
        lista.arraySize = pantallas.Count;
        for (int i = 0; i < pantallas.Count; i++) lista.GetArrayElementAtIndex(i).objectReferenceValue = pantallas[i];
        d.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>Ficha + todas las pantallas grandes de la leyenda (en los minijuegos, sus páginas).</summary>
    private static List<GameObject> Pantallas(Transform raiz, string ficha)
    {
        List<GameObject> lista = new List<GameObject>();
        foreach (Transform hijo in raiz)
        {
            if (hijo.name == "Ambiente de la leyenda") continue;
            bool esMinijuego = hijo.GetComponent<TundaFlowController>() != null || hijo.GetComponent<SilbonFlowController>() != null;
            if (!esMinijuego) { lista.Add(hijo.gameObject); continue; }
            foreach (Transform pantalla in hijo)
                if (pantalla.GetComponentInChildren<Graphic>(true) != null) lista.Add(pantalla.gameObject);
        }
        return lista;
    }

    /// <summary>Los íconos de altavoz de cada página ahora silencian/activan el ambiente.</summary>
    private static void ConectarBotonesSonido(Transform raiz)
    {
        foreach (Transform t in raiz.GetComponentsInChildren<Transform>(true))
        {
            if (t.name != "Sonido") continue;
            Transform on = t.Find("Activado");
            Transform off = t.Find("Desactivado (1)");
            if (on == null || off == null) continue;

            foreach (Transform icono in new[] { on, off })
            {
                Button b = icono.GetComponent<Button>();
                if (b == null) continue;
                for (int i = b.onClick.GetPersistentEventCount() - 1; i >= 0; i--)
                    UnityEventTools.RemovePersistentListener(b.onClick, i);
                EditorUtility.SetDirty(b);
            }

            IconoSonidoPagina icon = t.GetComponent<IconoSonidoPagina>();
            if (icon == null) icon = t.gameObject.AddComponent<IconoSonidoPagina>();
            SerializedObject d = new SerializedObject(icon);
            d.FindProperty("activado").objectReferenceValue = on.gameObject;
            d.FindProperty("desactivado").objectReferenceValue = off.gameObject;
            d.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static void ConectarSilbido(Transform canvas)
    {
        SilbonFlowController silbon = canvas.Find("Silbon_todo/SilbonMinijuego")?.GetComponent<SilbonFlowController>();
        if (silbon == null) return;
        SerializedObject d = new SerializedObject(silbon);
        d.FindProperty("silbido").objectReferenceValue = AssetDatabase.LoadAssetAtPath<AudioClip>(
            Contenido + "Llanos Orientales - El silbon/Sonido el silbon - solo silbido.wav");
        SerializedProperty tramos = d.FindProperty("tramosSilbido");
        tramos.arraySize = TramosSilbido.Length;
        for (int i = 0; i < TramosSilbido.Length; i++) tramos.GetArrayElementAtIndex(i).vector2Value = TramosSilbido[i];
        d.FindProperty("normalizarSilbido").boolValue = true;
        d.ApplyModifiedPropertiesWithoutUndo();
    }

    // ================= Video =================

    private static VideoClip BuscarVideo(string carpeta)
    {
        foreach (string guid in AssetDatabase.FindAssets("t:VideoClip", new[] { Contenido.TrimEnd('/') + "/" + carpeta.TrimEnd('/') }))
        {
            VideoClip v = AssetDatabase.LoadAssetAtPath<VideoClip>(AssetDatabase.GUIDToAssetPath(guid));
            if (v != null) return v;
        }
        return null;
    }

    private static void CrearReproductor(Transform pagina, VideoClip clip, string leyenda)
    {
        Transform previo = pagina.Find("Video");
        if (previo != null) Object.DestroyImmediate(previo.gameObject);

        // Color de acento: el del botón CONTINUAR de la propia página.
        Image botonContinuar = pagina.Find("Button")?.GetComponent<Image>();
        Color acento = botonContinuar != null ? botonContinuar.color : new Color32(216, 191, 120, 255);
        acento.a = 1f;

        // Marco 16:9 entre el encabezado y el botón CONTINUAR.
        GameObject raiz = Img("Video", pagina, redondeado, Color.black, 18);
        Anclar(raiz, new Vector2(0.5f, 0.5f), new Vector2(0, 8), new Vector2(880, 495));
        raiz.AddComponent<Mask>().showMaskGraphic = true;
        raiz.transform.SetSiblingIndex(pagina.Find("Button") != null ? pagina.Find("Button").GetSiblingIndex() : pagina.childCount - 1);

        VideoPlayer vp = raiz.AddComponent<VideoPlayer>();
        vp.playOnAwake = false;
        vp.source = VideoSource.VideoClip;
        AudioSource audio = raiz.GetComponent<AudioSource>();
        if (audio == null) audio = raiz.AddComponent<AudioSource>();
        audio.playOnAwake = false;
        ReproductorVideoLeyenda rep = raiz.AddComponent<ReproductorVideoLeyenda>();

        // Imagen del video (tocarla pausa / reanuda)
        GameObject pantalla = Nodo("Pantalla", raiz.transform, typeof(CanvasRenderer), typeof(RawImage));
        Estirar(pantalla);
        RawImage raw = pantalla.GetComponent<RawImage>();
        raw.color = Color.black;
        Boton(pantalla, raw, rep.AlternarReproduccion);

        // Portada con botón grande
        GameObject portada = Img("Portada", raiz.transform, null, new Color(0, 0, 0, 0.45f));
        Estirar(portada);
        Boton(portada, portada.GetComponent<Image>(), rep.Reproducir);
        Txt("Rótulo", portada.transform, "VIDEO", texto, 14, Crema, TextAlignmentOptions.Left,
            new Vector2(0, 1), new Vector2(26, -22), new Vector2(200, 24), FontStyles.Bold, 5f);
        GameObject grande = Img("Botón reproducir", portada.transform, circulo, acento);
        Anclar(grande, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(132, 132));
        grande.GetComponent<Image>().raycastTarget = false;
        GameObject anillo = Img("Aro", grande.transform, circulo, new Color(Oscuro.r, Oscuro.g, Oscuro.b, 0.9f));
        Anclar(anillo, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(96, 96));
        anillo.GetComponent<Image>().raycastTarget = false;
        GameObject tri = Img("Triángulo", grande.transform, triangulo, Crema);
        Anclar(tri, new Vector2(0.5f, 0.5f), new Vector2(5, 0), new Vector2(30, 52));
        tri.GetComponent<Image>().raycastTarget = false;

        // Aviso sin video
        GameObject sinVideo = Img("Sin video", raiz.transform, null, new Color32(42, 36, 30, 255));
        Estirar(sinVideo);
        Txt("Título", sinVideo.transform, "VIDEO EN PREPARACIÓN", titulo, 28, Crema, TextAlignmentOptions.Center,
            new Vector2(0.5f, 0.5f), new Vector2(-400, 40), new Vector2(800, 44), FontStyles.Bold);
        Txt("Texto", sinVideo.transform, "Muy pronto podrás ver aquí la historia de " + leyenda + ".", texto, 18,
            new Color32(168, 156, 134, 255), TextAlignmentOptions.Center, new Vector2(0.5f, 0.5f), new Vector2(-400, -10), new Vector2(800, 30));

        // Barra de controles
        GameObject controles = Img("Controles", raiz.transform, null, new Color(0, 0, 0, 0.55f));
        RectTransform rc = (RectTransform)controles.transform;
        rc.anchorMin = new Vector2(0, 0); rc.anchorMax = new Vector2(1, 0); rc.pivot = new Vector2(0.5f, 0);
        rc.anchoredPosition = Vector2.zero; rc.sizeDelta = new Vector2(0, 56);
        controles.GetComponent<Image>().raycastTarget = true;

        GameObject alternar = Img("Reproducir o pausar", controles.transform, null, new Color(1, 1, 1, 0));
        RectTransform ra = (RectTransform)alternar.transform;
        ra.anchorMin = ra.anchorMax = new Vector2(0, 0.5f); ra.pivot = new Vector2(0, 0.5f);
        ra.anchoredPosition = new Vector2(14, 0); ra.sizeDelta = new Vector2(44, 44);
        Boton(alternar, alternar.GetComponent<Image>(), rep.AlternarReproduccion);
        GameObject iconoPlay = Img("Icono reproducir", alternar.transform, triangulo, Crema);
        Anclar(iconoPlay, new Vector2(0.5f, 0.5f), new Vector2(2, 0), new Vector2(15, 26));
        iconoPlay.GetComponent<Image>().raycastTarget = false;
        GameObject iconoPausa = Nodo("Icono pausa", alternar.transform);
        Anclar(iconoPausa, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(18, 24));
        foreach (float x in new[] { -5f, 5f })
        {
            GameObject barra = Img("Barra", iconoPausa.transform, redondeado, Crema, 2);
            Anclar(barra, new Vector2(0.5f, 0.5f), new Vector2(x, 0), new Vector2(6, 22));
            barra.GetComponent<Image>().raycastTarget = false;
        }
        iconoPausa.SetActive(false);

        GameObject zona = Img("Zona de la barra", controles.transform, null, new Color(1, 1, 1, 0));
        RectTransform rz = (RectTransform)zona.transform;
        rz.anchorMin = new Vector2(0, 0.5f); rz.anchorMax = new Vector2(1, 0.5f); rz.pivot = new Vector2(0.5f, 0.5f);
        rz.offsetMin = new Vector2(72, -16); rz.offsetMax = new Vector2(-150, 16);
        BarraTiempoVideo barraTiempo = zona.AddComponent<BarraTiempoVideo>();
        SerializedObject db = new SerializedObject(barraTiempo);
        db.FindProperty("reproductor").objectReferenceValue = rep;
        db.ApplyModifiedPropertiesWithoutUndo();
        GameObject pista = Img("Pista", zona.transform, redondeado, new Color(1, 1, 1, 0.25f), 4);
        RectTransform rp = (RectTransform)pista.transform;
        rp.anchorMin = new Vector2(0, 0.5f); rp.anchorMax = new Vector2(1, 0.5f);
        rp.offsetMin = new Vector2(0, -4); rp.offsetMax = new Vector2(0, 4);
        pista.GetComponent<Image>().raycastTarget = false;
        GameObject relleno = Img("Relleno", pista.transform, redondeado, acento, 4);
        RectTransform rr = (RectTransform)relleno.transform;
        rr.anchorMin = Vector2.zero; rr.anchorMax = new Vector2(0, 1); rr.pivot = new Vector2(0, 0.5f);
        rr.offsetMin = rr.offsetMax = Vector2.zero;
        relleno.GetComponent<Image>().raycastTarget = false;

        TMP_Text tiempo = Txt("Tiempo", controles.transform, "0:00 / 0:00", texto, 15, Crema, TextAlignmentOptions.Right,
            new Vector2(1, 0.5f), new Vector2(-136, 14), new Vector2(120, 28), FontStyles.Bold, 1f);

        // Borde de acento por encima de todo
        GameObject linea = Img("Borde", raiz.transform, borde, new Color(acento.r, acento.g, acento.b, 0.8f), 18);
        Estirar(linea);
        linea.GetComponent<Image>().raycastTarget = false;

        SerializedObject d = new SerializedObject(rep);
        d.FindProperty("video").objectReferenceValue = clip;
        d.FindProperty("pantalla").objectReferenceValue = raw;
        d.FindProperty("portada").objectReferenceValue = portada;
        d.FindProperty("sinVideo").objectReferenceValue = sinVideo;
        d.FindProperty("controles").objectReferenceValue = controles;
        d.FindProperty("iconoReproducir").objectReferenceValue = iconoPlay;
        d.FindProperty("iconoPausa").objectReferenceValue = iconoPausa;
        d.FindProperty("rellenoProgreso").objectReferenceValue = rr;
        d.FindProperty("tiempo").objectReferenceValue = tiempo;
        d.ApplyModifiedPropertiesWithoutUndo();

        sinVideo.SetActive(clip == null);
        portada.SetActive(clip != null);
        controles.SetActive(clip != null);
    }

    /// <summary>Triángulo blanco (ícono de reproducir) con bordes suavizados, para teñir de cualquier color.</summary>
    private static Sprite SpriteTriangulo(string ruta)
    {
        if (!System.IO.File.Exists(ruta))
        {
            const int w = 64, h = 74;
            Vector2 a = new Vector2(4, 4), b = new Vector2(4, h - 4), c = new Vector2(w - 4, h / 2f);
            Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    // Distancia con signo a cada lado (positiva hacia dentro); el mínimo define el borde.
                    float d = Mathf.Min(Lado(a, b, p), Mathf.Min(Lado(b, c, p), Lado(c, a, p)));
                    tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(d + 0.5f)));
                }
            }
            tex.Apply();
            System.IO.File.WriteAllBytes(ruta, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(ruta);
            TextureImporter imp = (TextureImporter)AssetImporter.GetAtPath(ruta);
            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = true;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
    }

    private static float Lado(Vector2 p1, Vector2 p2, Vector2 p)
    {
        Vector2 dir = (p2 - p1).normalized;
        Vector2 normal = new Vector2(dir.y, -dir.x); // hacia el interior para vértices en sentido horario
        return Vector2.Dot(p - p1, normal);
    }

    // ================= Utilidades UI =================

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

    private static void Boton(GameObject go, Graphic grafico, UnityEngine.Events.UnityAction accion)
    {
        Button b = go.AddComponent<Button>();
        grafico.raycastTarget = true;
        b.targetGraphic = grafico;
        b.transition = Selectable.Transition.None;
        b.navigation = new Navigation { mode = Navigation.Mode.None };
        UnityEventTools.AddPersistentListener(b.onClick, accion);
    }

    private static TMP_Text Txt(string nombre, Transform parent, string valor, TMP_FontAsset fuente, float tam, Color color,
        TextAlignmentOptions alineacion, Vector2 ancla, Vector2 pos, Vector2 caja, FontStyles estilo = FontStyles.Normal, float espaciado = 0f)
    {
        GameObject go = Nodo(nombre, parent, typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform r = (RectTransform)go.transform;
        r.anchorMin = r.anchorMax = ancla;
        r.pivot = new Vector2(0, 1);
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

    private static void Anclar(GameObject go, Vector2 ancla, Vector2 pos, Vector2 tam)
    {
        RectTransform r = (RectTransform)go.transform;
        r.anchorMin = r.anchorMax = ancla;
        r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos;
        r.sizeDelta = tam;
    }
}
