using UnityEngine;

// El mar: ponlo en el plano del agua (si no lo pones, Kai lo busca solo por nombre: "Mar", "Agua", "Water", "Ocean"...).
//  - Su altura es el nivel del mar.
//  - Sus colliders pasan a ser "trigger": Kai ya no puede pararse encima del agua.
//  - Si Kai se mete donde el agua le cubre mas de "Profundidad Ahogo", se ahoga: pierde vida y vuelve
//    a la ultima orilla segura (como en Zelda).
//  - "Solo En Su Area": agua local (una laguna, la bahia del astillero...). Solo cuenta encima de ella,
//    asi puede subir y bajar con la Marea sin mover el mar de todo el archipielago.
public class Mar : MonoBehaviour
{
    [Tooltip("Ajuste fino del nivel del agua respecto a este objeto (positivo = mas alto)")]
    public float ajusteNivel = 0f;
    [Tooltip("Agua local: solo cuenta cuando Kai esta encima de ella (lagunas, bahias con marea)")]
    public bool soloEnSuArea = false;

    public static Mar Actual;   // el mar grande
    static readonly System.Collections.Generic.List<Mar> locales = new System.Collections.Generic.List<Mar>();
    public static bool Existe => Actual || locales.Count > 0;
    public static float Nivel => Actual ? Actual.Superficie() + Actual.ajusteNivel : float.NegativeInfinity;

    // nivel del agua en un punto: el mar grande o el agua local que este debajo (la mas alta)
    public static float NivelEn(Vector3 p)
    {
        float n = Nivel;
        foreach (var m in locales)
        {
            if (!m || !m.isActiveAndEnabled || !m.Cubre(p)) continue;
            n = Mathf.Max(n, m.Superficie() + m.ajusteNivel);
        }
        return n;
    }

    bool Cubre(Vector3 p)
    {
        var r = GetComponentInChildren<Renderer>();
        Bounds b = r ? r.bounds : new Bounds(transform.position, transform.lossyScale * 10f);
        return p.x >= b.min.x && p.x <= b.max.x && p.z >= b.min.z && p.z <= b.max.z;
    }

    // altura de la superficie: lo mas alto del dibujo del agua (o del collider, o del objeto)
    float Superficie()
    {
        var r = GetComponentInChildren<Renderer>();
        if (r) return r.bounds.max.y;
        var c = GetComponentInChildren<Collider>();
        if (c) return c.bounds.max.y;
        return transform.position.y;
    }

    void Awake()
    {
        if (soloEnSuArea) { if (!locales.Contains(this)) locales.Add(this); }
        else { anterior = Actual; Actual = this; }
        foreach (var c in GetComponentsInChildren<Collider>()) c.isTrigger = true;   // no se puede caminar sobre el agua
    }

    Mar anterior;
    // convierte este agua en local (lo usa la Marea al ponerle el componente por codigo)
    public void HacerLocal()
    {
        soloEnSuArea = true;
        if (Actual == this) Actual = anterior;
        if (!locales.Contains(this)) locales.Add(this);
    }

    void OnDestroy() { if (Actual == this) Actual = null; locales.Remove(this); }

    // por si no lo pusiste: busca el agua por nombre
    public static void Buscar()
    {
        if (Actual) return;
        string[] nombres = { "Mar", "mar", "Agua", "agua", "Water", "water", "Ocean", "ocean", "Sea", "Oceano", "Océano" };
        foreach (var n in nombres)
        {
            var go = GameObject.Find(n);
            if (go) { go.AddComponent<Mar>(); Debug.Log("[Mar] Use \"" + n + "\" como el mar. Si no es, ponle el componente Mar al plano del agua.", go); return; }
        }
    }
}
