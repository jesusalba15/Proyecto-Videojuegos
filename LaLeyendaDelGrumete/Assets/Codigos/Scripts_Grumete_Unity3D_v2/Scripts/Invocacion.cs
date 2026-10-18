using UnityEngine;

// Invocar a la tripulacion (como las habilidades de los Elegidos en Zelda: Breath of the Wild).
//   G -> invoca al tripulante elegido: aparece su espiritu junto a Kai y su bonus funciona mientras dura.
//   H -> cambia de tripulante (si tienes varios).
// Despues de usarlo hay que esperar a que se recargue.
//   Mara   "Viento de Mara"   : barco +50% de velocidad y Kai +15% a pie
//   Pico   "Regateo"          : el Taller cobra 25% menos (invocalo antes de entrar)
//   Sombra "Manto de Sombra"  : los guardias tardan mucho mas en verte
//   Bruno  "Escudo de Bruno"  : recibes la mitad del dano
// Ponlo en Kai (si no esta, el HUD lo agrega solo). En "Modelos" puedes arrastrar el modelo 3D de cada
// tripulante (Mara, Pico, Sombra, Bruno); si lo dejas vacio aparece un espiritu sencillo.
public class Invocacion : MonoBehaviour
{
    [Tooltip("Modelo de cada tripulante en orden: Mara, Pico, Sombra, Bruno (opcional)")]
    public GameObject[] modelos = new GameObject[4];
    [Tooltip("Segundos que dura cada invocacion (Mara, Pico, Sombra, Bruno)")]
    public float[] duracion = { 30f, 45f, 20f, 15f };
    [Tooltip("Segundos de recarga despues de terminar (Mara, Pico, Sombra, Bruno)")]
    public float[] recarga = { 60f, 60f, 45f, 50f };
    public float escalaEspiritu = 0.9f;

    public static Invocacion Actual;
    public static readonly Color[] Colores =
    {
        new Color(0.35f, 0.9f, 0.9f),   // Mara: turquesa (viento)
        new Color(1f, 0.8f, 0.3f),      // Pico: dorado (dinero)
        new Color(0.6f, 0.45f, 1f),     // Sombra: morado
        new Color(1f, 0.45f, 0.3f),     // Bruno: rojo (escudo)
    };
    public static readonly string[] Poderes = { "Viento de Mara", "Regateo de Pico", "Manto de Sombra", "Escudo de Bruno" };

    public int Elegido { get; private set; } = -1;
    readonly float[] activoHasta = new float[4], listoEn = new float[4];
    GameObject espiritu; int espirituDe = -1;
    Material fantasma;

    void Awake() { Actual = this; }
    void OnDestroy() { if (Actual == this) Actual = null; }

    // ------------------------------------------------------------ estado (lo usa Tripulacion.cs)
    public bool Activo(int m) => m >= 0 && m < 4 && Time.time < activoHasta[m];
    public float Restante(int m) => Activo(m) ? activoHasta[m] - Time.time : 0f;
    public float RecargaRestante(int m) => m < 0 ? 0f : Mathf.Max(0f, listoEn[m] - Time.time);
    public float FraccionRecarga(int m) => m < 0 || Activo(m) ? 0f : Mathf.Clamp01(RecargaRestante(m) / Mathf.Max(1f, recarga[m]));
    public float FraccionActivo(int m) => Activo(m) ? Mathf.Clamp01(Restante(m) / Mathf.Max(1f, duracion[m])) : 0f;

    void Update()
    {
        if (Elegido < 0 || !Tripulacion.Tiene((Tripulacion.Miembro)Elegido)) Elegido = Siguiente(-1);
        if (HUD.Pausado || MapaMundo.Abierto || Taller.Abierto) { Espiritu(); return; }

        if (Input.GetKeyDown(KeyCode.H)) Cambiar();
        if (Input.GetKeyDown(KeyCode.G)) Invocar();
        Espiritu();
    }

    int Siguiente(int desde)
    {
        for (int k = 1; k <= 4; k++)
        {
            int i = ((desde < 0 ? -1 : desde) + k + 4) % 4;
            if (Tripulacion.Tiene((Tripulacion.Miembro)i)) return i;
        }
        return -1;
    }

    void Cambiar()
    {
        if (Tripulacion.Cuantos == 0) { GameState.Avisar("Aún no tienes tripulación. Recluta tripulantes en las islas.", 2f); return; }
        Elegido = Siguiente(Elegido);
        if (Elegido >= 0) GameState.Avisar("Tripulante elegido: " + Tripulacion.Nombres[Elegido] + " (" + Poderes[Elegido] + "). G para invocar.", 2f);
    }

    void Invocar()
    {
        int m = Elegido;
        if (m < 0) { GameState.Avisar("Aún no tienes tripulación. Recluta tripulantes en las islas.", 2f); return; }
        if (Activo(m)) return;
        if (RecargaRestante(m) > 0f) { GameState.Avisar(Tripulacion.Nombres[m] + " descansa: faltan " + Mathf.CeilToInt(RecargaRestante(m)) + " s.", 1.5f); return; }

        activoHasta[m] = Time.time + duracion[m];
        listoEn[m] = activoHasta[m] + recarga[m];
        Color c = Colores[m];
        Efectos.Anillo(transform.position, 2.4f, c, 0.7f);
        Efectos.Anillo(transform.position, 1.2f, Color.white, 0.4f);
        HUD.Numero(transform.position + Vector3.up * 2.4f, "¡" + Poderes[m] + "!", c, 1.2f);
        GameState.Avisar(Poderes[m] + ": " + Tripulacion.Bonus[m] + " por " + Mathf.RoundToInt(duracion[m]) + " s.", 2.5f);
    }

    // ------------------------------------------------------------ espiritu que acompana a Kai
    void Espiritu()
    {
        int activo = -1;
        for (int i = 0; i < 4; i++) if (Activo(i)) { activo = i; break; }
        if (activo != espirituDe)
        {
            if (espiritu) { Efectos.Anillo(espiritu.transform.position, 1.2f, Colores[espirituDe], 0.4f); Destroy(espiritu); }
            espiritu = null; espirituDe = activo;
            if (activo >= 0) CrearEspiritu(activo);
        }
        if (!espiritu) return;

        // flota a la izquierda y un poco atras de Kai
        Vector3 objetivo = transform.position - transform.right * 1.3f - transform.forward * 0.7f
                         + Vector3.up * (0.25f + Mathf.Sin(Time.time * 2.5f) * 0.15f);
        espiritu.transform.position = Vector3.Lerp(espiritu.transform.position, objetivo, 6f * Time.deltaTime);
        espiritu.transform.rotation = Quaternion.Slerp(espiritu.transform.rotation, transform.rotation, 6f * Time.deltaTime);
        // parpadea los ultimos 3 segundos
        float r = Restante(activo);
        bool ver = r > 3f || Mathf.Repeat(Time.time * 6f, 1f) > 0.35f;
        foreach (var rd in espiritu.GetComponentsInChildren<Renderer>()) rd.enabled = ver;
    }

    void CrearEspiritu(int m)
    {
        Color c = Colores[m];
        if (!fantasma) fantasma = CrearFantasma();
        var mat = new Material(fantasma);
        Color t = new Color(c.r, c.g, c.b, 0.45f);
        mat.color = t; if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", t);

        GameObject go;
        if (modelos != null && m < modelos.Length && modelos[m])
        {
            go = Instantiate(modelos[m]);
            go.SetActive(true);
            // solo queda el dibujo: sin scripts (menos la animacion), sin colisiones ni fisica
            foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>(true)) if (!(mb is AnimacionSimple)) Destroy(mb);
            foreach (var co in go.GetComponentsInChildren<Collider>(true)) Destroy(co);
            foreach (var rb in go.GetComponentsInChildren<Rigidbody>(true)) Destroy(rb);
            foreach (var cc in go.GetComponentsInChildren<CharacterController>(true)) Destroy(cc);
        }
        else
        {
            // espiritu sencillo: cuerpo y cabeza
            go = new GameObject("Espiritu");
            var cuerpo = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            cuerpo.transform.SetParent(go.transform, false); cuerpo.transform.localPosition = Vector3.up * 0.8f; cuerpo.transform.localScale = new Vector3(0.6f, 0.6f, 0.6f);
            var cabeza = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cabeza.transform.SetParent(go.transform, false); cabeza.transform.localPosition = Vector3.up * 1.65f; cabeza.transform.localScale = Vector3.one * 0.45f;
            foreach (var co in go.GetComponentsInChildren<Collider>()) DestroyImmediate(co);
        }
        go.name = "Espiritu_" + Tripulacion.Nombres[m];
        go.transform.localScale *= escalaEspiritu;
        foreach (var rd in go.GetComponentsInChildren<Renderer>())
        {
            var mats = rd.sharedMaterials;
            for (int i = 0; i < mats.Length; i++) mats[i] = mat;
            rd.sharedMaterials = mats;
            rd.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
        // luz suave del color del tripulante
        var luz = new GameObject("Luz").AddComponent<Light>();
        luz.transform.SetParent(go.transform, false); luz.transform.localPosition = Vector3.up * 1.2f;
        luz.type = LightType.Point; luz.color = c; luz.range = 4f; luz.intensity = 1.5f;

        go.transform.position = transform.position + Vector3.up * 2f;
        espiritu = go;
    }

    // material transparente que brilla (URP Unlit en modo transparente)
    static Material CrearFantasma()
    {
        var sh = Shader.Find("Universal Render Pipeline/Unlit");
        if (!sh) sh = Shader.Find("Sprites/Default");
        var m = new Material(sh);
        if (m.HasProperty("_Surface"))
        {
            m.SetFloat("_Surface", 1f); m.SetFloat("_Blend", 0f);
            m.SetOverrideTag("RenderType", "Transparent");
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }
        m.renderQueue = 3000;
        return m;
    }
}
