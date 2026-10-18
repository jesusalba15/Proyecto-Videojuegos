using UnityEngine;
using UnityEngine.UI;

// Minimapa circular en tiempo real (arriba a la derecha).
// Una camara mira desde arriba a Kai (o al barco) y gira con la camara del jugador,
// asi "arriba" en el minimapa siempre es hacia donde estas mirando. Lo crea el HUD solo.
// Teclas: + / - para acercar o alejar.
[DefaultExecutionOrder(1000)]   // se actualiza despues de la camara del jugador, para no ir un cuadro atrasado
public class Minimapa : MonoBehaviour
{
    public float alturaCamara = 60f;
    public float[] zooms = { 18f, 28f, 42f };   // radio en metros que muestra el minimapa
    public int resolucion = 384;
    [Tooltip("Marcado: el minimapa gira con la camara. Desmarcado: norte siempre arriba")]
    public bool girarConCamara = true;

    Transform objetivo;
    Camera cam;
    RenderTexture rt;
    RectTransform marco, capaIconos, norte;
    float radioPx;
    int zoom = 1;
    Sprite circulo, triangulo;
    readonly System.Collections.Generic.Dictionary<MarcadorMinimapa, Image> iconos =
        new System.Collections.Generic.Dictionary<MarcadorMinimapa, Image>();

    public static Minimapa Crear(RectTransform padre, Transform objetivo, Font fuente, Color dorado, Color tinta)
    {
        var go = new GameObject("Minimapa", typeof(RectTransform));
        go.transform.SetParent(padre, false);
        var m = go.AddComponent<Minimapa>();
        m.Construir(objetivo, fuente, dorado, tinta);
        return m;
    }

    void Construir(Transform obj, Font fuente, Color dorado, Color tinta)
    {
        objetivo = obj;
        circulo = Circulo(128);
        triangulo = Triangulo(64);

        marco = GetComponent<RectTransform>();
        marco.anchorMin = marco.anchorMax = marco.pivot = new Vector2(1, 1);
        marco.anchoredPosition = new Vector2(-30, -30);
        marco.sizeDelta = new Vector2(270, 270);
        radioPx = 118f;

        // camara cenital que dibuja en una textura
        rt = new RenderTexture(resolucion, resolucion, 16) { name = "MinimapaRT" };
        var camGO = new GameObject("CamaraMinimapa");
        cam = camGO.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = zooms[zoom];
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.16f, 0.42f, 0.55f);
        cam.targetTexture = rt;
        cam.nearClipPlane = 1f; cam.farClipPlane = alturaCamara + 40f;
        cam.depth = -50;

        // aro de pergamino y borde dorado
        Imagen(marco, "Borde", circulo, dorado, Vector2.zero, new Vector2(270, 270));
        Imagen(marco, "Pergamino", circulo, new Color(0.62f, 0.5f, 0.32f), Vector2.zero, new Vector2(256, 256));

        // mascara circular con la vista en vivo (tinte calido, como tinta sobre pergamino)
        var mascara = Imagen(marco, "Mascara", circulo, Color.white, Vector2.zero, new Vector2(2 * radioPx, 2 * radioPx));
        mascara.gameObject.AddComponent<Mask>().showMaskGraphic = false;
        var vista = new GameObject("Vista", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
        vista.transform.SetParent(mascara.transform, false);
        var rv = vista.rectTransform; rv.anchorMin = Vector2.zero; rv.anchorMax = Vector2.one; rv.offsetMin = rv.offsetMax = Vector2.zero;
        vista.texture = rt;
        vista.color = new Color(1f, 0.93f, 0.8f);
        vista.raycastTarget = false;

        capaIconos = new GameObject("Iconos", typeof(RectTransform)).GetComponent<RectTransform>();
        capaIconos.SetParent(mascara.transform, false);
        capaIconos.anchorMin = capaIconos.anchorMax = capaIconos.pivot = new Vector2(0.5f, 0.5f);
        capaIconos.sizeDelta = new Vector2(2 * radioPx, 2 * radioPx);

        // los iconos que salen del borde van en otra capa (sin mascara)
        // Kai: flecha fija en el centro apuntando arriba
        var flechaBorde = Imagen(marco, "KaiBorde", triangulo, tinta, Vector2.zero, new Vector2(30, 30));
        Imagen(marco, "Kai", triangulo, Color.white, Vector2.zero, new Vector2(22, 22));

        // "N" del norte que gira por el borde
        norte = new GameObject("Norte", typeof(RectTransform)).GetComponent<RectTransform>();
        norte.SetParent(marco, false);
        norte.anchorMin = norte.anchorMax = new Vector2(0.5f, 0.5f); norte.sizeDelta = new Vector2(34, 34);
        Imagen(norte, "Fondo", circulo, tinta, Vector2.zero, new Vector2(34, 34));
        var n = new GameObject("Texto", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
        n.transform.SetParent(norte, false);
        n.rectTransform.anchorMin = Vector2.zero; n.rectTransform.anchorMax = Vector2.one; n.rectTransform.offsetMin = n.rectTransform.offsetMax = Vector2.zero;
        n.font = fuente; n.text = "N"; n.fontSize = 20; n.fontStyle = FontStyle.Bold; n.color = dorado;
        n.alignment = TextAnchor.MiddleCenter; n.raycastTarget = false;

        var ayuda = new GameObject("Ayuda", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
        ayuda.transform.SetParent(marco, false);
        ayuda.rectTransform.anchorMin = ayuda.rectTransform.anchorMax = ayuda.rectTransform.pivot = new Vector2(0.5f, 0f);
        ayuda.rectTransform.anchoredPosition = new Vector2(0, -26); ayuda.rectTransform.sizeDelta = new Vector2(300, 24);
        ayuda.font = fuente; ayuda.fontSize = 16; ayuda.fontStyle = FontStyle.Italic; ayuda.color = new Color(1, 1, 1, 0.8f);
        ayuda.alignment = TextAnchor.MiddleCenter; ayuda.text = "M: mapa vivo   + / -: zoom   N: girar"; ayuda.raycastTarget = false;
        ayuda.gameObject.AddComponent<Shadow>().effectColor = new Color(0, 0, 0, 0.7f);
        ayuda.gameObject.SetActive(false);   // las teclas ya estan en el panel de Controles
    }

    void LateUpdate()
    {
        if (!objetivo || !cam) return;

        if (Input.GetKeyDown(KeyCode.KeypadPlus) || Input.GetKeyDown(KeyCode.Equals)) zoom = Mathf.Max(0, zoom - 1);
        if (Input.GetKeyDown(KeyCode.KeypadMinus) || Input.GetKeyDown(KeyCode.Minus)) zoom = Mathf.Min(zooms.Length - 1, zoom + 1);
        cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, zooms[zoom], 8f * Time.unscaledDeltaTime);

        // "arriba" del minimapa = hacia donde mira la camara del jugador (o Kai si no hay camara)
        float yaw = !girarConCamara ? 0f : Camera.main ? Camera.main.transform.eulerAngles.y : objetivo.eulerAngles.y;
        if (Input.GetKeyDown(KeyCode.N)) girarConCamara = !girarConCamara;
        cam.transform.position = objetivo.position + Vector3.up * alturaCamara;
        cam.transform.rotation = Quaternion.Euler(90f, yaw, 0f);

        norte.anchoredPosition = Rotar(new Vector2(0, radioPx + 6f), yaw);
        RefrescarIconos(yaw);
    }

    void RefrescarIconos(float yaw)
    {
        float escala = radioPx / cam.orthographicSize;
        // quitar iconos de marcadores que ya no existen
        var borrar = new System.Collections.Generic.List<MarcadorMinimapa>();
        foreach (var par in iconos) if (!par.Key || !MarcadorMinimapa.Todos.Contains(par.Key)) borrar.Add(par.Key);
        foreach (var k in borrar) { if (iconos[k]) Destroy(iconos[k].gameObject); iconos.Remove(k); }

        foreach (var m in MarcadorMinimapa.Todos)
        {
            if (!iconos.TryGetValue(m, out Image img))
            {
                img = Imagen(capaIconos, "Icono_" + m.name, m.tipo == MarcadorMinimapa.Tipo.Objetivo ? Estrella() : circulo,
                             m.ColorIcono(), Vector2.zero, Vector2.one * m.Tamano());
                img.gameObject.AddComponent<Outline>().effectColor = new Color(0, 0, 0, 0.8f);
                iconos[m] = img;
            }
            Vector3 d = m.transform.position - objetivo.position;
            Vector2 p = Rotar(new Vector2(d.x, d.z) * escala, yaw);
            bool dentro = p.magnitude <= radioPx - 6f;
            bool ver = m.Activo && (dentro || m.siempreVisible);
            img.gameObject.SetActive(ver);
            if (!ver) continue;
            if (!dentro) p = p.normalized * (radioPx - 10f);   // pegado al borde, senalando la direccion
            img.rectTransform.anchoredPosition = p;
            float pulso = m.tipo == MarcadorMinimapa.Tipo.Objetivo || m.tipo == MarcadorMinimapa.Tipo.Jefe
                ? 1f + Mathf.Sin(Time.unscaledTime * 5f) * 0.15f : 1f;
            img.rectTransform.localScale = Vector3.one * pulso * (dentro ? 1f : 0.8f);
        }
    }

    static Vector2 Rotar(Vector2 v, float grados)
    {
        float r = grados * Mathf.Deg2Rad, c = Mathf.Cos(r), s = Mathf.Sin(r);
        return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
    }

    Image Imagen(Transform padre, string n, Sprite sp, Color col, Vector2 pos, Vector2 tam)
    {
        var img = new GameObject(n, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        img.transform.SetParent(padre, false);
        var r = img.rectTransform; r.anchorMin = r.anchorMax = r.pivot = new Vector2(0.5f, 0.5f);
        r.anchoredPosition = pos; r.sizeDelta = tam;
        img.sprite = sp; img.color = col; img.raycastTarget = false;
        return img;
    }

    void OnDestroy()
    {
        if (cam) Destroy(cam.gameObject);
        if (rt) rt.Release();
    }

    // ---------------- sprites generados por codigo
    static Sprite Circulo(int n)
    {
        var t = new Texture2D(n, n, TextureFormat.RGBA32, false); float r = n / 2f;
        for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
            t.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(r - Vector2.Distance(new Vector2(x + .5f, y + .5f), new Vector2(r, r)))));
        t.Apply(); return Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(.5f, .5f));
    }

    static Sprite Triangulo(int n)
    {
        var t = new Texture2D(n, n, TextureFormat.RGBA32, false);
        for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
        {
            // flecha apuntando arriba con muesca abajo
            float u = (x + .5f) / n - .5f, v = (y + .5f) / n;
            bool dentro = Mathf.Abs(u) < (1f - v) * 0.5f && v > 0.08f + Mathf.Abs(u) * 0.6f;
            t.SetPixel(x, y, new Color(1, 1, 1, dentro ? 1 : 0));
        }
        t.Apply(); return Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(.5f, .5f));
    }

    static Sprite estrella;
    static Sprite Estrella()
    {
        if (estrella) return estrella;
        int n = 64; var t = new Texture2D(n, n, TextureFormat.RGBA32, false);
        for (int y = 0; y < n; y++) for (int x = 0; x < n; x++)
        {
            Vector2 p = new Vector2(x + .5f - n / 2f, y + .5f - n / 2f);
            float ang = Mathf.Atan2(p.y, p.x), r = p.magnitude / (n / 2f);
            float borde = 0.55f + 0.45f * Mathf.Pow(Mathf.Abs(Mathf.Cos(ang * 2.5f + Mathf.PI / 2f)), 3f);
            t.SetPixel(x, y, new Color(1, 1, 1, r < borde ? 1 : 0));
        }
        t.Apply(); estrella = Sprite.Create(t, new Rect(0, 0, n, n), new Vector2(.5f, .5f));
        return estrella;
    }
}
