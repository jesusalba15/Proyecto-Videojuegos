using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Escena Mapa: el mapa vivo del GDD (Propuesta 2).
//  - Islas reveladas vs. veladas por niebla (IslaMapa en cada isla).
//  - Al pasar el mouse: nombre, tipo, descripcion, recompensa y requisitos como sellos de cera.
//  - Rutas de tinta punteada que se dibujan solas la primera vez que aparece una isla.
//  - Ficha del barco en la isla actual; al zarpar viaja por la ruta y empieza la navegacion.
// Teclas: clic en una isla para elegirla, Enter para zarpar, M o Esc para cerrar.
public class MapaVivo : MonoBehaviour
{
    public static MapaVivo Actual;

    [Header("Apariencia")]
    public Color colorTinta = new Color(0.25f, 0.13f, 0.06f);
    public float anchoRuta = 0.12f;
    public float alturaRuta = 0.05f;        // por encima del pergamino
    [Tooltip("Modelo pequeno para la ficha del barco (opcional; si esta vacio se crea uno simple)")]
    public GameObject fichaBarco;
    public float duracionTinta = 1.6f;

    [Header("Mesa del capitan")]
    [Tooltip("Pone una base de madera tallada debajo de cada isla, como fichas de un juego de mesa")]
    public bool basesDeMadera = true;
    public Color colorMadera = new Color(0.42f, 0.26f, 0.13f);
    [Tooltip("Luz calida que ilumina la isla elegida")]
    public bool luzDeFoco = true;
    public Color colorFoco = new Color(1f, 0.78f, 0.45f);

    // compatibilidad con la version anterior
    [HideInInspector] public Transform ruta, islaFaro;
    public bool Revelada => true;

    IslaMapa[] islas;
    readonly Dictionary<IslaMapa, LineRenderer> rutas = new Dictionary<IslaMapa, LineRenderer>();
    readonly Dictionary<IslaMapa, Text> etiquetas = new Dictionary<IslaMapa, Text>();
    IslaMapa sobre, elegida;
    Transform barco;
    bool viajando, animando;
    Light foco;
    IslaMapa ultimaEnfocada;

    // UI
    Font fuente; Sprite cuadro, circulo;
    RectTransform lienzo, panel;
    Text tNombre, tTipo, tDesc, tRecompensa, tEstado, tAyuda;
    RectTransform filaSellos;
    Image botonZarpar; Text tBoton;
    readonly Color dorado = new Color(0.95f, 0.75f, 0.25f), cera = new Color(0.62f, 0.1f, 0.1f);

    void Awake() { Actual = this; }
    void OnDestroy() { if (Actual == this) Actual = null; }

    IEnumerator Start()
    {
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        islas = FindObjectsByType<IslaMapa>(FindObjectsSortMode.None);
        System.Array.Sort(islas, (a, b) => a.id.CompareTo(b.id));
        ConstruirUI();

        // la ruta vieja (un cubo) ya no hace falta
        if (ruta) ruta.gameObject.SetActive(false);

        // estado inicial: islas ya vistas aparecen directo; las nuevas esperan su animacion
        var nuevas = new List<IslaMapa>();
        foreach (var isla in islas)
        {
            bool nueva = isla.Revelada && !GameState.IslaVista(isla.id);
            if (nueva) nuevas.Add(isla);
            MostrarIsla(isla, isla.Revelada && !nueva, false);
        }
        if (basesDeMadera) foreach (var isla in islas) CrearBase(isla);
        if (luzDeFoco) CrearFoco();
        ColocarBarco();
        elegida = SiguienteDestino();
        MostrarInfo(elegida);

        if (nuevas.Count > 0)
        {
            animando = true;
            GameState.Avisar("La Tinta de Marea reacciona a tus hazañas...", 3f);
            yield return new WaitForSeconds(0.8f);
            foreach (var isla in nuevas)
            {
                yield return RevelarIsla(isla);
                GameState.MarcarVista(isla.id);
                if (isla.id == 2) GameState.RutaRevelada = true;
                GameState.Avisar("Nueva ruta: " + isla.nombre, 2.5f);
                elegida = isla; MostrarInfo(isla);
                if (MapaCamara.Actual) MapaCamara.Actual.Enfocar(isla.transform.position);
            }
            animando = false;
        }
    }

    // ================================================================ islas y rutas
    void MostrarIsla(IslaMapa isla, bool visible, bool animar)
    {
        // velada: modelo oscuro y cubierto de niebla
        isla.niebla.gameObject.SetActive(!visible);
        foreach (var r in isla.modelo) Teñir(r, visible ? Color.white : new Color(0.35f, 0.33f, 0.3f));
        if (visible && isla.origen) CrearRuta(isla, 1f);
    }

    IEnumerator RevelarIsla(IslaMapa isla)
    {
        // 1) se disipa la niebla
        Vector3 e0 = isla.niebla.localScale;
        for (float t = 0; t < 0.8f; t += Time.deltaTime)
        {
            isla.niebla.localScale = Vector3.Lerp(e0, e0 * 1.6f, t / 0.8f);
            foreach (var r in isla.niebla.GetComponentsInChildren<Renderer>())
            { var c = r.sharedMaterial.color; c.a = Mathf.Lerp(0.92f, 0f, t / 0.8f); r.sharedMaterial.color = c; }
            yield return null;
        }
        isla.niebla.gameObject.SetActive(false);
        // 2) la tinta dibuja la ruta
        if (isla.origen)
            for (float t = 0; t < duracionTinta; t += Time.deltaTime) { CrearRuta(isla, Suave(t / duracionTinta)); yield return null; }
        if (isla.origen) CrearRuta(isla, 1f);
        // 3) la isla toma color y da un saltito
        foreach (var r in isla.modelo) Teñir(r, Color.white);
        Vector3 e = isla.escalaOriginal;
        for (float t = 0; t < 0.5f; t += Time.deltaTime)
        {
            isla.transform.localScale = e * (1f + Mathf.Sin(t / 0.5f * Mathf.PI) * 0.2f);
            yield return null;
        }
        isla.transform.localScale = e;
    }

    void CrearRuta(IslaMapa isla, float progreso)
    {
        if (!rutas.TryGetValue(isla, out var lr))
        {
            var go = new GameObject("Ruta_" + isla.nombre);
            lr = go.AddComponent<LineRenderer>();
            var mat = new Material(Shader.Find("Sprites/Default")) { mainTexture = Punteado() };
            lr.sharedMaterial = mat;
            lr.textureMode = LineTextureMode.Tile;
            lr.startColor = lr.endColor = colorTinta;
            lr.widthMultiplier = anchoRuta;
            lr.numCapVertices = 4;
            lr.alignment = LineAlignment.TransformZ;
            go.transform.rotation = Quaternion.Euler(90, 0, 0);   // la linea queda acostada sobre el pergamino
            rutas[isla] = lr;
        }
        // curva suave (como trazo a mano) del origen a la isla
        Vector3 a = isla.origen.transform.position, b = isla.transform.position;
        Vector3 lado = Vector3.Cross(b - a, Vector3.up).normalized * Vector3.Distance(a, b) * 0.12f;
        Vector3 c = (a + b) / 2f + lado;
        int n = Mathf.Max(2, Mathf.CeilToInt(24 * progreso));
        lr.positionCount = n;
        for (int i = 0; i < n; i++)
        {
            float t = progreso * i / (n - 1);
            Vector3 p = (1 - t) * (1 - t) * a + 2 * (1 - t) * t * c + t * t * b;
            p.y = alturaRuta + PisoY(p);
            lr.SetPosition(i, p);
        }
        float largo = Vector3.Distance(a, b) * progreso;
        lr.sharedMaterial.mainTextureScale = new Vector2(largo / (anchoRuta * 3f), 1f);
    }

    float pisoY = float.NaN;
    float PisoY(Vector3 p)
    {
        if (float.IsNaN(pisoY))
        {
            // altura del pergamino: lo que haya debajo de la primera isla
            pisoY = islas.Length > 0 ? islas[0].transform.position.y : 0f;
            if (islas.Length > 0 && Physics.Raycast(islas[0].transform.position + Vector3.up * 0.01f + Vector3.right * 0.01f, Vector3.down, out RaycastHit h, 5f))
                pisoY = h.point.y;
        }
        return pisoY;
    }

    void ColocarBarco()
    {
        if (fichaBarco) barco = Instantiate(fichaBarco).transform;
        else
        {
            barco = new GameObject("FichaBarco").transform;
            var casco = GameObject.CreatePrimitive(PrimitiveType.Cube); casco.transform.SetParent(barco, false);
            casco.transform.localScale = new Vector3(0.35f, 0.18f, 0.8f); casco.transform.localPosition = new Vector3(0, 0.09f, 0);
            Teñir(casco.GetComponent<Renderer>(), new Color(0.45f, 0.27f, 0.12f));
            var vela = GameObject.CreatePrimitive(PrimitiveType.Cube); vela.transform.SetParent(barco, false);
            vela.transform.localScale = new Vector3(0.5f, 0.45f, 0.04f); vela.transform.localPosition = new Vector3(0, 0.45f, 0);
            Teñir(vela.GetComponent<Renderer>(), new Color(0.95f, 0.92f, 0.82f));
            foreach (var col in barco.GetComponentsInChildren<Collider>()) Destroy(col);
        }
        var actual = Isla(GameState.IslaActual) ?? (islas.Length > 0 ? islas[0] : null);
        if (actual) barco.position = PosBarco(actual);
    }

    Vector3 PosBarco(IslaMapa isla)
    {
        var b = isla.GetComponent<Collider>().bounds;
        return new Vector3(b.max.x + 0.3f, PisoY(b.center) + 0.02f, b.min.z - 0.1f);
    }

    void Update()
    {
        if (barco && !viajando)   // la ficha se mece como en el agua
        {
            barco.rotation = Quaternion.Euler(Mathf.Sin(Time.time * 2f) * 6f, -30f, Mathf.Sin(Time.time * 1.6f) * 6f);
        }
        ActualizarFoco();
        if (viajando || animando) { ActualizarEtiquetas(); return; }

        if (Input.GetKeyDown(KeyCode.M) || Input.GetKeyDown(KeyCode.Escape)) Cerrar();
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) Zarpar(elegida);
        ActualizarEtiquetas();

        // la isla elegida o bajo el mouse se agranda un poco
        foreach (var isla in islas)
        {
            float meta = (isla == sobre || isla == elegida) && isla.Revelada ? 1.1f : 1f;
            isla.transform.localScale = Vector3.Lerp(isla.transform.localScale, isla.escalaOriginal * meta, 10f * Time.deltaTime);
        }
    }

    // ================================================================ mesa: bases, luz y camara
    void CrearBase(IslaMapa isla)
    {
        var b = isla.GetComponent<Collider>().bounds;
        float r = Mathf.Max(b.extents.x, b.extents.z) * 1.15f;
        var disco = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        disco.name = "Base_" + isla.nombre;
        Destroy(disco.GetComponent<Collider>());
        disco.transform.position = new Vector3(b.center.x, PisoY(b.center) + 0.04f, b.center.z);
        disco.transform.localScale = new Vector3(r * 2f, 0.04f, r * 2f);
        Teñir(disco.GetComponent<Renderer>(), colorMadera);
        var aro = GameObject.CreatePrimitive(PrimitiveType.Cylinder);   // borde dorado
        Destroy(aro.GetComponent<Collider>());
        aro.transform.position = disco.transform.position - Vector3.up * 0.01f;
        aro.transform.localScale = new Vector3(r * 2.12f, 0.035f, r * 2.12f);
        Teñir(aro.GetComponent<Renderer>(), new Color(0.75f, 0.55f, 0.2f));
    }

    void CrearFoco()
    {
        var go = new GameObject("LuzFoco");
        foco = go.AddComponent<Light>();
        foco.type = LightType.Spot; foco.color = colorFoco; foco.intensity = 0f;
        foco.spotAngle = 40f; foco.range = 14f; foco.shadows = LightShadows.Soft;
        go.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
    }

    void ActualizarFoco()
    {
        var isla = sobre ? sobre : elegida;
        if (foco)
        {
            if (isla)
            {
                Vector3 meta = isla.transform.position + Vector3.up * 6f;
                foco.transform.position = Vector3.Lerp(foco.transform.position, meta, 8f * Time.deltaTime);
                foco.intensity = Mathf.Lerp(foco.intensity, isla.Revelada ? 40f : 15f, 5f * Time.deltaTime);
            }
            else foco.intensity = Mathf.Lerp(foco.intensity, 0f, 5f * Time.deltaTime);
        }
        if (!viajando && elegida != ultimaEnfocada && MapaCamara.Actual)
        {
            ultimaEnfocada = elegida;
            if (elegida) MapaCamara.Actual.Enfocar(elegida.transform.position); else MapaCamara.Actual.SinFoco();
        }
    }

    // ================================================================ interaccion (llamada desde IslaMapa)
    public void Sobre(IslaMapa isla) { sobre = isla; MostrarInfo(isla); }
    public void Fuera(IslaMapa isla) { if (sobre == isla) { sobre = null; MostrarInfo(elegida); } }
    public void Clic(IslaMapa isla)
    {
        if (viajando || animando) return;
        if (isla == elegida) { Zarpar(isla); return; }   // doble clic = zarpar
        elegida = isla; MostrarInfo(isla);
    }

    public void Zarpar() { Zarpar(elegida); }

    public void Zarpar(IslaMapa isla)
    {
        if (!isla || viajando || animando) return;
        if (isla.EsActual) { Cerrar(); return; }
        if (!isla.Revelada) { GameState.Avisar("La tinta aún no revela esta isla.", 2f); return; }
        if (!isla.CumpleRequisitos) { GameState.Avisar("Te falta: " + isla.Falta(), 2.5f); return; }
        if (!isla.disponibleEnDemo) { GameState.Avisar(isla.nombre + ": próximamente.", 2f); return; }
        StartCoroutine(Viaje(isla));
    }

    IEnumerator Viaje(IslaMapa isla)
    {
        viajando = true;
        GameState.Avisar("¡Zarpamos hacia " + isla.nombre + "!", 3f);
        Vector3 a = barco.position, b = PosBarco(isla);
        Vector3 c = (a + b) / 2f + Vector3.Cross(b - a, Vector3.up).normalized * Vector3.Distance(a, b) * 0.12f;
        for (float t = 0; t < 1.6f; t += Time.deltaTime)
        {
            float k = Suave(t / 1.6f);
            Vector3 p = (1 - k) * (1 - k) * a + 2 * (1 - k) * k * c + k * k * b;
            if (MapaCamara.Actual) MapaCamara.Actual.Enfocar(p);
            Vector3 dir = p - barco.position; dir.y = 0;
            if (dir.sqrMagnitude > 0.0001f) barco.rotation = Quaternion.LookRotation(dir);
            barco.position = p;
            yield return null;
        }
        GameState.IslaDestino = isla.id;
        GameState.CargarEscena("Navegacion");
    }

    public void Cerrar()
    {
        if (viajando) return;
        GameState.Avisar("", 0f);
        GameState.CargarEscena("Isla");
    }

    IslaMapa Isla(int id) { foreach (var i in islas) if (i.id == id) return i; return null; }

    IslaMapa SiguienteDestino()
    {
        foreach (var i in islas) if (!i.EsActual && i.Revelada && i.CumpleRequisitos && i.disponibleEnDemo) return i;
        return Isla(GameState.IslaActual) ?? (islas.Length > 0 ? islas[0] : null);
    }

    // ================================================================ UI
    void ConstruirUI()
    {
        fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        cuadro = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(.5f, .5f));
        circulo = Circulo(96);
        var cv = new GameObject("UI_Mapa", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        cv.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        cv.GetComponent<Canvas>().sortingOrder = 90;   // debajo del HUD (avisos y fundido)
        var sc = cv.GetComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1920, 1080); sc.matchWidthOrHeight = 0.5f;
        lienzo = cv.GetComponent<RectTransform>();

        var titulo = Texto(lienzo, "MAPA VIVO", 46, FontStyle.Bold, dorado, TextAnchor.UpperCenter);
        Poner(titulo.rectTransform, new Vector2(0.5f, 1), new Vector2(0, -24), new Vector2(800, 60));

        // panel de informacion (derecha), como una hoja pegada al mapa
        panel = Caja(lienzo, new Color(0.93f, 0.85f, 0.66f, 0.97f));
        Poner(panel, new Vector2(1, 0.5f), new Vector2(-40, 20), new Vector2(430, 560));
        var borde = Caja(panel, new Color(0.45f, 0.3f, 0.15f, 1f)); borde.SetAsFirstSibling();
        borde.anchorMin = Vector2.zero; borde.anchorMax = Vector2.one; borde.offsetMin = new Vector2(-5, -5); borde.offsetMax = new Vector2(5, 5);
        Color tinta = colorTinta;
        tNombre = Texto(panel, "", 34, FontStyle.Bold, tinta, TextAnchor.UpperLeft); Poner(tNombre.rectTransform, new Vector2(0, 1), new Vector2(28, -22), new Vector2(380, 84));
        tTipo = Texto(panel, "", 20, FontStyle.Italic, new Color(0.45f, 0.28f, 0.12f), TextAnchor.UpperLeft); Poner(tTipo.rectTransform, new Vector2(0, 1), new Vector2(28, -104), new Vector2(380, 28));
        tDesc = Texto(panel, "", 21, FontStyle.Normal, tinta, TextAnchor.UpperLeft); Poner(tDesc.rectTransform, new Vector2(0, 1), new Vector2(28, -140), new Vector2(380, 110));
        var tr = Texto(panel, "REQUISITOS", 18, FontStyle.Bold, new Color(0.45f, 0.28f, 0.12f), TextAnchor.UpperLeft); Poner(tr.rectTransform, new Vector2(0, 1), new Vector2(28, -256), new Vector2(380, 24));
        filaSellos = new GameObject("Sellos", typeof(RectTransform)).GetComponent<RectTransform>();
        filaSellos.SetParent(panel, false); Poner(filaSellos, new Vector2(0, 1), new Vector2(28, -286), new Vector2(380, 100));
        tRecompensa = Texto(panel, "", 20, FontStyle.Normal, tinta, TextAnchor.UpperLeft); Poner(tRecompensa.rectTransform, new Vector2(0, 1), new Vector2(28, -392), new Vector2(380, 56));
        tEstado = Texto(panel, "", 20, FontStyle.Italic, cera, TextAnchor.UpperCenter); Poner(tEstado.rectTransform, new Vector2(0, 0), new Vector2(0, 92), new Vector2(430, 28));
        botonZarpar = Caja(panel, tinta).GetComponent<Image>();
        Poner(botonZarpar.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 26), new Vector2(300, 60));
        botonZarpar.raycastTarget = true;
        var bt = botonZarpar.gameObject.AddComponent<Button>(); bt.onClick.AddListener(() => Zarpar(elegida));
        tBoton = Texto(botonZarpar.rectTransform, "ZARPAR", 28, FontStyle.Bold, new Color(0.93f, 0.85f, 0.66f), TextAnchor.MiddleCenter);
        Estirar(tBoton.rectTransform);

        tAyuda = Texto(lienzo, "Clic: elegir isla   ·   Doble clic o Enter: zarpar   ·   M / Esc: cerrar", 20, FontStyle.Italic, new Color(1, 1, 1, 0.85f), TextAnchor.MiddleCenter);
        Poner(tAyuda.rectTransform, new Vector2(0.5f, 0), new Vector2(0, 14), new Vector2(1200, 30));
        tAyuda.gameObject.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, 0.8f);

        // nombres flotando sobre cada isla
        foreach (var isla in islas)
        {
            var t = Texto(lienzo, "", 22, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
            t.rectTransform.sizeDelta = new Vector2(320, 30);
            var o = t.gameObject.AddComponent<Outline>(); o.effectColor = new Color(0.2f, 0.1f, 0.04f, 0.95f); o.effectDistance = new Vector2(2, -2);
            etiquetas[isla] = t;
        }
    }

    void ActualizarEtiquetas()
    {
        var cam = Camera.main; if (!cam) return;
        foreach (var par in etiquetas)
        {
            var isla = par.Key; var t = par.Value;
            var b = isla.GetComponent<Collider>().bounds;
            Vector3 s = cam.WorldToScreenPoint(new Vector3(b.center.x, b.max.y, b.max.z));
            RectTransformUtility.ScreenPointToLocalPointInRectangle(lienzo, s, null, out Vector2 local);
            t.rectTransform.anchoredPosition = local + new Vector2(0, 26);
            bool vista = isla.Revelada && (GameState.IslaVista(isla.id) || !animando);
            t.text = !vista ? "???" : isla.EsActual ? isla.nombre + "  (aquí)" : isla.nombre;
            t.color = !vista ? new Color(0.8f, 0.8f, 0.8f) : isla == elegida ? dorado : Color.white;
        }
    }

    void MostrarInfo(IslaMapa isla)
    {
        if (!isla) { panel.gameObject.SetActive(false); return; }
        panel.gameObject.SetActive(true);
        foreach (Transform h in filaSellos) Destroy(h.gameObject);

        if (!isla.Revelada)
        {
            tNombre.text = "???";
            tTipo.text = "Velada por la niebla";
            tDesc.text = "La Tinta de Marea aún no muestra esta tierra. Consigue más fragmentos de mapa.";
            tRecompensa.text = "";
            Sello("Fragmentos " + isla.fragmentosParaRevelar, GameState.Fragmentos >= isla.fragmentosParaRevelar, 0);
            Estado("Faltan fragmentos", false, false);
            return;
        }
        tNombre.text = isla.nombre;
        tTipo.text = isla.tipo == IslaMapa.TipoIsla.Libre ? "Isla libre · combate abierto" : "Isla de la Corona · solo sigilo, de noche";
        tDesc.text = isla.descripcion;
        tRecompensa.text = string.IsNullOrEmpty(isla.recompensa) ? "" : "<b>Recompensa:</b> " + isla.recompensa;

        int i = 0;
        if (isla.requiereAgilidad > 0) Sello("Agilidad " + isla.requiereAgilidad, GameState.Agilidad >= isla.requiereAgilidad, i++);
        if (isla.requiereCombate > 0) Sello("Combate " + isla.requiereCombate, GameState.Combate >= isla.requiereCombate, i++);
        if (isla.requiereSigilo > 0) Sello("Sigilo " + isla.requiereSigilo, GameState.Sigilo >= isla.requiereSigilo, i++);
        if (isla.requiereNavegacion > 0) Sello("Navegación " + isla.requiereNavegacion, GameState.Navegacion >= isla.requiereNavegacion, i++);
        if (i == 0) Sello("Sin requisitos", true, 0);

        if (isla.EsActual) Estado("Estás aquí", true, true, "VOLVER");
        else if (!isla.CumpleRequisitos) Estado("Tus destrezas aún no bastan", false, false);
        else if (!isla.disponibleEnDemo) Estado("Próximamente", false, false);
        else Estado("", true, true, "ZARPAR");
    }

    void Estado(string texto, bool ok, bool boton, string textoBoton = "ZARPAR")
    {
        tEstado.text = texto;
        tEstado.color = ok ? new Color(0.25f, 0.45f, 0.15f) : cera;
        botonZarpar.gameObject.SetActive(boton);
        tBoton.text = textoBoton;
    }

    // sello de cera: rojo si falta, dorado si ya lo cumples
    void Sello(string texto, bool ok, int i)
    {
        int col = i % 2, fila = i / 2;
        var s = new GameObject("Sello", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        s.transform.SetParent(filaSellos, false);
        Poner(s.rectTransform, new Vector2(0, 1), new Vector2(col * 190, -fila * 50), new Vector2(42, 42));
        s.sprite = circulo; s.color = ok ? dorado : cera; s.raycastTarget = false;
        var marca = Texto(s.rectTransform, ok ? "●" : "!", 24, FontStyle.Bold, ok ? colorTinta : new Color(1f, 0.85f, 0.7f), TextAnchor.MiddleCenter);
        Estirar(marca.rectTransform);
        var t = Texto(filaSellos, texto, 20, ok ? FontStyle.Bold : FontStyle.Normal, colorTinta, TextAnchor.MiddleLeft);
        Poner(t.rectTransform, new Vector2(0, 1), new Vector2(col * 190 + 50, -fila * 50 - 4), new Vector2(140, 34));
    }

    // ================================================================ utilidades
    static float Suave(float t) { t = Mathf.Clamp01(t); return t * t * (3f - 2f * t); }

    static void Teñir(Renderer r, Color c)
    {
        if (!r) return;
        var mpb = new MaterialPropertyBlock(); r.GetPropertyBlock(mpb);
        mpb.SetColor("_BaseColor", c); mpb.SetColor("_Color", c); r.SetPropertyBlock(mpb);
    }

    Text Texto(RectTransform padre, string txt, int tam, FontStyle st, Color col, TextAnchor al)
    {
        var t = new GameObject("Texto", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
        t.transform.SetParent(padre, false);
        t.font = fuente; t.text = txt; t.fontSize = tam; t.fontStyle = st; t.color = col; t.alignment = al;
        t.raycastTarget = false; t.supportRichText = true;
        t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    RectTransform Caja(RectTransform padre, Color col)
    {
        var img = new GameObject("Caja", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        img.transform.SetParent(padre, false); img.sprite = cuadro; img.color = col; img.raycastTarget = false;
        return img.rectTransform;
    }

    static void Poner(RectTransform r, Vector2 ancla, Vector2 pos, Vector2 tam)
    {
        r.anchorMin = r.anchorMax = r.pivot = ancla; r.anchoredPosition = pos; r.sizeDelta = tam;
    }

    static void Estirar(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }

    static Texture2D punteado;
    static Texture2D Punteado()
    {
        if (punteado) return punteado;
        punteado = new Texture2D(32, 4, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Repeat, filterMode = FilterMode.Bilinear };
        for (int x = 0; x < 32; x++) for (int y = 0; y < 4; y++) punteado.SetPixel(x, y, new Color(1, 1, 1, x < 20 ? 1 : 0));
        punteado.Apply();
        return punteado;
    }

    static Sprite Circulo(int n)
    {
        var t = new Texture2D(n, n, TextureFormat.RGBA32, false); float r = n / 2f;
        for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            t.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(r - Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(r, r)))));
        t.Apply(); return Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(.5f, .5f));
    }
}
