using UnityEngine;
using UnityEngine.UI;

// Pantalla de victoria al recuperar el Corazon de las Mareas (la llama RecompensaIsla con "Es Final").
// Muestra el resumen de la partida y deja seguir explorando o volver al menu.
public static class PantallaFinal
{
    static GameObject ventana;

    public static void Mostrar()
    {
        if (ventana) { ventana.SetActive(true); Pausar(true); return; }
        var fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        var cuadro = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(.5f, .5f));
        Color dorado = new Color(0.95f, 0.75f, 0.3f);

        ventana = new GameObject("PantallaFinal", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var c = ventana.GetComponent<Canvas>(); c.renderMode = RenderMode.ScreenSpaceOverlay; c.sortingOrder = 300;
        var sc = ventana.GetComponent<CanvasScaler>(); sc.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize; sc.referenceResolution = new Vector2(1920, 1080); sc.matchWidthOrHeight = 0.5f;
        var raiz = ventana.GetComponent<RectTransform>();

        var fondo = Img(raiz, cuadro, new Color(0.02f, 0.03f, 0.06f, 0.92f)); Estirar(fondo.rectTransform);
        fondo.raycastTarget = true;

        // ventana con marco dorado, placa con el titulo y el resumen en una banda hundida
        var panel = Img(raiz, cuadro, Color.white); panel.rectTransform.sizeDelta = new Vector2(1240, 660);
        if (!HUD.Vestir(panel, HUD.PielPanel)) Adornos.Decorar(panel, new Color(0.07f, 0.08f, 0.12f, 0.96f), Adornos.Oro);
        else Adornos.Sombra(panel.gameObject);
        var p = panel.rectTransform;
        var titulo = Texto(p, fuente, "¡EL CORAZÓN DE LAS MAREAS ES TUYO!", 46, dorado, Vector2.zero, new Vector2(1000, 70));
        Adornos.Placa(p, titulo, 1020f, 88f);
        Texto(p, fuente, "Kai, el grumete de Caleta Gris, venció a la Almirante Renata Voss.\nAhora es una leyenda del archipiélago.", 30, new Color(0.93f, 0.88f, 0.76f), new Vector2(0, 205), new Vector2(1100, 100));
        Adornos.Separador(p, new Vector2(0.5f, 0.5f), new Vector2(0, 140), 700f, dorado);
        var banda = Img(p, cuadro, Color.white); Adornos.Hundir(banda);
        banda.rectTransform.anchoredPosition = new Vector2(0, 10); banda.rectTransform.sizeDelta = new Vector2(820, 210);

        string trip = "";
        for (int i = 0; i < 4; i++)
            if (Tripulacion.Tiene((Tripulacion.Miembro)i)) trip += (trip == "" ? "" : ", ") + Tripulacion.Nombres[i];
        Texto(p, fuente,
              "Nivel " + Progreso.Nivel + "\n" +
              "Tripulación: " + (trip == "" ? "solo Kai" : trip) + "\n" +
              "Doblones: " + Equipo.Doblones + "\n" +
              "Islas visitadas: " + Visitadas() + " de " + Archipielago.Islas.Length,
              30, Color.white, new Vector2(0, 10), new Vector2(800, 200));

        Boton(p, fuente, cuadro, dorado, "SEGUIR EXPLORANDO", new Vector2(-230, -215), () => { ventana.SetActive(false); Pausar(false); });
        Boton(p, fuente, cuadro, dorado, "MENÚ PRINCIPAL", new Vector2(230, -215), () => { Pausar(false); GameState.CargarEscena("Menu"); });
        Pausar(true);
    }

    static int Visitadas() { int n = 0; foreach (var i in Archipielago.Islas) if (GameState.IslaPisada(i.id)) n++; return n; }

    static void Pausar(bool si)
    {
        HUD.Pausado = si; Time.timeScale = si ? 0f : 1f;
        Cursor.lockState = si ? CursorLockMode.None : CursorLockMode.Locked; Cursor.visible = si;
    }

    static Image Img(RectTransform p, Sprite s, Color c)
    {
        var i = new GameObject("Img", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        i.transform.SetParent(p, false); i.sprite = s; i.color = c; i.raycastTarget = false; return i;
    }

    static Text Texto(RectTransform p, Font f, string s, int tam, Color c, Vector2 pos, Vector2 caja)
    {
        var t = new GameObject("Texto", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
        t.transform.SetParent(p, false);
        t.font = HUD.FuenteTitulos && tam >= 40 ? HUD.FuenteTitulos : f; t.text = s; t.fontSize = tam; t.color = c; t.alignment = TextAnchor.MiddleCenter; t.fontStyle = FontStyle.Bold;
        t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow; t.raycastTarget = false;
        t.rectTransform.anchoredPosition = pos; t.rectTransform.sizeDelta = caja;
        t.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(3, -3);
        return t;
    }

    static void Boton(RectTransform p, Font f, Sprite s, Color c, string txt, Vector2 pos, UnityEngine.Events.UnityAction accion)
    {
        var b = Img(p, s, c); b.raycastTarget = true;
        bool piel = HUD.Vestir(b, HUD.PielBoton);
        b.rectTransform.anchoredPosition = pos; b.rectTransform.sizeDelta = new Vector2(400, 80);
        if (!piel) Adornos.VestirBoton(b, null);
        b.gameObject.AddComponent<Button>().onClick.AddListener(accion);
        var t = new GameObject("Texto", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
        t.transform.SetParent(b.rectTransform, false);
        t.font = f; t.text = txt; t.fontSize = 30; t.fontStyle = FontStyle.Bold; t.color = piel ? Color.white : Adornos.Crema; t.alignment = TextAnchor.MiddleCenter;
        {
            var o = t.gameObject.AddComponent<Outline>(); o.effectColor = new Color(0.22f, 0.09f, 0.02f); o.effectDistance = new Vector2(2.2f, -2.2f);
            var o2 = t.gameObject.AddComponent<Outline>(); o2.effectColor = new Color(0.22f, 0.09f, 0.02f); o2.effectDistance = new Vector2(-1.5f, 1.5f);
        }
        Estirar(t.rectTransform);
    }

    static void Estirar(RectTransform r) { r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one; r.offsetMin = r.offsetMax = Vector2.zero; }
}
