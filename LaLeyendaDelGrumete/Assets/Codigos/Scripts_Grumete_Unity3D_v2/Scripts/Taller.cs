using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Taller del pueblo (Fase 3): Kai presiona E cerca y se abre una ventana con dos pestanas:
//   MEJORAS: subir el dano de cada arma (+15% por nivel) y la velocidad del barco.
//   TIENDA : comprar grog, ron, agua de coco y bombas de humo.
// Ponlo en un NPC o en una mesa de herrero (Empty + este componente). E o Esc para cerrar.
public class Taller : MonoBehaviour
{
    public string nombreTaller = "Taller de Barbagrís";
    public float radio = 2.5f;

    [Header("Precios de la tienda (doblones)")]
    public int precioGrog = 15;
    public int precioRon = 40;
    public int precioCoco = 8;
    public int precioBomba = 25;

    public static readonly List<Taller> Todos = new List<Taller>();
    public static bool Abierto;
    public static int CuadroCierre = -1;   // para que el mismo Esc no abra tambien la pausa

    GameObject ventana;
    RectTransform lista;
    Text textoRecursos, textoTitulo;
    Image tabMejoras, tabTienda;
    bool enTienda;
    int cuadroApertura;
    Font fuente; Sprite cuadro;
    readonly Color dorado = new Color(0.95f, 0.75f, 0.3f), oscuro = new Color(0.07f, 0.06f, 0.05f, 0.95f), papel = new Color(0.93f, 0.88f, 0.76f);

    void OnEnable() { Todos.Add(this); }
    void OnDisable() { Todos.Remove(this); if (ventana && ventana.activeSelf) Cerrar(); }

    void Start() { MarcadorMinimapa.Agregar(gameObject, MarcadorMinimapa.Tipo.Personaje, false, "T", nombreTaller); }

    public bool Cerca(Vector3 p) => Vector3.Distance(p, transform.position) < radio;

    void Update()
    {
        if (Abierto && ventana && ventana.activeSelf)
        {
            if (Time.frameCount != cuadroApertura && (Input.GetKeyDown(KeyCode.E) || Input.GetKeyDown(KeyCode.Escape))) Cerrar();
            return;
        }
        var kai = GameState.Kai;
        if (kai && !HUD.Pausado && !Abierto && Cerca(kai.position) && Input.GetKeyDown(KeyCode.E)) Abrir();
    }

    // ================================================================ abrir / cerrar
    void Abrir()
    {
        if (!ventana) Construir();
        Abierto = true; cuadroApertura = Time.frameCount;
        HUD.Pausado = true; Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None; Cursor.visible = true;
        ventana.SetActive(true);
        Refrescar();
    }

    void Cerrar()
    {
        Abierto = false; CuadroCierre = Time.frameCount;
        if (ventana) ventana.SetActive(false);
        HUD.Pausado = false; Time.timeScale = 1f;
    }

    // ================================================================ ventana (hecha por codigo)
    void Construir()
    {
        fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        cuadro = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(.5f, .5f));

        ventana = new GameObject("Taller_UI", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = ventana.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.sortingOrder = 150;
        var sc = ventana.GetComponent<CanvasScaler>();
        sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1920, 1080); sc.matchWidthOrHeight = 0.5f;
        var raiz = ventana.GetComponent<RectTransform>();

        var velo = Img(raiz, new Color(0, 0, 0, 0.6f)); Estirar(velo.rectTransform, 0); velo.raycastTarget = true;
        var marco = Img(raiz, dorado); Poner(marco.rectTransform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(1060, 760));
        var fondo = Img(marco.rectTransform, new Color(0, 0, 0, 0)); Estirar(fondo.rectTransform, 14);
        // con la piel del HUD (por ejemplo madera): el marco es el sprite del panel; si no, marco dorado con relieve
        if (HUD.Vestir(marco, HUD.PielPanel)) { fondo.color = new Color(0.05f, 0.03f, 0.02f, 0.55f); Estirar(fondo.rectTransform, 40); Adornos.Sombra(marco.gameObject); }
        else Adornos.Decorar(marco, new Color(0.11f, 0.07f, 0.04f, 0.97f), Adornos.Oro);

        textoTitulo = Texto(fondo.rectTransform, nombreTaller.ToUpper(), 40, FontStyle.Bold, dorado, TextAnchor.MiddleCenter);
        if (HUD.FuenteTitulos) textoTitulo.font = HUD.FuenteTitulos;
        Poner(textoTitulo.rectTransform, new Vector2(.5f, 1), new Vector2(0, -16), new Vector2(900, 56));
        textoTitulo.fontSize = 34;
        Adornos.Placa(marco.rectTransform, textoTitulo, 560f, 62f);
        textoRecursos = Texto(fondo.rectTransform, "", 24, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter);
        Poner(textoRecursos.rectTransform, new Vector2(.5f, 1), new Vector2(0, -58), new Vector2(900, 34));
        textoRecursos.gameObject.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, 0.8f);

        tabMejoras = Boton(fondo.rectTransform, new Vector2(-130, -104), new Vector2(240, 50), "MEJORAS", () => { enTienda = false; Refrescar(); });
        tabTienda = Boton(fondo.rectTransform, new Vector2(130, -104), new Vector2(240, 50), "TIENDA", () => { enTienda = true; Refrescar(); });

        lista = new GameObject("Lista", typeof(RectTransform)).GetComponent<RectTransform>();
        lista.SetParent(fondo.rectTransform, false);
        Adornos.Separador(fondo.rectTransform, new Vector2(.5f, 1), new Vector2(0, -172), 700f, dorado);
        Poner(lista, new Vector2(.5f, 1), new Vector2(0, -192), new Vector2(880, 480));

        var cerrar = Boton(fondo.rectTransform, new Vector2(0, -660), new Vector2(260, 52), "CERRAR (E)", Cerrar);
        cerrar.rectTransform.anchorMin = cerrar.rectTransform.anchorMax = new Vector2(.5f, 1);
        ventana.SetActive(false);
    }

    void Refrescar()
    {
        bool claro = HUD.PanelClaro;
        textoRecursos.text = claro
            ? "<color=#8A5A00>" + Equipo.Doblones + " doblones</color>     <color=#6B3A14>" + Equipo.Madera + " madera</color>     <color=#3E4650>" + Equipo.Chatarra + " chatarra</color>"
            : "<color=#F2C040>" + Equipo.Doblones + " doblones</color>     <color=#D9A066>" + Equipo.Madera + " madera</color>     <color=#BFC4CC>" + Equipo.Chatarra + " chatarra</color>";
        var sr = textoRecursos.GetComponent<Shadow>(); if (sr) sr.effectColor = claro ? new Color(1, 1, 1, 0.3f) : new Color(0, 0, 0, 0.8f);
        Color activo = Color.white, inactivo = new Color(0.45f, 0.45f, 0.45f);
        tabMejoras.color = enTienda ? inactivo : activo;
        tabTienda.color = enTienda ? activo : inactivo;
        for (int i = lista.childCount - 1; i >= 0; i--) Destroy(lista.GetChild(i).gameObject);

        int fila = 0;
        if (!enTienda)
        {
            for (int a = 1; a <= 3; a++)
            {
                var id = (Equipo.IdArma)a; var arma = Equipo.Armas[a]; int nv = Mejoras.NivelArma(id);
                if (!Equipo.Tiene(id)) { Fila(fila++, arma.nombre, "Aún no la tienes: se consigue en la aventura.", "", null, arma.icono); continue; }
                if (nv >= Mejoras.MaxArma) { Fila(fila++, arma.nombre + "  +" + nv, "Al máximo: daño +" + Mathf.RoundToInt(nv * Mejoras.BonoPorNivelArma * 100) + "%", "", null, arma.icono); continue; }
                var precio = Mejoras.PrecioArma(id);
                Fila(fila++, arma.nombre + (nv > 0 ? "  +" + nv : ""), "Afilar a +" + (nv + 1) + ": daño +" + Mathf.RoundToInt((nv + 1) * Mejoras.BonoPorNivelArma * 100) + "%   ·   " + Mejoras.Texto(precio),
                     "MEJORAR", Mejoras.Alcanza(precio) ? (UnityEngine.Events.UnityAction)(() => { Mejoras.Pagar(precio); Mejoras.NivelArmaPor[(int)id]++; Exito(arma.nombre + " +" + Mejoras.NivelArma(id)); }) : null, arma.icono);
            }
            int nvv = Mejoras.NivelVela;
            if (nvv >= Mejoras.MaxVela) Fila(fila++, "Vela del barco  +" + nvv, "Al máximo: velocidad +" + Mathf.RoundToInt(nvv * Mejoras.BonoPorNivelVela * 100) + "%", "", null, "vela");
            else
            {
                var pv = Mejoras.PrecioVela();
                Fila(fila++, "Vela del barco" + (nvv > 0 ? "  +" + nvv : ""), "Vela nueva +" + (nvv + 1) + ": velocidad +" + Mathf.RoundToInt((nvv + 1) * Mejoras.BonoPorNivelVela * 100) + "%   ·   " + Mejoras.Texto(pv),
                     "MEJORAR", Mejoras.Alcanza(pv) ? (UnityEngine.Events.UnityAction)(() => { Mejoras.Pagar(pv); Mejoras.NivelVela++; Exito("Vela del barco +" + Mejoras.NivelVela); }) : null, "vela");
            }
        }
        else
        {
            Producto(fila++, Equipo.Pociones, "grog", precioGrog, "Cura 40 de vida.");
            Producto(fila++, Equipo.Pociones, "ron", precioRon, "Cura 100 de vida.");
            Producto(fila++, Equipo.Pociones, "coco", precioCoco, "Recupera toda la estamina.");
            Producto(fila++, Equipo.Objetos, "humo", precioBomba, "Nadie te ve durante 4 segundos.");
        }
    }

    void Producto(int fila, Equipo.Consumible[] tabla, string icono, int precio, string desc)
    {
        var c = System.Array.Find(tabla, x => x.icono == icono);
        if (c == null) return;
        precio = Mathf.CeilToInt(precio * Tripulacion.DescuentoTaller);   // descuento de Pico
        Fila(fila, c.nombre + "   <size=20><color=#FFFFFF99>(tienes " + c.cantidad + ")</color></size>", desc + "   ·   <color=#F2C040>" + precio + " doblones</color>",
             "COMPRAR", Equipo.Doblones >= precio ? (UnityEngine.Events.UnityAction)(() => { Equipo.Doblones -= precio; c.cantidad++; Exito(c.nombre); }) : null, icono);
    }

    void Exito(string que)
    {
        GameState.Avisar("¡Listo! " + que, 1.5f);
        Sonidos.Tocar("monedas", null, 0.8f);
        Refrescar();
    }

    // una fila: ficha con marco, icono a la izquierda, nombre, descripcion y boton a la derecha (accion null = boton apagado)
    void Fila(int i, string nombre, string desc, string boton, UnityEngine.Events.UnityAction accion, string icono = "")
    {
        bool claro = HUD.PanelClaro;
        Color tinta = new Color(0.22f, 0.13f, 0.05f), tintaSuave = new Color(0.36f, 0.24f, 0.12f);
        var f = Img(lista, Color.white);
        Poner(f.rectTransform, new Vector2(.5f, 1), new Vector2(0, -i * 100), new Vector2(880, 90));
        // sobre pergamino: ficha de papel claro con borde de madera; sobre fondo oscuro: ficha oscura con borde dorado
        if (claro) Adornos.Decorar(f, new Color(0.96f, 0.9f, 0.76f, 1f), new Color(0.5f, 0.3f, 0.12f), false);
        else Adornos.Decorar(f, new Color(0.16f, 0.11f, 0.07f, 0.95f), dorado, false);

        // casilla del icono
        var hueco = Img(f.rectTransform, Color.white); Adornos.Hundir(hueco);
        Poner(hueco.rectTransform, new Vector2(0, .5f), new Vector2(14, 0), new Vector2(66, 66));
        Sprite sp = CargarIcono(icono);
        var ic = Img(hueco.rectTransform, sp ? Color.white : dorado);
        if (sp) { ic.sprite = sp; ic.preserveAspect = true; Estirar(ic.rectTransform, 6); }
        else { ic.sprite = Adornos.Rombo(); Poner(ic.rectTransform, new Vector2(.5f, .5f), Vector2.zero, new Vector2(26, 26)); }

        var n = Texto(f.rectTransform, nombre, 27, FontStyle.Bold, claro ? tinta : papel, TextAnchor.UpperLeft);
        Poner(n.rectTransform, new Vector2(0, 1), new Vector2(96, -12), new Vector2(560, 34)); n.rectTransform.pivot = new Vector2(0, 1);
        var d = Texto(f.rectTransform, desc, 19, FontStyle.Normal, claro ? tintaSuave : new Color(1, 1, 1, 0.8f), TextAnchor.UpperLeft);
        Poner(d.rectTransform, new Vector2(0, 1), new Vector2(96, -50), new Vector2(580, 30)); d.rectTransform.pivot = new Vector2(0, 1);
        if (claro) { n.text = Oscurecer(n.text); d.text = Oscurecer(d.text); }
        if (boton == "") return;
        var b = Boton(f.rectTransform, new Vector2(-20, 0), new Vector2(180, 54), boton, accion);
        b.rectTransform.anchorMin = b.rectTransform.anchorMax = b.rectTransform.pivot = new Vector2(1, .5f);
        b.rectTransform.anchoredPosition = new Vector2(-16, 0);
        if (accion == null)
        {
            b.color = new Color(0.6f, 0.6f, 0.6f); b.GetComponent<Button>().interactable = false;
            var t = b.GetComponentInChildren<Text>();
            t.text = boton == "COMPRAR" || boton == "MEJORAR" ? "FALTA" : boton;
            t.color = new Color(1f, 0.75f, 0.7f);
        }
    }

    // los colores claros de los precios no se leen sobre papel: se cambian por tonos oscuros
    static string Oscurecer(string s) => s.Replace("#F2C040", "#8A5A00").Replace("#D9A066", "#6B3A14").Replace("#BFC4CC", "#3E4650").Replace("#FFFFFF99", "#5A4630");

    readonly Dictionary<string, Sprite> iconos = new Dictionary<string, Sprite>();
    Sprite CargarIcono(string nombre)
    {
        if (string.IsNullOrEmpty(nombre)) return null;
        if (iconos.TryGetValue(nombre, out var sp)) return sp;
        var tex = Resources.Load<Texture2D>("Iconos/" + nombre);
        sp = tex ? Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(.5f, .5f)) : null;
        iconos[nombre] = sp;
        return sp;
    }

    // ---------------- utilidades
    Image Img(RectTransform padre, Color c)
    {
        var img = new GameObject("Img", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        img.transform.SetParent(padre, false); img.sprite = cuadro; img.color = c; img.raycastTarget = false;
        return img;
    }

    Text Texto(RectTransform padre, string s, int tam, FontStyle st, Color c, TextAnchor al)
    {
        var t = new GameObject("Texto", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
        t.transform.SetParent(padre, false);
        t.font = fuente; t.text = s; t.fontSize = tam; t.fontStyle = st; t.color = c; t.alignment = al; t.supportRichText = true; t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    Image Boton(RectTransform padre, Vector2 pos, Vector2 tam, string s, UnityEngine.Events.UnityAction accion)
    {
        var img = Img(padre, dorado); img.raycastTarget = true;
        Poner(img.rectTransform, new Vector2(.5f, 1), pos, tam);
        var b = img.gameObject.AddComponent<Button>();
        if (accion != null) b.onClick.AddListener(accion);
        bool piel = HUD.Vestir(img, HUD.PielBoton);
        var t = Texto(img.rectTransform, s, 22, FontStyle.Bold, Color.white, TextAnchor.MiddleCenter); Estirar(t.rectTransform, 0);
        if (!piel) Adornos.VestirBoton(img, t);
        if (piel)
        {
            var o = t.gameObject.AddComponent<Outline>(); o.effectColor = new Color(0.22f, 0.09f, 0.02f); o.effectDistance = new Vector2(2.2f, -2.2f);
            var o2 = t.gameObject.AddComponent<Outline>(); o2.effectColor = new Color(0.22f, 0.09f, 0.02f); o2.effectDistance = new Vector2(-1.5f, 1.5f);
        }
        return img;
    }

    static void Poner(RectTransform r, Vector2 ancla, Vector2 pos, Vector2 tam) { r.anchorMin = r.anchorMax = r.pivot = ancla; r.anchoredPosition = pos; r.sizeDelta = tam; }
    static void Estirar(RectTransform r, float m) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = new Vector2(m, m); r.offsetMax = new Vector2(-m, -m); }

    void OnDrawGizmosSelected() { Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(transform.position, radio); }
}
