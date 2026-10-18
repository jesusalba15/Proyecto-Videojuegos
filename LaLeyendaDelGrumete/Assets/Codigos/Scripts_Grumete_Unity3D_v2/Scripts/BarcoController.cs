using System.Collections.Generic;
using UnityEngine;

// Barco que Kai maneja como un carro de GTA, en la MISMA escena de las islas (mundo abierto).
//   E cerca del barco  -> subir (Kai se sienta en el "Asiento" y la camara sigue al barco)
//   W / S              -> vela (avanzar / frenar y reversa)      A / D -> timon
//   E manejando        -> bajar en el muelle o en la orilla mas cercana
//   M                  -> mapa del archipielago
// Las islas que aun no puedes visitar (sin revelar, sin destrezas o "proximamente") tienen corrientes
// que alejan el barco. Mara da bonus de velocidad.
// La proa del modelo debe apuntar hacia la flecha azul (eje Z) de la raiz.
[RequireComponent(typeof(Rigidbody))]
public class BarcoController : MonoBehaviour
{
    public float velocidad = 9f;
    public float bonusMara = 1.5f;
    public float velocidadGiro = 70f;     // grados por segundo
    public float aceleracion = 0.6f;      // que tan rapido llega a la velocidad maxima
    [Range(0f, 1f)] public float factorReversa = 0.4f;
    [HideInInspector] public Vector3 corriente;

    [Header("Subir y bajar")]
    [Tooltip("Donde se sienta Kai (un Empty hijo del barco, en la cubierta junto al timon)")]
    public Transform asiento;
    public float radioSubir = 4.5f;
    [Tooltip("Que tan lejos busca tierra para bajarse")]
    public float radioBajar = 9f;

    [Header("Islas bloqueadas")]
    public float margenCorriente = 25f;   // metros alrededor de la ZonaIsla donde empieza la corriente
    public float fuerzaCorriente = 7f;

    public static BarcoController Actual;
    public bool Manejando { get; private set; }
    // Para el aviso "E: Bajar del barco": solo cuando el barco va casi quieto y hay tierra cerca
    public bool PuedeBajarAqui => Manejando && Mathf.Abs(velActual) < 1.5f && tierraCerca;
    bool tierraCerca; float proximaTierra;

    Rigidbody rb;
    float velActual, proximoAviso, proximaRevision;
    PlayerController kai;
    readonly List<ZonaIsla> zonas = new List<ZonaIsla>();

    void Awake() { Actual = this; }
    void OnDestroy() { if (Actual == this) Actual = null; }

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.sleepThreshold = 0f;
        zonas.AddRange(FindObjectsByType<ZonaIsla>(FindObjectsSortMode.None));
        // escena vieja de Navegacion (sin Kai caminando): se maneja desde el inicio
        if (!FindFirstObjectByType<PlayerController>()) Manejando = true;
    }

    public bool PuedeSubir(Vector3 p) => !Manejando && isActiveAndEnabled && Vector3.Distance(p, transform.position) < radioSubir;

    void Update()
    {
        if (HUD.Pausado || MapaMundo.Abierto) return;
        if (!Manejando)
        {
            if (!kai) kai = FindFirstObjectByType<PlayerController>();
            if (kai && !kai.EstaMuerto && PuedeSubir(kai.transform.position) && Input.GetKeyDown(KeyCode.E)) Subir();
            return;
        }
        if (!kai) return;
        if (Time.time > proximaTierra) { proximaTierra = Time.time + 0.3f; tierraCerca = BuscarTierra(out _, out _); }
        if (Input.GetKeyDown(KeyCode.E)) Bajar();
        else if (Input.GetKeyDown(KeyCode.M)) MapaMundo.Abrir(MapaMundo.Pestana.Mundo);
    }

    // ---------------- subir / bajar
    void Subir()
    {
        Manejando = true;
        var cc = kai.GetComponent<CharacterController>();
        if (cc) cc.enabled = false;
        kai.enabled = false;
        var hab = kai.GetComponent<HabilidadesKai>(); if (hab) hab.enabled = false;
        var anim = kai.GetComponentInChildren<AnimacionSimple>();
        if (anim) { anim.Quieto = true; anim.Agachado = false; }   // sentado: no camina aunque el barco se mueva
        Transform punto = asiento ? asiento : transform;
        kai.transform.SetParent(punto, true);
        kai.transform.localPosition = asiento ? Vector3.zero : Vector3.up * 1f;
        kai.transform.localRotation = Quaternion.identity;
        if (CamaraGTA.Actual) CamaraGTA.Actual.SeguirBarco(transform);
        GameState.Avisar("W/S vela · A/D timón · E bajar · M mapa", 3f);
    }

    void Bajar()
    {
        if (!BuscarTierra(out Vector3 destino, out Quaternion mira))
        {
            if (Time.time > proximoAviso) { GameState.Avisar("No hay tierra cerca para bajarte. Acércate a un muelle o a la orilla.", 2f); proximoAviso = Time.time + 2f; }
            return;
        }
        Manejando = false;
        velActual = 0f; rb.SetVel(Vector3.zero);
        kai.transform.SetParent(null, true);
        kai.transform.SetPositionAndRotation(destino, mira);
        var cc = kai.GetComponent<CharacterController>();
        if (cc) cc.enabled = true;
        kai.enabled = true;
        var hab = kai.GetComponent<HabilidadesKai>(); if (hab) hab.enabled = true;
        var anim = kai.GetComponentInChildren<AnimacionSimple>();
        if (anim) anim.Quieto = false;
        if (CamaraGTA.Actual) CamaraGTA.Actual.SeguirKai();
    }

    // Primero un muelle cercano; si no, la orilla (suelo que no sea agua) alrededor del barco
    bool BuscarTierra(out Vector3 punto, out Quaternion mira)
    {
        mira = Quaternion.LookRotation(Vector3.ProjectOnPlane(transform.forward, Vector3.up));
        Muelle mejor = null; float md = radioBajar + 6f;
        foreach (var m in Muelle.Todos)
        {
            if (!m) continue;
            float d = Vector3.Distance(m.PuntoLlegada.position, transform.position);
            if (d < md) { md = d; mejor = m; }
        }
        if (mejor) { punto = mejor.PuntoLlegada.position + Vector3.up * 0.1f; mira = mejor.PuntoLlegada.rotation; return true; }

        float nivelAgua = transform.position.y;
        for (float r = 3f; r <= radioBajar; r += 1.5f)
            for (int i = 0; i < 16; i++)
            {
                float a = i / 16f * Mathf.PI * 2f;
                Vector3 p = transform.position + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * r + Vector3.up * 30f;
                if (!Physics.Raycast(p, Vector3.down, out RaycastHit hit, 60f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) continue;
                if (hit.collider.GetComponentInParent<BarcoController>() || hit.collider.GetComponentInParent<AguaAnimada>()) continue;
                if (hit.point.y < nivelAgua + 0.15f || hit.normal.y < 0.6f) continue;   // agua o pared
                punto = hit.point + Vector3.up * 0.1f;
                Vector3 haciaTierra = punto - transform.position; haciaTierra.y = 0f;
                if (haciaTierra.sqrMagnitude > 0.01f) mira = Quaternion.LookRotation(haciaTierra);
                return true;
            }
        punto = Vector3.zero;
        return false;
    }

    // ---------------- navegar
    void FixedUpdate()
    {
        float avance = Manejando && !HUD.Pausado ? Input.GetAxisRaw("Vertical") : 0f;
        float giro = Manejando && !HUD.Pausado ? Input.GetAxisRaw("Horizontal") : 0f;
        float max = velocidad * (Tripulacion.Invocado(Tripulacion.Miembro.Mara) ? bonusMara : 1f) * Mejoras.BonoVela;   // vela mejorada en el taller

        float objetivo = avance >= 0f ? avance * max : avance * max * factorReversa;
        velActual = Mathf.MoveTowards(velActual, objetivo, aceleracion * max * Time.fixedDeltaTime);

        // Girar el timon (en reversa el giro se invierte, como en un bote real)
        float sentido = velActual < -0.1f ? -1f : 1f;
        rb.angularVelocity = Vector3.zero; // que los choques no lo dejen girando solo
        rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, giro * velocidadGiro * sentido * Time.fixedDeltaTime, 0f));

        if (Manejando) CorrientesDeIslasBloqueadas();
        Vector3 deseada = transform.forward * velActual + corriente;
        rb.SetVel(Vector3.Lerp(rb.GetVel(), deseada, 0.2f));
        corriente = Vector3.zero; // las corrientes la vuelven a llenar en este paso de fisica
    }

    void CorrientesDeIslasBloqueadas()
    {
        Vector3 p = transform.position;
        foreach (var z in zonas)
        {
            if (!z || z.idIsla == GameState.IslaActual) continue;
            var isla = Archipielago.Buscar(z.idIsla);
            if (isla == null || (isla.Revelada && isla.Cumple && isla.Disponible)) continue;
            Rect r = z.Area;
            Rect grande = new Rect(r.xMin - margenCorriente, r.yMin - margenCorriente, r.width + margenCorriente * 2f, r.height + margenCorriente * 2f);
            if (!grande.Contains(new Vector2(p.x, p.z))) continue;
            Vector3 fuera = p - z.transform.position; fuera.y = 0f;
            corriente += fuera.normalized * fuerzaCorriente;
            velActual *= 0.97f;
            if (Time.time > proximoAviso)
            {
                proximoAviso = Time.time + 3f;
                string razon = !isla.Revelada ? "la tinta aún no revela esta isla" : !isla.Disponible ? "próximamente" : "te falta " + isla.Falta();
                GameState.Avisar("Fuertes corrientes te alejan de " + (isla.Revelada ? isla.nombre : "una isla desconocida") + ": " + razon + ".", 2.5f);
            }
        }
    }

    void OnCollisionEnter(Collision c)
    {
        // Al chocar con una roca pierde velocidad
        velActual *= 0.3f;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, radioSubir);
        if (asiento) { Gizmos.color = Color.yellow; Gizmos.DrawWireSphere(asiento.position, 0.3f); }
    }
}
