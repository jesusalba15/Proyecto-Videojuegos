using UnityEngine;

// Farol, antorcha o ventana que se prende de noche (necesita un CicloDia en la escena).
// Ponlo en el farol: usa las luces que tenga dentro (si no tiene, crea una luz calida arriba).
// Si tiene LuzVela, la llama sigue titilando. En "Objetos Noche" puedes poner cosas que solo
// se ven de noche (una llama, un vidrio brillante...).
public class Farol : MonoBehaviour
{
    public float intensidad = 2.5f;
    public float alcance = 8f;
    public Color color = new Color(1f, 0.72f, 0.4f);
    [Tooltip("Tambien prendido de dia (por ejemplo dentro de una cueva)")]
    public bool siempreEncendido = false;
    public GameObject[] objetosNoche;

    Light[] luces;
    float[] base_;
    LuzVela[] velas;
    float nivel = -1f;

    void Start()
    {
        luces = GetComponentsInChildren<Light>(true);
        if (luces.Length == 0)
        {
            var go = new GameObject("LuzFarol");
            go.transform.SetParent(transform, false);
            var r = GetComponentInChildren<Renderer>();
            go.transform.position = r ? new Vector3(r.bounds.center.x, r.bounds.max.y - 0.2f, r.bounds.center.z) : transform.position + Vector3.up * 2f;
            var l = go.AddComponent<Light>();
            l.type = LightType.Point; l.range = alcance; l.color = color; l.intensity = intensidad; l.shadows = LightShadows.None;
            luces = new[] { l };
        }
        base_ = new float[luces.Length];
        for (int i = 0; i < luces.Length; i++) base_[i] = luces[i].intensity > 0f ? luces[i].intensity : intensidad;
        velas = GetComponentsInChildren<LuzVela>(true);
    }

    void Update()
    {
        // se prende al atardecer y se apaga al amanecer, suave
        float meta = siempreEncendido ? 1f : 1f - Mathf.InverseLerp(0.15f, 0.6f, CicloDia.Luz);
        float antes = nivel;
        nivel = nivel < 0f ? meta : Mathf.MoveTowards(nivel, meta, Time.deltaTime * 0.8f);
        if (Mathf.Abs(antes - nivel) < 0.0001f) return;

        for (int i = 0; i < luces.Length; i++)
        {
            if (!luces[i]) continue;
            luces[i].enabled = nivel > 0.01f;
            luces[i].intensity = base_[i] * nivel;
        }
        foreach (var v in velas) if (v) v.factor = nivel;
        if (objetosNoche != null) foreach (var o in objetosNoche) if (o) o.SetActive(nivel > 0.5f);
    }
}
