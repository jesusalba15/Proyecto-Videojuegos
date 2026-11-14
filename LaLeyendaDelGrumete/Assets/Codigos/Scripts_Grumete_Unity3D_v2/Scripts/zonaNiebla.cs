using System.Collections.Generic;
using UnityEngine;

// Zona de neblina (mundo abierto: varias zonas en la misma escena).
//  - Pon este script en un objeto vacio con un BoxCollider o SphereCollider (se marca solo como Trigger).
//  - Cuando Kai entra, la niebla de la escena sube suavemente hasta "densidad"; al salir vuelve a la normal.
//  - "borde": metros fuera de la zona en los que la niebla ya empieza a notarse (transicion suave).
//  - Si hay varias zonas, manda la que mas cerca/dentro tenga a Kai.
// Usa la niebla de Lighting > Environment > Fog (modo Exponential). Si estaba apagada, la enciende con densidad 0.
[RequireComponent(typeof(Collider))]
public class ZonaNiebla : MonoBehaviour
{
    [Tooltip("Densidad dentro de la zona (0.008 muy ligera, 0.015 ligera, 0.03 media, 0.06 espesa)")]
    public float densidad = 0.015f;
    [Tooltip("Color de la neblina")]
    public Color color = new Color(0.78f, 0.82f, 0.86f);
    [Tooltip("Metros fuera del borde del collider en los que la niebla se va desvaneciendo")]
    public float borde = 10f;
    [Tooltip("Rapidez con que la niebla entra y sale")]
    public float suavizado = 1.5f;

    static readonly List<ZonaNiebla> zonas = new List<ZonaNiebla>();
    static bool baseGuardada, fogBase;
    static float densidadBase, densidadActual;
    static Color colorBase, colorActual;

    Collider col;

    // Por si "Enter Play Mode Options" no recarga los static entre partidas en el editor
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reiniciar() { zonas.Clear(); baseGuardada = false; }

    void Awake()
    {
        col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    void OnEnable() { if (!zonas.Contains(this)) zonas.Add(this); }

    void OnDisable()
    {
        zonas.Remove(this);
        if (zonas.Count == 0 && baseGuardada) Restaurar();
    }

    void Update()
    {
        if (zonas.Count == 0 || zonas[0] != this) return;   // solo una zona calcula y aplica el resultado
        GuardarBase();

        ZonaNiebla mejor = null;
        float pesoMax = 0f;
        if (GameState.Kai)
        {
            Vector3 p = GameState.Kai.position;
            foreach (var z in zonas)
            {
                if (!z) continue;
                float w = z.Peso(p);
                if (w > pesoMax) { pesoMax = w; mejor = z; }
            }
        }

        float objDensidad = mejor ? Mathf.Lerp(densidadBase, mejor.densidad, pesoMax) : densidadBase;
        Color objColor = mejor ? Color.Lerp(colorBase, mejor.color, pesoMax) : colorBase;

        float t = 1f - Mathf.Exp(-suavizado * Time.deltaTime);
        densidadActual = Mathf.Lerp(densidadActual, objDensidad, t);
        colorActual = Color.Lerp(colorActual, objColor, t);

        RenderSettings.fogDensity = densidadActual;
        RenderSettings.fogColor = colorActual;
    }

    // 1 = Kai dentro de la zona, 0 = Kai a "borde" metros o mas
    float Peso(Vector3 p)
    {
        if (!col || !col.enabled) return 0f;
        float d = Vector3.Distance(p, col.ClosestPoint(p));   // 0 si esta dentro
        if (borde <= 0f) return d < 0.01f ? 1f : 0f;
        return Mathf.Clamp01(1f - d / borde);
    }

    static void GuardarBase()
    {
        if (baseGuardada) return;
        baseGuardada = true;
        fogBase = RenderSettings.fog;
        if (!RenderSettings.fog)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Exponential;
            RenderSettings.fogDensity = 0f;
        }
        else if (RenderSettings.fogMode == FogMode.Linear)
        {
            Debug.LogWarning("ZonaNiebla: la niebla de la escena esta en modo Linear; se cambia a Exponential para poder variar la densidad.");
            RenderSettings.fogMode = FogMode.Exponential;
        }
        densidadBase = densidadActual = RenderSettings.fogDensity;
        colorBase = colorActual = RenderSettings.fogColor;
    }

    static void Restaurar()
    {
        RenderSettings.fogDensity = densidadBase;
        RenderSettings.fogColor = colorBase;
        RenderSettings.fog = fogBase;
        baseGuardada = false;
    }

    void OnDrawGizmos()
    {
        var c = GetComponent<Collider>();
        if (!c) return;
        Gizmos.color = new Color(color.r, color.g, color.b, 0.15f);
        Gizmos.DrawCube(c.bounds.center, c.bounds.size);
        Gizmos.color = new Color(0.8f, 0.85f, 0.9f, 0.6f);
        Gizmos.DrawWireCube(c.bounds.center, c.bounds.size + Vector3.one * borde * 2f);
    }
}