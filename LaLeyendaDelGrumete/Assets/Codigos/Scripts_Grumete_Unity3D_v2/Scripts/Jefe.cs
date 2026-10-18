using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Convierte un Rival en JEFE (Fase 4). Ponlo en el mismo objeto que el Rival.
//  - Al entrar en su zona dice su frase y aparece la barra grande abajo.
//  - Cada pocos segundos hace un ATAQUE ESPECIAL con aviso rojo en el suelo (sal de la zona antes de que se llene):
//      Golpe de area  : circulo alrededor del jefe.
//      Embestida      : franja recta hacia Kai y el jefe carga.
//      Bombardeo      : (solo fase 2) circulos que caen donde esta Kai.
//  - FASE 2 (al bajar de cierta vida): se enfurece, va mas rapido, se pone rojo y aparecen sus secuaces.
//  - Resiste el control: aturdir y empujar le hacen poco y el garfio no lo arrastra.
//  - Al morir: experiencia, botin, arma (si el Rival tiene "Arma Que Suelta") y el evento "Al Morir"
//    (por ejemplo abrir una reja o activar el siguiente objetivo).
// Si Kai cae, el combate se reinicia (vida completa y fase 1).
[RequireComponent(typeof(Rival))]
public class Jefe : MonoBehaviour
{
    [Header("Frases")]
    public string fraseInicio = "¿Un grumete en MI faro? ¡Te voy a echar al mar!";
    public string fraseFuria = "¡Ahora sí me hiciste enojar!";
    public string fraseDerrota = "Imposible... un simple grumete...";

    [Header("Ritmo")]
    [Range(0.1f, 0.9f)] public float vidaFase2 = 0.5f;   // fraccion de vida para la fase 2
    public float esperaEspecial = 6f;
    public float esperaEspecialFase2 = 4f;
    [Range(0f, 1f)] public float resistencia = 0.7f;      // cuanto resiste aturdir y empujar

    [Header("Golpe de area")]
    public bool usarGolpeArea = true;
    public float radioArea = 4.5f;
    public float avisoArea = 1.3f;
    public int danoArea = 35;

    [Header("Embestida")]
    public bool usarEmbestida = true;
    public float distanciaEmbestida = 11f;
    public float avisoEmbestida = 0.9f;
    public int danoEmbestida = 30;

    [Header("Bombardeo (solo fase 2)")]
    public bool usarBombardeo = true;
    public int bombas = 3;
    public float radioBomba = 2.5f;
    public float avisoBomba = 1.1f;
    public int danoBomba = 25;

    [Header("Fase 2")]
    public float multVelocidad = 1.35f;
    [Tooltip("Enemigos de la escena (desactivados) que aparecen al entrar en fase 2")]
    public GameObject[] secuaces;

    [Header("Musica")]
    [Tooltip("Nombre del sonido en Resources/Sonidos que suena durante la pelea (vacio = sin musica)")]
    public string musicaPelea = "musica_jefe";
    [Tooltip("En la fase 2 la musica va un poco mas rapida")]
    public float tonoFase2 = 1.08f;

    [Header("Al morir")]
    public UnityEngine.Events.UnityEvent alMorir;

    public static Jefe Actual;
    public int Fase { get; private set; } = 1;
    public Rival Rival => r;

    Rival r;
    PlayerController kai;
    AnimacionSimple anim;
    float proximoEspecial, velBase, esperaBase;
    bool presentado, terminado, enEspecial, conMusica;
    static readonly Color Rojo = new Color(1f, 0.15f, 0.1f);

    // un jefe nunca es el rival del duelo (si lo duplicaste de Tomas, esto evita que desaparezca)
    void Awake() { GetComponent<Rival>().esDuelo = false; }

    void Start()
    {
        r = GetComponent<Rival>();
        r.esJefe = true;
        r.resistenciaControl = resistencia;
        kai = r.jugador ? r.jugador : FindFirstObjectByType<PlayerController>();
        anim = GetComponentInChildren<AnimacionSimple>();
        velBase = r.velocidad; esperaBase = r.esperaEntreAtaques;
        foreach (var s in secuaces) if (s) s.SetActive(false);
    }

    void OnDisable() { if (Actual == this) Actual = null; Musica(false); }

    // prende o apaga la musica de pelea solo cuando cambia (asi varios jefes en la escena no se pisan)
    void Musica(bool si)
    {
        if (string.IsNullOrEmpty(musicaPelea) || si == conMusica) return;
        conMusica = si;
        Sonidos.Musica(si ? musicaPelea : null);
        Sonidos.TonoMusica = 1f;
    }

    void Update()
    {
        if (terminado || !r) return;
        if (r.Muerto) { Derrota(); return; }
        if (!kai) return;

        // Kai cayo: se cancela todo y el jefe vuelve a la fase 1 (el Rival se cura solo)
        if (kai.EstaMuerto)
        {
            if (enEspecial) { StopAllCoroutines(); enEspecial = false; r.Ocupado = false; r.giroBloqueado = false; }
            if (Fase == 2) VolverFase1();
            presentado = false;
            if (Actual == this) Actual = null;   // Kai cayo: la barra del jefe se quita (como en Dark Souls)
            Musica(false);
            return;
        }

        // una vez empezada la pelea la barra queda fija, aunque Kai salga un momento de la zona
        if (presentado && Plano(kai.transform.position - transform.position).magnitude > 60f) { presentado = false; if (Actual == this) Actual = null; Musica(false); }
        if (Rival.EnCombate != r) return;
        Actual = this;
        if (!presentado)
        {
            presentado = true;
            Musica(true);
            GameState.Hablar(r.nombre, fraseInicio, 3.5f);
            proximoEspecial = Time.time + 3.5f;
        }
        if (Fase == 1 && r.Vida <= r.vidaMaxima * vidaFase2) EntrarFase2();
        if (!enEspecial && Time.time >= proximoEspecial) StartCoroutine(Especial());
    }

    // ================================================================ fases
    void EntrarFase2()
    {
        Fase = 2;
        if (conMusica) Sonidos.TonoMusica = tonoFase2;
        r.velocidad = velBase * multVelocidad;
        r.esperaEntreAtaques = esperaBase * 0.7f;
        Colorear.Poner(gameObject, new Color(1f, 0.55f, 0.5f));
        GameState.Hablar(r.nombre, fraseFuria, 2.5f);
        Efectos.Anillo(transform.position, 5f, Rojo, 0.8f);
        Efectos.Sacudir(0.3f, 0.5f);
        foreach (var s in secuaces)
            if (s && !s.activeSelf) { s.SetActive(true); Efectos.Anillo(s.transform.position, 2f, Rojo, 0.6f); }
        proximoEspecial = Time.time + 1.5f;
    }

    void VolverFase1()
    {
        Fase = 1;
        r.velocidad = velBase; r.esperaEntreAtaques = esperaBase;
        Colorear.Restaurar(gameObject);
        presentado = false;   // al volver, repite la frase de inicio
    }

    void Derrota()
    {
        terminado = true;
        StopAllCoroutines(); r.Ocupado = false; r.giroBloqueado = false;
        Colorear.Restaurar(gameObject);
        GameState.Hablar(r.nombre, fraseDerrota, 3f);
        if (conMusica) Sonidos.Tocar("victoria", null, 1f, 0f, 1f);
        Musica(false);
        Efectos.Anillo(transform.position, 6f, new Color(1f, 0.85f, 0.3f), 1f);
        Efectos.Sacudir(0.35f, 0.6f);
        alMorir?.Invoke();
        if (Actual == this) Actual = null;
    }

    // ================================================================ ataques especiales
    IEnumerator Especial()
    {
        enEspecial = true;
        r.Ocupado = true;
        var opciones = new List<int>();
        if (usarGolpeArea) opciones.Add(0);
        if (usarEmbestida) opciones.Add(1);
        if (usarBombardeo && Fase == 2) { opciones.Add(2); opciones.Add(2); }   // en fase 2 bombardea mas seguido
        if (opciones.Count > 0)
        {
            switch (opciones[Random.Range(0, opciones.Count)])
            {
                case 0: yield return GolpeArea(); break;
                case 1: yield return Embestida(); break;
                case 2: yield return Bombardeo(); break;
            }
        }
        r.Ocupado = false;
        enEspecial = false;
        proximoEspecial = Time.time + (Fase == 2 ? esperaEspecialFase2 : esperaEspecial);
    }

    IEnumerator GolpeArea()
    {
        Vector3 c = transform.position;
        float aviso = Fase == 2 ? avisoArea * 0.8f : avisoArea;
        Efectos.AvisoCirculo(c, radioArea, aviso, Rojo);
        yield return new WaitForSeconds(aviso);
        if (anim) anim.Atacar();
        Efectos.Anillo(c, radioArea, new Color(1f, 0.5f, 0.15f), 0.5f);
        Efectos.Sacudir(0.3f, 0.35f);
        if (Plano(kai.transform.position - c).magnitude < radioArea) Daniar(danoArea);
        yield return new WaitForSeconds(0.5f);
    }

    IEnumerator Embestida()
    {
        Vector3 desde = transform.position;
        Vector3 dir = Plano(kai.transform.position - desde).normalized;
        if (dir.sqrMagnitude < 0.01f) dir = transform.forward;
        transform.rotation = Quaternion.LookRotation(dir);
        float largo = distanciaEmbestida;
        // no atravesar paredes ni salir de la zona por una pared
        if (Physics.Raycast(desde + Vector3.up, dir, out RaycastHit hit, largo, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
            && !hit.collider.GetComponentInParent<PlayerController>() && !hit.collider.GetComponentInParent<Rival>())
            largo = Mathf.Max(1f, hit.distance - 0.8f);
        Vector3 hasta = desde + dir * largo;
        Efectos.AvisoLinea(desde, hasta, 1.8f, avisoEmbestida, Rojo);
        yield return new WaitForSeconds(avisoEmbestida);

        bool golpeo = false;
        float dur = 0.35f;
        r.giroBloqueado = true;   // carga en linea recta, no persigue a Kai mientras embiste
        for (float t = 0; t < dur; t += Time.deltaTime)
        {
            transform.position = Vector3.Lerp(desde, hasta, t / dur);
            if (!golpeo && Plano(kai.transform.position - transform.position).magnitude < r.Radio + 1.1f)   // segun el tamano del jefe
            {
                golpeo = true;
                Daniar(danoEmbestida);
                Efectos.Sacudir(0.25f, 0.3f);
            }
            yield return null;
        }
        transform.position = hasta;
        r.giroBloqueado = false;
        if (anim) anim.Atacar();
        yield return new WaitForSeconds(0.6f);
    }

    IEnumerator Bombardeo()
    {
        GameState.Avisar("¡Cuidado, barriles explosivos!", 1.2f);
        for (int i = 0; i < bombas; i++)
        {
            Vector3 p = kai.transform.position + (i == 0 ? Vector3.zero : new Vector3(Random.Range(-2f, 2f), 0f, Random.Range(-2f, 2f)));
            StartCoroutine(Explotar(p));
            yield return new WaitForSeconds(0.45f);
        }
        yield return new WaitForSeconds(avisoBomba);
    }

    IEnumerator Explotar(Vector3 p)
    {
        Efectos.AvisoCirculo(p, radioBomba, avisoBomba, new Color(1f, 0.45f, 0.1f));
        yield return new WaitForSeconds(avisoBomba);
        Efectos.Anillo(p, radioBomba * 1.2f, new Color(1f, 0.6f, 0.2f), 0.4f);
        Efectos.Sacudir(0.2f, 0.2f);
        if (!kai.EstaMuerto && Plano(kai.transform.position - p).magnitude < radioBomba) Daniar(danoBomba);
    }

    // los especiales no se bloquean del todo: bloquear (con aguante) quita dos tercios del dano
    void Daniar(int dano)
    {
        if (kai.EstaMuerto) return;
        if (kai.bloqueando && GameState.Aguante >= 30f) { kai.Gastar(30f); dano = Mathf.Max(1, dano / 3); }
        GameState.DaniarKai(dano, kai.transform.position);
    }

    static Vector3 Plano(Vector3 v) { v.y = 0f; return v; }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.2f, 0.1f);
        Gizmos.DrawWireSphere(transform.position, radioArea);
    }
}
