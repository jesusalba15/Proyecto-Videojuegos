using System.Collections;
using UnityEngine;

// Efectos visuales sencillos hechos por codigo (no necesitan assets):
//   Efectos.Anillo(pos, radio, color)            anillo que se expande en el suelo (habilidades de area)
//   Efectos.Cono(pos, adelante, radio, color)     abanico frente a Kai (golpes de frente)
//   Efectos.Linea(desde, hasta, color)            cuerda del garfio / rastro de una estocada
//   Efectos.Sacudir(fuerza, duracion)             sacude la camara
// Fase 7 (combate con mas peso):
//   Efectos.Chispas(pos, dir, color, cantidad)    chispas que saltan al golpear (con gravedad)
//   Efectos.Tajo(pos, adelante, radio, color)     arco del corte del arma frente a Kai
//   Efectos.Polvo(pos, color, cantidad)           nubecitas de polvo (muertes, aterrizajes)
//   Efectos.Destello(pos, color)                  fogonazo de luz corto (golpes criticos)
//   Efectos.Impacto(segundos)                     congela el juego un instante al pegar (hit-stop)
//   Efectos.Columna(pos, color)                   chispas que suben (subir de nivel, botin)
public class Efectos : MonoBehaviour
{
    static Material material;

    static Material Mat()
    {
        if (material) return material;
        var sh = Shader.Find("Sprites/Default");
        if (!sh) sh = Shader.Find("Universal Render Pipeline/Unlit");
        material = new Material(sh);
        return material;
    }

    static LineRenderer NuevaLinea(string nombre, int puntos, float ancho, Color color)
    {
        var go = new GameObject(nombre);
        var lr = go.AddComponent<LineRenderer>();
        lr.sharedMaterial = Mat();
        lr.positionCount = puntos; lr.useWorldSpace = true;
        lr.widthMultiplier = ancho; lr.startColor = lr.endColor = color;
        lr.numCapVertices = 2;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        return lr;
    }

    public static void Anillo(Vector3 pos, float radio, Color color, float duracion = 0.45f)
    {
        var lr = NuevaLinea("EfectoAnillo", 49, 0.16f, color);
        lr.gameObject.AddComponent<Efectos>().StartCoroutine(AnimarAnillo(lr, pos + Vector3.up * 0.08f, radio, color, duracion));
    }

    static IEnumerator AnimarAnillo(LineRenderer lr, Vector3 c, float radio, Color color, float dur)
    {
        for (float t = 0; t < dur; t += Time.deltaTime)
        {
            float k = t / dur, r = Mathf.Lerp(radio * 0.25f, radio, 1f - (1f - k) * (1f - k));
            for (int i = 0; i < 49; i++)
            {
                float a = i / 48f * Mathf.PI * 2f;
                lr.SetPosition(i, c + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r));
            }
            var col = color; col.a = 1f - k; lr.startColor = lr.endColor = col;
            lr.widthMultiplier = Mathf.Lerp(0.3f, 0.06f, k);
            yield return null;
        }
        Destroy(lr.gameObject);
    }

    public static void Cono(Vector3 pos, Vector3 adelante, float radio, Color color, float angulo = 120f, float duracion = 0.35f)
    {
        var lr = NuevaLinea("EfectoCono", 22, 0.14f, color);
        adelante.y = 0f; adelante.Normalize();
        lr.gameObject.AddComponent<Efectos>().StartCoroutine(AnimarCono(lr, pos + Vector3.up * 0.4f, adelante, radio, angulo, color, duracion));
    }

    static IEnumerator AnimarCono(LineRenderer lr, Vector3 c, Vector3 f, float radio, float angulo, Color color, float dur)
    {
        float baseAng = Mathf.Atan2(f.z, f.x);
        for (float t = 0; t < dur; t += Time.deltaTime)
        {
            float k = t / dur, r = Mathf.Lerp(radio * 0.4f, radio, k);
            lr.SetPosition(0, c);
            for (int i = 1; i < 21; i++)
            {
                float a = baseAng + Mathf.Lerp(-angulo / 2f, angulo / 2f, (i - 1) / 19f) * Mathf.Deg2Rad;
                lr.SetPosition(i, c + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r));
            }
            lr.SetPosition(21, c);
            var col = color; col.a = 1f - k; lr.startColor = lr.endColor = col;
            yield return null;
        }
        Destroy(lr.gameObject);
    }

    public static void Linea(Vector3 desde, Vector3 hasta, Color color, float duracion = 0.3f)
    {
        var lr = NuevaLinea("EfectoLinea", 2, 0.08f, color);
        lr.SetPosition(0, desde); lr.SetPosition(1, hasta);
        lr.gameObject.AddComponent<Efectos>().StartCoroutine(Desvanecer(lr, color, duracion));
    }

    static IEnumerator Desvanecer(LineRenderer lr, Color color, float dur)
    {
        for (float t = 0; t < dur; t += Time.deltaTime)
        {
            var col = color; col.a = 1f - t / dur; lr.startColor = lr.endColor = col;
            yield return null;
        }
        Destroy(lr.gameObject);
    }

    // ---------------- avisos en el suelo (ataques de jefes, estilo Albion): una zona roja que se llena
    //   cuando se llena del todo, el golpe cae ahi. Da tiempo de salir corriendo.
    public static void AvisoCirculo(Vector3 pos, float radio, float duracion, Color color)
    {
        var borde = NuevaLinea("AvisoBorde", 49, 0.12f, color);
        var relleno = NuevaLinea("AvisoRelleno", 49, 0.1f, new Color(color.r, color.g, color.b, 0.35f));
        borde.loop = relleno.loop = true;
        borde.gameObject.AddComponent<Efectos>().StartCoroutine(AnimarAvisoCirculo(borde, relleno, pos + Vector3.up * 0.1f, radio, duracion, color));
    }

    static IEnumerator AnimarAvisoCirculo(LineRenderer borde, LineRenderer relleno, Vector3 c, float radio, float dur, Color color)
    {
        for (int i = 0; i < 49; i++)
        {
            float a = i / 48f * Mathf.PI * 2f;
            borde.SetPosition(i, c + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radio);
        }
        for (float t = 0; t < dur; t += Time.deltaTime)
        {
            float k = t / dur, r = radio * k;
            // un anillo muy grueso centrado en r/2 parece un disco que crece
            for (int i = 0; i < 49; i++)
            {
                float a = i / 48f * Mathf.PI * 2f;
                relleno.SetPosition(i, c + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * (r * 0.5f));
            }
            relleno.widthMultiplier = Mathf.Max(0.05f, r);
            float parpadeo = k > 0.75f ? (Mathf.Sin(Time.time * 40f) * 0.5f + 0.5f) : 1f;
            borde.startColor = borde.endColor = new Color(color.r, color.g, color.b, 0.9f * parpadeo);
            yield return null;
        }
        Destroy(relleno.gameObject);
        Destroy(borde.gameObject);
    }

    // franja roja en linea recta (embestida de un jefe)
    public static void AvisoLinea(Vector3 desde, Vector3 hasta, float ancho, float duracion, Color color)
    {
        desde.y += 0.1f; hasta.y += 0.1f;
        var fondo = NuevaLinea("AvisoLinea", 2, ancho, new Color(color.r, color.g, color.b, 0.25f));
        var relleno = NuevaLinea("AvisoLineaRelleno", 2, ancho, new Color(color.r, color.g, color.b, 0.55f));
        fondo.numCapVertices = relleno.numCapVertices = 0;
        fondo.SetPosition(0, desde); fondo.SetPosition(1, hasta);
        fondo.gameObject.AddComponent<Efectos>().StartCoroutine(AnimarAvisoLinea(fondo, relleno, desde, hasta, duracion));
    }

    static IEnumerator AnimarAvisoLinea(LineRenderer fondo, LineRenderer relleno, Vector3 desde, Vector3 hasta, float dur)
    {
        for (float t = 0; t < dur; t += Time.deltaTime)
        {
            relleno.SetPosition(0, desde); relleno.SetPosition(1, Vector3.Lerp(desde, hasta, t / dur));
            yield return null;
        }
        Destroy(relleno.gameObject);
        Destroy(fondo.gameObject);
    }

    // ================================================================ Fase 7
    static Efectos anfitrion;   // objeto oculto que corre las corrutinas que no tienen un objeto propio
    static Efectos Anfitrion
    {
        get
        {
            if (!anfitrion) { var go = new GameObject("Efectos"); anfitrion = go.AddComponent<Efectos>(); }
            return anfitrion;
        }
    }

    // ---------------- chispas: rayitas que salen disparadas y caen
    public static void Chispas(Vector3 pos, Vector3 dir, Color color, int cantidad = 10, float fuerza = 6f, float vida = 0.35f, float abanico = 0.8f)
    {
        cantidad = Mathf.Clamp(cantidad, 1, 40);
        var raiz = new GameObject("Chispas");
        var lineas = new LineRenderer[cantidad];
        var vel = new Vector3[cantidad]; var pts = new Vector3[cantidad];
        if (dir.sqrMagnitude < 0.001f) dir = Vector3.up;
        dir.Normalize();
        for (int i = 0; i < cantidad; i++)
        {
            lineas[i] = NuevaLinea("Chispa", 2, Random.Range(0.025f, 0.05f), color);
            lineas[i].numCapVertices = 0;
            lineas[i].transform.SetParent(raiz.transform, false);
            Vector3 d = (dir + Random.insideUnitSphere * abanico).normalized;
            vel[i] = d * fuerza * Random.Range(0.5f, 1.2f);
            pts[i] = pos;
        }
        raiz.AddComponent<Efectos>().StartCoroutine(AnimarChispas(raiz, lineas, pts, vel, color, vida));
    }

    static IEnumerator AnimarChispas(GameObject raiz, LineRenderer[] l, Vector3[] p, Vector3[] v, Color color, float vida)
    {
        for (float t = 0; t < vida; t += Time.deltaTime)
        {
            float k = t / vida;
            var col = Color.Lerp(Color.white, color, Mathf.Clamp01(k * 3f)); col.a = 1f - k * k;
            for (int i = 0; i < l.Length; i++)
            {
                v[i] += Vector3.down * 14f * Time.deltaTime;
                v[i] *= 1f - 2.5f * Time.deltaTime;   // el aire las frena
                p[i] += v[i] * Time.deltaTime;
                l[i].SetPosition(0, p[i]);
                l[i].SetPosition(1, p[i] - v[i] * 0.035f);   // la cola apunta hacia donde viene
                l[i].startColor = col; l[i].endColor = new Color(col.r, col.g, col.b, 0f);
            }
            yield return null;
        }
        Destroy(raiz);
    }

    // chispas hacia arriba en circulo (subir de nivel, recoger botin)
    public static void Columna(Vector3 pos, Color color, int cantidad = 18, float alto = 1f)
    {
        Chispas(pos + Vector3.up * 0.2f, Vector3.up, color, cantidad, 5f * alto, 0.7f, 0.45f);
        Anillo(pos, 1.2f, color, 0.5f);
    }

    // ---------------- tajo: un arco que se dibuja de un lado al otro (el filo del arma)
    public static void Tajo(Vector3 pos, Vector3 adelante, float radio, Color color, float angulo = 140f, float duracion = 0.22f, bool deDerecha = true)
    {
        adelante.y = 0f;
        if (adelante.sqrMagnitude < 0.001f) adelante = Vector3.forward;
        adelante.Normalize();
        var lr = NuevaLinea("EfectoTajo", 18, 0.35f, color);
        lr.numCapVertices = 0;
        lr.gameObject.AddComponent<Efectos>().StartCoroutine(AnimarTajo(lr, pos, adelante, radio, angulo, color, duracion, deDerecha));
    }

    static IEnumerator AnimarTajo(LineRenderer lr, Vector3 c, Vector3 f, float radio, float angulo, Color color, float dur, bool deDerecha)
    {
        Vector3 derecha = Vector3.Cross(Vector3.up, f);
        float barrido = dur * 0.45f;   // primero se dibuja, luego se apaga
        int n = lr.positionCount;
        for (float t = 0; t < dur; t += Time.deltaTime)
        {
            float avance = Mathf.Clamp01(t / barrido);
            float apaga = Mathf.Clamp01((t - barrido) / (dur - barrido));
            float cola = Mathf.Max(0f, avance - 0.55f) + apaga * 0.45f;   // el inicio del arco se borra detras del filo
            for (int i = 0; i < n; i++)
            {
                float u = Mathf.Lerp(cola, avance, i / (float)(n - 1));
                float a = Mathf.Lerp(-angulo / 2f, angulo / 2f, deDerecha ? 1f - u : u) * Mathf.Deg2Rad;
                Vector3 d = f * Mathf.Cos(a) + derecha * Mathf.Sin(a);
                // el arco baja un poco al final (corte en diagonal)
                lr.SetPosition(i, c + d * radio + Vector3.up * Mathf.Lerp(0.25f, -0.25f, u));
            }
            var cabeza = color; cabeza.a = 0.9f * (1f - apaga);
            var fin = color; fin.a = 0f;
            lr.startColor = fin; lr.endColor = cabeza;
            lr.startWidth = 0.02f; lr.endWidth = Mathf.Lerp(0.38f, 0.1f, apaga);
            yield return null;
        }
        Destroy(lr.gameObject);
    }

    // ---------------- polvo: bolitas transparentes que crecen, suben y se desvanecen
    static Material matPolvo;
    public static void Polvo(Vector3 pos, Color color, int cantidad = 8, float tamano = 1f)
    {
        if (!matPolvo) matPolvo = new Material(Mat());
        var raiz = new GameObject("Polvo");
        var bolas = new Transform[cantidad]; var vel = new Vector3[cantidad]; var mats = new Material[cantidad]; var esc = new float[cantidad];
        for (int i = 0; i < cantidad; i++)
        {
            var b = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            var c = b.GetComponent<Collider>(); if (c) Destroy(c);
            var r = b.GetComponent<Renderer>();
            mats[i] = new Material(matPolvo);
            r.sharedMaterial = mats[i];
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off; r.receiveShadows = false;
            b.transform.SetParent(raiz.transform, false);
            Vector2 circ = Random.insideUnitCircle.normalized;
            b.transform.position = pos + new Vector3(circ.x, 0.1f, circ.y) * 0.3f * tamano;
            vel[i] = new Vector3(circ.x, Random.Range(0.3f, 0.8f), circ.y) * Random.Range(1.2f, 2.4f) * tamano;
            esc[i] = Random.Range(0.25f, 0.45f) * tamano;
            bolas[i] = b.transform;
        }
        raiz.AddComponent<Efectos>().StartCoroutine(AnimarPolvo(raiz, bolas, vel, mats, esc, color, 0.8f));
    }

    static IEnumerator AnimarPolvo(GameObject raiz, Transform[] b, Vector3[] v, Material[] m, float[] e, Color color, float dur)
    {
        for (float t = 0; t < dur; t += Time.deltaTime)
        {
            float k = t / dur;
            for (int i = 0; i < b.Length; i++)
            {
                v[i] *= 1f - 3f * Time.deltaTime;
                b[i].position += v[i] * Time.deltaTime;
                b[i].localScale = Vector3.one * e[i] * Mathf.Lerp(0.6f, 2.2f, Mathf.Sqrt(k));
                var c = color; c.a = color.a * (1f - k) * (1f - k);
                m[i].color = c;
            }
            yield return null;
        }
        foreach (var x in m) Destroy(x);
        Destroy(raiz);
    }

    // ---------------- destello: luz puntual que se apaga rapido
    public static void Destello(Vector3 pos, Color color, float intensidad = 4f, float alcance = 5f, float duracion = 0.15f)
    {
        var go = new GameObject("Destello");
        go.transform.position = pos;
        var l = go.AddComponent<Light>();
        l.type = LightType.Point; l.color = color; l.range = alcance; l.intensity = intensidad; l.shadows = LightShadows.None;
        go.AddComponent<Efectos>().StartCoroutine(AnimarDestello(l, intensidad, duracion));
    }

    static IEnumerator AnimarDestello(Light l, float i0, float dur)
    {
        for (float t = 0; t < dur; t += Time.deltaTime) { l.intensity = i0 * (1f - t / dur); yield return null; }
        Destroy(l.gameObject);
    }

    // ---------------- hit-stop: el juego se congela unas centesimas al pegar (el golpe se siente)
    static bool enImpacto;
    public static void Impacto(float segundos = 0.05f)
    {
        if (enImpacto || HUD.Pausado || Time.timeScale < 0.99f) return;
        Anfitrion.StartCoroutine(AnimarImpacto(Mathf.Min(segundos, 0.15f)));
    }

    static IEnumerator AnimarImpacto(float s)
    {
        enImpacto = true;
        Time.timeScale = 0.05f;
        yield return new WaitForSecondsRealtime(s);
        if (Mathf.Abs(Time.timeScale - 0.05f) < 0.001f) Time.timeScale = 1f;   // si se pauso mientras tanto, no lo toca
        enImpacto = false;
    }

    // ---------------- sacudida de camara (la aplica CamaraGTA en su LateUpdate)
    public static float SacudidaHasta, SacudidaFuerza;
    const float SacudidaMaxima = 1f;   // ninguna sacudida dura mas de 1 segundo

    // Al dar Play se borra la sacudida anterior (si Unity no recarga los scripts, el valor viejo
    // quedaba guardado y la camara temblaba al iniciar).
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reiniciar() { SacudidaHasta = 0f; SacudidaFuerza = 0f; material = null; matPolvo = null; enImpacto = false; anfitrion = null; }

    public static void Sacudir(float fuerza = 0.15f, float duracion = 0.2f)
    {
        duracion = Mathf.Min(duracion, SacudidaMaxima);
        SacudidaFuerza = Mathf.Max(fuerza, Time.time < SacudidaHasta ? SacudidaFuerza : 0f);
        SacudidaHasta = Time.time + duracion;
    }

    public static Vector3 OffsetSacudida()
    {
        float falta = SacudidaHasta - Time.time;
        if (falta <= 0f || falta > SacudidaMaxima || HUD.Pausado) return Vector3.zero;   // valor imposible: se ignora
        return Random.insideUnitSphere * SacudidaFuerza * Mathf.Clamp01(falta / 0.15f);   // se apaga suave al final
    }
}
