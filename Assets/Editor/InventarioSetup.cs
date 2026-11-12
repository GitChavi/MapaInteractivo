using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Crea en MapaInteractivo la barra de progreso de leyendas y el inventario de objetos.
/// Recorta los íconos de los objetos de las pantallas de recompensa de cada leyenda.
/// Tools > Mapa interactivo > Crear inventario y barra de progreso.
/// </summary>
internal static class InventarioSetup
{
    private const string ScenePath = "Assets/Scenes/MapaInteractivo.unity";
    private const string Carpeta = "Assets/Mapa/popUps/Inventario/";
    private const string RaizNombre = "Inventario y progreso";

    private const string FuenteTitulo = "Assets/Fonts/Cinzel-VariableFont_wght SDF.asset";
    private const string FuenteTexto = "Assets/Fonts/SpaceGrotesk-VariableFont_wght SDF.asset";

    // Paleta tomada de las pantallas de recompensa del proyecto.
    private static readonly Color Oscuro = new Color32(30, 26, 22, 255);
    private static readonly Color Tarjeta = new Color32(42, 36, 30, 255);
    private static readonly Color Pista = new Color32(59, 51, 42, 255);
    private static readonly Color Bloqueado = new Color32(52, 45, 38, 255);
    private static readonly Color Dorado = new Color32(216, 191, 120, 255);
    private static readonly Color Crema = new Color32(237, 227, 204, 255);
    private static readonly Color Apagado = new Color32(168, 156, 134, 255);
    private static readonly Color Verde = new Color32(90, 110, 88, 255);

    private const int RadioSprite = 40; // radio de las esquinas de los sprites redondeados (px)

    private static TMP_FontAsset titulo;
    private static TMP_FontAsset texto;
    private static Sprite redondeado, borde, circulo, anillo;

    private struct Leyenda
    {
        public string id, nombre, region, objeto, descripcion, recompensa, arte;
        public float cx, cy, radio; // recorte del medallón dentro del arte de recompensa (px, y desde arriba)
    }

    private static readonly Leyenda[] Leyendas =
    {
        new Leyenda { id = "tunda", nombre = "La Tunda", region = "Pacífica", objeto = "Molinillo de madera",
            descripcion = "Una pista que revela a La Tunda y rompe su ilusión.",
            recompensa = "Tunda_todo/TundaMinijuego/04 - Recompensa",
            arte = "Assets/Mapa/popUps/Tunda/La Tunda — 04 · Recompensa.png", cx = 250, cy = 470, radio = 94 },
        new Leyenda { id = "mohan", nombre = "El Mohán", region = "Amazonía", objeto = "Piedra de río",
            descripcion = "Simboliza la fuerza indomable de las corrientes del río y el hábitat sumergido del guardián.",
            recompensa = "Mohan_todo/MohanP4",
            arte = "Assets/Mapa/popUps/Mohan/Mohan Recompensa.png", cx = 1361, cy = 690, radio = 150 },
        new Leyenda { id = "madremonte", nombre = "La Madremonte", region = "Andina", objeto = "Báculo",
            descripcion = "Un bastón adornado con cristales que conecta con la energía de la selva.",
            recompensa = "MadreMonte_Todo/MadreMonteP4",
            arte = "Assets/Mapa/popUps/Madremonte/Group 62.png", cx = 638, cy = 256, radio = 74 },
        new Leyenda { id = "bachue", nombre = "Bachué", region = "Andina", objeto = "Esmeralda en bruto",
            descripcion = "Una memoria que permanece: la laguna de Iguaque ha despertado.",
            recompensa = "Bachue_todo/BachueP5",
            arte = "Assets/Mapa/popUps/Bachue/Bachué — 03 · Recompensa.png", cx = 821, cy = 449, radio = 98 },
        new Leyenda { id = "silbon", nombre = "El Silbón", region = "Orinoquía", objeto = "Hueso desgastado",
            descripcion = "Un recuerdo de los caminos que nunca debiste recorrer.",
            recompensa = "Silbon_todo/SilbonMinijuego/03 - Recompensa",
            arte = "Assets/Mapa/popUps/Silbon/Silbón recompensa.png", cx = 1361, cy = 692, radio = 148 },
    };

    [MenuItem("Tools/Mapa interactivo/Crear inventario y barra de progreso")]
    private static void Crear()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) return;

        titulo = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FuenteTitulo);
        texto = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FuenteTexto);
        redondeado = SpriteRedondeado("UI/Redondeado.png", 0, 1f);
        borde = SpriteRedondeado("UI/Borde redondeado.png", 3, 0f);
        circulo = SpriteCirculo("UI/Circulo.png", 0f);
        anillo = SpriteCirculo("UI/Anillo.png", 10f);

        Scene scene = EditorSceneManager.GetSceneByPath(ScenePath);
        bool openedHere = !scene.IsValid() || !scene.isLoaded;
        if (openedHere) scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Additive);

        try
        {
            Transform canvas = Buscar(scene, "Canvas");
            if (canvas == null) { Debug.LogError("No se encontró el Canvas de MapaInteractivo."); return; }

            Transform previo = canvas.Find(RaizNombre);
            if (previo != null) Object.DestroyImmediate(previo.gameObject);

            GameObject raiz = Nodo(RaizNombre, canvas);
            Estirar(Rect(raiz));
            raiz.transform.SetAsLastSibling();

            ProgresoLeyendas progreso = raiz.AddComponent<ProgresoLeyendas>();
            InventarioPanel inventario = raiz.AddComponent<InventarioPanel>();

            // ---- Datos de las leyendas
            SerializedObject datos = new SerializedObject(progreso);
            SerializedProperty lista = datos.FindProperty("leyendas");
            lista.arraySize = Leyendas.Length;
            for (int i = 0; i < Leyendas.Length; i++)
            {
                Leyenda l = Leyendas[i];
                SerializedProperty e = lista.GetArrayElementAtIndex(i);
                e.FindPropertyRelative("id").stringValue = l.id;
                e.FindPropertyRelative("nombreLeyenda").stringValue = l.nombre;
                e.FindPropertyRelative("region").stringValue = l.region;
                e.FindPropertyRelative("objeto").stringValue = l.objeto;
                e.FindPropertyRelative("descripcion").stringValue = l.descripcion;
                e.FindPropertyRelative("icono").objectReferenceValue = Icono(l);
                Transform pantalla = canvas.Find(l.recompensa);
                if (pantalla == null) Debug.LogWarning("No se encontró la recompensa de " + l.nombre + ": " + l.recompensa);
                e.FindPropertyRelative("pantallaRecompensa").objectReferenceValue = pantalla != null ? pantalla.gameObject : null;
            }
            datos.ApplyModifiedPropertiesWithoutUndo();

            // ---- HUD del mapa
            GameObject hud = Nodo("HUD del mapa", raiz.transform, typeof(CanvasGroup));
            Estirar(Rect(hud));
            hud.AddComponent<HudMapaVisibilidad>();
            SerializedObject datosHud = new SerializedObject(hud.GetComponent<HudMapaVisibilidad>());
            List<GameObject> ocultar = PantallasGrandes(canvas);
            SerializedProperty listaOcultar = datosHud.FindProperty("ocultarSiActivo");
            listaOcultar.arraySize = ocultar.Count;
            for (int i = 0; i < ocultar.Count; i++) listaOcultar.GetArrayElementAtIndex(i).objectReferenceValue = ocultar[i];
            datosHud.ApplyModifiedPropertiesWithoutUndo();

            Button botonInventario = CrearHud(hud.transform, progreso, out GameObject indicador, out TMP_Text numero);

            // ---- Panel del inventario
            GameObject panel = CrearPanel(raiz.transform, progreso, inventario, out RanuraInventario[] ranuras,
                out Image detIcono, out GameObject detBloqueado, out Image detAnillo, out TMP_Text detTitulo, out TMP_Text detTexto);

            SerializedObject datosInv = new SerializedObject(inventario);
            datosInv.FindProperty("progreso").objectReferenceValue = progreso;
            datosInv.FindProperty("panel").objectReferenceValue = panel;
            SerializedProperty listaRanuras = datosInv.FindProperty("ranuras");
            listaRanuras.arraySize = ranuras.Length;
            for (int i = 0; i < ranuras.Length; i++) listaRanuras.GetArrayElementAtIndex(i).objectReferenceValue = ranuras[i];
            datosInv.FindProperty("botonAbrir").objectReferenceValue = botonInventario;
            datosInv.FindProperty("indicadorNuevos").objectReferenceValue = indicador;
            datosInv.FindProperty("numeroNuevos").objectReferenceValue = numero;
            datosInv.FindProperty("detalleIcono").objectReferenceValue = detIcono;
            datosInv.FindProperty("detalleBloqueado").objectReferenceValue = detBloqueado;
            datosInv.FindProperty("detalleAnillo").objectReferenceValue = detAnillo;
            datosInv.FindProperty("detalleTitulo").objectReferenceValue = detTitulo;
            datosInv.FindProperty("detalleTexto").objectReferenceValue = detTexto;
            datosInv.ApplyModifiedPropertiesWithoutUndo();

            UnityEventTools.AddPersistentListener(botonInventario.onClick, inventario.Abrir);

            // El mapa no se arrastra mientras el inventario está abierto.
            MapTouchNavigation nav = canvas.Find("mapa")?.GetComponent<MapTouchNavigation>();
            if (nav != null)
            {
                SerializedObject datosNav = new SerializedObject(nav);
                datosNav.FindProperty("inventoryPanel").objectReferenceValue = panel;
                datosNav.ApplyModifiedPropertiesWithoutUndo();
            }

            panel.SetActive(false);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("Inventario y barra de progreso creados en Canvas/" + RaizNombre + ".");
        }
        finally
        {
            if (openedHere) EditorSceneManager.CloseScene(scene, true);
        }
    }

    // ================= HUD =================

    private static Button CrearHud(Transform parent, ProgresoLeyendas progreso, out GameObject indicador, out TMP_Text numero)
    {
        GameObject tarjeta = Imagen("Tarjeta de progreso", parent, redondeado, new Color(Oscuro.r, Oscuro.g, Oscuro.b, 0.94f), 22);
        Anclar(Rect(tarjeta), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-28, -24), new Vector2(570, 100));
        Borde(tarjeta.transform, new Color(Dorado.r, Dorado.g, Dorado.b, 0.55f), 22);

        Texto("Etiqueta", tarjeta.transform, "LEYENDAS DESCUBIERTAS", texto, 15, Dorado, TextAlignmentOptions.Left,
            new Vector2(0, 1), new Vector2(24, -18), new Vector2(240, 30), FontStyles.Bold, 5f);
        TMP_Text contador = Texto("Contador", tarjeta.transform, "0 / 5", titulo, 28, Crema, TextAlignmentOptions.Right,
            new Vector2(0, 1), new Vector2(240, -12), new Vector2(100, 40), FontStyles.Bold);
        RectTransform relleno = Barra(tarjeta.transform, new Vector2(24, -62), 316, 16);

        BarraProgresoLeyendas barra = tarjeta.AddComponent<BarraProgresoLeyendas>();
        SerializedObject datosBarra = new SerializedObject(barra);
        datosBarra.FindProperty("progreso").objectReferenceValue = progreso;
        datosBarra.FindProperty("relleno").objectReferenceValue = relleno;
        datosBarra.FindProperty("contador").objectReferenceValue = contador;
        datosBarra.FindProperty("destacarAlAvanzar").objectReferenceValue = contador.rectTransform;
        datosBarra.ApplyModifiedPropertiesWithoutUndo();

        GameObject separador = Imagen("Separador", tarjeta.transform, null, new Color(Dorado.r, Dorado.g, Dorado.b, 0.25f));
        Anclar(Rect(separador), new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(363, 0), new Vector2(2, 60));

        GameObject boton = Imagen("Botón inventario", tarjeta.transform, redondeado, Crema, 12);
        Anclar(Rect(boton), new Vector2(1, 0.5f), new Vector2(1, 0.5f), new Vector2(-20, 0), new Vector2(164, 58));
        Button b = BotonDe(boton);
        Texto("Texto", boton.transform, "INVENTARIO", texto, 18, Tarjeta, TextAlignmentOptions.Center,
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(164, 58), FontStyles.Bold, 4f, centrado: true);

        indicador = Imagen("Indicador de nuevos", boton.transform, circulo, Verde);
        Anclar(Rect(indicador), new Vector2(1, 1), new Vector2(0.5f, 0.5f), new Vector2(-6, -6), new Vector2(30, 30));
        Image aro = Imagen("Aro", indicador.transform, anillo, Crema).GetComponent<Image>();
        Estirar(aro.rectTransform);
        numero = Texto("Número", indicador.transform, "1", texto, 15, Crema, TextAlignmentOptions.Center,
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(30, 30), FontStyles.Bold, 0f, centrado: true);
        indicador.SetActive(false);
        return b;
    }

    // ================= Panel =================

    private static GameObject CrearPanel(Transform parent, ProgresoLeyendas progreso, InventarioPanel inventario,
        out RanuraInventario[] ranuras, out Image detIcono, out GameObject detBloqueado, out Image detAnillo,
        out TMP_Text detTitulo, out TMP_Text detTexto)
    {
        GameObject panel = Nodo("Panel inventario", parent);
        Estirar(Rect(panel));

        GameObject fondo = Imagen("Oscurecer", panel.transform, null, new Color(0, 0, 0, 0.72f));
        Estirar(Rect(fondo));
        UnityEventTools.AddPersistentListener(BotonDe(fondo).onClick, inventario.Cerrar); // tocar fuera cierra

        GameObject ventana = Imagen("Ventana", panel.transform, redondeado, Oscuro, 28);
        Anclar(Rect(ventana), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1180, 750));
        Borde(ventana.transform, new Color(Dorado.r, Dorado.g, Dorado.b, 0.6f), 28);
        ventana.GetComponent<Image>().raycastTarget = true; // tocar la ventana no la cierra

        // Encabezado
        GameObject pill = Imagen("Etiqueta", ventana.transform, redondeado, Bloqueado, 20);
        Anclar(Rect(pill), new Vector2(0, 1), new Vector2(0, 1), new Vector2(48, -40), new Vector2(360, 40));
        Texto("Texto", pill.transform, "CRÓNICAS DE COLOMBIA · COLECCIÓN", texto, 14, Dorado, TextAlignmentOptions.Center,
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(360, 40), FontStyles.Bold, 3f, centrado: true);
        Texto("Título", ventana.transform, "INVENTARIO", titulo, 46, Crema, TextAlignmentOptions.Left,
            new Vector2(0, 1), new Vector2(46, -92), new Vector2(600, 64), FontStyles.Bold);
        Texto("Subtítulo", ventana.transform, "Objetos recuperados al completar cada leyenda del mapa.", texto, 19, Apagado,
            TextAlignmentOptions.Left, new Vector2(0, 1), new Vector2(48, -154), new Vector2(640, 30));

        // Progreso dentro del panel
        GameObject bloque = Nodo("Progreso", ventana.transform);
        Anclar(Rect(bloque), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-120, -58), new Vector2(330, 70));
        TMP_Text contador = Texto("Contador", bloque.transform, "0 / 5 objetos", texto, 16, Dorado, TextAlignmentOptions.Right,
            new Vector2(0, 1), new Vector2(0, 0), new Vector2(330, 28), FontStyles.Bold, 3f);
        RectTransform relleno = Barra(bloque.transform, new Vector2(0, -40), 330, 14);
        BarraProgresoLeyendas barra = bloque.AddComponent<BarraProgresoLeyendas>();
        SerializedObject datosBarra = new SerializedObject(barra);
        datosBarra.FindProperty("progreso").objectReferenceValue = progreso;
        datosBarra.FindProperty("relleno").objectReferenceValue = relleno;
        datosBarra.FindProperty("contador").objectReferenceValue = contador;
        datosBarra.FindProperty("formatoContador").stringValue = "{0} / {1}  OBJETOS";
        datosBarra.ApplyModifiedPropertiesWithoutUndo();

        // Cerrar
        GameObject cerrar = Imagen("Cerrar", ventana.transform, circulo, Crema);
        Anclar(Rect(cerrar), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-36, -36), new Vector2(56, 56));
        UnityEventTools.AddPersistentListener(BotonDe(cerrar).onClick, inventario.Cerrar);
        foreach (float angulo in new[] { 45f, -45f })
        {
            GameObject barraX = Imagen("Aspa", cerrar.transform, redondeado, Tarjeta, 2);
            Anclar(Rect(barraX), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(26, 4));
            barraX.transform.localEulerAngles = new Vector3(0, 0, angulo);
            barraX.GetComponent<Image>().raycastTarget = false;
        }

        // Tarjetas
        const float ancho = 200f, alto = 320f, espacio = 22f;
        float inicio = -(Leyendas.Length * ancho + (Leyendas.Length - 1) * espacio) / 2f + ancho / 2f;
        ranuras = new RanuraInventario[Leyendas.Length];
        for (int i = 0; i < Leyendas.Length; i++)
            ranuras[i] = CrearRanura(ventana.transform, i, new Vector2(inicio + i * (ancho + espacio), -200), new Vector2(ancho, alto));

        // Detalle
        GameObject detalle = Imagen("Detalle", ventana.transform, redondeado, Tarjeta, 20);
        Anclar(Rect(detalle), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 36), new Vector2(1088, 140));
        Borde(detalle.transform, new Color(Dorado.r, Dorado.g, Dorado.b, 0.3f), 20);
        Medallon(detalle.transform, new Vector2(0, 0.5f), new Vector2(0, 0.5f), new Vector2(28, 0), 100, 24,
            out detAnillo, out detIcono, out detBloqueado);
        detTitulo = Texto("Título", detalle.transform, "Tu colección", titulo, 26, Crema, TextAlignmentOptions.Left,
            new Vector2(0, 1), new Vector2(156, -20), new Vector2(900, 38), FontStyles.Bold);
        detTexto = Texto("Texto", detalle.transform, "", texto, 18, new Color32(220, 210, 190, 255), TextAlignmentOptions.TopLeft,
            new Vector2(0, 1), new Vector2(156, -60), new Vector2(900, 70));
        return panel;
    }

    private static RanuraInventario CrearRanura(Transform parent, int indice, Vector2 pos, Vector2 tam)
    {
        GameObject tarjeta = Imagen("Objeto " + (indice + 1), parent, redondeado, Tarjeta, 20);
        Anclar(Rect(tarjeta), new Vector2(0.5f, 1), new Vector2(0.5f, 1), pos, tam);
        Button boton = BotonDe(tarjeta);
        Image contorno = Borde(tarjeta.transform, Dorado, 20);

        GameObject seleccion = Imagen("Selección", tarjeta.transform, borde, Crema, 18);
        Rect(seleccion).anchorMin = Vector2.zero;
        Rect(seleccion).anchorMax = Vector2.one;
        Rect(seleccion).offsetMin = new Vector2(-6, -6);
        Rect(seleccion).offsetMax = new Vector2(6, 6);
        seleccion.GetComponent<Image>().raycastTarget = false;
        seleccion.SetActive(false);

        Medallon(tarjeta.transform, new Vector2(0.5f, 1), new Vector2(0.5f, 1), new Vector2(0, -24), 132, 30,
            out Image aro, out Image icono, out GameObject bloqueado);

        TMP_Text nombre = Texto("Nombre", tarjeta.transform, "? ? ?", titulo, 19, Crema, TextAlignmentOptions.Center,
            new Vector2(0.5f, 1), new Vector2(-90, -164), new Vector2(180, 50), FontStyles.Bold);
        nombre.enableAutoSizing = true;
        nombre.fontSizeMin = 14;
        nombre.fontSizeMax = 19;
        TMP_Text leyenda = Texto("Leyenda", tarjeta.transform, "", texto, 15, Apagado, TextAlignmentOptions.Top,
            new Vector2(0.5f, 1), new Vector2(-90, -216), new Vector2(180, 44));

        GameObject estado = Imagen("Estado", tarjeta.transform, redondeado, Verde, 15);
        Anclar(Rect(estado), new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(0, 16), new Vector2(132, 30));
        estado.GetComponent<Image>().raycastTarget = false;
        TMP_Text textoEstado = Texto("Texto", estado.transform, "BLOQUEADO", texto, 12, Crema, TextAlignmentOptions.Center,
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(132, 30), FontStyles.Bold, 4f, centrado: true);

        GameObject nuevo = Imagen("Nuevo", tarjeta.transform, redondeado, Dorado, 12);
        Anclar(Rect(nuevo), new Vector2(1, 1), new Vector2(1, 1), new Vector2(-10, -10), new Vector2(70, 24));
        nuevo.GetComponent<Image>().raycastTarget = false;
        Texto("Texto", nuevo.transform, "NUEVO", texto, 11, Tarjeta, TextAlignmentOptions.Center,
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(70, 24), FontStyles.Bold, 4f, centrado: true);
        nuevo.SetActive(false);

        RanuraInventario ranura = tarjeta.AddComponent<RanuraInventario>();
        SerializedObject d = new SerializedObject(ranura);
        d.FindProperty("boton").objectReferenceValue = boton;
        d.FindProperty("borde").objectReferenceValue = contorno;
        d.FindProperty("anillo").objectReferenceValue = aro;
        d.FindProperty("icono").objectReferenceValue = icono;
        d.FindProperty("bloqueado").objectReferenceValue = bloqueado;
        d.FindProperty("nombre").objectReferenceValue = nombre;
        d.FindProperty("leyenda").objectReferenceValue = leyenda;
        d.FindProperty("fondoEstado").objectReferenceValue = estado.GetComponent<Image>();
        d.FindProperty("textoEstado").objectReferenceValue = textoEstado;
        d.FindProperty("marcaNueva").objectReferenceValue = nuevo;
        d.FindProperty("seleccion").objectReferenceValue = seleccion;
        d.ApplyModifiedPropertiesWithoutUndo();
        return ranura;
    }

    /// <summary>Medallón: anillo dorado + ícono circular, o un "?" si está bloqueado.</summary>
    private static void Medallon(Transform parent, Vector2 ancla, Vector2 pivote, Vector2 pos, float tam, float margen,
        out Image aro, out Image icono, out GameObject bloqueado)
    {
        GameObject medallon = Nodo("Medallón", parent);
        Anclar(Rect(medallon), ancla, pivote, pos, new Vector2(tam, tam));

        GameObject fondoIcono = Imagen("Fondo", medallon.transform, circulo, Bloqueado);
        Anclar(Rect(fondoIcono), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(tam - 8, tam - 8));
        fondoIcono.GetComponent<Image>().raycastTarget = false;

        bloqueado = Nodo("Bloqueado", medallon.transform);
        Estirar(Rect(bloqueado));
        Texto("Interrogación", bloqueado.transform, "?", titulo, tam * 0.42f, Apagado, TextAlignmentOptions.Center,
            new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(tam, tam), FontStyles.Bold, 0f, centrado: true);

        GameObject img = Imagen("Ícono", medallon.transform, null, Color.white);
        Anclar(Rect(img), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(tam - margen * 0.5f, tam - margen * 0.5f));
        icono = img.GetComponent<Image>();
        icono.preserveAspect = true;
        icono.raycastTarget = false;
        img.SetActive(false);

        GameObject ring = Imagen("Anillo", medallon.transform, anillo, Dorado);
        Estirar(Rect(ring));
        aro = ring.GetComponent<Image>();
        aro.raycastTarget = false;
    }

    /// <summary>Pista de la barra con relleno y 4 divisiones (una por leyenda). Devuelve el relleno.</summary>
    private static RectTransform Barra(Transform parent, Vector2 pos, float ancho, float alto)
    {
        GameObject pista = Imagen("Barra", parent, redondeado, Pista, alto / 2f);
        Anclar(Rect(pista), new Vector2(0, 1), new Vector2(0, 1), pos, new Vector2(ancho, alto));
        pista.GetComponent<Image>().raycastTarget = false;

        GameObject relleno = Imagen("Relleno", pista.transform, redondeado, Dorado, alto / 2f);
        RectTransform r = Rect(relleno);
        r.anchorMin = Vector2.zero;
        r.anchorMax = new Vector2(0, 1);
        r.offsetMin = r.offsetMax = Vector2.zero;
        r.pivot = new Vector2(0, 0.5f);
        relleno.GetComponent<Image>().raycastTarget = false;

        for (int i = 1; i < Leyendas.Length; i++)
        {
            GameObject marca = Imagen("División " + i, pista.transform, null, Oscuro);
            RectTransform m = Rect(marca);
            m.anchorMin = new Vector2((float)i / Leyendas.Length, 0);
            m.anchorMax = new Vector2((float)i / Leyendas.Length, 1);
            m.sizeDelta = new Vector2(3, 0);
            m.anchoredPosition = Vector2.zero;
            marca.GetComponent<Image>().raycastTarget = false;
        }
        relleno.SetActive(false);
        return r;
    }

    // ================= Íconos y sprites =================

    private static Sprite Icono(Leyenda l)
    {
        string ruta = Carpeta + "Iconos/" + l.objeto + ".png";
        if (!File.Exists(ruta))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(ruta));
            Texture2D fuente = new Texture2D(2, 2);
            fuente.LoadImage(File.ReadAllBytes(l.arte));
            const int s = 256;
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            float cy = fuente.height - l.cy;
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float u = (x + 0.5f) / s * 2f - 1f, v = (y + 0.5f) / s * 2f - 1f;
                    Color c = fuente.GetPixelBilinear((l.cx + u * l.radio) / fuente.width, (cy + v * l.radio) / fuente.height);
                    c.a *= Mathf.Clamp01(s / 2f - Mathf.Sqrt(u * u + v * v) * s / 2f);
                    tex.SetPixel(x, y, c);
                }
            }
            tex.Apply();
            File.WriteAllBytes(ruta, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            Object.DestroyImmediate(fuente);
            ImportarSprite(ruta, Vector4.zero, true);
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
    }

    private static Sprite SpriteRedondeado(string nombre, int grosorBorde, float alphaInterior)
    {
        string ruta = Carpeta + nombre;
        if (!File.Exists(ruta))
        {
            const int s = 128;
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    // Distancia con signo a un rectángulo redondeado (positiva hacia dentro).
                    float mitad = s / 2f;
                    float qx = Mathf.Abs(x + 0.5f - mitad) - (mitad - RadioSprite);
                    float qy = Mathf.Abs(y + 0.5f - mitad) - (mitad - RadioSprite);
                    float fuera = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude;
                    float dentro = -(fuera + Mathf.Min(Mathf.Max(qx, qy), 0f) - RadioSprite);
                    float forma = Mathf.Clamp01(dentro + 0.5f);
                    float alpha = grosorBorde <= 0 ? forma : forma * Mathf.Lerp(alphaInterior, 1f, Mathf.Clamp01(grosorBorde + 0.5f - dentro));
                    tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
                }
            }
            Guardar(tex, ruta);
            ImportarSprite(ruta, Vector4.one * (RadioSprite + 4), false);
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
    }

    private static Sprite SpriteCirculo(string nombre, float grosorAnillo)
    {
        string ruta = Carpeta + nombre;
        if (!File.Exists(ruta))
        {
            const int s = 256;
            Texture2D tex = new Texture2D(s, s, TextureFormat.RGBA32, false);
            for (int y = 0; y < s; y++)
            {
                for (int x = 0; x < s; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(s / 2f, s / 2f));
                    float dentro = s / 2f - d;
                    float alpha = Mathf.Clamp01(dentro + 0.5f);
                    if (grosorAnillo > 0f) alpha *= Mathf.Clamp01(grosorAnillo + 0.5f - dentro);
                    tex.SetPixel(x, y, new Color(1, 1, 1, alpha));
                }
            }
            Guardar(tex, ruta);
            ImportarSprite(ruta, Vector4.zero, true);
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
    }

    private static void Guardar(Texture2D tex, string ruta)
    {
        tex.Apply();
        Directory.CreateDirectory(Path.GetDirectoryName(ruta));
        File.WriteAllBytes(ruta, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
    }

    private static void ImportarSprite(string ruta, Vector4 bordes, bool mipmaps)
    {
        AssetDatabase.ImportAsset(ruta);
        TextureImporter imp = (TextureImporter)AssetImporter.GetAtPath(ruta);
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.alphaIsTransparency = true;
        imp.mipmapEnabled = mipmaps;
        imp.spriteBorder = bordes;
        imp.SaveAndReimport();
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

    /// <summary>Imagen; radio = radio de esquina deseado en unidades de UI (0 = sin escalar).</summary>
    private static GameObject Imagen(string nombre, Transform parent, Sprite sprite, Color color, float radio = 0f)
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
        return go;
    }

    private static Image Borde(Transform parent, Color color, float radio)
    {
        GameObject go = Imagen("Borde", parent, borde, color, radio);
        Estirar(Rect(go));
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
        ColorBlock colores = b.colors;
        colores.highlightedColor = new Color(1f, 1f, 1f, 1f);
        colores.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        colores.selectedColor = Color.white;
        b.colors = colores;
        b.navigation = new Navigation { mode = Navigation.Mode.None };
        return b;
    }

    private static TMP_Text Texto(string nombre, Transform parent, string valor, TMP_FontAsset fuente, float tam, Color color,
        TextAlignmentOptions alineacion, Vector2 ancla, Vector2 pos, Vector2 caja,
        FontStyles estilo = FontStyles.Normal, float espaciado = 0f, bool centrado = false)
    {
        GameObject go = Nodo(nombre, parent, typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        RectTransform r = Rect(go);
        r.anchorMin = r.anchorMax = ancla;
        r.pivot = centrado ? new Vector2(0.5f, 0.5f) : new Vector2(0, 1);
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
        t.overflowMode = TextOverflowModes.Overflow;
        t.raycastTarget = false;
        return t;
    }

    private static RectTransform Rect(GameObject go) => go.GetComponent<RectTransform>();

    private static void Estirar(RectTransform r)
    {
        r.anchorMin = Vector2.zero;
        r.anchorMax = Vector2.one;
        r.offsetMin = r.offsetMax = Vector2.zero;
        r.pivot = new Vector2(0.5f, 0.5f);
    }

    private static void Anclar(RectTransform r, Vector2 ancla, Vector2 pivote, Vector2 pos, Vector2 tam)
    {
        r.anchorMin = r.anchorMax = ancla;
        r.pivot = pivote;
        r.anchoredPosition = pos;
        r.sizeDelta = tam;
    }

    /// <summary>Pantallas de las leyendas que tapan el mapa (todo menos las fichas pequeñas).</summary>
    private static List<GameObject> PantallasGrandes(Transform canvas)
    {
        List<GameObject> lista = new List<GameObject>();
        foreach (Transform raiz in canvas)
        {
            if (!raiz.name.EndsWith("_todo") && !raiz.name.EndsWith("_Todo")) continue;
            string ficha = raiz.name.Substring(0, raiz.name.Length - 5);
            foreach (Transform hijo in raiz)
            {
                if (hijo.name == ficha) continue;
                bool esMinijuego = hijo.GetComponent<TundaFlowController>() != null || hijo.GetComponent<SilbonFlowController>() != null;
                if (!esMinijuego)
                {
                    lista.Add(hijo.gameObject);
                    continue;
                }
                foreach (Transform pantalla in hijo)
                    if (pantalla.GetComponent<Graphic>() != null || pantalla.GetComponentInChildren<Graphic>(true) != null)
                        lista.Add(pantalla.gameObject);
            }
        }
        return lista;
    }

    private static Transform Buscar(Scene scene, string nombre)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
                if (t.name == nombre) return t;
        return null;
    }
}
