using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Adornos para que ningun panel se vea plano. Todo se dibuja por codigo (no necesita imagenes):
//   Marco      -> panel con esquinas redondeadas, borde dorado doble, bisel y degradado
//   Bisel      -> ficha redondeada con luz arriba y sombra abajo (se tine: dorado, gris...)
//   Hundido    -> hueco oscuro para barras, deslizadores y casillas
//   Tecla      -> tecla de teclado en relieve
//   Perilla    -> boton redondo con brillo (monedas, asas)
//   Rombo      -> remache dorado para las esquinas y los separadores
// Lo usan el HUD, el Taller, el mapa y la pantalla final.
public static class Adornos
{
    public static readonly Color Oro = new Color(0.95f, 0.75f, 0.28f);
    public static readonly Color Madera = new Color(0.40f, 0.25f, 0.12f);
    public static readonly Color Vino = new Color(0.42f, 0.10f, 0.08f);
    public static readonly Color Crema = new Color(0.97f, 0.91f, 0.78f);

    static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();
    static Sprite bisel, hundido, perilla, rombo, punto;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reiniciar() { cache.Clear(); bisel = hundido = perilla = rombo = punto = null; }

    // ================================================================ aplicar a paneles
    // Viste un panel: marco con relieve, sombra debajo y remaches en las esquinas.
    public static void Decorar(Image img, Color cuerpo, Color borde, bool esquinas = true)
    {
        if (!img) return;
        var rt = img.rectTransform;
        bool chico = Mathf.Min(rt.rect.height > 1f ? rt.rect.height : rt.sizeDelta.y, rt.rect.width > 1f ? rt.rect.width : rt.sizeDelta.x) < 80f;
        img.sprite = Marco(cuerpo, borde, chico ? 1 : 2);
        img.type = Image.Type.Sliced; img.color = Color.white;
        Sombra(img.gameObject, chico ? 4f : 7f);
        if (esquinas) Esquinas(rt, borde, chico ? 10f : 16f, chico ? 3f : 5f);
    }

    // Panel que ya tiene sprite de un paquete (madera, pergamino): solo la sombra para que "flote"
    public static void Sombra(GameObject go, float d = 7f)
    {
        if (!go || go.GetComponent<Shadow>()) return;
        var s = go.AddComponent<Shadow>();
        s.effectColor = new Color(0f, 0f, 0f, 0.5f); s.effectDistance = new Vector2(d, -d);
    }

    public static void Esquinas(RectTransform panel, Color c, float tam, float dentro)
    {
        Vector2[] a = { new Vector2(0, 0), new Vector2(1, 0), new Vector2(0, 1), new Vector2(1, 1) };
        foreach (var ancla in a)
        {
            var r = Imagen(panel, "Remache", Rombo(), Color.Lerp(c, Color.white, 0.15f));
            r.rectTransform.anchorMin = r.rectTransform.anchorMax = ancla;
            r.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            r.rectTransform.sizeDelta = new Vector2(tam, tam);
            r.rectTransform.anchoredPosition = new Vector2(ancla.x < 0.5f ? dentro + tam * 0.3f : -dentro - tam * 0.3f, ancla.y < 0.5f ? dentro + tam * 0.3f : -dentro - tam * 0.3f);
        }
    }

    // Linea  ·──◆──·  centrada en 'pos' (respecto al ancla dada)
    public static RectTransform Separador(RectTransform padre, Vector2 ancla, Vector2 pos, float ancho, Color c)
    {
        var g = new GameObject("Separador", typeof(RectTransform)).GetComponent<RectTransform>();
        g.SetParent(padre, false);
        g.anchorMin = g.anchorMax = ancla; g.pivot = new Vector2(0.5f, 0.5f);
        g.anchoredPosition = pos; g.sizeDelta = new Vector2(ancho, 16);
        float mitad = ancho * 0.5f - 14f;
        for (int lado = -1; lado <= 1; lado += 2)
        {
            var l = Imagen(g, "Linea", Blanco(), new Color(c.r, c.g, c.b, 0.75f));
            l.rectTransform.anchorMin = l.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            l.rectTransform.pivot = new Vector2(lado < 0 ? 1f : 0f, 0.5f);
            l.rectTransform.anchoredPosition = new Vector2(lado * 12f, 0); l.rectTransform.sizeDelta = new Vector2(mitad - 6f, 2f);
            var sombra = Imagen(l.rectTransform, "Sombra", Blanco(), new Color(0, 0, 0, 0.45f));
            sombra.rectTransform.anchorMin = new Vector2(0, 0); sombra.rectTransform.anchorMax = new Vector2(1, 0);
            sombra.rectTransform.pivot = new Vector2(0.5f, 1f); sombra.rectTransform.anchoredPosition = Vector2.zero; sombra.rectTransform.sizeDelta = new Vector2(0, 2f);
            var p = Imagen(g, "Punto", Punto(), c);
            p.rectTransform.anchorMin = p.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            p.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            p.rectTransform.anchoredPosition = new Vector2(lado * (mitad + 10f), 0); p.rectTransform.sizeDelta = new Vector2(7, 7);
        }
        var r = Imagen(g, "Rombo", Rombo(), c);
        r.rectTransform.anchorMin = r.rectTransform.anchorMax = r.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        r.rectTransform.anchoredPosition = Vector2.zero; r.rectTransform.sizeDelta = new Vector2(18, 18);
        return g;
    }

    // Placa (cinta) en el borde de arriba del panel donde va el titulo. Mete el texto dentro.
    public static RectTransform Placa(RectTransform panel, Text titulo, float ancho, float alto = 58f)
    {
        var p = Imagen(panel, "Placa", Marco(Vino, Oro, 1), Color.white);
        p.type = Image.Type.Sliced;
        var r = p.rectTransform;
        r.anchorMin = r.anchorMax = new Vector2(0.5f, 1f); r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = new Vector2(0, -6f); r.sizeDelta = new Vector2(ancho, alto);
        Sombra(p.gameObject, 4f);
        // "orejas" con rombos a los lados
        for (int lado = -1; lado <= 1; lado += 2)
        {
            var o = Imagen(r, "Oreja", Rombo(), Oro);
            o.rectTransform.anchorMin = o.rectTransform.anchorMax = new Vector2(lado < 0 ? 0f : 1f, 0.5f);
            o.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            o.rectTransform.anchoredPosition = Vector2.zero; o.rectTransform.sizeDelta = new Vector2(22, 22);
        }
        if (titulo)
        {
            titulo.transform.SetParent(r, false);
            var tr = titulo.rectTransform;
            tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one; tr.pivot = new Vector2(0.5f, 0.5f);
            tr.offsetMin = new Vector2(20, 4); tr.offsetMax = new Vector2(-20, -4);
            titulo.alignment = TextAnchor.MiddleCenter;
            titulo.color = Oro;
            titulo.horizontalOverflow = HorizontalWrapMode.Overflow;
            var sh = titulo.GetComponent<Shadow>();
            if (!sh) sh = titulo.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0.1f, 0.02f, 0.01f, 0.95f); sh.effectDistance = new Vector2(2, -2);
        }
        return r;
    }

    // Boton de madera con borde dorado (cuando no hay sprite de boton del paquete)
    public static void VestirBoton(Image img, Text texto)
    {
        if (!img) return;
        img.sprite = Marco(Madera, Oro, 1); img.type = Image.Type.Sliced; img.color = Color.white;
        Sombra(img.gameObject, 4f);
        if (texto)
        {
            texto.color = Crema;
            var o = texto.GetComponent<Outline>(); if (!o) o = texto.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(0.15f, 0.07f, 0.02f, 1f); o.effectDistance = new Vector2(1.6f, -1.6f);
        }
    }

    public static void Hundir(Image img) { if (!img) return; img.sprite = Hundido(); img.type = Image.Type.Sliced; img.color = Color.white; }
    public static void Biselar(Image img) { if (!img) return; img.sprite = Bisel(); img.type = Image.Type.Sliced; }

    // banda suave detras de una fila de opciones
    public static Image Fila(RectTransform padre, Vector2 posArribaIzq, Vector2 tam)
    {
        var f = Imagen(padre, "Fila", Hundido(), new Color(1f, 1f, 1f, 0.45f));
        f.type = Image.Type.Sliced;
        f.rectTransform.anchorMin = f.rectTransform.anchorMax = f.rectTransform.pivot = new Vector2(0, 1);
        f.rectTransform.anchoredPosition = posArribaIzq; f.rectTransform.sizeDelta = tam;
        return f;
    }

    static Image Imagen(RectTransform padre, string n, Sprite s, Color c)
    {
        var img = new GameObject(n, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        img.transform.SetParent(padre, false); img.sprite = s; img.color = c; img.raycastTarget = false;
        return img;
    }

    // ================================================================ sprites generados
    static Sprite blanco;
    static Sprite Blanco() { if (!blanco) blanco = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(.5f, .5f)); return blanco; }

    // distancia hacia adentro de un rectangulo redondeado (positivo = adentro)
    static float Dentro(float x, float y, float w, float h, float radio)
    {
        float qx = Mathf.Abs(x - w * 0.5f) - (w * 0.5f - radio);
        float qy = Mathf.Abs(y - h * 0.5f) - (h * 0.5f - radio);
        float fuera = new Vector2(Mathf.Max(qx, 0f), Mathf.Max(qy, 0f)).magnitude + Mathf.Min(Mathf.Max(qx, qy), 0f) - radio;
        return -fuera;
    }

    static Sprite Hacer(Color[] px, int n, float bordeSlice)
    {
        var t = new Texture2D(n, n, TextureFormat.RGBA32, false);
        t.wrapMode = TextureWrapMode.Clamp; t.filterMode = FilterMode.Bilinear;
        t.SetPixels(px); t.Apply();
        return Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(.5f, .5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(bordeSlice, bordeSlice, bordeSlice, bordeSlice));
    }

    static Color Mezclar(Color abajo, Color arriba) { float a = arriba.a; return new Color(Mathf.Lerp(abajo.r, arriba.r, a), Mathf.Lerp(abajo.g, arriba.g, a), Mathf.Lerp(abajo.b, arriba.b, a), Mathf.Max(abajo.a, a)); }
    static Color Por(Color c, float k) => new Color(Mathf.Clamp01(c.r * k), Mathf.Clamp01(c.g * k), Mathf.Clamp01(c.b * k), c.a);

    // escala 1 = paneles chicos (32 px), 2 = paneles grandes (64 px)
    // redondeo = radio de las esquinas (7 normal, 1 = cuadrado)
    public static Sprite Marco(Color cuerpo, Color borde, int escala, float redondeo = 7f)
    {
        string clave = "M" + escala + cuerpo + borde + redondeo;
        if (cache.TryGetValue(clave, out var s) && s) return s;
        int n = 32 * escala; float e = escala;
        float radio = Mathf.Max(0.6f, redondeo * e);
        Color negro = new Color(0.07f, 0.04f, 0.02f, 1f);
        float alfa = Mathf.Max(cuerpo.a, 0.9f);
        var px = new Color[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Dentro(x + 0.5f, y + 0.5f, n, n, radio);
                float v = (y + 0.5f) / n;                       // 0 abajo, 1 arriba
                Color c;
                if (d < 1f * e) c = negro;                                                   // contorno
                else if (d < 3.2f * e) c = Por(borde, Mathf.Lerp(0.62f, 1.25f, v) * (d < 1.6f * e ? 1.15f : 1f)); // borde dorado con luz arriba
                else if (d < 3.9f * e) c = Por(negro, 1f);                                   // ranura
                else if (d < 4.5f * e) c = Por(borde, 0.7f);                                 // hilo dorado interior
                else
                {
                    c = Por(new Color(cuerpo.r, cuerpo.g, cuerpo.b, alfa), Mathf.Lerp(0.6f, 1.4f, v));
                    float k = d - 4.5f * e;                                                  // sombra interior junto al borde
                    if (k < 3f * e) c = Mezclar(c, new Color(0, 0, 0, 0.45f * (1f - k / (3f * e))));
                    if (v > 0.7f && k < 1.2f * e) c = Mezclar(c, new Color(1, 1, 1, 0.08f)); // brillo del bisel arriba
                }
                c.a = d < 1f * e ? Mathf.Clamp01(d + 0.5f) : c.a;                            // borde suave
                if (d <= -0.5f) c = new Color(0, 0, 0, 0);
                px[y * n + x] = c;
            }
        s = Hacer(px, n, 11f * e);
        cache[clave] = s;
        return s;
    }

    // ficha en tonos de gris con bisel: se tine con el color de la Image
    public static Sprite Bisel()
    {
        if (bisel) return bisel;
        int n = 32; var px = new Color[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Dentro(x + 0.5f, y + 0.5f, n, n, 6f), v = (y + 0.5f) / n;
                float g = Mathf.Lerp(0.72f, 1f, v);
                if (d < 1.2f) g = 0.25f;
                else if (d < 3f) g = v > 0.5f ? 1f : 0.55f;
                px[y * n + x] = d <= -0.5f ? new Color(0, 0, 0, 0) : new Color(g, g, g, Mathf.Clamp01(d + 0.5f));
            }
        return bisel = Hacer(px, n, 9f);
    }

    public static Sprite Hundido() => Hundido(5f);
    public static Sprite Hundido(float redondeo)
    {
        if (redondeo == 5f && hundido) return hundido;
        string clave = "H" + redondeo;
        if (redondeo != 5f && cache.TryGetValue(clave, out var hs) && hs) return hs;
        int n = 24; var px = new Color[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Dentro(x + 0.5f, y + 0.5f, n, n, Mathf.Max(0.6f, redondeo)), v = (y + 0.5f) / n;
                Color c = new Color(0.03f, 0.02f, 0.02f, 0.7f);
                if (v > 0.5f && d < 3f) c = new Color(0, 0, 0, 0.9f);              // sombra arriba (esta hundido)
                if (v < 0.5f && d < 1.4f) c = new Color(1f, 0.9f, 0.7f, 0.35f);   // luz abajo
                c.a *= Mathf.Clamp01(d + 0.5f);
                px[y * n + x] = d <= -0.5f ? new Color(0, 0, 0, 0) : c;
            }
        var sp = Hacer(px, n, 7f);
        if (redondeo == 5f) hundido = sp; else cache[clave] = sp;
        return sp;
    }

    public static Sprite Tecla(Color cuerpo)
    {
        string clave = "T" + cuerpo;
        if (cache.TryGetValue(clave, out var s) && s) return s;
        int n = 32; var px = new Color[n * n];
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Dentro(x + 0.5f, y + 0.5f, n, n, 6f), v = (y + 0.5f) / n;
                Color c;
                if (d < 1.2f) c = new Color(0.06f, 0.03f, 0.01f, 1f);
                else if (d < 2.4f) c = Por(Oro, v > 0.4f ? 1.1f : 0.6f);
                else if (y < 7) c = Por(cuerpo, 0.5f);                                // costado de la tecla (relieve)
                else c = Por(cuerpo, Mathf.Lerp(0.9f, 1.3f, v));
                c.a = Mathf.Clamp01(d + 0.5f);
                px[y * n + x] = d <= -0.5f ? new Color(0, 0, 0, 0) : c;
            }
        s = Hacer(px, n, 10f);
        cache[clave] = s;
        return s;
    }

    // circulo con brillo arriba a la izquierda y borde oscuro (tintable)
    public static Sprite Perilla()
    {
        if (perilla) return perilla;
        int n = 64; var px = new Color[n * n]; float r = n * 0.5f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float dx = x + 0.5f - r, dy = y + 0.5f - r, dist = Mathf.Sqrt(dx * dx + dy * dy);
                float d = r - dist;
                float g = Mathf.Lerp(0.6f, 1f, (dy / r + 1f) * 0.5f);
                if (d < 2.5f) g = 0.3f;
                else if (d < 6f) g = dy > 0 ? 1f : 0.55f;
                float brillo = Mathf.Clamp01(1f - new Vector2(dx + r * 0.3f, dy - r * 0.35f).magnitude / (r * 0.45f));
                g = Mathf.Min(1f, g + brillo * 0.35f);
                px[y * n + x] = new Color(g, g, g, Mathf.Clamp01(d + 0.5f));
            }
        var t = new Texture2D(n, n, TextureFormat.RGBA32, false); t.wrapMode = TextureWrapMode.Clamp; t.SetPixels(px); t.Apply();
        return perilla = Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(.5f, .5f));
    }

    // rombo dorado con centro mas oscuro (tintable)
    public static Sprite Rombo()
    {
        if (rombo) return rombo;
        int n = 32; var px = new Color[n * n]; float r = n * 0.5f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float m = Mathf.Abs(x + 0.5f - r) + Mathf.Abs(y + 0.5f - r);
                float d = (r - 1f) - m;
                float g = d < 2f ? 0.25f : d < 5f ? (y > r ? 1f : 0.7f) : (y > r ? 0.8f : 0.6f);
                px[y * n + x] = new Color(g, g, g, Mathf.Clamp01(d + 0.5f));
            }
        var t = new Texture2D(n, n, TextureFormat.RGBA32, false); t.wrapMode = TextureWrapMode.Clamp; t.SetPixels(px); t.Apply();
        return rombo = Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(.5f, .5f));
    }

    // degradado vertical: luz blanca arriba, transparente al medio y sombra abajo (para barras)
    // vineta: transparente al centro y opaca en los bordes (pantalla roja al recibir dano)
    static Sprite vineta;
    public static Sprite Vineta()
    {
        if (vineta) return vineta;
        int n = 64; var px = new Color[n * n]; float r = n * 0.5f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = new Vector2((x + 0.5f - r) / r, (y + 0.5f - r) / r).magnitude;   // 0 centro, 1 borde
                float a = Mathf.Clamp01((d - 0.55f) / 0.6f); a = a * a * (3f - 2f * a);
                px[y * n + x] = new Color(1, 1, 1, a);
            }
        var t = new Texture2D(n, n, TextureFormat.RGBA32, false); t.wrapMode = TextureWrapMode.Clamp; t.SetPixels(px); t.Apply();
        return vineta = Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(.5f, .5f));
    }

    static Sprite brillo;
    public static Sprite Brillo()
    {
        if (brillo) return brillo;
        int w = 4, h = 32; var px = new Color[w * h];
        for (int y = 0; y < h; y++)
        {
            float v = (y + 0.5f) / h;   // 0 abajo, 1 arriba
            Color c = new Color(0, 0, 0, 0);
            if (v > 0.5f) c = new Color(1, 1, 1, Mathf.Lerp(0f, 0.42f, (v - 0.5f) / 0.5f));
            if (v > 0.86f) c = new Color(1, 1, 1, 0.55f);                      // linea de luz
            if (v < 0.3f) c = new Color(0, 0, 0, Mathf.Lerp(0.38f, 0f, v / 0.3f));
            for (int x = 0; x < w; x++) px[y * w + x] = c;
        }
        var t = new Texture2D(w, h, TextureFormat.RGBA32, false); t.wrapMode = TextureWrapMode.Clamp; t.SetPixels(px); t.Apply();
        return brillo = Sprite.Create(t, new Rect(0, 0, w, h), new Vector2(.5f, .5f));
    }

    static Sprite Punto()
    {
        if (punto) return punto;
        int n = 16; var px = new Color[n * n]; float r = n * 0.5f;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = r - new Vector2(x + 0.5f - r, y + 0.5f - r).magnitude;
                px[y * n + x] = new Color(1, 1, 1, Mathf.Clamp01(d));
            }
        var t = new Texture2D(n, n, TextureFormat.RGBA32, false); t.SetPixels(px); t.Apply();
        return punto = Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(.5f, .5f));
    }
}
