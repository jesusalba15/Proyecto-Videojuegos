using System.Collections.Generic;
using UnityEngine;

// Zona de la Corona (Isla 4 y la muralla de la Isla 6). Como pide el GDD: "las islas de la Corona prohiben
// el combate directo por completo". Dentro del cuadro Kai no puede atacar ni usar habilidades:
// tiene que esconderse (C junto a barriles), usar bombas de humo o invocar a Sombra.
// Ponlo en un objeto vacio y ajusta el tamano (cuadro azul en la escena).
public class ZonaCorona : MonoBehaviour
{
    public Vector3 tamano = new Vector3(80f, 40f, 80f);
    [TextArea(1, 3)] public string aviso = "En tierras de la Corona no se pelea: si te ven, te encierran. Escóndete.";
    [Tooltip("Mensaje al entrar a la zona (vacio = ninguno)")]
    public string alEntrar = "Territorio de la Corona: nada de pelear. Muévete en las sombras.";

    static readonly List<ZonaCorona> todas = new List<ZonaCorona>();
    static float proximoAviso;
    bool estaba;

    void OnEnable() { if (!todas.Contains(this)) todas.Add(this); }
    void OnDisable() { todas.Remove(this); }

    public bool Contiene(Vector3 p)
    {
        Vector3 l = transform.InverseTransformPoint(p);
        Vector3 s = transform.lossyScale;
        Vector3 m = new Vector3(tamano.x / Mathf.Max(0.001f, s.x), tamano.y / Mathf.Max(0.001f, s.y), tamano.z / Mathf.Max(0.001f, s.z)) * 0.5f;
        return Mathf.Abs(l.x) <= m.x && Mathf.Abs(l.y) <= m.y && Mathf.Abs(l.z) <= m.z;
    }

    public static ZonaCorona En(Vector3 p) { foreach (var z in todas) if (z && z.Contiene(p)) return z; return null; }

    // la llaman el ataque y las habilidades de Kai: true = no se puede (y avisa)
    public static bool ProhibeCombate(Vector3 p)
    {
        var z = En(p);
        if (!z) return false;
        if (Time.time > proximoAviso) { proximoAviso = Time.time + 2.5f; GameState.Avisar(z.aviso, 2.5f); }
        return true;
    }

    void Update()
    {
        var kai = GameState.Kai;
        bool dentro = kai && Contiene(kai.position);
        if (dentro && !estaba && !string.IsNullOrEmpty(alEntrar)) GameState.Avisar(alEntrar, 3f);
        estaba = dentro;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.3f, 0.5f, 1f, 0.7f);
        Gizmos.matrix = Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one);
        Gizmos.DrawWireCube(Vector3.zero, tamano);
    }
}
