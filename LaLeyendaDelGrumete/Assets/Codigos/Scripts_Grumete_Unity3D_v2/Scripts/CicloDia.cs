using UnityEngine;

// Ciclo de dia y noche. Ponlo en un objeto vacio de la escena de la isla (por ejemplo "CicloDia").
//  - Mueve el sol (la Directional Light), cambia su color y la luz ambiente: amanecer naranja,
//    mediodia blanco, atardecer rojizo y noche azul con luna.
//  - De noche se prenden los faroles (componente Farol) y los guardias ven menos lejos.
//  - F4 adelanta 2 horas (para probar).
public class CicloDia : MonoBehaviour
{
    [Header("Tiempo")]
    [Tooltip("Minutos reales que dura un dia completo")]
    public float minutosPorDia = 10f;
    [Range(0f, 24f)] public float horaInicial = 9f;
    [Tooltip("La noche pasa mas rapido que el dia (2 = el doble de rapido)")]
    public float rapidezNoche = 2f;

    [Header("Luces (si las dejas vacias se buscan o se crean solas)")]
    public Light sol;
    public Light luna;
    public float intensidadSol = 1.2f;
    public float intensidadLuna = 0.35f;
    [Tooltip("Hacia donde sale el sol (giro en Y)")]
    public float rumbo = -30f;

    [Header("Colores")]
    public Color colorAmanecer = new Color(1f, 0.6f, 0.35f);
    public Color colorMediodia = new Color(1f, 0.96f, 0.88f);
    public Color colorAtardecer = new Color(1f, 0.45f, 0.25f);
    public Color colorLuna = new Color(0.55f, 0.65f, 1f);
    public Color ambienteDia = new Color(0.62f, 0.66f, 0.72f);
    public Color ambienteNoche = new Color(0.12f, 0.15f, 0.28f);

    [Header("Cielos (Skybox) - opcional")]
    [Tooltip("Cielo de dia (vacio = el que ya tiene la escena)")]
    public Material cieloDia;
    [Tooltip("Cielo del amanecer y el atardecer")]
    public Material cieloAtardecer;
    [Tooltip("Cielo de noche")]
    public Material cieloNoche;
    [Tooltip("Segundos que tarda el cambio de un cielo a otro")]
    public float duracionCambioCielo = 1.5f;

    [Header("Guardias")]
    [Tooltip("De noche los guardias ven esta fraccion de su alcance normal")]
    [Range(0.3f, 1f)] public float visionNocturna = 0.65f;

    public static CicloDia Actual;
    public static float Hora => Actual ? Actual.hora : 12f;
    public static bool EsDeNoche => Actual && (Actual.hora < 6f || Actual.hora >= 19f);
    // 1 = pleno dia, 0 = noche cerrada (con transiciones al amanecer y al atardecer)
    public static float Luz => Actual ? Actual.luzDia : 1f;
    public static float FactorVision => Actual ? Mathf.Lerp(Actual.visionNocturna, 1f, Actual.luzDia) : 1f;

    float hora, luzDia = 1f;
    Material cieloOriginal, cielo;
    float ambienteOriginal;
    bool tieneExposicion;
    // cielos propios: copias para no tocar los materiales del proyecto
    Material[] cielos = new Material[3]; float[] exposicionBase = new float[3];
    int cieloActual = -1, cieloSiguiente = -1; float fundido = 1f; bool usaCielos;

    void Awake() { Actual = this; hora = horaInicial; }

    void Start()
    {
        if (!sol) sol = RenderSettings.sun;
        if (!sol)
            foreach (var l in FindObjectsByType<Light>(FindObjectsSortMode.None))
                if (l.type == LightType.Directional) { sol = l; break; }
        if (!luna)
        {
            luna = new GameObject("Luna").AddComponent<Light>();
            luna.type = LightType.Directional; luna.shadows = LightShadows.None;
            luna.color = colorLuna; luna.intensity = 0f;
        }
        // copia del cielo para no modificar el material original del proyecto
        cieloOriginal = RenderSettings.skybox;
        if (cieloOriginal)
        {
            cielo = new Material(cieloOriginal);
            RenderSettings.skybox = cielo;
            tieneExposicion = cielo.HasProperty("_Exposure");
        }
        ambienteOriginal = RenderSettings.ambientIntensity;
        // cielos de dia / atardecer / noche
        Material[] fuentes = { cieloDia ? cieloDia : cieloOriginal, cieloAtardecer, cieloNoche };
        for (int i = 0; i < 3; i++)
            if (fuentes[i])
            {
                cielos[i] = new Material(fuentes[i]);
                exposicionBase[i] = cielos[i].HasProperty("_Exposure") ? cielos[i].GetFloat("_Exposure") : 1f;
            }
        usaCielos = cieloAtardecer || cieloNoche;
        if (usaCielos) { cieloActual = CieloPara(hora); PonerCielo(cieloActual); }
        Aplicar();
    }

    void OnDisable()
    {
        if (cieloOriginal) RenderSettings.skybox = cieloOriginal;
        RenderSettings.ambientIntensity = ambienteOriginal;
        if (Actual == this) Actual = null;
    }

    void Update()
    {
        float velocidad = 24f / Mathf.Max(0.1f, minutosPorDia * 60f);   // horas por segundo
        if (hora < 6f || hora >= 19f) velocidad *= rapidezNoche;
        hora = Mathf.Repeat(hora + velocidad * Time.deltaTime, 24f);
        if (Input.GetKeyDown(KeyCode.F4)) { hora = Mathf.Repeat(hora + 2f, 24f); GameState.Avisar("Hora: " + TextoHora(), 1f); }
        Aplicar();
    }

    void Aplicar()
    {
        // luz del dia: sube de 5 a 7 h y baja de 17.5 a 19.5 h
        float subida = Mathf.InverseLerp(5f, 7f, hora);
        float bajada = 1f - Mathf.InverseLerp(17.5f, 19.5f, hora);
        luzDia = Mathf.Clamp01(Mathf.Min(subida, bajada));

        // sol: 6 h en el horizonte, 12 h arriba, 18 h en el otro horizonte
        float angulo = (hora - 6f) / 12f * 180f;
        if (sol)
        {
            sol.transform.rotation = Quaternion.Euler(angulo, rumbo, 0f);
            Color c = hora < 12f ? Color.Lerp(colorAmanecer, colorMediodia, Mathf.InverseLerp(6f, 10f, hora))
                                 : Color.Lerp(colorMediodia, colorAtardecer, Mathf.InverseLerp(14.5f, 18.5f, hora));
            sol.color = c;
            sol.intensity = intensidadSol * luzDia;
            sol.enabled = luzDia > 0.01f;
        }
        if (luna)
        {
            luna.transform.rotation = Quaternion.Euler(angulo + 180f, rumbo, 0f);   // del lado opuesto al sol
            luna.intensity = intensidadLuna * (1f - luzDia);
            luna.enabled = luzDia < 0.99f;
        }

        // luz ambiente y cielo
        if (RenderSettings.ambientMode == UnityEngine.Rendering.AmbientMode.Flat)
            RenderSettings.ambientLight = Color.Lerp(ambienteNoche, ambienteDia, luzDia);
        else
            RenderSettings.ambientIntensity = Mathf.Lerp(0.35f, ambienteOriginal, luzDia);
        if (usaCielos) CambiarCielo();
        else if (cielo && tieneExposicion) cielo.SetFloat("_Exposure", Mathf.Lerp(0.18f, 1.1f, luzDia));
        if (RenderSettings.fog) RenderSettings.fogColor = Color.Lerp(new Color(0.08f, 0.1f, 0.2f), new Color(0.75f, 0.82f, 0.9f), luzDia);
    }

    // 0 = dia, 1 = amanecer/atardecer, 2 = noche (si falta alguno, usa el mas parecido)
    int CieloPara(float h)
    {
        int c = (h >= 7f && h < 16.5f) ? 0 : (h >= 19.5f || h < 5f) ? 2 : 1;
        if (!cielos[c]) c = c == 1 ? (h < 12f ? 0 : 2) : 1;
        if (!cielos[c]) c = 0;
        return c;
    }

    void PonerCielo(int i)
    {
        if (i < 0 || !cielos[i]) return;
        RenderSettings.skybox = cielos[i];
        DynamicGI.UpdateEnvironment();   // la luz ambiente toma el color del cielo nuevo
    }

    // cambio suave: el cielo actual se oscurece, se cambia y el nuevo se aclara
    void CambiarCielo()
    {
        int meta = CieloPara(hora);
        if (meta != cieloActual && cieloSiguiente < 0) cieloSiguiente = meta;
        float paso = Time.deltaTime / Mathf.Max(0.1f, duracionCambioCielo * 0.5f);
        if (cieloSiguiente >= 0)
        {
            fundido = Mathf.MoveTowards(fundido, 0f, paso);
            if (fundido <= 0f) { cieloActual = cieloSiguiente; cieloSiguiente = -1; PonerCielo(cieloActual); }
        }
        else fundido = Mathf.MoveTowards(fundido, 1f, paso);
        if (cieloActual >= 0 && cielos[cieloActual] && cielos[cieloActual].HasProperty("_Exposure"))
            cielos[cieloActual].SetFloat("_Exposure", exposicionBase[cieloActual] * Mathf.Lerp(0.25f, 1f, fundido));
    }

    public static string TextoHora()
    {
        float h = Hora;
        int hh = Mathf.FloorToInt(h), mm = Mathf.FloorToInt((h - hh) * 60f / 10f) * 10;
        return hh.ToString("00") + ":" + mm.ToString("00");
    }
}
