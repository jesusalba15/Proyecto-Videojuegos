using UnityEngine;

// Marea (Isla 3, Astillero Hundido). Como dice el GDD: "algunas rutas solo existen de noche cuando baja el agua".
// Ponlo en el plano de agua de una bahia o laguna (NO en el mar grande): de dia esta alta y de noche baja,
// dejando al descubierto lo que haya debajo (el casco hundido, una pasarela, una cueva...).
//  - Si el objeto no tiene el componente Mar, se lo pone solo (como agua local): tocarla sigue haciendo dano.
//  - Si Kai se queda abajo cuando sube la marea, el agua lo alcanza y lo devuelve a la orilla.
//  - Necesita un CicloDia en la escena (sin el, la marea queda siempre alta). F4 adelanta 2 horas para probar.
public class Marea : MonoBehaviour
{
    [Tooltip("Metros que baja el agua con la marea baja")]
    public float bajada = 2.5f;
    [Tooltip("Isla donde esta (para los avisos solo cuando Kai esta en ella)")]
    public int idIsla = 3;
    [Tooltip("Objetos que solo se ven con marea baja (por ejemplo un brillo sobre el casco)")]
    public GameObject[] soloMareaBaja;
    [Tooltip("Para probar: la marea queda siempre baja")]
    public bool siempreBaja = false;

    public static Marea Actual;
    // 0 = marea alta, 1 = marea baja del todo
    public static float Bajamar => Actual ? Actual.bajamar : 0f;
    public static bool EstaBaja => Bajamar > 0.85f;

    float alturaAlta, bajamar = -1f;
    int estadoAviso;   // 0 alta, 1 bajando/baja, 2 subiendo

    void Awake()
    {
        Actual = this;
        alturaAlta = transform.position.y;
        var m = GetComponent<Mar>();
        if (!m) m = gameObject.AddComponent<Mar>();
        m.HacerLocal();   // agua local: no cambia el nivel del mar grande
    }

    void OnDisable() { if (Actual == this) Actual = null; }

    void Update()
    {
        // la marea sigue a la luz: baja al anochecer, queda abajo toda la noche y sube al amanecer
        float meta = siempreBaja ? 1f : (CicloDia.Actual ? 1f - CicloDia.Luz : 0f);
        float antes = bajamar;
        bajamar = bajamar < 0f ? meta : Mathf.MoveTowards(bajamar, meta, Time.deltaTime * 0.08f);   // ~12 s de bajada completa
        var p = transform.position;
        p.y = alturaAlta - bajada * Suave(bajamar);
        transform.position = p;
        if (soloMareaBaja != null) foreach (var o in soloMareaBaja) if (o) o.SetActive(bajamar > 0.85f);

        if (antes < 0f || GameState.IslaActual != idIsla) return;
        if (estadoAviso != 1 && bajamar > antes && bajamar > 0.15f)
        {
            estadoAviso = 1;
            GameState.Avisar("La marea está bajando... el casco hundido empieza a asomar.", 3f);
        }
        else if (estadoAviso == 1 && bajamar < antes && bajamar < 0.9f)
        {
            estadoAviso = 2;
            GameState.Avisar("¡La marea sube! Sal del casco antes de que te alcance el agua.", 3.5f);
        }
        else if (estadoAviso == 2 && bajamar <= 0.01f) estadoAviso = 0;
    }

    static float Suave(float k) => k * k * (3f - 2f * k);

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.8f);
        var r = GetComponentInChildren<Renderer>();
        Vector3 c = r ? r.bounds.center : transform.position;
        Vector3 t = r ? new Vector3(r.bounds.size.x, 0.05f, r.bounds.size.z) : new Vector3(10f, 0.05f, 10f);
        float alta = Application.isPlaying ? alturaAlta : transform.position.y;
        Gizmos.DrawWireCube(new Vector3(c.x, alta, c.z), t);
        Gizmos.color = new Color(0.3f, 0.7f, 1f, 0.35f);
        Gizmos.DrawWireCube(new Vector3(c.x, alta - bajada, c.z), t);   // hasta donde baja
    }
}
