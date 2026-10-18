using System.Collections.Generic;
using UnityEngine;

// Marca el area de una isla (mapa local, deteccion de "en que isla estoy" y activacion en el mapa del mundo).
// Ponlo en un objeto vacio en el centro de la isla y ajusta el tamano (el cuadro amarillo en la escena).
// Una isla aparece como disponible en el mapa del archipielago en cuanto existe su ZonaIsla.
public class ZonaIsla : MonoBehaviour
{
    [Tooltip("Id de la isla (1 Caleta Gris, 2 Faro Roto, 3 Astillero, 4 Bahia de la Corona, 5 Fuerte de Roca, 6 Fortaleza de Voss)")]
    public int idIsla = 1;
    [Tooltip("Ancho (X) y largo (Z) del area que se ve en el mapa")]
    public Vector2 tamano = new Vector2(80f, 40f);

    [Header("Objetivo en el HUD mientras Kai esta en esta isla")]
    [Tooltip("Frase de arriba del objetivo")]
    [TextArea(2, 4)] public string objetivo = "";
    [Tooltip("Pasos con casilla (se marcan solos). Si lo dejas vacio, solo se muestra la frase.")]
    public Paso[] pasos;
    [Tooltip("Cuando esta recompensa se da, la isla cuenta como completada")]
    public RecompensaIsla recompensaPrincipal;

    // un paso del objetivo: se marca cuando se da su recompensa, se juntan sus objetos o se acepta su encargo
    [System.Serializable]
    public class Paso
    {
        public string texto = "";
        [Tooltip("Hecho cuando se da esta recompensa (por ejemplo la del jefe)")]
        public RecompensaIsla recompensa;
        [Tooltip("Hecho cuando se juntan los ObjetoMision de este grupo")]
        public string grupoObjetos = "";
        public int cantidad = 0;
        [Tooltip("Hecho cuando se acepta este encargo (o, si tiene grupo, cuando se entrega)")]
        public Encargo encargo;
        [Tooltip("Hecho cuando se abre este bloqueo (porton de Bruno, cerradura de Sombra)")]
        public Bloqueo bloqueo;
        [Tooltip("No se muestra hasta que el paso anterior este hecho")]
        public bool ocultoHastaAnterior = false;
    }

    public static bool Hecho(Paso p)
    {
        if (p.recompensa) return p.recompensa.Hecha;
        if (p.bloqueo) return p.bloqueo.Abierto;
        if (!string.IsNullOrEmpty(p.grupoObjetos)) return ObjetoMision.Contar(p.grupoObjetos) >= Mathf.Max(1, p.cantidad) || (p.encargo && p.encargo.Terminado);
        if (p.encargo) return p.encargo.EstaAceptado;
        return false;
    }

    public bool TieneObjetivo => !string.IsNullOrEmpty(objetivo) || (pasos != null && pasos.Length > 0);

    // texto para el HUD: la frase y debajo los pasos como "[x] ..." / "[ ] ..." (el HUD los dibuja con casilla)
    public string TextoObjetivo()
    {
        if (pasos == null || pasos.Length == 0) return objetivo;
        var lineas = new List<string>();
        if (!string.IsNullOrEmpty(objetivo)) lineas.Add(objetivo);
        bool anterior = true;
        foreach (var p in pasos)
        {
            if (p == null || string.IsNullOrEmpty(p.texto)) continue;
            if (p.ocultoHastaAnterior && !anterior) break;
            bool h = Hecho(p);
            string cuenta = !h && !string.IsNullOrEmpty(p.grupoObjetos) && p.cantidad > 1
                ? " (" + Mathf.Min(ObjetoMision.Contar(p.grupoObjetos), p.cantidad) + "/" + p.cantidad + ")" : "";
            lineas.Add((h ? "[x] " : "[ ] ") + p.texto + cuenta);
            anterior = h;
        }
        return string.Join("\n", lineas.ToArray());
    }

    public Rect Area => new Rect(transform.position.x - tamano.x / 2f, transform.position.z - tamano.y / 2f, tamano.x, tamano.y);
    // completada: la recompensa principal ya se dio y todos los pasos estan hechos
    public bool Completada
    {
        get
        {
            if (!recompensaPrincipal || !recompensaPrincipal.Hecha) return false;
            if (pasos != null) foreach (var p in pasos) if (p != null && !string.IsNullOrEmpty(p.texto) && !Hecho(p)) return false;
            return true;
        }
    }

    static readonly List<ZonaIsla> todas = new List<ZonaIsla>();
    public static IReadOnlyList<ZonaIsla> Todas => todas;
    public static bool Existe(int id) { foreach (var z in todas) if (z && z.idIsla == id) return true; return false; }
    public static ZonaIsla De(int id) { foreach (var z in todas) if (z && z.idIsla == id) return z; return null; }

    void OnEnable() { if (!todas.Contains(this)) todas.Add(this); }
    void OnDisable() { todas.Remove(this); }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.85f, 0.2f, 0.8f);
        Gizmos.DrawWireCube(transform.position, new Vector3(tamano.x, 2f, tamano.y));
    }
}
