using UnityEngine;

// Ponlo en el mismo objeto que el Rival: al morir, el enemigo suelta esta pieza de barco
// (ademas de su bolsa de botin normal). Si Kai ya la tiene, no suelta nada.
[RequireComponent(typeof(Rival))]
public class Sueltapieza : MonoBehaviour
{
    [Tooltip("Id de la pieza en PiezasBarco.Catalogo (vela, timon, casco, canon, brujula)")]
    public string idPieza = "vela";
    [Tooltip("1 = siempre. Usa 1 si el enemigo no vuelve a aparecer, o la pieza se perderia")]
    [Range(0f, 1f)] public float probabilidad = 1f;
    [Tooltip("Separacion desde el enemigo para que no quede encima de la bolsa de botin")]
    public Vector3 desplazamiento = new Vector3(0.9f, 0f, 0f);

    Rival rival;
    bool soltada;

    void Awake() { rival = GetComponent<Rival>(); }

    void Update()
    {
        if (soltada || !rival.Muerto) return;
        soltada = true;
        if (PiezasBarco.Tiene(idPieza) || Random.value > probabilidad) return;
        Piezasuelo.Crear(Suelo(transform.position + transform.rotation * desplazamiento), idPieza);
    }

    // baja el punto hasta el suelo (ignora al propio enemigo y los triggers como el Mar)
    Vector3 Suelo(Vector3 p)
    {
        var hits = Physics.RaycastAll(p + Vector3.up * 2f, Vector3.down, 10f, ~0, QueryTriggerInteraction.Ignore);
        float mejor = float.MaxValue; Vector3 punto = p;
        foreach (var h in hits)
        {
            if (h.collider.transform.IsChildOf(transform)) continue;
            if (h.distance < mejor) { mejor = h.distance; punto = h.point; }
        }
        return punto;
    }
}