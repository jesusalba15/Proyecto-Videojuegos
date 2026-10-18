using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Interfaz completa del juego, construida por codigo (no hay que armar nada a mano).
// Va en la Main Camera de cada escena (Isla, Mapa, Navegacion).
//  - HUD de exploracion (GDD, Propuesta 1): vida y sigilo abajo a la izquierda,
//    mapa vivo en miniatura arriba a la derecha, tripulacion, objetivo actual.
//  - Indicador de interaccion ([E] Hablar...), dialogos con nombre y avisos.
//  - Menu de pausa (Esc) con Continuar, Controles, Reiniciar y Menu principal.
//  - Fundido a negro entre escenas.
// F1 = ir al mapa con las 3 pruebas hechas. F2 = ir directo al Faro Roto.
public class HUD : MonoBehaviour
{
    public static HUD Actual;
    public static bool Pausado;

    [Header("Colores (paleta del GDD)")]
    public Color pergamino = new Color(0.86f, 0.76f, 0.56f, 0.95f);
    public Color tinta = new Color(0.30f, 0.20f, 0.12f);
    public Color dorado = new Color(0.95f, 0.75f, 0.25f);
    public Color panelOscuro = new Color(0.06f, 0.09f, 0.14f, 0.72f);
    public Color vida = new Color(0.86f, 0.25f, 0.25f);
    public Color turquesa = new Color(0.22f, 0.72f, 0.75f);

    public float duracionFundido = 0.45f;

    [Header("Apariencia (opcional): arrastra sprites de tus packs de UI")]
    [Tooltip("Marco de los paneles (ideal con bordes 9-slice)")] public Sprite spritePanel;
    [Tooltip("Marco de cada casilla del triangulo de equipo")] public Sprite spriteCasilla;
    [Tooltip("Fondo de las barras de vida / aguante")] public Sprite spriteBarraFondo;
    [Tooltip("Relleno de las barras")] public Sprite spriteBarraRelleno;
    [Tooltip("Botones de los menus")] public Sprite spriteBoton;
    [Tooltip("Pergamino del mapa en miniatura")] public Sprite spritePergamino;
    [Tooltip("Imagen de fondo del menu principal")] public Sprite spriteFondoMenu;
    [Tooltip("Fuente para titulos (opcional)")] public Font fuenteTitulos;
    [Tooltip("Fuente para el texto normal de paneles y avisos (opcional, vacio = Arial)")] public Font fuenteTexto;
    [Tooltip("Marcado si el Sprite Panel es claro (pergamino, papel): los textos de los paneles se escriben en tinta oscura")]
    public bool panelClaro = true;
    [Tooltip("Margen interior de los paneles con piel (para que el texto no toque el borde del marco)")]
    public float margenPanel = 30f;
    [Tooltip("Borde oscuro alrededor de las letras de los botones (para que se lean sobre la madera)")]
    public Color bordeLetraBoton = new Color(0.22f, 0.09f, 0.02f, 1f);

    // La misma piel la usan el Taller y la pantalla final (ventanas hechas por codigo)
    public static Sprite PielPanel => Actual ? Actual.spritePanel : null;
    public static Sprite PielBoton => Actual ? Actual.spriteBoton : null;
    public static Font FuenteTitulos => Actual ? Actual.fuenteTitulos : null;
    public static bool PanelClaro => Actual && Actual.spritePanel && Actual.panelClaro;
    // pone un sprite del paquete en una Image (9-slice si el sprite tiene bordes)
    public static bool Vestir(Image img, Sprite s)
    {
        if (!img || !s) return false;
        img.sprite = s; img.color = Color.white;
        img.type = s.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
        return true;
    }
    public Color aguante = new Color(0.45f, 0.72f, 0.25f);

    [Header("Musica (opcional, suena en bucle en esta escena)")]
    public AudioClip musica;
    [Range(0f, 1f)] public float volumenMusica = 0.6f;
    AudioSource fuenteMusicaEscena;
    Image vineta;
    static float herida;
    // pantalla roja en los bordes cuando Kai recibe dano (fuerza 0 a 1)
    public static void Herida(float fuerza) { herida = Mathf.Clamp01(Mathf.Max(herida, fuerza)); }

    [Header("Menu principal")]
    [TextArea(6, 12)] public string[] creditos =
    {
        "<b>La Leyenda del Grumete</b>",
        "Grupo 6 · Motores de Videojuegos · UAO 2026",
        "",
        "<b>Diseño y desarrollo:</b> (escribe aquí los nombres del grupo)",
        "",
        "<b>Modelos 3D:</b> Quaternius (Pirate Kit) y Kenney (Pirate Kit), licencia CC0",
        "<b>Motor:</b> Unity 6",
    };

    // --- referencias internas ---
    Font fuente; Sprite cuadro, circulo;
    Text tituloMenu;
    Minimapa minimapa;
    RectTransform raiz;
    GameObject grupoExploracion, grupoMiniMapa, panelPausa, panelControles, panelObjetivo;
    RectTransform cajaPausa;
    RectTransform[] filasTarea = new RectTransform[5]; Image[] marcasTarea = new Image[5]; Text[] textosTarea = new Text[5];
    Text textoSigilo;
    GameObject cajaReloj; Image iconoReloj; Text textoReloj;
    GameObject cajaInvocar; Image invIcono, invReloj, invActivo; Text invLetra, invNombre, invSeg;
    CanvasGroup grupoObjetivo; float objetivoCambio = -100f, altoObjetivo = 150f; string objetivoAnterior; bool objetivoAbierto; Text tituloObjetivo;
    System.Action menuVolver; GameObject[] menuPaneles;   // menu principal: Esc tambien vuelve atras
    Image barraVida, barraVidaEco, barraAguante, barraEnemigo, barraEnemigoEco, barraSigilo, fundido, puntoFaro, rutaMini, marcoActual, iconoMara;
    Text textoNivel, textoDoblones, textoNombreItem, textoEnemigo, textoObjetivo, textoPrompt, textoAviso, textoHablante;
    GameObject cajaSigilo, cajaPrompt, cajaAviso, etiquetaHablante, filaMara;
    Text[] destrezas = new Text[4];
    Image[] iconosEquipo = new Image[3]; Text[] letrasEquipo = new Text[3]; Text[] cantidadesEquipo = new Text[3];
    RectTransform[] casillas = new RectTransform[3];
    GameObject cajaEnemigo;
    // barra de habilidades (estilo Albion) y recursos
    GameObject barraHab, panelRecursos;
    Image[] habMarco = new Image[4], habIcono = new Image[4], habReloj = new Image[4];
    Text[] habNombre = new Text[4], habSeg = new Text[4];
    float[] habAntes = new float[4];
    Text textoArmaBarra, textoMadera, textoChatarra, teclaPrompt;
    Image barraRecolectar, barraXp;
    Text textoXp, textoTripulacion;
    int armaBarra = -1;
    int[] ultimoEquipo = { -1, -1, -1 };
    float nombreItemHasta;
    readonly System.Collections.Generic.Dictionary<string, Sprite> iconos = new System.Collections.Generic.Dictionary<string, Sprite>();
    RectTransform capaNumeros;
    class BarraFlotante { public Rival r; public RectTransform caja; public Image vida, eco; }
    readonly System.Collections.Generic.List<BarraFlotante> barrasEnemigos = new System.Collections.Generic.List<BarraFlotante>();
    class NumeroFlotante { public Text t; public Vector3 pos; public float inicio, escala; }
    readonly System.Collections.Generic.List<NumeroFlotante> numeros = new System.Collections.Generic.List<NumeroFlotante>();
    Image[] fondoDestrezas = new Image[4];
    PlayerController kai;
    Barbagris[] barbagris; Cofre[] cofres;
    string escena;
    bool EsIsla => escena.StartsWith("Isla");   // "Isla", "Isla_Faro", "Isla_Astillero"...
    bool transicionando;

    // ================================================================ ciclo de vida
    void Awake()
    {
        Actual = this;
        Pausado = false;
        Time.timeScale = 1f;
        escena = SceneManager.GetActiveScene().name;
        Construir();
    }

    void Start()
    {
        kai = FindFirstObjectByType<PlayerController>();
        if (kai && !kai.GetComponent<Invocacion>()) kai.gameObject.AddComponent<Invocacion>();   // invocar tripulacion (G / H)
        barbagris = FindObjectsByType<Barbagris>(FindObjectsSortMode.None);
        cofres = FindObjectsByType<Cofre>(FindObjectsSortMode.None);
        foreach (var rv in FindObjectsByType<Rival>(FindObjectsSortMode.None))
            if (!rv.esJefe) CrearBarraEnemigo(rv);
        CrearMinimapa();
        if (EsIsla || escena == "Navegacion")
        {
            var mm = MapaMundo.Crear(raiz, fuente, dorado, tinta, pergamino);
            var aviso = raiz.Find("Aviso");   // los avisos y el fundido quedan por encima del mapa
            mm.transform.SetSiblingIndex(aviso ? aviso.GetSiblingIndex() : raiz.childCount - 2);
        }
        StartCoroutine(Fundir(1f, 0f));
        Sonidos.Ambiente();   // mar, viento y gaviotas de fondo
        if (musica)
        {
            var fuenteAudio = gameObject.AddComponent<AudioSource>();
            fuenteAudio.clip = musica; fuenteAudio.loop = true; fuenteAudio.volume = volumenMusica;
            fuenteAudio.spatialBlend = 0f; fuenteAudio.Play();
            fuenteMusicaEscena = fuenteAudio;
        }
    }

    void OnDestroy()
    {
        if (Actual == this) Actual = null;
        Pausado = false;
        Time.timeScale = 1f;
    }

    void Update()
    {
        if (!transicionando)
        {
            if (escena == "Menu" && menuVolver != null && Input.GetKeyDown(KeyCode.Escape))
                foreach (var x in menuPaneles) if (x && x.activeSelf) { menuVolver(); break; }
            if (Input.GetKeyDown(KeyCode.Escape) && escena != "Mapa" && escena != "Menu" && !MapaMundo.Abierto && !Taller.Abierto && Time.frameCount != Taller.CuadroCierre) AlternarPausa();
            if (!Pausado)
            {
                if (Input.GetKeyDown(KeyCode.F1))
                {
                    GameState.Agilidad = GameState.Combate = GameState.Sigilo = 1;
                    GameState.PruebasHechas = 3;
                    GameState.Fragmentos = 1;
                    Equipo.ArmasObtenidas |= 1 << (int)Equipo.IdArma.Sable;
                    MapaMundo.Abrir(MapaMundo.Pestana.Mundo);
                }
                if (escena == "Navegacion" && Input.GetKeyDown(KeyCode.M)) MapaMundo.Abrir(MapaMundo.Pestana.Mundo);
                if (Input.GetKeyDown(KeyCode.F3))   // pruebas: todas las armas y nivel 6 (todas las habilidades)
                {
                    Equipo.ArmasObtenidas = 0b1111;
                    if (Progreso.Nivel < 6) { Progreso.Nivel = 6; Progreso.Xp = 0; GameState.VidaKai = Progreso.VidaMax; }
                    GameState.Avisar("Modo prueba: todas las armas y nivel 6.", 2f);
                }
                var muFaro = Muelle.De(2);
                if (Input.GetKeyDown(KeyCode.F2) && muFaro && kai && !(BarcoController.Actual && BarcoController.Actual.Manejando))
                    kai.Reaparecer(muFaro.PuntoLlegada);   // mundo abierto: teletransporta al muelle del Faro Roto
                else if (Input.GetKeyDown(KeyCode.F2))
                {
                    GameState.IslaActual = 2;
                    GameState.CargarEscena(Archipielago.Buscar(2)?.escena ?? "Isla");
                }
            }
        }
        ActualizarMundo();
        Refrescar();
        RefrescarVineta();
        if (tituloMenu)   // el titulo "respira" suavemente
            tituloMenu.rectTransform.localScale = Vector3.one * (1f + Mathf.Sin(Time.unscaledTime * 1.4f) * 0.015f);
    }

    void RefrescarVineta()
    {
        if (fuenteMusicaEscena)   // la musica de la escena se calla mientras suena la del jefe
            fuenteMusicaEscena.volume = Mathf.MoveTowards(fuenteMusicaEscena.volume, Sonidos.HayMusica ? 0f : volumenMusica, Time.unscaledDeltaTime * 0.5f);
        if (!vineta) return;
        herida = Mathf.MoveTowards(herida, 0f, Time.unscaledDeltaTime * 1.6f);
        // con poca vida late suave
        bool poca = kai && !kai.EstaMuerto && GameState.VidaKai > 0 && GameState.VidaKai < Progreso.VidaMax * 0.25f && escena != "Menu";
        float baja = poca ? 0.16f + 0.08f * Mathf.Sin(Time.unscaledTime * 4.5f) : 0f;
        float a = Pausado ? 0f : Mathf.Max(herida * 0.55f, baja);
        vineta.enabled = a > 0.005f;
        if (vineta.enabled) vineta.color = new Color(0.75f, 0.05f, 0.04f, a);
    }

    // ================================================================ pausa y escenas
    public void AlternarPausa()
    {
        Pausado = !Pausado;
        Time.timeScale = Pausado ? 0f : 1f;
        panelPausa.SetActive(Pausado);
        panelControles.SetActive(false);
        if (cajaPausa) cajaPausa.anchoredPosition = Vector2.zero;
        Cursor.lockState = Pausado ? CursorLockMode.None : Cursor.lockState;
        Cursor.visible = Pausado || Cursor.lockState != CursorLockMode.Locked;
    }

    public void Transicion(string nombre)
    {
        if (!transicionando) StartCoroutine(TransicionRutina(nombre));
    }

    IEnumerator TransicionRutina(string nombre)
    {
        transicionando = true;
        Pausado = false;
        Time.timeScale = 1f;
        yield return Fundir(0f, 1f);
        SceneManager.LoadScene(nombre);
    }

    IEnumerator Fundir(float desde, float hasta)
    {
        fundido.gameObject.SetActive(true);
        for (float t = 0f; t < duracionFundido; t += Time.unscaledDeltaTime)
        {
            fundido.color = new Color(0, 0, 0, Mathf.Lerp(desde, hasta, t / duracionFundido));
            yield return null;
        }
        fundido.color = new Color(0, 0, 0, hasta);
        fundido.raycastTarget = hasta > 0.5f;
        if (hasta <= 0f) fundido.gameObject.SetActive(false);
    }

    // ================================================================ actualizacion por frame
    void Refrescar()
    {
        // Vida y aguante (con "eco" amarillo que baja despacio, como en Dark Souls)
        float v = Mathf.Clamp01((float)GameState.VidaKai / Progreso.VidaMax);
        // como en Dark Souls: la barra de vida se alarga al subir de nivel
        // el panel y el marco crecen junto con la barra
        var fondoVida = (RectTransform)barraVida.transform.parent;
        var panelVida = (RectTransform)fondoVida.parent;
        float anchoVida = Mathf.Min(560f, 342f * Progreso.VidaMax / 100f);
        fondoVida.sizeDelta = new Vector2(anchoVida, fondoVida.sizeDelta.y);
        var marcoVida = panelVida.Find("MarcoVIDA") as RectTransform;
        if (marcoVida) marcoVida.sizeDelta = new Vector2(anchoVida + 6f, marcoVida.sizeDelta.y);
        panelVida.sizeDelta = new Vector2(anchoVida + 118f, panelVida.sizeDelta.y);
        barraXp.fillAmount = Mathf.Lerp(barraXp.fillAmount, Progreso.Fraccion, 8f * Time.unscaledDeltaTime);
        textoXp.text = Progreso.Nivel >= Progreso.NivelMaximo ? "NIVEL " + Progreso.Nivel + "  (MÁXIMO)" : "NIVEL " + Progreso.Nivel + "     " + Progreso.Xp + " / " + Progreso.XpParaSubir + " EXP";
        barraVida.fillAmount = Mathf.Lerp(barraVida.fillAmount, v, 14f * Time.unscaledDeltaTime);
        barraVidaEco.fillAmount = Mathf.MoveTowards(Mathf.Max(barraVidaEco.fillAmount, barraVida.fillAmount), v, 0.35f * Time.unscaledDeltaTime);
        barraAguante.fillAmount = Mathf.Lerp(barraAguante.fillAmount, GameState.Aguante / 100f, 14f * Time.unscaledDeltaTime);
        int nivel = Progreso.Nivel;
        textoNivel.text = nivel.ToString();
        textoDoblones.text = Equipo.Doblones.ToString();

        // Barra del enemigo (abajo al centro)
        // jefe: la barra aparece al empezar la pelea y se queda hasta que muere (o hasta que Kai cae)
        var r = Jefe.Actual && Jefe.Actual.Rival ? Jefe.Actual.Rival : Rival.EnCombate;
        bool jefe = r != null && r.esJefe && !r.Muerto;
        cajaEnemigo.SetActive(jefe && EsIsla);
        if (jefe)
        {
            bool furioso = Jefe.Actual && Jefe.Actual.Rival == r && Jefe.Actual.Fase == 2;
            textoEnemigo.text = r.nombre + (furioso ? "   <color=#FF6A3D>¡FURIOSO!</color>" : "");
            float ve = Mathf.Clamp01((float)r.Vida / r.vidaMaxima);
            barraEnemigo.fillAmount = Mathf.Lerp(barraEnemigo.fillAmount, ve, 14f * Time.unscaledDeltaTime);
            barraEnemigoEco.fillAmount = Mathf.MoveTowards(Mathf.Max(barraEnemigoEco.fillAmount, barraEnemigo.fillAmount), ve, 0.35f * Time.unscaledDeltaTime);
        }

        // Triangulo de equipo
        RefrescarEquipo();
        RefrescarNumeros();
        RefrescarBarrasEnemigos();

        // Sigilo (solo cerca de guardias, como dice el GDD)
        cajaSigilo.SetActive(GameState.MostrarDeteccion);
        if (GameState.MostrarDeteccion)
        {
            barraSigilo.fillAmount = GameState.Deteccion / 100f;
            barraSigilo.color = Color.Lerp(new Color(1f, 0.9f, 0.3f), new Color(0.95f, 0.2f, 0.2f), GameState.Deteccion / 100f);
            // persiguiendo: la etiqueta cambia a ALERTA y parpadea en rojo
            if (!textoSigilo) textoSigilo = cajaSigilo.GetComponentInChildren<Text>();
            bool alerta = Sereno.Persiguiendo;
            textoSigilo.text = alerta ? "¡ALERTA!" : "SIGILO";
            textoSigilo.color = alerta ? Color.Lerp(new Color(1f, 0.3f, 0.2f), Color.white, Mathf.PingPong(Time.unscaledTime * 3f, 1f)) : new Color(1f, 0.93f, 0.75f);
        }

        // Destrezas
        int[] val = { GameState.Agilidad, GameState.Combate, GameState.Sigilo, GameState.Navegacion };
        for (int i = 0; i < 4; i++)
        {
            bool ok = val[i] > 0;
            fondoDestrezas[i].color = ok ? dorado : new Color(0.06f, 0.09f, 0.14f, 0.7f);
            destrezas[i].color = ok ? tinta : new Color(1f, 1f, 1f, 0.55f);
        }

        // Mapa vivo en miniatura: la tinta se extiende con el fragmento
        bool revelado = GameState.Fragmentos >= 1;
        float objetivo = revelado ? 1f : 0f;
        puntoFaro.color = new Color(puntoFaro.color.r, puntoFaro.color.g, puntoFaro.color.b,
            Mathf.MoveTowards(puntoFaro.color.a, objetivo, 2f * Time.unscaledDeltaTime));
        rutaMini.fillAmount = Mathf.MoveTowards(rutaMini.fillAmount, objetivo, 1.2f * Time.unscaledDeltaTime);
        marcoActual.rectTransform.anchoredPosition = GameState.IslaActual == 2 ? new Vector2(72, 14) : new Vector2(-70, -12);
        marcoActual.rectTransform.localScale = Vector3.one * (1f + Mathf.Sin(Time.unscaledTime * 4f) * 0.08f);

        // Tripulacion
        iconoMara.color = GameState.TieneMara == 1 ? turquesa : new Color(1, 1, 1, 0.15f);
        if (textoTripulacion)
        {
            string tr = "";
            for (int i = 0; i < 4; i++)
            {
                bool tiene = Tripulacion.Tiene((Tripulacion.Miembro)i);
                string n = i == 0 ? "Mara" : Tripulacion.Nombres[i];
                bool papel = false;   // el panel de tripulacion ahora es oscuro (madera): colores claros
                tr += (i > 0 ? "  " : "") + (tiene ? "<color=" + (papel ? "#1E7F7F" : "#5AD6D6") + "><b>" + n + "</b></color>"
                                                   : "<color=" + (papel ? "#3A2A1A66" : "#FFFFFF40") + ">" + n + "</color>");
            }
            textoTripulacion.text = tr;
        }

        // Objetivo actual
        string objetivoNuevo = TextoObjetivo();
        if (objetivoNuevo != objetivoAnterior) { objetivoAnterior = objetivoNuevo; MostrarObjetivo(objetivoNuevo); objetivoCambio = Time.unscaledTime; }
        // desplegable: Tab lo abre o lo cierra; cuando cambia el objetivo se abre solo 8 s.
        // Cerrado solo queda la placa "OBJETIVO (Tab)".
        if (!Pausado && !MapaMundo.Abierto && Input.GetKeyDown(KeyCode.Tab)) { objetivoAbierto = !objetivoAbierto; objetivoCambio = -100f; }
        // mientras hay un aviso o dialogo abajo, el objetivo se cierra (y se abre despues si acaba de cambiar)
        bool hayAviso = GameState.AvisoActual != "";
        if (hayAviso && Time.unscaledTime - objetivoCambio < 8f) objetivoCambio = Time.unscaledTime;
        bool verObjetivo = (objetivoAbierto || Time.unscaledTime - objetivoCambio < 8f) && !hayAviso;
        if (grupoObjetivo)
        {
            grupoObjetivo.alpha = Mathf.MoveTowards(grupoObjetivo.alpha, verObjetivo ? 1f : 0f, Time.unscaledDeltaTime * 4f);
            // cerrado se encoge hacia abajo y solo queda la placa
            var ro = (RectTransform)panelObjetivo.transform;
            ro.sizeDelta = new Vector2(ro.sizeDelta.x, Mathf.MoveTowards(ro.sizeDelta.y, verObjetivo ? altoObjetivo : 0f, Time.unscaledDeltaTime * 900f));
        }
        if (tituloObjetivo) tituloObjetivo.text = verObjetivo ? "OBJETIVO" : "OBJETIVO  (Tab)";

        // Indicador de interaccion
        string p = PromptActual();
        string tecla = "E";
        if (p.StartsWith("F:")) { tecla = "F"; p = p.Substring(2); }
        bool hablando = GameState.AvisoActual != "" && GameState.HablanteActual != "";
        cajaPrompt.SetActive(p != "" && !Pausado && !hablando);   // al hablar se quita la "E" y solo queda el dialogo
        if (p != "") { textoPrompt.text = p; teclaPrompt.text = tecla; }
        var rc = Recurso.Recolectando;
        barraRecolectar.gameObject.SetActive(rc);
        if (rc) barraRecolectar.fillAmount = rc.Progreso;

        RefrescarHabilidades();
        RefrescarInvocacion();
        // reloj: sol dorado de dia, luna azulada de noche
        bool hayCiclo = CicloDia.Actual && EsIsla;
        if (cajaReloj.activeSelf != hayCiclo) cajaReloj.SetActive(hayCiclo);
        if (hayCiclo)
        {
            textoReloj.text = CicloDia.TextoHora();
            iconoReloj.color = Color.Lerp(new Color(0.75f, 0.82f, 1f), dorado, CicloDia.Luz);
        }
        textoMadera.text = Equipo.Madera.ToString();
        textoChatarra.text = Equipo.Chatarra.ToString();

        // Avisos y dialogos
        string a = GameState.AvisoActual;
        cajaAviso.SetActive(a != "");
        if (a != "")
        {
            textoAviso.text = a;
            string h = GameState.HablanteActual;
            etiquetaHablante.SetActive(h != "");
            textoHablante.text = h;
        }
    }

    // Separa el texto del objetivo: las lineas "[x] ..." / "[ ] ..." van como filas con casilla
    void MostrarObjetivo(string s)
    {
        var cabecera = new System.Collections.Generic.List<string>();
        int n = 0;
        foreach (var linea in s.Split('\n'))
        {
            bool hecha = linea.StartsWith("[x]"), tarea = hecha || linea.StartsWith("[ ]");
            if (!tarea || n >= filasTarea.Length) { cabecera.Add(linea); continue; }
            textosTarea[n].text = linea.Substring(3).Trim();
            textosTarea[n].color = hecha ? new Color(0.55f, 0.92f, 0.5f) : Color.white;
            marcasTarea[n].enabled = hecha;
            n++;
        }
        textoObjetivo.text = string.Join("\n", cabecera.ToArray());
        int lineas = Mathf.Max(1, cabecera.Count + (textoObjetivo.text.Length > 34 * cabecera.Count ? 1 : 0));
        float y = -26f - lineas * 24f - 4f;
        textoObjetivo.rectTransform.sizeDelta = new Vector2(340, lineas * 24f);
        for (int i = 0; i < filasTarea.Length; i++)
        {
            filasTarea[i].gameObject.SetActive(i < n);
            filasTarea[i].anchoredPosition = new Vector2(20, y - i * 28f);
        }
        altoObjetivo = 26f + lineas * 24f + n * 28f + 22f;
    }

    string TextoObjetivo()
    {
        if (escena == "Navegacion") return "Navega hasta <b>" + (Archipielago.Buscar(GameState.IslaDestino)?.nombre ?? "tu destino") + "</b>.\n<size=20>W avanzar · S frenar · A/D timón · M mapa</size>";
        // islas con objetivo propio (ZonaIsla > Objetivo)
        var zonaObj = ZonaIsla.De(GameState.IslaActual);
        if (zonaObj && zonaObj.TieneObjetivo && !(BarcoController.Actual && BarcoController.Actual.Manejando))
            return zonaObj.Completada ? "¡Isla completada! Abre el mapa (<b>M</b>) y traza la ruta a la siguiente isla." : zonaObj.TextoObjetivo();
        var bc = BarcoController.Actual;
        string destino = Archipielago.Buscar(GameState.IslaDestino)?.nombre ?? "tu destino";
        if (bc && bc.Manejando)
            return (GameState.RutaTrazada ? "Navega hasta <b>" + destino + "</b> (sigue la estrella del minimapa)." : "Navega libre por el archipiélago. Traza una ruta con <b>M</b>.")
                   + "\n<size=20>W/S vela · A/D timón · E bajar</size>";
        if (GameState.IslaActual == 2)
            return GameState.TieneMara == 0
                ? "Busca el <b>cofre de tripulante</b> en la Isla del Faro Roto."
                : "¡Mara se unió! Abre el mapa (<b>M</b>) y busca la siguiente isla.";
        if (GameState.PruebasHechas < 3)
            return "Supera las pruebas de Barbagrís:\n" +
                   Check(GameState.Agilidad) + " Trepar el mástil\n" +
                   Check(GameState.Combate) + " Ganar el duelo\n" +
                   Check(GameState.Sigilo) + " Colarse en la bodega";
        if (GameState.Fragmentos < 1) return "Vuelve con <b>Barbagrís</b> en el muelle.";
        return GameState.RutaTrazada
            ? "Sube a tu <b>barco</b> en el muelle (E) y navega hasta <b>" + destino + "</b>."
            : "Abre el mapa (<b>M</b>), elige el <b>Faro Roto</b> y traza la ruta.";
    }

    // ================================================================ mundo abierto
    // Cada 0.4 s mira en que ZonaIsla esta Kai (a pie o en barco) y actualiza la isla actual.
    // Tambien pone una estrella en el minimapa sobre la isla de la ruta trazada.
    float proximoMundo;
    ZonaIsla[] zonasMundo;
    GameObject marcaDestino;

    void ActualizarMundo()
    {
        if (!EsIsla || !GameState.Kai || Time.unscaledTime < proximoMundo) return;
        proximoMundo = Time.unscaledTime + 0.4f;
        if (zonasMundo == null) zonasMundo = FindObjectsByType<ZonaIsla>(FindObjectsSortMode.None);
        Vector3 p = GameState.Kai.position;
        foreach (var z in zonasMundo)
            if (z && z.idIsla != GameState.IslaActual && z.Area.Contains(new Vector2(p.x, p.z))) { EntrarIsla(z.idIsla); break; }

        ZonaIsla destino = null;
        if (GameState.RutaTrazada) foreach (var z in zonasMundo) if (z && z.idIsla == GameState.IslaDestino) destino = z;
        if (destino)
        {
            if (!marcaDestino) marcaDestino = new GameObject("DestinoRuta");
            marcaDestino.transform.position = destino.transform.position;
            marcaDestino.SetActive(true);
            var m = MarcadorMinimapa.Agregar(marcaDestino, MarcadorMinimapa.Tipo.Objetivo, true, "", "Destino");
            m.etiqueta = "Destino: " + (Archipielago.Buscar(destino.idIsla)?.nombre ?? "");
        }
        else if (marcaDestino) marcaDestino.SetActive(false);
    }

    void EntrarIsla(int id)
    {
        GameState.IslaActual = id;
        string nombre = Archipielago.Buscar(id)?.nombre ?? "Isla";
        if (GameState.RutaTrazada && id == GameState.IslaDestino) GameState.RutaTrazada = false;   // llegaste al destino
        if (id >= 2) GameState.Navegacion = Mathf.Max(GameState.Navegacion, 1);
        if (!GameState.IslaPisada(id))
        {
            GameState.MarcarPisada(id);
            GameState.Avisar("Llegaste a " + nombre, 3.5f);
            if (id != 1) Progreso.Ganar(Progreso.XpIslaNueva);
        }
        else GameState.Avisar(nombre, 1.5f);
    }

    string Check(int v) => v > 0 ? "[x]" : "[ ]";   // el HUD lo dibuja como casilla (verde = cumplida)

    string PromptActual()
    {
        if (!kai || kai.EstaMuerto) return "";
        var barco = BarcoController.Actual;
        if (barco && barco.Manejando) return barco.PuedeBajarAqui ? "Bajar del barco" : "";   // navegando no se muestra
        Vector3 pos = kai.transform.position;
        if (barco && barco.PuedeSubir(pos)) return "Subir al barco";
        foreach (var rc2 in RecompensaIsla.Todas)
            if (rc2 && rc2.PideHablar && rc2.Cerca(pos)) return string.IsNullOrEmpty(rc2.hablante) ? "Examinar" : "Hablar con " + rc2.hablante;
        foreach (var en in Encargo.Todos)
            if (en && en.Cerca(pos)) return en.Prompt();
        foreach (var om in ObjetoMision.Todos)
            if (om && om.Cerca(pos)) return "F:Recoger " + om.nombre;
        foreach (var bq in Bloqueo.Todos)
            if (bq && bq.Cerca(pos)) return bq.Prompt();
        foreach (var ta in Taller.Todos)
            if (ta && ta.Cerca(pos)) return "Abrir el taller";
        foreach (var ar in ArmaEnSuelo.Todos)
            if (ar && ar.Cerca(pos)) return "F:Recoger " + ar.Nombre;
        foreach (var bo in Botin.Todos)
            if (bo && Vector3.Distance(pos, bo.transform.position) < Botin.Radio) return "F:Recoger botín";
        if (Recurso.Recolectando) return "F:Recogiendo " + Recurso.Recolectando.NombreRecurso + "...";
        foreach (var rr in Recurso.Todos)
            if (rr && rr.Cerca(pos)) return "F:Mantén F: recoger " + rr.NombreRecurso;
        foreach (var b in barbagris)
            if (b && Vector3.Distance(pos, b.transform.position) < b.radio)
                return GameState.PruebasHechas >= 3 && GameState.Fragmentos < 1 ? "Recibir el primer nudo" : "Hablar con Barbagrís";
        foreach (var c in cofres)
            if (c && GameState.TieneMara == 0 && Vector3.Distance(pos, c.transform.position) < c.radio)
                return "Abrir el cofre";
        foreach (var mu in Muelle.Todos)
            if (mu && mu.Cerca(pos)) { string t = mu.Prompt(); if (t != "") return t; }
        return "";
    }

    // ================================================================ construccion
    void Construir()
    {
        fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        cuadro = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
        circulo = CrearCirculo(128);

        var canvasGO = new GameObject("UI_Juego", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        raiz = canvasGO.GetComponent<RectTransform>();

        if (!FindFirstObjectByType<EventSystem>())
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

        bool esMapa = escena == "Mapa";

        // ---------- Estado de Kai (arriba a la izquierda, estilo Dark Souls)
        grupoExploracion = Grupo("Exploracion");
        var estado = new GameObject("Estado", typeof(RectTransform)).GetComponent<RectTransform>();
        estado.SetParent(grupoExploracion.transform, false);
        estado.anchorMin = estado.anchorMax = estado.pivot = new Vector2(0, 1);
        estado.anchoredPosition = new Vector2(30, -26); estado.sizeDelta = new Vector2(700, 130);
        // emblema con el nivel (1 + destrezas)
        // aro dorado por fuera y circulo oscuro por dentro (el hijo se dibuja encima del padre)
        var emblema = Img(estado, "Emblema", Adornos.Perilla(), dorado, new Vector2(0, 1), new Vector2(0, -52), new Vector2(100, 100));   // centrado a la altura de las 3 barras
        var interior = Img(emblema.transform, "Interior", circulo, new Color(0.08f, 0.08f, 0.1f, 0.95f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(86, 86));
        textoNivel = Etiqueta(interior.transform, "1", 46, FontStyle.Bold, dorado, Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter);
        Estirar(textoNivel.rectTransform, 0); Titulo(textoNivel);
        // barras
        // barras: todas iguales, cada una en su panel con nombre (mismo estilo que la de SIGILO)
        barraVida = BarraConPanel(estado, "VIDA", new Vector2(116, -36), vida, out barraVidaEco);
        barraAguante = BarraConPanel(estado, "ESTAMINA", new Vector2(116, -82), aguante, out _);
        barraXp = BarraConPanel(estado, "EXP", new Vector2(116, -128), new Color(0.62f, 0.45f, 0.95f), out var ecoXp);
        ecoXp.gameObject.SetActive(false);
        // texto del nivel DENTRO de la barra de experiencia, grande y con borde para que se lea
        textoXp = Etiqueta(barraXp.transform.parent, "", 15, FontStyle.Bold, Color.white, Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter);
        Estirar(textoXp.rectTransform, -4);
        var oxp = textoXp.gameObject.AddComponent<Outline>(); oxp.effectColor = new Color(0.12f, 0.05f, 0.2f, 1f); oxp.effectDistance = new Vector2(1.5f, -1.5f);
        // destrezas
        string[] nombres = { "AGI", "COM", "SIG", "NAV" };
        for (int i = 0; i < 4; i++)
        {
            fondoDestrezas[i] = Img(estado, "Destreza" + i, cuadro, Color.white, new Vector2(0, 1), new Vector2(116 + i * 72, -116), new Vector2(66, 28));
            Adornos.Biselar(fondoDestrezas[i]); Adornos.Sombra(fondoDestrezas[i].gameObject, 2f);
            destrezas[i] = Etiqueta(fondoDestrezas[i].transform, nombres[i], 18, FontStyle.Bold, Color.white, Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter);
            Estirar(destrezas[i].rectTransform, 0);
            fondoDestrezas[i].gameObject.SetActive(false);   // ocultas: el objetivo ya dice que falta
        }

        // ---------- Sigilo (debajo de las barras, igual que ellas; solo cerca de guardias)
        barraSigilo = BarraConPanel(estado, "SIGILO", new Vector2(116, -174), Color.yellow, out var ecoSig, new Color(0.18f, 0.05f, 0.04f, 0.94f));
        ecoSig.gameObject.SetActive(false);
        cajaSigilo = barraSigilo.transform.parent.parent.gameObject;

        // ---------- Triangulo de equipo + doblones (abajo a la derecha)
        var tri = new GameObject("Equipo", typeof(RectTransform)).GetComponent<RectTransform>();
        tri.SetParent(grupoExploracion.transform, false);
        tri.anchorMin = tri.anchorMax = tri.pivot = new Vector2(1, 0);
        tri.anchoredPosition = new Vector2(-30, 18); tri.sizeDelta = new Vector2(330, 300);   // pegado a la esquina de abajo
        casillas[0] = Casilla(tri, 0, new Vector2(165, 215), "Z");   // arriba: arma
        casillas[1] = Casilla(tri, 1, new Vector2(80, 75), "Q");     // abajo izq: pocion
        casillas[2] = Casilla(tri, 2, new Vector2(250, 75), "R");    // abajo der: objeto
        textoNombreItem = Etiqueta(tri, "", 24, FontStyle.Bold, Color.white, new Vector2(-200, -300), new Vector2(530, 34), TextAnchor.MiddleCenter);
        textoNombreItem.gameObject.SetActive(false);   // sin el nombre del objeto ("Grog de curación")
        // doblones, madera y chatarra: fila sin fondo ENCIMA de las barras, cada uno en columnas iguales
        var fila = new GameObject("Recursos", typeof(RectTransform)).GetComponent<RectTransform>();
        fila.SetParent(estado, false);
        fila.anchorMin = fila.anchorMax = fila.pivot = new Vector2(0, 1);
        fila.anchoredPosition = new Vector2(116, 0); fila.sizeDelta = new Vector2(420, 30);
        panelRecursos = fila.gameObject;
        const float columna = 130f;
        var moneda = Img(fila, "Moneda", Adornos.Perilla(), dorado, new Vector2(0, 1), new Vector2(0, -1), new Vector2(28, 28));
        textoDoblones = Etiqueta(fila, "0", 24, FontStyle.Bold, dorado, new Vector2(36, 0), new Vector2(90, 30), TextAnchor.MiddleLeft);
        Titulo(textoDoblones);
        var im1 = Img(fila, "Madera", cuadro, new Color(0.6f, 0.4f, 0.22f), new Vector2(0, 1), new Vector2(columna, -1), new Vector2(28, 28));
        Adornos.Biselar(im1);
        var lm = Etiqueta(im1.transform, "M", 16, FontStyle.Bold, Color.white, Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter); Estirar(lm.rectTransform, 0);
        textoMadera = Etiqueta(fila, "0", 24, FontStyle.Bold, Color.white, new Vector2(columna + 36, 0), new Vector2(90, 30), TextAnchor.MiddleLeft);
        Titulo(textoMadera);
        var im2 = Img(fila, "Chatarra", Adornos.Perilla(), new Color(0.62f, 0.65f, 0.7f), new Vector2(0, 1), new Vector2(columna * 2, -1), new Vector2(28, 28));
        var lc = Etiqueta(im2.transform, "C", 16, FontStyle.Bold, tinta, Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter); Estirar(lc.rectTransform, 0);
        textoChatarra = Etiqueta(fila, "0", 24, FontStyle.Bold, Color.white, new Vector2(columna * 2 + 36, 0), new Vector2(90, 30), TextAnchor.MiddleLeft);
        Titulo(textoChatarra);

        // ---------- Barra del enemigo (abajo al centro)
        var en = new GameObject("Enemigo", typeof(RectTransform)).GetComponent<RectTransform>();
        en.SetParent(grupoExploracion.transform, false);
        en.anchorMin = en.anchorMax = en.pivot = new Vector2(0.5f, 0);
        en.anchoredPosition = new Vector2(0, 178); en.sizeDelta = new Vector2(900, 70);   // abajo al centro, encima del dialogo
        cajaEnemigo = en.gameObject;
        textoEnemigo = Etiqueta(en, "", 26, FontStyle.Normal, Color.white, new Vector2(0, 0), new Vector2(900, 34), TextAnchor.MiddleLeft);
        Titulo(textoEnemigo);
        barraEnemigo = Barra(en, "VidaEnemigo", new Vector2(0, -38), new Vector2(900, 18), new Color(0.75f, 0.12f, 0.1f), out barraEnemigoEco);

        // ---------- Barra de habilidades del arma (abajo al centro, estilo Albion): teclas 1 2 3 4
        var bh = new GameObject("Habilidades", typeof(RectTransform)).GetComponent<RectTransform>();
        bh.SetParent(grupoExploracion.transform, false);
        bh.anchorMin = bh.anchorMax = bh.pivot = new Vector2(0, 0);
        bh.anchoredPosition = new Vector2(44, 24); bh.sizeDelta = new Vector2(400, 120);   // abajo a la izquierda
        bh.localScale = Vector3.one * 0.8f;   // un poco mas pequena
        barraHab = bh.gameObject;
        // tabla de madera detras de las 4 casillas, con el nombre del arma en una placa arriba
        var tabla = Img(bh, "Tabla", cuadro, Color.white, new Vector2(0.5f, 0), new Vector2(0, -12), new Vector2(432, 134));
        tabla.rectTransform.pivot = new Vector2(0.5f, 0);
        Adornos.Decorar(tabla, new Color(0.14f, 0.08f, 0.04f, 0.93f), Adornos.Oro);
        textoArmaBarra = Etiqueta(bh, "", 20, FontStyle.Bold, dorado, new Vector2(0, 0), new Vector2(400, 28), TextAnchor.MiddleCenter);
        Titulo(textoArmaBarra);
        Adornos.Placa(tabla.rectTransform, textoArmaBarra, 220f, 36f);
        for (int i = 0; i < 4; i++)
        {
            habMarco[i] = Img(bh, "Hab" + i, cuadro, dorado, new Vector2(0.5f, 0), new Vector2(-150 + i * 100, 0), new Vector2(88, 88));
            habMarco[i].rectTransform.pivot = new Vector2(0.5f, 0);
            Adornos.Biselar(habMarco[i]); Adornos.Sombra(habMarco[i].gameObject, 4f);
            var fondo = Img(habMarco[i].transform, "Fondo", cuadro, new Color(0.07f, 0.06f, 0.05f, 0.95f), Vector2.zero, Vector2.zero, Vector2.zero);
            Estirar(fondo.rectTransform, 4); Adornos.Hundir(fondo); fondo.color = new Color(0.35f, 0.3f, 0.25f, 1f);
            habIcono[i] = Img(habMarco[i].transform, "Icono", cuadro, Color.gray, Vector2.zero, Vector2.zero, Vector2.zero);
            Estirar(habIcono[i].rectTransform, 6); habIcono[i].preserveAspect = true;
            habNombre[i] = Etiqueta(habMarco[i].transform, "", 15, FontStyle.Bold, Color.white, Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter);
            Estirar(habNombre[i].rectTransform, 7); habNombre[i].GetComponent<Shadow>().effectColor = new Color(0, 0, 0, 0.9f);
            habReloj[i] = Img(habMarco[i].transform, "Reloj", cuadro, new Color(0, 0, 0, 0.72f), Vector2.zero, Vector2.zero, Vector2.zero);
            Estirar(habReloj[i].rectTransform, 3);
            habReloj[i].type = Image.Type.Filled; habReloj[i].fillMethod = Image.FillMethod.Radial360;
            habReloj[i].fillOrigin = 2; habReloj[i].fillClockwise = false; habReloj[i].fillAmount = 0f;
            habSeg[i] = Etiqueta(habMarco[i].transform, "", 34, FontStyle.Bold, Color.white, Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter);
            Estirar(habSeg[i].rectTransform, 0); Titulo(habSeg[i]);
            var tk = Img(habMarco[i].transform, "Tecla", Adornos.Perilla(), dorado, new Vector2(0, 1), new Vector2(-10, 10), new Vector2(30, 30));
            var tkt = Etiqueta(tk.transform, (i + 1).ToString(), 18, FontStyle.Bold, tinta, Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter);
            Estirar(tkt.rectTransform, 0);
        }

        // ---------- Invocacion de la tripulacion (al lado de la barra de habilidades): G invocar, H cambiar
        var ci = Img(grupoExploracion.transform, "Invocacion", cuadro, Color.white, new Vector2(0, 0), new Vector2(392, 24 + 84), new Vector2(84, 84));
        Adornos.Decorar(ci, new Color(0.14f, 0.08f, 0.04f, 0.93f), Adornos.Oro, false);
        cajaInvocar = ci.gameObject;
        var hueco = Img(ci.transform, "Hueco", cuadro, Color.white, Vector2.zero, Vector2.zero, Vector2.zero);
        Estirar(hueco.rectTransform, 6); Adornos.Hundir(hueco);
        invIcono = Img(ci.transform, "Retrato", Adornos.Perilla(), Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(56, 56));
        invLetra = Etiqueta(invIcono.transform, "", 30, FontStyle.Bold, tinta, Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter);
        Estirar(invLetra.rectTransform, 0);
        invActivo = Img(ci.transform, "Duracion", circulo, new Color(1f, 1f, 1f, 0.35f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(70, 70));
        invActivo.type = Image.Type.Filled; invActivo.fillMethod = Image.FillMethod.Radial360; invActivo.fillOrigin = 2; invActivo.fillAmount = 0f;
        invActivo.transform.SetSiblingIndex(hueco.transform.GetSiblingIndex() + 1);   // detras del retrato: aro que se va vaciando
        invReloj = Img(ci.transform, "Recarga", cuadro, new Color(0, 0, 0, 0.7f), Vector2.zero, Vector2.zero, Vector2.zero);
        Estirar(invReloj.rectTransform, 6);
        invReloj.type = Image.Type.Filled; invReloj.fillMethod = Image.FillMethod.Radial360; invReloj.fillOrigin = 2; invReloj.fillClockwise = false; invReloj.fillAmount = 0f;
        invSeg = Etiqueta(ci.transform, "", 28, FontStyle.Bold, Color.white, Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter);
        Estirar(invSeg.rectTransform, 0); Titulo(invSeg);
        var tg = Img(ci.transform, "Tecla", Adornos.Perilla(), dorado, new Vector2(0, 1), new Vector2(-10, 10), new Vector2(28, 28));
        var ltg = Etiqueta(tg.transform, "G", 16, FontStyle.Bold, tinta, Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter); Estirar(ltg.rectTransform, 0);
        invNombre = Etiqueta(ci.transform, "", 15, FontStyle.Bold, new Color(1f, 0.93f, 0.75f), new Vector2(-28, 22), new Vector2(140, 20), TextAnchor.MiddleCenter);
        var sn = invNombre.GetComponent<Shadow>(); sn.effectColor = new Color(0, 0, 0, 0.9f); sn.effectDistance = new Vector2(1.5f, -1.5f);
        cajaInvocar.SetActive(false);

        // ---------- Vineta roja (dano y vida baja), detras de todo el HUD
        vineta = Img(raiz, "Herida", Adornos.Vineta(), new Color(0.75f, 0.05f, 0.04f, 0f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Estirar(vineta.rectTransform, 0);
        vineta.raycastTarget = false; vineta.enabled = false;
        vineta.transform.SetSiblingIndex(0);
        herida = 0f;

        // ---------- Capa de numeros de dano
        capaNumeros = new GameObject("Numeros", typeof(RectTransform)).GetComponent<RectTransform>();
        capaNumeros.SetParent(raiz, false); Estirar(capaNumeros, 0);

        // ---------- Objetivo (arriba a la izquierda)
        // debajo del minimapa (derecha), compacto; se aclara cuando cambia y luego se vuelve transparente
        var obj = Panel(grupoExploracion.transform, "Objetivo", new Vector2(0.5f, 0), new Vector2(0, 30), new Vector2(380, 150), panelOscuro);   // abajo al centro
        panelObjetivo = obj.gameObject;
        grupoObjetivo = obj.gameObject.AddComponent<CanvasGroup>(); grupoObjetivo.blocksRaycasts = false;
        tituloObjetivo = Etiqueta(obj, "OBJETIVO", 18, FontStyle.Bold, dorado, new Vector2(0, 0), new Vector2(220, 28), TextAnchor.MiddleCenter);
        ConPlaca(obj, tituloObjetivo, 220f, 32f);
        textoObjetivo = Etiqueta(obj, "", 19, FontStyle.Normal, Color.white, new Vector2(20, -26), new Vector2(340, 60), TextAnchor.UpperLeft);
        // filas con casilla para las tareas: vacia = pendiente, verde = cumplida
        for (int i = 0; i < filasTarea.Length; i++)
        {
            var fr = new GameObject("Tarea" + i, typeof(RectTransform)).GetComponent<RectTransform>();
            fr.SetParent(obj, false); fr.anchorMin = fr.anchorMax = fr.pivot = new Vector2(0, 1);
            fr.sizeDelta = new Vector2(340, 26);
            var casillaT = Img(fr, "Casilla", Adornos.Hundido(1f), Color.white, new Vector2(0, 1), new Vector2(0, -2), new Vector2(22, 22));
            casillaT.type = Image.Type.Sliced;
            var bordeT = casillaT.gameObject.AddComponent<Outline>(); bordeT.effectColor = new Color(dorado.r, dorado.g, dorado.b, 0.8f); bordeT.effectDistance = new Vector2(1, -1);
            marcasTarea[i] = Img(casillaT.transform, "Marca", Adornos.Bisel(), new Color(0.35f, 0.82f, 0.35f), Vector2.zero, Vector2.zero, Vector2.zero);
            marcasTarea[i].type = Image.Type.Sliced; Estirar(marcasTarea[i].rectTransform, 3);
            textosTarea[i] = Etiqueta(fr, "", 19, FontStyle.Normal, Color.white, new Vector2(32, 0), new Vector2(308, 26), TextAnchor.MiddleLeft);
            filasTarea[i] = fr;
        }
        textoObjetivo.supportRichText = true;

        // ---------- Reloj del dia (debajo del minimapa; solo si hay CicloDia en la escena)
        var rel = Img(grupoExploracion.transform, "Reloj", cuadro, Color.white, new Vector2(1, 1), new Vector2(-225, -306), new Vector2(120, 32));
        Adornos.Decorar(rel, new Color(0.14f, 0.08f, 0.04f, 0.9f), Adornos.Oro, false);
        cajaReloj = rel.gameObject;
        iconoReloj = Img(rel.transform, "SolLuna", Adornos.Perilla(), dorado, new Vector2(0, 0.5f), new Vector2(10, 11), new Vector2(22, 22));
        textoReloj = Etiqueta(rel.transform, "", 18, FontStyle.Bold, new Color(1f, 0.93f, 0.75f), new Vector2(38, 0), new Vector2(76, 32), TextAnchor.MiddleLeft);
        cajaReloj.SetActive(false);

        // ---------- Mapa vivo en miniatura + tripulacion (arriba a la derecha)
        grupoMiniMapa = Grupo("MiniMapa");
        var mapa = Panel(grupoMiniMapa.transform, "Mapa", new Vector2(1, 1), new Vector2(-30, -30), new Vector2(300, 220), pergamino);
        if (spritePergamino) { var im = mapa.GetComponent<Image>(); im.sprite = spritePergamino; im.color = Color.white; im.type = spritePergamino.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple; }
        Etiqueta(mapa, "Mapa vivo", 20, FontStyle.Bold, tinta, new Vector2(0, -8), new Vector2(300, 28), TextAnchor.UpperCenter);
        var centro = new GameObject("Centro", typeof(RectTransform)).GetComponent<RectTransform>();
        centro.SetParent(mapa, false); centro.anchorMin = centro.anchorMax = new Vector2(0.5f, 0.45f);
        rutaMini = Img(centro, "Ruta", cuadro, tinta, new Vector2(0.5f, 0.5f), new Vector2(0, 1), new Vector2(150, 5));
        rutaMini.type = Image.Type.Filled; rutaMini.fillMethod = Image.FillMethod.Horizontal; rutaMini.fillAmount = GameState.Fragmentos >= 1 ? 1 : 0;
        rutaMini.rectTransform.localRotation = Quaternion.Euler(0, 0, 10f);
        Img(centro, "Caleta", circulo, new Color(0.35f, 0.62f, 0.32f), new Vector2(0.5f, 0.5f), new Vector2(-70, -12), new Vector2(46, 46));
        puntoFaro = Img(centro, "Faro", circulo, new Color(0.35f, 0.62f, 0.32f, GameState.Fragmentos >= 1 ? 1 : 0), new Vector2(0.5f, 0.5f), new Vector2(72, 14), new Vector2(40, 40));
        var niebla = Img(centro, "Niebla", circulo, new Color(0.5f, 0.5f, 0.5f, 0.6f), new Vector2(0.5f, 0.5f), new Vector2(10, -48), new Vector2(30, 30));
        var q = Etiqueta(niebla.transform, "?", 22, FontStyle.Bold, Color.white, Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter); Estirar(q.rectTransform, 0);
        marcoActual = Img(centro, "Aqui", circulo, new Color(0.95f, 0.75f, 0.25f, 0.55f), new Vector2(0.5f, 0.5f), new Vector2(-70, -12), new Vector2(62, 62));
        marcoActual.transform.SetSiblingIndex(1);
        Etiqueta(mapa, "M: abrir mapa", 17, FontStyle.Italic, tinta, new Vector2(0, -194), new Vector2(300, 24), TextAnchor.UpperCenter);

        var trip = Panel(grupoMiniMapa.transform, "Tripulacion", new Vector2(1, 1), new Vector2(-30, -360), new Vector2(300, 70), panelOscuro);   // debajo del minimapa y su ayuda
        filaMara = trip.gameObject;
        filaMara.SetActive(false);   // panel de tripulacion oculto (no hace falta en pantalla)
        Etiqueta(trip, "TRIPULACIÓN", 18, FontStyle.Bold, dorado, new Vector2(16, -8), new Vector2(200, 24), TextAnchor.UpperLeft);
        iconoMara = Img(trip, "Mara", circulo, turquesa, new Vector2(0, 1), new Vector2(16, -32), new Vector2(32, 32));
        var m = Etiqueta(iconoMara.transform, "M", 18, FontStyle.Bold, Color.white, Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter); Estirar(m.rectTransform, 0);
        // los 4 tripulantes: en color los reclutados, apagados los que faltan
        iconoMara.gameObject.SetActive(false);
        textoTripulacion = Etiqueta(trip, "", 19, FontStyle.Normal, Color.white, new Vector2(16, -36), new Vector2(276, 26), TextAnchor.UpperLeft);

        // ---------- Indicador de interaccion (abajo al centro)
        var pr = Panel(raiz, "Prompt", new Vector2(0.5f, 0), new Vector2(0, 440), new Vector2(460, 56), panelOscuro);
        pr.pivot = new Vector2(0.5f, 0);
        cajaPrompt = pr.gameObject;
        var tecla = Img(pr, "Tecla", cuadro, dorado, new Vector2(0, 0.5f), new Vector2(14, 0), new Vector2(40, 40));
        Adornos.Biselar(tecla);
        tecla.rectTransform.pivot = new Vector2(0, 0.5f);
        var e = Etiqueta(tecla.transform, "E", 24, FontStyle.Bold, tinta, Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter); Estirar(e.rectTransform, 0);
        teclaPrompt = e;
        textoPrompt = Etiqueta(pr, "", 24, FontStyle.Bold, Color.white, new Vector2(66, 0), new Vector2(380, 56), TextAnchor.MiddleLeft);
        barraRecolectar = Img(pr, "Recoleccion", cuadro, new Color(0.45f, 0.85f, 0.4f), new Vector2(0, 0), new Vector2(8, 5), new Vector2(444, 6));
        barraRecolectar.rectTransform.pivot = Vector2.zero;
        barraRecolectar.type = Image.Type.Filled; barraRecolectar.fillMethod = Image.FillMethod.Horizontal;
        textoPrompt.rectTransform.anchorMin = textoPrompt.rectTransform.anchorMax = new Vector2(0, 0.5f);
        textoPrompt.rectTransform.pivot = new Vector2(0, 0.5f);

        // ---------- Avisos y dialogos (abajo al centro)
        // mas abajo y mas pequeno (el centro de abajo ya esta libre)
        var av = Panel(raiz, "Aviso", new Vector2(0.5f, 0), new Vector2(0, 62), new Vector2(780, 96), new Color(0.06f, 0.09f, 0.14f, 0.85f));
        av.pivot = new Vector2(0.5f, 0);
        cajaAviso = av.gameObject;
        textoAviso = Etiqueta(av, "", 23, FontStyle.Normal, Color.white, Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter);
        Estirar(textoAviso.rectTransform, 18);
        textoAviso.horizontalOverflow = HorizontalWrapMode.Wrap;
        var et = Panel(av, "Hablante", new Vector2(0, 1), new Vector2(24, 12), new Vector2(210, 34), dorado);
        et.pivot = new Vector2(0, 0);
        etiquetaHablante = et.gameObject;
        textoHablante = Etiqueta(et, "", 19, FontStyle.Bold, tinta, Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter);
        Estirar(textoHablante.rectTransform, 0);

        // ---------- Menu de pausa (desde aqui los paneles si usan la piel del paquete)
        pielActiva = true;
        panelPausa = new GameObject("Pausa", typeof(RectTransform), typeof(Image));
        panelPausa.transform.SetParent(raiz, false);
        var pp = panelPausa.GetComponent<Image>(); pp.color = new Color(0, 0, 0, 0.65f);
        Estirar(panelPausa.GetComponent<RectTransform>(), 0);
        var caja = Panel(panelPausa.transform, "Caja", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(520, 560), pergamino);
        cajaPausa = caja;
        caja.pivot = new Vector2(0.5f, 0.5f);
        ConPlaca(caja, Etiqueta(caja, "PAUSA", 40, FontStyle.Bold, dorado, new Vector2(0, -30), new Vector2(520, 70), TextAnchor.UpperCenter), 260f);
        Adornos.Separador(caja, new Vector2(0.5f, 1f), new Vector2(0, -88), 360f, new Color(0.55f, 0.33f, 0.14f));
        Boton(caja, "Continuar", new Vector2(0, -120), AlternarPausa);
        Boton(caja, "Controles", new Vector2(0, -210), () =>
        {
            bool ver = !panelControles.activeSelf;
            panelControles.SetActive(ver);
            caja.anchoredPosition = new Vector2(ver ? -470f : 0f, 0f);   // la pausa se corre a la izquierda para dejar ver los controles
        });
        Boton(caja, "Reiniciar isla", new Vector2(0, -300), () => { GameState.VidaKai = Progreso.VidaMax; GameState.CargarEscena(escena); });
        Boton(caja, "Menú principal", new Vector2(0, -390), () => GameState.CargarEscena("Menu"));
        Adornos.Separador(caja, new Vector2(0.5f, 0f), new Vector2(0, 50), 360f, new Color(0.55f, 0.33f, 0.14f));
        panelControles = PanelControles(panelPausa.transform);
        panelPausa.SetActive(false);

        // ---------- Fundido
        fundido = Img(raiz, "Fundido", cuadro, Color.black, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Estirar(fundido.rectTransform, 0);

        // ---------- Que se ve en cada escena
        if (esMapa) { grupoExploracion.SetActive(false); grupoMiniMapa.SetActive(false); }
        if (escena == "Menu") ConstruirMenu();
        AjustarPanelesConPiel();
        AdornarPaneles();
        fundido.transform.SetAsLastSibling();
        if (escena == "Navegacion") { cajaSigilo.SetActive(false); grupoMiniMapa.SetActive(false); casillas[0].parent.gameObject.SetActive(false); barraHab.SetActive(false); panelRecursos.SetActive(false); }
    }

    // ---------- Menu principal (escena "Menu")
    void ConstruirMenu()
    {
        grupoExploracion.SetActive(false); grupoMiniMapa.SetActive(false);
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;

        // fondo: la escena 3D se ve detras; si pusiste una imagen, va encima de la escena
        if (spriteFondoMenu)
        {
            var fondo = Img(raiz, "FondoMenu", spriteFondoMenu, Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            Estirar(fondo.rectTransform, 0); fondo.transform.SetSiblingIndex(0);
        }
        // degradado oscuro abajo para que los botones se lean sobre cualquier fondo
        var velo = Img(raiz, "Velo", CrearDegradado(), new Color(0.02f, 0.04f, 0.09f, 0.85f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Estirar(velo.rectTransform, 0); velo.transform.SetSiblingIndex(spriteFondoMenu ? 1 : 0);

        // titulo
        tituloMenu = Etiqueta(raiz, "La Leyenda\ndel Grumete", 120, FontStyle.Bold, dorado, Vector2.zero, new Vector2(1400, 300), TextAnchor.MiddleCenter);
        var tr = tituloMenu.rectTransform;
        tr.anchorMin = tr.anchorMax = tr.pivot = new Vector2(0.5f, 1f);
        tr.anchoredPosition = new Vector2(0, -70);
        tituloMenu.lineSpacing = 0.85f;
        if (fuenteTitulos) tituloMenu.font = fuenteTitulos;
        var so = tituloMenu.GetComponent<Shadow>(); so.effectColor = new Color(0.25f, 0.12f, 0.05f, 0.9f); so.effectDistance = new Vector2(6, -6);
        var ol = tituloMenu.gameObject.AddComponent<Outline>(); ol.effectColor = new Color(0.2f, 0.1f, 0.04f, 1f); ol.effectDistance = new Vector2(3, -3);

        var sub = Etiqueta(raiz, "Un grumete. Un mapa en blanco. Un océano por descubrir.", 32, FontStyle.Italic, Color.white, Vector2.zero, new Vector2(1400, 50), TextAnchor.MiddleCenter);
        sub.rectTransform.anchorMin = sub.rectTransform.anchorMax = sub.rectTransform.pivot = new Vector2(0.5f, 1f);
        sub.rectTransform.anchoredPosition = new Vector2(0, -375);
        Titulo(sub);

        // botones al centro; al abrir un panel los botones se esconden y el panel sale en el centro
        // con un boton "Atrás" que vuelve a mostrar los botones
        var menu = new GameObject("Botones", typeof(RectTransform)).GetComponent<RectTransform>();
        menu.SetParent(raiz, false);
        menu.anchorMin = menu.anchorMax = menu.pivot = new Vector2(0.5f, 0f);
        menu.anchoredPosition = new Vector2(0, 40); menu.sizeDelta = new Vector2(380, 520);

        var opciones = PanelOpciones(raiz);
        var controles = PanelControles(raiz);
        GameObject[] paneles = { opciones, controles };
        System.Action volver = () =>
        {
            foreach (var x in paneles) x.SetActive(false);
            menu.gameObject.SetActive(true); sub.gameObject.SetActive(true);
        };
        System.Action<GameObject> mostrar = (p) =>
        {
            foreach (var x in paneles) x.SetActive(false);
            p.SetActive(true);
            menu.gameObject.SetActive(false);
            sub.gameObject.SetActive(false);   // el subtitulo se esconde para dejarle espacio al panel
        };
        foreach (var pnl in paneles)
        {
            var rc = pnl.GetComponent<RectTransform>();
            // centrado en el espacio entre el final del titulo (~200 px sobre el centro) y el borde de abajo (-540):
            // el medio de ese espacio es -170 (la placa de arriba y el boton de abajo sobresalen casi igual)
            rc.anchorMin = rc.anchorMax = rc.pivot = new Vector2(0.5f, 0.5f);
            rc.anchoredPosition = new Vector2(0, -167);
            // boton Atrás montado sobre el borde de abajo (como la placa del titulo arriba)
            Boton(rc, "Atrás", Vector2.zero, () => volver());
            var at = rc.GetChild(rc.childCount - 1) as RectTransform;
            at.anchorMin = at.anchorMax = new Vector2(0.5f, 0f); at.pivot = new Vector2(0.5f, 0.5f);
            at.anchoredPosition = new Vector2(0, spritePanel ? -margenPanel : 0f);   // el margen de la piel lo vuelve a subir
            at.sizeDelta = new Vector2(240, 58);
        }
        menuVolver = volver; menuPaneles = paneles;

        float y = 0f, paso = -82f;
        if (GameState.HayPartida) { Boton(menu, "Continuar", new Vector2(0, y), GameState.Continuar); y += paso; }
        Boton(menu, "Nueva partida", new Vector2(0, y), GameState.NuevaPartida); y += paso;
        Boton(menu, "Opciones", new Vector2(0, y), () => mostrar(opciones)); y += paso;
        Boton(menu, "Controles", new Vector2(0, y), () => mostrar(controles)); y += paso;
        Boton(menu, "Salir", new Vector2(0, y), () =>
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        });
        foreach (Transform b in menu) b.GetComponent<RectTransform>().sizeDelta = new Vector2(360, 66);

        var version = Etiqueta(raiz, "v0.2 · Grupo 6", 18, FontStyle.Normal, new Color(1, 1, 1, 0.6f), Vector2.zero, new Vector2(400, 30), TextAnchor.LowerRight);
        version.rectTransform.anchorMin = version.rectTransform.anchorMax = version.rectTransform.pivot = new Vector2(1, 0);
        version.rectTransform.anchoredPosition = new Vector2(-24, 16);
    }

    GameObject PanelOpciones(Transform padre)
    {
        var c = Panel(padre, "Opciones", new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(620, 330), panelOscuro);   // alto pensado para la placa arriba y el boton Atrás abajo
        ConPlaca(c, Etiqueta(c, "OPCIONES", 32, FontStyle.Bold, dorado, new Vector2(0, -24), new Vector2(620, 40), TextAnchor.UpperCenter), 300f);
        Deslizador(c, "Volumen", -85, Opciones.Volumen, 0f, 1f, v => { Opciones.Volumen = v; Opciones.Guardar(); }, v => Mathf.RoundToInt(v * 100) + "%");
        Deslizador(c, "Sensibilidad del mouse", -179, Opciones.Sensibilidad, 0.3f, 2f, v => { Opciones.Sensibilidad = v; Opciones.Guardar(); }, v => v.ToString("0.0") + "x");
        c.gameObject.SetActive(false);
        return c.gameObject;
    }

    void Deslizador(Transform padre, string titulo, float y, float valor, float min, float max,
                    System.Action<float> alCambiar, System.Func<float, string> formato)
    {
        Adornos.Fila((RectTransform)padre, new Vector2(24, y + 8), new Vector2(572, 82));
        Etiqueta(padre, titulo, 24, FontStyle.Normal, Color.white, new Vector2(40, y), new Vector2(380, 34), TextAnchor.MiddleLeft);
        var num = Etiqueta(padre, formato(valor), 22, FontStyle.Bold, dorado, new Vector2(460, y), new Vector2(120, 34), TextAnchor.MiddleRight);
        var fondo = Img(padre, "Slider_" + titulo, cuadro, new Color(0, 0, 0, 0.55f), new Vector2(0, 1), new Vector2(40, y - 42), new Vector2(540, 18));
        Adornos.Hundir(fondo);
        var relleno = Img(fondo.transform, "Relleno", cuadro, dorado, Vector2.zero, Vector2.zero, Vector2.zero);
        Adornos.Biselar(relleno);
        relleno.rectTransform.anchorMin = Vector2.zero; relleno.rectTransform.anchorMax = new Vector2(0, 1);
        relleno.rectTransform.pivot = new Vector2(0, 0.5f); relleno.rectTransform.offsetMin = relleno.rectTransform.offsetMax = Vector2.zero;
        var area = new GameObject("Area", typeof(RectTransform)).GetComponent<RectTransform>();
        area.SetParent(fondo.transform, false); Estirar(area, 0);
        var asa = Img(area, "Asa", Adornos.Perilla(), new Color(1f, 0.92f, 0.7f), new Vector2(0, 0.5f), Vector2.zero, new Vector2(32, 32));
        asa.preserveAspect = true;
        asa.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        asa.rectTransform.anchorMin = new Vector2(0, 0); asa.rectTransform.anchorMax = new Vector2(0, 1);
        asa.rectTransform.sizeDelta = new Vector2(34, 16);
        fondo.raycastTarget = true; asa.raycastTarget = true;
        var sl = fondo.gameObject.AddComponent<Slider>();
        sl.fillRect = relleno.rectTransform; sl.handleRect = asa.rectTransform; sl.targetGraphic = asa;
        sl.minValue = min; sl.maxValue = max; sl.value = valor;
        sl.onValueChanged.AddListener(v => { num.text = formato(v); alCambiar(v); });
    }

    void Interruptor(Transform padre, string titulo, float y, bool valor, System.Action<bool> alCambiar)
    {
        Adornos.Fila((RectTransform)padre, new Vector2(24, y + 6), new Vector2(572, 56));
        Etiqueta(padre, titulo, 24, FontStyle.Normal, Color.white, new Vector2(40, y), new Vector2(420, 44), TextAnchor.MiddleLeft);
        var caja = Img(padre, "Toggle_" + titulo, cuadro, new Color(0, 0, 0, 0.55f), new Vector2(0, 1), new Vector2(536, y - 2), new Vector2(44, 40));
        caja.raycastTarget = true;
        Adornos.Hundir(caja);
        var marca = Img(caja.transform, "Marca", Adornos.Rombo(), dorado, Vector2.zero, Vector2.zero, Vector2.zero);
        Estirar(marca.rectTransform, 6); marca.preserveAspect = true;
        var tg = caja.gameObject.AddComponent<Toggle>();
        tg.targetGraphic = caja; tg.graphic = marca; tg.isOn = valor;
        tg.onValueChanged.AddListener(v => alCambiar(v));
    }

    static Sprite CrearDegradado()
    {
        var tex = new Texture2D(1, 64, TextureFormat.RGBA32, false);
        for (int y = 0; y < 64; y++) tex.SetPixel(0, y, new Color(1, 1, 1, Mathf.Clamp01(1f - y / 40f)));
        tex.Apply(); tex.wrapMode = TextureWrapMode.Clamp;
        return Sprite.Create(tex, new Rect(0, 0, 1, 64), new Vector2(0.5f, 0.5f));
    }

    public GameObject PanelControles(Transform padre)
    {
        // dos columnas con categorias; cada tecla es un "botoncito" dibujado
        // panel mas grande, con aire por todos lados: placa arriba, dos columnas al medio y espacio abajo para "Atrás"
        var c = Panel(padre, "Controles", new Vector2(0.5f, 0.5f), new Vector2(420, 0), new Vector2(820, 600), panelOscuro);
        c.pivot = new Vector2(0.5f, 0.5f);
        var titulo = Etiqueta(c, "CONTROLES", 32, FontStyle.Bold, dorado, new Vector2(0, -14), new Vector2(820, 44), TextAnchor.UpperCenter);
        ConPlaca(c, titulo, 320f);
        // dos bandas hundidas iguales y centradas (24 px a cada lado, 20 px entre ellas)
        const float anchoBanda = 376f, arriba = -58f, altoBanda = 480f;
        float x1 = 24f, x2 = 24f + anchoBanda + 20f;
        Adornos.Fila(c, new Vector2(x1, arriba), new Vector2(anchoBanda, altoBanda));
        Adornos.Fila(c, new Vector2(x2, arriba), new Vector2(anchoBanda, altoBanda));
        float ix1 = x1 + 22f, ix2 = x2 + 22f, inicio = arriba - 20f;

        float y = inicio;
        y = Categoria(c, "MOVIMIENTO", ix1, y);
        y = FilaControl(c, ix1, y, "W A S D", "moverse");
        y = FilaControl(c, ix1, y, "Shift", "correr");
        y = FilaControl(c, ix1, y, "Espacio", "saltar");
        y = FilaControl(c, ix1, y, "C", "agacharse / esconderse");
        y = FilaControl(c, ix1, y, "V", "distancia de cámara");
        y -= 10f;
        y = Categoria(c, "MUNDO", ix1, y);
        y = FilaControl(c, ix1, y, "E", "hablar / abrir / barco");
        y = FilaControl(c, ix1, y, "F", "recoger");
        y = FilaControl(c, ix1, y, "M", "mapa");
        FilaControl(c, ix1, y, "Esc", "pausa");

        y = inicio;
        y = Categoria(c, "COMBATE", ix2, y);
        y = FilaControl(c, ix2, y, "Clic", "atacar");
        y = FilaControl(c, ix2, y, "1 2 3 4", "habilidades");
        y = FilaControl(c, ix2, y, "Clic der.", "bloquear");
        y -= 10f;
        y = Categoria(c, "EQUIPO", ix2, y);
        y = FilaControl(c, ix2, y, "Z", "cambiar arma");
        y = FilaControl(c, ix2, y, "X", "cambiar poción");
        y = FilaControl(c, ix2, y, "T", "cambiar objeto");
        y = FilaControl(c, ix2, y, "Q", "tomar poción");
        y = FilaControl(c, ix2, y, "R", "usar objeto");
        y = FilaControl(c, ix2, y, "Tab", "ver objetivo");
        y = FilaControl(c, ix2, y, "G", "invocar tripulante");
        FilaControl(c, ix2, y, "H", "cambiar tripulante");

        c.gameObject.SetActive(false);
        return c.gameObject;
    }

    float Categoria(RectTransform padre, string nombre, float x, float y)
    {
        var t = Etiqueta(padre, nombre, 22, FontStyle.Bold, dorado, new Vector2(x, y), new Vector2(340, 28), TextAnchor.MiddleLeft);
        Titulo(t);
        return y - 32f;
    }

    // [tecla] accion
    float FilaControl(RectTransform padre, float x, float y, string tecla, string accion)
    {
        float ancho = Mathf.Max(38f, 16f + tecla.Length * 12f);
        var caja = Img(padre, "Tecla_" + tecla, Adornos.Tecla(new Color(0.34f, 0.2f, 0.09f)), Color.white, new Vector2(0, 1), new Vector2(x, y + 1), new Vector2(ancho, 32));
        caja.type = Image.Type.Sliced;
        if (spriteCasilla) Piel(caja, spriteCasilla, Color.white);
        Adornos.Sombra(caja.gameObject, 2f);
        var tt = Etiqueta(caja.transform, tecla, 17, FontStyle.Bold, Color.white, Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter);
        Estirar(tt.rectTransform, 0);
        Etiqueta(padre, accion, 21, FontStyle.Normal, Color.white, new Vector2(x + ancho + 12f, y), new Vector2(330f - ancho, 30), TextAnchor.MiddleLeft);
        return y - 34f;
    }

    // ================================================================ utilidades de construccion
    GameObject Grupo(string n)
    {
        var g = new GameObject(n, typeof(RectTransform));
        g.transform.SetParent(raiz, false);
        Estirar(g.GetComponent<RectTransform>(), 0);
        return g;
    }

    readonly System.Collections.Generic.List<RectTransform> panelesConPiel = new System.Collections.Generic.List<RectTransform>();

    // Despues de construir: a los paneles con piel les da margen interior y, si son claros (pergamino),
    // pasa sus textos de blanco a tinta oscura para que se lean. Los textos de los botones no se tocan.
    void AjustarPanelesConPiel()
    {
        Color tintaOscura = new Color(0.2f, 0.12f, 0.05f), tituloOscuro = new Color(0.5f, 0.24f, 0.04f);
        foreach (var p in panelesConPiel)
        {
            if (!p) continue;
            Acolchar(p, margenPanel);
            if (!panelClaro) continue;
            foreach (var t in p.GetComponentsInChildren<Text>(true))
            {
                if (EnBoton(t.transform) || (t.transform.parent && t.transform.parent.name.StartsWith("Tecla_"))) continue;
                Color c = t.color;
                bool blanco = c.r > 0.85f && c.g > 0.85f && c.b > 0.85f;
                bool dor = Mathf.Abs(c.r - dorado.r) < 0.1f && Mathf.Abs(c.g - dorado.g) < 0.1f && Mathf.Abs(c.b - dorado.b) < 0.15f;
                if (blanco) t.color = new Color(tintaOscura.r, tintaOscura.g, tintaOscura.b, c.a);
                else if (dor) t.color = tituloOscuro;
                var sh = t.GetComponent<Shadow>();
                if (sh && !(sh is Outline)) sh.effectColor = new Color(1f, 1f, 1f, 0.25f);   // grabado suave sobre papel
            }
        }
    }

    // GetComponentInParent no ve objetos apagados (los paneles empiezan ocultos): se sube a mano
    static bool EnBoton(Transform t)
    {
        for (var x = t; x; x = x.parent) if (x.GetComponent<Button>()) return true;
        return false;
    }

    // agranda el panel y corre su contenido hacia adentro 'm' pixeles por cada lado
    static void Acolchar(RectTransform panel, float m)
    {
        if (m <= 0f) return;
        panel.sizeDelta += new Vector2(m * 2f, m * 2f);
        // el panel crece desde su pivote: se corrige para que crezca parejo hacia los cuatro lados
        panel.anchoredPosition += new Vector2((panel.pivot.x - 0.5f) * 2f * m, (panel.pivot.y - 0.5f) * 2f * m);
        for (int i = 0; i < panel.childCount; i++)
        {
            var c = panel.GetChild(i) as RectTransform;
            if (!c) continue;
            if (c.anchorMin != c.anchorMax) { c.offsetMin += new Vector2(m, m); c.offsetMax -= new Vector2(m, m); continue; }
            Vector2 d = Vector2.zero;
            if (c.anchorMin.x < 0.01f) d.x = m; else if (c.anchorMin.x > 0.99f) d.x = -m;
            if (c.anchorMin.y > 0.99f) d.y = -m; else if (c.anchorMin.y < 0.01f) d.y = m;
            c.anchoredPosition += d;
        }
    }

    RectTransform Panel(Transform padre, string n, Vector2 ancla, Vector2 pos, Vector2 tam, Color col)
    {
        var img = Img(padre, n, cuadro, col, ancla, pos, tam);
        img.rectTransform.pivot = ancla;
        if (spritePanel && col == panelOscuro && pielActiva) { Piel(img, spritePanel, new Color(1, 1, 1, 0.95f)); panelesConPiel.Add(img.rectTransform); }
        paraAdornar.Add(new PanelAdornar { rt = img.rectTransform, color = col });
        return img.rectTransform;
    }

    // ---------------- adornos (que ningun panel se vea plano)
    bool pielActiva;   // la piel del paquete (pergamino/madera) solo va en pausa y menus, no en el HUD del juego
    struct PanelAdornar { public RectTransform rt; public Color color; }
    readonly System.Collections.Generic.List<PanelAdornar> paraAdornar = new System.Collections.Generic.List<PanelAdornar>();
    readonly System.Collections.Generic.List<Text> titulosPlaca = new System.Collections.Generic.List<Text>();
    readonly System.Collections.Generic.List<RectTransform> panelesPlaca = new System.Collections.Generic.List<RectTransform>();
    readonly System.Collections.Generic.List<float> anchosPlaca = new System.Collections.Generic.List<float>();
    readonly System.Collections.Generic.List<float> altosPlaca = new System.Collections.Generic.List<float>();

    // el titulo de este panel va en una placa sobre el borde de arriba
    void ConPlaca(RectTransform panel, Text titulo, float ancho, float alto = 58f) { panelesPlaca.Add(panel); titulosPlaca.Add(titulo); anchosPlaca.Add(ancho); altosPlaca.Add(alto); }

    // se llama al final de Construir (despues del margen de la piel, para no correr los adornos)
    void AdornarPaneles()
    {
        foreach (var p in paraAdornar)
        {
            if (!p.rt) continue;
            var img = p.rt.GetComponent<Image>();
            bool conSprite = panelesConPiel.Contains(p.rt) || (p.rt.name == "Mapa" && spritePergamino);
            if (conSprite) { Adornos.Sombra(p.rt.gameObject); continue; }
            Color c = p.color, borde = Adornos.Oro;
            // cada panel del HUD con su propio estilo
            Color madera = new Color(0.14f, 0.08f, 0.04f, 0.93f);
            switch (p.rt.name)
            {
                case "Objetivo": case "Doblones": case "Recursos": case "Prompt": case "Tripulacion": c = madera; break;
                case "Sigilo": c = new Color(0.16f, 0.05f, 0.04f, 0.93f); break;   // rojo oscuro: peligro
            }
            bool esPapel = Mathf.Abs(c.r - pergamino.r) < 0.05f && Mathf.Abs(c.g - pergamino.g) < 0.05f;
            bool esOro = Mathf.Abs(c.r - dorado.r) < 0.05f && Mathf.Abs(c.g - dorado.g) < 0.05f;
            if (esPapel) borde = new Color(0.55f, 0.33f, 0.14f);
            if (esOro) borde = new Color(0.32f, 0.18f, 0.06f);
            bool grande = p.rt.sizeDelta.y >= 100f && p.rt.sizeDelta.x >= 250f;
            Adornos.Decorar(img, c, borde, grande);
        }
        for (int i = 0; i < panelesPlaca.Count; i++)
        {
            if (!panelesPlaca[i]) continue;
            var t = titulosPlaca[i];
            if (t && t.fontSize > 40) t.fontSize = 40;
            if (t && fuenteTitulos) t.font = fuenteTitulos;
            var placa = Adornos.Placa(panelesPlaca[i], t, anchosPlaca[i], altosPlaca[i]);
            if (panelObjetivo && panelesPlaca[i] == panelObjetivo.transform)
            {
                var cg = placa.gameObject.AddComponent<CanvasGroup>(); cg.ignoreParentGroups = true; cg.blocksRaycasts = false;
            }
        }
    }

    static void Piel(Image img, Sprite sp, Color tinte)
    {
        img.sprite = sp; img.color = tinte;
        img.type = sp.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
    }

    // Panel de 460 x 40 con el nombre a la izquierda y la barra adentro (todas las barras del HUD son iguales)
    Image BarraConPanel(RectTransform padre, string nombre, Vector2 pos, Color col, out Image eco, Color? cuerpo = null)
    {
        var p = Img(padre, "Panel" + nombre, cuadro, Color.white, new Vector2(0, 1), pos, new Vector2(460, 40));
        Adornos.Decorar(p, cuerpo ?? new Color(0.06f, 0.07f, 0.1f, 0.92f), Adornos.Oro, false);
        // letra normal (no la de titulos) para que se lea
        var t = Etiqueta(p.transform, nombre, 15, FontStyle.Bold, new Color(1f, 0.93f, 0.75f), new Vector2(14, -8), new Vector2(92, 24), TextAnchor.MiddleLeft);
        var sh = t.GetComponent<Shadow>(); sh.effectColor = new Color(0, 0, 0, 0.9f); sh.effectDistance = new Vector2(1.5f, -1.5f);
        return Barra(p.transform, nombre, new Vector2(104, -11), new Vector2(342, 18), col, out eco);
    }

    // Barra estilo Dark Souls: fondo, "eco" amarillo que baja lento y relleno de color
    Image Barra(Transform padre, string n, Vector2 pos, Vector2 tam, Color col, out Image eco)
    {
        // marco dorado alrededor de cada barra (tambien si usas el fondo de barra del paquete)
        {
            // marco dorado alrededor de la barra (mismo estilo que los paneles)
            float m = 3f;   // marco pegado a la barra
            var marcoB = Img(padre, "Marco" + n, cuadro, Color.white, new Vector2(0, 1), pos + new Vector2(-m, m), tam + new Vector2(2 * m, 2 * m));
            marcoB.sprite = Adornos.Marco(new Color(0.08f, 0.06f, 0.04f, 1f), new Color(0.93f, 0.52f, 0.16f), 1, 1f); marcoB.type = Image.Type.Sliced;   // cuadrado, madera naranja como los botones
            Adornos.Sombra(marcoB.gameObject, 3f);
        }
        var fondo = Img(padre, n, cuadro, new Color(0, 0, 0, 0.6f), new Vector2(0, 1), pos, tam);
        fondo.sprite = Adornos.Hundido(1f); fondo.type = Image.Type.Sliced; fondo.color = Color.white;   // hueco cuadrado
        eco = Img(fondo.transform, "Eco", cuadro, new Color(0.95f, 0.8f, 0.35f, 0.85f), Vector2.zero, Vector2.zero, Vector2.zero);
        Estirar(eco.rectTransform, 2);
        eco.type = Image.Type.Filled; eco.fillMethod = Image.FillMethod.Horizontal;
        var relleno = Img(fondo.transform, "Relleno", spriteBarraRelleno ? spriteBarraRelleno : cuadro, col, Vector2.zero, Vector2.zero, Vector2.zero);
        Estirar(relleno.rectTransform, 2);
        relleno.type = Image.Type.Filled; relleno.fillMethod = Image.FillMethod.Horizontal;
        // brillo de vidrio arriba y sombra abajo: la barra se ve como un tubo, no plana
        var brillo = Img(fondo.transform, "Brillo", Adornos.Brillo(), Color.white, Vector2.zero, Vector2.zero, Vector2.zero);
        Estirar(brillo.rectTransform, 2);
        return relleno;
    }

    // Casilla del triangulo de equipo: marco, icono, tecla y cantidad
    RectTransform Casilla(Transform padre, int i, Vector2 centro, string tecla)
    {
        // sin sprite propio: rombo oscuro con borde dorado (como las casillas de Dark Souls)
        if (!spriteCasilla)
        {
            var borde = Img(padre, "Borde" + i, cuadro, dorado, Vector2.zero, centro, new Vector2(96, 96));
            borde.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            borde.rectTransform.localRotation = Quaternion.Euler(0, 0, 45f);
            Adornos.Biselar(borde); Adornos.Sombra(borde.gameObject, 4f);
        }
        var marco = Img(padre, "Casilla" + i, cuadro, new Color(0.08f, 0.07f, 0.06f, 0.88f), Vector2.zero, centro, spriteCasilla ? new Vector2(124, 124) : new Vector2(88, 88));
        marco.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        if (spriteCasilla) Piel(marco, spriteCasilla, Color.white);
        else { marco.rectTransform.localRotation = Quaternion.Euler(0, 0, 45f); Adornos.Biselar(marco); }
        var cont = new GameObject("Contenido", typeof(RectTransform)).GetComponent<RectTransform>();
        cont.SetParent(padre, false);
        cont.anchorMin = cont.anchorMax = Vector2.zero; cont.pivot = new Vector2(0.5f, 0.5f);
        cont.anchoredPosition = centro; cont.sizeDelta = new Vector2(124, 124);
        iconosEquipo[i] = Img(cont, "Icono", circulo, Color.white, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(92, 92));
        iconosEquipo[i].preserveAspect = true;
        letrasEquipo[i] = Etiqueta(iconosEquipo[i].transform, "", 34, FontStyle.Bold, Color.white, Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter);
        Estirar(letrasEquipo[i].rectTransform, 0);
        cantidadesEquipo[i] = Etiqueta(cont, "", 26, FontStyle.Bold, Color.white, new Vector2(64, -88), new Vector2(56, 34), TextAnchor.MiddleRight);
        Titulo(cantidadesEquipo[i]);
        var t = Img(cont, "Tecla", circulo, dorado, new Vector2(0, 1), new Vector2(-4, 4), new Vector2(34, 34));
        var tl = Etiqueta(t.transform, tecla, 18, FontStyle.Bold, tinta, Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter);
        Estirar(tl.rectTransform, 0);
        return cont;
    }

    // casilla del tripulante: retrato con su color, aro mientras esta invocado y reloj oscuro mientras se recarga
    void RefrescarInvocacion()
    {
        var inv = Invocacion.Actual;
        int m = inv ? inv.Elegido : -1;
        bool ver = m >= 0 && EsIsla && !(BarcoController.Actual && BarcoController.Actual.Manejando && m != 0);
        if (cajaInvocar.activeSelf != ver) cajaInvocar.SetActive(ver);
        if (!ver) return;
        Color c = Invocacion.Colores[m];
        bool activo = inv.Activo(m);
        float rec = inv.RecargaRestante(m);
        invIcono.color = activo || rec <= 0f ? c : Color.Lerp(c, Color.gray, 0.6f);
        invLetra.text = Tripulacion.Nombres[m].Substring(0, 1);
        invNombre.text = Tripulacion.Nombres[m].Split(' ')[0].ToUpper() + (Tripulacion.Cuantos > 1 ? "  (H)" : "");
        invActivo.color = new Color(c.r, c.g, c.b, 0.9f);
        invActivo.fillAmount = inv.FraccionActivo(m);
        invReloj.fillAmount = inv.FraccionRecarga(m);
        invSeg.text = activo ? "" : rec > 0f ? Mathf.CeilToInt(rec).ToString() : "";
        if (activo) invIcono.rectTransform.localScale = Vector3.one * (1f + Mathf.Sin(Time.unscaledTime * 6f) * 0.05f);
        else invIcono.rectTransform.localScale = Vector3.one;
    }

    void Titulo(Text t)
    {
        if (fuenteTitulos) t.font = fuenteTitulos;
        var sh = t.GetComponent<Shadow>(); sh.effectColor = new Color(0, 0, 0, 0.85f); sh.effectDistance = new Vector2(2, -2);
    }

    // ================================================================ equipo
    void RefrescarEquipo()
    {
        if (!iconosEquipo[0]) return;
        int[] sel = { Equipo.arma, Equipo.pocion, Equipo.objeto };
        string[] iconoN = { Equipo.ArmaActual.icono, Equipo.PocionActual.icono, Equipo.ObjetoActual.icono };
        string[] nombreN = { Equipo.ArmaActual.nombre, Equipo.PocionActual.nombre, Equipo.ObjetoActual.nombre };
        for (int i = 0; i < 3; i++)
        {
            if (sel[i] != ultimoEquipo[i])
            {
                if (ultimoEquipo[i] >= 0) { nombreItemHasta = Time.unscaledTime + 2f; textoNombreItem.text = nombreN[i]; StartCoroutine(Rebote(casillas[i])); }
                ultimoEquipo[i] = sel[i];
                Sprite sp = Icono(iconoN[i]);
                iconosEquipo[i].sprite = sp ? sp : circulo;
                iconosEquipo[i].color = sp ? Color.white : ColorDe(iconoN[i]);
                letrasEquipo[i].text = sp ? "" : nombreN[i].Substring(0, 1);
            }
        }
        var p = Equipo.PocionActual; var o = Equipo.ObjetoActual;
        cantidadesEquipo[0].text = "";
        cantidadesEquipo[1].text = p.cantidad.ToString();
        cantidadesEquipo[2].text = o.efecto == "nada" ? Equipo.Doblones.ToString() : o.cantidad.ToString();
        iconosEquipo[1].color = new Color(iconosEquipo[1].color.r, iconosEquipo[1].color.g, iconosEquipo[1].color.b, p.cantidad > 0 ? 1f : 0.35f);
        // como en Dark Souls: el nombre de la pocion queda visible; al cambiar algo se muestra su nombre un momento
        if (Time.unscaledTime > nombreItemHasta) textoNombreItem.text = p.nombre;
    }

    // ================================================================ barra de habilidades
    void RefrescarHabilidades()
    {
        if (!barraHab || !barraHab.activeInHierarchy) return;
        var arma = Equipo.ArmaActual; var hs = arma.habilidades ?? new Equipo.Habilidad[0]; var hk = HabilidadesKai.Actual;
        int nvArma = Mejoras.NivelArmaPor[Mathf.Clamp(Equipo.arma, 0, 3)];
        int clave = Equipo.arma * 1000 + nvArma * 100 + Progreso.Nivel + (Equipo.Tiene((Equipo.IdArma)Equipo.arma) ? 0 : 50);
        if (armaBarra != clave)   // cambia el arma o el nivel: redibujar las casillas
        {
            armaBarra = clave;
            textoArmaBarra.text = arma.nombre.ToUpper() + (nvArma > 0 ? "  +" + nvArma : "");
            for (int i = 0; i < 4; i++)
            {
                if (i >= hs.Length)   // los punos no tienen habilidades
                {
                    habIcono[i].sprite = cuadro; habIcono[i].color = new Color(0, 0, 0, 0);   // casilla vacia: se ve el hueco
                    habNombre[i].text = ""; habReloj[i].fillAmount = 0f; habSeg[i].text = "";
                    habMarco[i].color = new Color(0.3f, 0.27f, 0.22f);
                    continue;
                }
                Sprite sp = IconoHab(hs[i].icono);
                bool libre = Equipo.HabilidadDesbloqueada(i);
                habIcono[i].sprite = sp ? sp : cuadro;
                habIcono[i].color = !libre ? new Color(0.2f, 0.2f, 0.2f) : sp ? Color.white : Color.Lerp(hs[i].color, Color.black, 0.45f);
                habNombre[i].text = !libre ? "Nv " + Equipo.NivelHabilidad[i] : sp ? "" : hs[i].nombre;
            }
        }
        for (int i = 0; i < 4 && i < hs.Length; i++)
        {
            if (!Equipo.HabilidadDesbloqueada(i)) { habMarco[i].color = new Color(0.3f, 0.27f, 0.22f); habReloj[i].fillAmount = 0f; habSeg[i].text = ""; continue; }
            float resta = hk ? hk.Restante(i) : 0f;
            habReloj[i].fillAmount = hk ? hk.Fraccion(i) : 0f;
            habSeg[i].text = resta > 0f ? Mathf.CeilToInt(resta).ToString() : "";
            bool sinAguante = GameState.Aguante < hs[i].aguante;
            habMarco[i].color = resta > 0f ? new Color(0.35f, 0.3f, 0.22f) : sinAguante ? new Color(0.3f, 0.45f, 0.75f) : dorado;
            if (habAntes[i] > 0f && resta <= 0f) StartCoroutine(Rebote(habMarco[i].rectTransform));   // brilla al estar lista
            habAntes[i] = resta;
        }
    }

    Sprite IconoHab(string nombre)
    {
        string clave = "hab_" + nombre;
        if (iconos.TryGetValue(clave, out var sp)) return sp;
        var tex = Resources.Load<Texture2D>("Habilidades/" + nombre);
        sp = tex ? Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f)) : null;
        iconos[clave] = sp;
        return sp;
    }

    IEnumerator Rebote(RectTransform rt)
    {
        for (float t = 0; t < 0.2f; t += Time.unscaledDeltaTime)
        {
            rt.localScale = Vector3.one * (1f + Mathf.Sin(t / 0.2f * Mathf.PI) * 0.18f);
            yield return null;
        }
        rt.localScale = Vector3.one;
    }

    Sprite Icono(string nombre)
    {
        if (iconos.TryGetValue(nombre, out var sp)) return sp;
        var tex = Resources.Load<Texture2D>("Iconos/" + nombre);
        sp = tex ? Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f)) : null;
        iconos[nombre] = sp;
        return sp;
    }

    static Color ColorDe(string icono)
    {
        switch (icono)
        {
            case "grog": return new Color(0.35f, 0.75f, 0.35f);
            case "ron": return new Color(0.65f, 0.35f, 0.15f);
            case "coco": return new Color(0.85f, 0.85f, 0.75f);
            case "humo": return new Color(0.45f, 0.45f, 0.5f);
            case "doblon": return new Color(0.95f, 0.75f, 0.25f);
            default: return new Color(0.7f, 0.55f, 0.35f);
        }
    }

    // ================================================================ minimapa en tiempo real
    void CrearMinimapa()
    {
        if (!EsIsla && escena != "Navegacion") return;
        Transform seguir = kai ? kai.transform : null;
        if (!seguir) { var barco = FindFirstObjectByType<BarcoController>(); if (barco) seguir = barco.transform; }
        if (!seguir) return;

        // iconos automaticos
        foreach (var r in FindObjectsByType<Rival>(FindObjectsSortMode.None))
            MarcadorMinimapa.Agregar(r.gameObject, r.esJefe ? MarcadorMinimapa.Tipo.Jefe : MarcadorMinimapa.Tipo.Enemigo, r.esJefe, "", r.nombre);
        foreach (var sr in FindObjectsByType<Sereno>(FindObjectsSortMode.None))
            MarcadorMinimapa.Agregar(sr.gameObject, MarcadorMinimapa.Tipo.Enemigo, false, "", "Sereno con farol");
        foreach (var c in cofres) MarcadorMinimapa.Agregar(c.gameObject, MarcadorMinimapa.Tipo.Cofre, false, "", "Cofre de tripulante").Activo = GameState.TieneMara == 0;
        foreach (var b in barbagris) MarcadorMinimapa.Agregar(b.gameObject, MarcadorMinimapa.Tipo.Personaje, false, "", "Barbagrís");
        foreach (var m in FindObjectsByType<Meta>(FindObjectsSortMode.None))
            MarcadorMinimapa.Agregar(m.gameObject, MarcadorMinimapa.Tipo.Objetivo, false, "", "Prueba de " + m.tipo.Trim().ToLower()).Activo = !m.Hecha();
        foreach (var ll in FindObjectsByType<Llegada>(FindObjectsSortMode.None))   // solo la costa del destino se marca
            if (ll.EsDestino) MarcadorMinimapa.Agregar(ll.gameObject, MarcadorMinimapa.Tipo.Objetivo, true, "", "Desembarco: " + (Archipielago.Buscar(GameState.IslaDestino)?.nombre ?? ""));

        minimapa = Minimapa.Crear(grupoMiniMapa.GetComponent<RectTransform>(), seguir, fuente, dorado, tinta);
        // el pergamino estatico ya no hace falta; la tripulacion baja debajo del minimapa
        var viejo = grupoMiniMapa.transform.Find("Mapa"); if (viejo) viejo.gameObject.SetActive(false);
        var trip = grupoMiniMapa.transform.Find("Tripulacion");
        if (trip) trip.GetComponent<RectTransform>().anchoredPosition = new Vector2(-30, -340);
        grupoMiniMapa.SetActive(true);
    }

    // ================================================================ barras sobre la cabeza (enemigos normales)
    void CrearBarraEnemigo(Rival rv)
    {
        var caja = new GameObject("Barra_" + rv.name, typeof(RectTransform)).GetComponent<RectTransform>();
        caja.SetParent(capaNumeros, false);
        caja.anchorMin = caja.anchorMax = caja.pivot = new Vector2(0.5f, 0.5f);
        caja.sizeDelta = new Vector2(150, 14);
        var vida = Barra(caja, "Vida", Vector2.zero, new Vector2(150, 14), new Color(0.8f, 0.12f, 0.1f), out Image eco);
        var fondo = vida.transform.parent.GetComponent<RectTransform>();
        fondo.anchorMin = fondo.anchorMax = fondo.pivot = new Vector2(0.5f, 0.5f);
        fondo.anchoredPosition = Vector2.zero;
        vida.fillAmount = eco.fillAmount = 1f;
        caja.gameObject.SetActive(false);
        barrasEnemigos.Add(new BarraFlotante { r = rv, caja = caja, vida = vida, eco = eco });
    }

    void RefrescarBarrasEnemigos()
    {
        var cam = Camera.main;
        foreach (var b in barrasEnemigos)
        {
            // Como en Dark Souls: aparece al pelear o al recibir dano, y se oculta un rato despues
            bool visible = b.r && b.r.gameObject.activeInHierarchy && !b.r.Muerto && cam &&
                           (Rival.EnCombate == b.r || Time.time - b.r.UltimoDano < 5f);
            Vector3 pantalla = visible ? cam.WorldToScreenPoint(b.r.transform.position + Vector3.up * b.r.alturaBarra) : Vector3.zero;
            if (visible && pantalla.z <= 0f) visible = false;
            b.caja.gameObject.SetActive(visible);
            if (!visible) continue;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(capaNumeros, pantalla, null, out Vector2 local);
            b.caja.anchoredPosition = local;
            // mas pequena si el enemigo esta lejos
            b.caja.localScale = Vector3.one * Mathf.Clamp(12f / Mathf.Max(1f, pantalla.z), 0.55f, 1.1f);
            float ve = Mathf.Clamp01((float)b.r.Vida / b.r.vidaMaxima);
            b.vida.fillAmount = Mathf.Lerp(b.vida.fillAmount, ve, 14f * Time.unscaledDeltaTime);
            b.eco.fillAmount = Mathf.MoveTowards(Mathf.Max(b.eco.fillAmount, b.vida.fillAmount), ve, 0.35f * Time.unscaledDeltaTime);
        }
    }

    // ================================================================ numeros de dano flotantes
    // Uso: HUD.Numero(posicionEnElMundo, "25", Color.yellow, 1f)
    public static void Numero(Vector3 posicionMundo, string texto, Color color, float escala = 1f)
    {
        if (Actual) Actual.CrearNumero(posicionMundo, texto, color, escala);
    }

    void CrearNumero(Vector3 pos, string texto, Color color, float escala)
    {
        var t = Etiqueta(capaNumeros, texto, 44, FontStyle.Bold, color, Vector2.zero, new Vector2(300, 60), TextAnchor.MiddleCenter);
        t.rectTransform.anchorMin = t.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        t.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        Titulo(t);
        var outline = t.gameObject.AddComponent<Outline>(); outline.effectColor = new Color(0, 0, 0, 0.8f); outline.effectDistance = new Vector2(2, -2);
        pos += new Vector3(Random.Range(-0.3f, 0.3f), 0f, Random.Range(-0.3f, 0.3f));
        numeros.Add(new NumeroFlotante { t = t, pos = pos, inicio = Time.unscaledTime, escala = escala });
    }

    void RefrescarNumeros()
    {
        var cam = Camera.main;
        for (int i = numeros.Count - 1; i >= 0; i--)
        {
            var n = numeros[i];
            float k = (Time.unscaledTime - n.inicio) / 0.9f;
            if (k >= 1f || !cam) { Destroy(n.t.gameObject); numeros.RemoveAt(i); continue; }
            Vector3 mundo = n.pos + Vector3.up * (k * 1.2f);
            Vector3 pantalla = cam.WorldToScreenPoint(mundo);
            n.t.enabled = pantalla.z > 0f;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(capaNumeros, pantalla, null, out Vector2 local);
            n.t.rectTransform.anchoredPosition = local;
            float pop = k < 0.15f ? Mathf.Lerp(1.6f, 1f, k / 0.15f) : 1f;
            n.t.rectTransform.localScale = Vector3.one * n.escala * pop;
            var c = n.t.color; c.a = k < 0.6f ? 1f : 1f - (k - 0.6f) / 0.4f; n.t.color = c;
        }
    }

    Image Img(Transform padre, string n, Sprite s, Color col, Vector2 ancla, Vector2 pos, Vector2 tam)
    {
        var go = new GameObject(n, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(padre, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = ancla;
        rt.pivot = ancla.x == 0.5f && ancla.y == 0.5f ? new Vector2(0.5f, 0.5f) : new Vector2(0, 1);
        rt.anchoredPosition = pos; rt.sizeDelta = tam;
        var img = go.GetComponent<Image>();
        img.sprite = s; img.color = col; img.raycastTarget = false;
        return img;
    }

    Text Etiqueta(Transform padre, string txt, int tam, FontStyle estilo, Color col, Vector2 pos, Vector2 caja, TextAnchor al)
    {
        var go = new GameObject("Texto", typeof(RectTransform), typeof(Text), typeof(Shadow));
        go.transform.SetParent(padre, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0, 1); rt.pivot = new Vector2(0, 1);
        rt.anchoredPosition = pos; rt.sizeDelta = caja;
        var t = go.GetComponent<Text>();
        t.font = fuenteTexto ? fuenteTexto : fuente; t.text = txt; t.fontSize = tam; t.fontStyle = estilo; t.color = col;
        t.alignment = al; t.raycastTarget = false; t.supportRichText = true;
        t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
        go.GetComponent<Shadow>().effectColor = new Color(0, 0, 0, col.grayscale > 0.5f ? 0.6f : 0f);
        return t;
    }

    void Boton(Transform padre, string txt, Vector2 pos, UnityEngine.Events.UnityAction accion)
    {
        var img = Img(padre, "Boton_" + txt, cuadro, tinta, new Vector2(0.5f, 1), pos, new Vector2(380, 70));
        if (spriteBoton) Piel(img, spriteBoton, Color.white);
        img.rectTransform.pivot = new Vector2(0.5f, 1);
        img.raycastTarget = true;
        var b = img.gameObject.AddComponent<Button>();
        var cols = b.colors;
        cols.normalColor = Color.white; cols.highlightedColor = new Color(1.25f, 1.15f, 0.9f);
        cols.pressedColor = new Color(0.8f, 0.8f, 0.8f); cols.fadeDuration = 0.08f;
        b.colors = cols;
        b.onClick.AddListener(accion);
        var t = Etiqueta(img.transform, txt, 30, FontStyle.Bold, spriteBoton ? Color.white : pergamino, Vector2.zero, Vector2.zero, TextAnchor.MiddleCenter);
        Estirar(t.rectTransform, 0);
        if (spriteBoton) { Titulo(t); BordeLetra(t); }
        else { Adornos.VestirBoton(img, t); Titulo(t); }
    }

    // contorno oscuro grueso en las letras (como los titulos de juegos de piratas)
    void BordeLetra(Text t)
    {
        var o = t.gameObject.AddComponent<Outline>();
        o.effectColor = bordeLetraBoton; o.effectDistance = new Vector2(2.2f, -2.2f);
        var o2 = t.gameObject.AddComponent<Outline>();
        o2.effectColor = bordeLetraBoton; o2.effectDistance = new Vector2(-1.5f, 1.5f);
    }

    static void Estirar(RectTransform rt, float margen)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(margen, margen); rt.offsetMax = new Vector2(-margen, -margen);
    }

    static Sprite CrearCirculo(int n)
    {
        var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
        float r = n / 2f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(r, r));
                tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(r - d)));
            }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, n, n), new Vector2(0.5f, 0.5f));
    }
}
