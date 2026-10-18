using System.Collections;
using UnityEngine;

// Enemigo cuerpo a cuerpo: se acerca y ataca apenas esta cerca (sin aviso en rojo).
// El dano se aplica en la mitad de la animacion de ataque.
// Al morir hace su animacion de muerte, suelta una bolsa de botin (F para recoger) y desaparece.
// "Es Duelo" marcado = el rival de la prueba de combate de Barbagris. Desmarcalo para piratas normales.
// Las habilidades de Kai lo pueden aturdir, empujar, atraer y hacer sangrar.
public class Rival : MonoBehaviour
{
    public PlayerController jugador;
    public Transform centroZona;
    public float radioZona = 7f;
    [Tooltip("Una vez que te ve, te sigue hasta esta distancia del centro de su zona. 0 = automatico (jefe: 3 veces Radio Zona, normal: 1.5 veces)")]
    public float radioPersecucion = 0f;
    public float velocidad = 2f;
    public float distanciaGolpe = 1.8f;
    public float esperaEntreAtaques = 1.2f;
    [Range(0f, 1f)] public float momentoImpacto = 0.5f; // fraccion de la animacion de ataque
    public int danoGolpe = 35;
    public int danoBloqueado = 5;
    public int vidaMaxima = 100;
    public string nombre = "Tomás, el chico de la aldea";
    [Tooltip("Marcado: barra grande abajo de la pantalla (jefe). Desmarcado: barra pequena sobre la cabeza")]
    public bool esJefe = false;
    public float alturaBarra = 1.5f;   // altura de la barra sobre la cabeza (enemigos normales)
    public float costoBloqueo = 25f;   // aguante que gasta Kai al bloquear un golpe
    [Tooltip("Marcado: es el rival de la prueba de combate (cuenta la prueba y no vuelve a aparecer)")]
    public bool esDuelo = true;

    [Tooltip("Experiencia que gana Kai al vencerlo (un jefe puede dar 200 o mas)")]
    public int experiencia = Progreso.XpEnemigo;
    [Tooltip("Arma que deja al morir si Kai aun no la tiene (Punos = ninguna). Ej: el capitan del Faro Roto suelta el Hacha")]
    public Equipo.IdArma armaQueSuelta = Equipo.IdArma.Punos;

    [Header("Botin al morir")]
    public int doblonesMin = 10;
    public int doblonesMax = 20;
    [Range(0f, 1f)] public float probPocion = 0.4f;    // probabilidad de soltar una pocion (casi siempre grog, a veces ron)
    [Range(0f, 1f)] public float probRon = 0.2f;       // de esas pociones, cuantas son ron en vez de grog
    [Range(0f, 1f)] public float probBomba = 0.15f;    // probabilidad de soltar una bomba de humo
    public int maderaMax = 1;
    public int chatarraMax = 1;

    // El rival con el que Kai esta peleando ahora (para la barra de enemigo del HUD)
    public static Rival EnCombate;
    public static readonly System.Collections.Generic.List<Rival> Todos = new System.Collections.Generic.List<Rival>();
    void OnEnable() { if (!Todos.Contains(this)) Todos.Add(this); }
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void LimpiarLista() { Todos.Clear(); }
    public int Vida => vida;
    // lo usa el componente Jefe: mientras hace un ataque especial el rival no camina ni ataca normal
    public bool Ocupado { get; set; }
    [HideInInspector] public float resistenciaControl;   // 0 = normal, 0.7 = jefe (aturdir y empujar le hacen poco)
    public bool Muerto => muerto;
    public float Radio => radioPropio;   // medio ancho del cuerpo (collider)
    public float UltimoDano { get; private set; } = -100f;   // momento del ultimo golpe recibido
    public float esperaAlDesaparecer = 1f;
    [Tooltip("Pega al enemigo al suelo (cae si esta en el aire y sube las pendientes del Terrain)")]
    public bool usarGravedad = true;
    [Tooltip("Gira siempre hacia Kai cuando esta cerca (tambien durante los ataques especiales del jefe)")]
    public bool mirarSiempre = true;
    public float velocidadGiro = 600f;   // grados por segundo
    [HideInInspector] public bool giroBloqueado;   // el Jefe lo pone en true durante la embestida

    [Header("Diagnostico (solo lectura: mira esto en Play si el enemigo no se mueve)")]
    [SerializeField] string estado = "";
    public float gravedad = -20f;

    int vida;
    bool atacando, golpeAplicado, muerto, jugadorEstabaMuerto, enCombate;
    float t, proximoAtaque, aturdidoHasta;
    float pies, vy, radioPropio, radioKai;   // distancia del pivote a la planta de los pies, y velocidad de caida
    bool desplazando;
    Vector3 inicio;
    Quaternion rotInicio;
    Renderer[] rends;
    AnimacionSimple anim;
    Collider col;

    void Start()
    {
        if (!jugador) jugador = FindFirstObjectByType<PlayerController>();   // por si quedo vacio el campo Jugador
        inicio = transform.position;
        rotInicio = transform.rotation;
        vida = vidaMaxima;
        rends = GetComponentsInChildren<Renderer>();
        anim = GetComponentInChildren<AnimacionSimple>();
        col = GetComponent<Collider>();
        if (!col) col = GetComponentInChildren<Collider>();
        // los pies se miden con el MODELO (no con el collider): asi no queda flotando si el collider es mas grande
        float suelaModelo = float.MaxValue;
        foreach (var rd in rends) if (rd && !(rd is ParticleSystemRenderer) && rd.enabled) suelaModelo = Mathf.Min(suelaModelo, rd.bounds.min.y);
        if (suelaModelo < float.MaxValue) pies = Mathf.Max(0f, transform.position.y - suelaModelo);
        else pies = col ? Mathf.Max(0f, transform.position.y - col.bounds.min.y) : 0f;
        if (!col) Debug.LogWarning("[Rival] " + name + " no tiene Collider: ponle un Capsule Collider para que los golpes lo encuentren mejor.", this);
        // grosor de los cuerpos: las distancias se miden de borde a borde (un jefe grande no se queda empujando a Kai)
        radioPropio = col ? Mathf.Max(col.bounds.extents.x, col.bounds.extents.z) : 0.4f;
        var ccKai = jugador ? jugador.GetComponent<CharacterController>() : null;
        radioKai = ccKai ? ccKai.radius * Mathf.Max(jugador.transform.lossyScale.x, jugador.transform.lossyScale.z) : 0.4f;

        // Si ya se gano el duelo (por ejemplo al volver del Faro Roto), no aparece
        if (esDuelo && GameState.Combate == 1) gameObject.SetActive(false);
    }

    float ocupadoDesde = -1f;

    void Update()
    {
        if (muerto) { estado = "Muerto"; return; }
        if (!jugador) { estado = "SIN JUGADOR: arrastra a Kai al campo Jugador"; return; }

        // seguro: si un ataque especial lo dejo "ocupado" mas de 6 s, se libera solo
        if (Ocupado) { if (ocupadoDesde < 0f) ocupadoDesde = Time.time; else if (Time.time - ocupadoDesde > 6f) { Ocupado = false; giroBloqueado = false; } }
        else ocupadoDesde = -1f;

        // Mientras Kai esta muerto el rival espera; cuando Kai revive, el duelo se reinicia
        if (jugador.EstaMuerto) { jugadorEstabaMuerto = true; atacando = false; if (EnCombate == this) EnCombate = null; return; }
        if (jugadorEstabaMuerto) { Reiniciar(); jugadorEstabaMuerto = false; }
        if (Time.time < aturdidoHasta || desplazando || Ocupado)   // aturdido, volando por un golpe o en un ataque especial
        {
            atacando = false;
            estado = Ocupado ? "Ataque especial" : desplazando ? "Empujado" : "Aturdido";
            return;
        }

        Vector3 d = jugador.transform.position - transform.position; d.y = 0f;
        float dist = Mathf.Max(0f, d.magnitude - radioPropio - radioKai);   // distancia entre los bordes de los dos cuerpos

        Vector3 centro = centroZona ? centroZona.position : inicio;
        Vector3 aCentro = jugador.transform.position - centro; aCentro.y = 0f;
        // entra en combate al pisar su zona y no te suelta hasta que te alejes mucho (radio de persecucion)
        float radioSeguir = radioPersecucion > 0f ? radioPersecucion : radioZona * (esJefe ? 3f : 1.5f);
        if (!enCombate && aCentro.magnitude < radioZona) enCombate = true;
        bool loAtacaron = Time.time - UltimoDano < 6f;   // si le pegan (aunque sea de lejos) se voltea y pelea
        if (loAtacaron) enCombate = true;
        if (enCombate && aCentro.magnitude > radioSeguir && !loAtacaron) enCombate = false;
        bool enZona = enCombate;

        if (dist > 0.01f && !mirarSiempre) transform.rotation = Quaternion.LookRotation(d);

        if (atacando)
        {
            t += Time.deltaTime;
            float duracion = anim ? anim.DuracionAtaque : 0.6f;

            if (!golpeAplicado && t >= duracion * momentoImpacto)
            {
                golpeAplicado = true;
                if (dist < distanciaGolpe + 0.5f)
                {
                    bool bloqueo = jugador.bloqueando && GameState.Aguante >= costoBloqueo;
                    if (jugador.bloqueando && !bloqueo) GameState.Avisar("¡Guardia rota! Te quedaste sin estamina.", 1.2f);
                    if (bloqueo)
                    {
                        jugador.Gastar(costoBloqueo);
                        Sonidos.Tocar("bloqueo", jugador.transform.position);
                        Vector3 frente = jugador.transform.position + Vector3.up * 1.1f + jugador.transform.forward * 0.5f;
                        Efectos.Chispas(frente, jugador.transform.forward + Vector3.up * 0.5f, new Color(1f, 0.9f, 0.55f), 14, 7f);
                        Efectos.Destello(frente, new Color(1f, 0.85f, 0.5f), 3f, 4f, 0.1f);
                    }
                    GameState.DaniarKai(bloqueo ? danoBloqueado : danoGolpe, jugador.transform.position);
                }
            }

            if (t >= duracion)
            {
                atacando = false;
                proximoAtaque = Time.time + esperaEntreAtaques;
            }
            estado = "Atacando";
            return;
        }

        EnCombate = enZona ? this : (EnCombate == this ? null : EnCombate);
        if (!enZona)
        {
            // Kai se escapo: vuelve caminando a su sitio
            Vector3 vuelta = inicio - transform.position; vuelta.y = 0f;
            if (vuelta.magnitude > 0.4f && velocidad > 0f)
            {
                transform.position += vuelta.normalized * Mathf.Min(velocidad * Time.deltaTime, vuelta.magnitude);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(vuelta), velocidadGiro * Time.deltaTime);
                estado = "Volviendo a su zona";
            }
            else estado = "Esperando: Kai esta fuera de su zona (" + aCentro.magnitude.ToString("0") + " m del centro, radio " + radioZona + ")";
            return;
        }
        estado = dist > distanciaGolpe ? (velocidad > 0f ? "Persiguiendo a Kai (" + dist.ToString("0.0") + " m)" : "VELOCIDAD EN 0: no puede caminar")
                                      : "Cerca de Kai: va a atacar";

        if (dist > distanciaGolpe)
        {
            // camina hasta quedar a distancia de golpe, sin meterse dentro de Kai
            float paso = Mathf.Min(velocidad * Time.deltaTime, dist - distanciaGolpe * 0.7f);
            if (paso > 0f) transform.position += d.normalized * paso;
        }
        else if (Time.time >= proximoAtaque && (!mirarSiempre || Vector3.Angle(transform.forward, d) < 50f))   // ataca cuando ya lo esta mirando
        {
            atacando = true;
            golpeAplicado = false;
            t = 0f;
            if (anim) anim.Atacar();
        }
    }

    void Reiniciar()
    {
        vida = vidaMaxima;
        enCombate = false;
        atacando = false;
        transform.SetPositionAndRotation(inicio, rotInicio);
        proximoAtaque = Time.time + 1f;
    }

    public void RecibirGolpe(int dano, bool critico = false)
    {
        if (muerto) return;
        if (esDuelo && GameState.Combate == 1) { estado = "Duelo ya ganado: no recibe dano"; return; }
        vida = Mathf.Max(0, vida - dano);
        UltimoDano = Time.time;
        // sonido del golpe segun el arma de Kai, y el quejido del enemigo
        bool filo = Equipo.arma == (int)Equipo.IdArma.Sable || Equipo.arma == (int)Equipo.IdArma.Hacha;
        Sonidos.Tocar(filo ? "espada_golpe" : "punio_golpe", transform.position, critico ? 1f : 0.85f);
        Sonidos.Tocar("enemigo_dolor", transform.position, esJefe ? 0.9f : 0.6f, 0.1f, 0.4f);
        EfectoGolpe(filo, critico);
        HUD.Numero(transform.position + Vector3.up * 1.3f, critico ? dano + "!" : dano.ToString(),
                   critico ? new Color(1f, 0.55f, 0.15f) : new Color(1f, 0.92f, 0.35f), critico ? 1.5f : 1.1f);

        if (vida <= 0)
        {
            StartCoroutine(Morir());
            return;
        }

        if (anim) anim.Golpeado();
        StartCoroutine(Parpadeo());
    }

    // centro del cuerpo (segun el modelo) para poner ahi las chispas
    Vector3 Centro()
    {
        if (rends != null)
            foreach (var r in rends) if (r && r.enabled) return r.bounds.center;
        return transform.position + Vector3.up * 1f;
    }

    // chispas del lado por donde llego el golpe, congelado corto y, si es critico, fogonazo
    void EfectoGolpe(bool filo, bool critico)
    {
        Vector3 haciaKai = jugador ? jugador.transform.position - transform.position : transform.forward;
        haciaKai.y = 0f;
        if (haciaKai.sqrMagnitude < 0.01f) haciaKai = transform.forward;
        haciaKai.Normalize();
        Vector3 punto = Centro() + haciaKai * Radio * 0.8f;
        Color color = filo ? new Color(1f, 0.82f, 0.4f) : new Color(1f, 0.95f, 0.85f);
        Efectos.Chispas(punto, haciaKai + Vector3.up * 0.4f, color, critico ? 20 : filo ? 11 : 6, critico ? 8f : 6f);
        if (!filo) Efectos.Polvo(punto, new Color(0.9f, 0.85f, 0.75f, 0.45f), 3, 0.5f);
        if (critico)
        {
            Efectos.Destello(punto, new Color(1f, 0.6f, 0.2f), 5f, 5f, 0.15f);
            Efectos.Sacudir(esJefe ? 0.16f : 0.1f, 0.12f);
        }
        Efectos.Impacto(critico ? 0.08f : esJefe ? 0.045f : 0.035f);
    }

    IEnumerator Morir()
    {
        muerto = true;
        if (EnCombate == this) EnCombate = null;
        if (col) col.enabled = false;
        if (esDuelo) GameState.CompletarPrueba("combate");
        Progreso.Ganar(experiencia);
        if (armaQueSuelta != Equipo.IdArma.Punos && !Equipo.Tiene(armaQueSuelta))
            ArmaEnSuelo.Crear(transform.position - transform.forward * 0.6f, armaQueSuelta);
        int pocion = -1;
        if (Random.value < probPocion)
            pocion = System.Array.FindIndex(Equipo.Pociones, p => p.icono == (Random.value < probRon ? "ron" : "grog"));
        Botin.Crear(transform.position + transform.forward * 0.6f, Random.Range(doblonesMin, doblonesMax + 1),
                    Random.Range(0, maderaMax + 1), Random.Range(0, chatarraMax + 1),
                    pocion, 1, Random.value < probBomba ? 1 : 0);

        float duracion = anim ? anim.Morir() : 0f;
        // cae al suelo: golpe sordo y polvo (a mitad de la animacion de morir)
        yield return new WaitForSeconds(duracion * 0.55f);
        Vector3 suelo = transform.position + Vector3.down * pies;
        Sonidos.Tocar("enemigo_cae", suelo, esJefe ? 1f : 0.8f);
        Efectos.Polvo(suelo, new Color(0.78f, 0.7f, 0.58f, 0.55f), esJefe ? 14 : 8, esJefe ? 1.6f : 1f);
        yield return new WaitForSeconds(duracion * 0.45f + esperaAlDesaparecer);
        // se desvanece hundiendose un poco en vez de desaparecer de golpe
        Efectos.Polvo(suelo, new Color(0.7f, 0.66f, 0.6f, 0.4f), 6, 0.8f);
        Vector3 desde = transform.position;
        usarGravedad = false;
        for (float t = 0f; t < 0.5f; t += Time.deltaTime)
        {
            transform.position = desde + Vector3.down * (t / 0.5f) * 0.6f;
            yield return null;
        }
        gameObject.SetActive(false);
    }

    void OnDisable() { if (EnCombate == this) EnCombate = null; Todos.Remove(this); }

    // ================================================================ mirar a Kai
    void MirarAKai()
    {
        if (!mirarSiempre || giroBloqueado || desplazando || !jugador || jugador.EstaMuerto || !enCombate) return;   // volviendo a su sitio mira hacia donde camina
        Vector3 d = jugador.transform.position - transform.position; d.y = 0f;
        if (d.sqrMagnitude < 0.01f || d.magnitude > radioZona * 2f + 6f) return;   // lejos: no lo sigue con la mirada
        Quaternion objetivo = Quaternion.LookRotation(d);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, objetivo, velocidadGiro * Time.deltaTime);
    }

    // ================================================================ gravedad
    // Despues de moverse (caminar, empujones, embestidas del jefe) busca el suelo debajo:
    // si esta en el aire cae, si el suelo sube (colina) se pega a el.
    void LateUpdate()
    {
        if (muerto) return;
        MirarAKai();
        if (!usarGravedad) return;
        Vector3 p = transform.position;
        float alto = pies + 1.5f;
        float sueloY = float.NegativeInfinity;
        foreach (var h in Physics.RaycastAll(p + Vector3.up * alto, Vector3.down, alto + 60f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (h.collider.transform.IsChildOf(transform) || h.collider.GetComponentInParent<PlayerController>() || h.collider.GetComponentInParent<Rival>()) continue;
            if (h.point.y > sueloY) sueloY = h.point.y;   // el mas alto debajo de el (no atraviesa puentes ni muelles)
        }
        if (float.IsNegativeInfinity(sueloY)) return;   // nada debajo (por ejemplo el mar sin collider): se queda como esta
        float objetivo = sueloY + pies;
        if (p.y > objetivo + 0.05f)
        {
            vy += gravedad * Time.deltaTime;
            p.y = Mathf.Max(objetivo, p.y + vy * Time.deltaTime);
        }
        else { p.y = objetivo; vy = 0f; }
        transform.position = p;
    }

    // ================================================================ efectos de las habilidades de Kai
    public void Aturdir(float segundos)
    {
        segundos *= 1f - resistenciaControl;
        if (muerto || segundos <= 0.1f) return;
        aturdidoHasta = Mathf.Max(aturdidoHasta, Time.time + segundos);
        atacando = false;
        HUD.Numero(transform.position + Vector3.up * 1.9f, "¡Aturdido!", new Color(0.55f, 0.8f, 1f), 0.8f);
    }

    // lo manda a volar alejandolo de 'desde'
    public void Empujar(Vector3 desde, float distancia)
    {
        if (muerto) return;
        distancia *= 1f - resistenciaControl;
        if (distancia < 0.3f) return;
        Vector3 d = transform.position - desde; d.y = 0f;
        if (d.sqrMagnitude < 0.01f) d = -transform.forward;
        StartCoroutine(Deslizar(transform.position + d.normalized * distancia, 0.25f));
    }

    // el garfio lo trae hasta 'punto'
    public void AtraerHacia(Vector3 punto)
    {
        if (muerto) return;
        if (resistenciaControl >= 0.5f) { HUD.Numero(transform.position + Vector3.up * 2f, "¡Muy pesado!", new Color(0.8f, 0.8f, 0.8f), 0.7f); return; }   // a un jefe no se le arrastra
        punto.y = transform.position.y;
        StartCoroutine(Deslizar(punto, 0.22f));
    }

    IEnumerator Deslizar(Vector3 destino, float dur)
    {
        desplazando = true; atacando = false;
        Vector3 desde = transform.position;
        destino.y = desde.y;
        // no atravesar paredes: se detiene antes de chocar
        Vector3 dir = destino - desde;
        if (dir.magnitude > 0.05f && Physics.Raycast(desde + Vector3.up * 0.8f, dir.normalized, out RaycastHit hit, dir.magnitude, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
            && !hit.collider.GetComponentInParent<PlayerController>() && hit.collider.gameObject != gameObject)
            destino = desde + dir.normalized * Mathf.Max(0f, hit.distance - 0.5f);
        for (float k = 0; k < dur; k += Time.deltaTime)
        {
            transform.position = Vector3.Lerp(desde, destino, 1f - (1f - k / dur) * (1f - k / dur));
            yield return null;
        }
        transform.position = destino;
        desplazando = false;
    }

    // dano por segundo (Herida abierta del garfio)
    public void Sangrar(int danoPorSegundo, int segundos) { if (!muerto) StartCoroutine(Sangrado(danoPorSegundo, segundos)); }

    IEnumerator Sangrado(int dano, int veces)
    {
        for (int i = 0; i < veces && !muerto; i++)
        {
            yield return new WaitForSeconds(1f);
            if (muerto) yield break;
            vida = Mathf.Max(0, vida - dano);
            UltimoDano = Time.time;
            HUD.Numero(transform.position + Vector3.up * 1.3f, dano.ToString(), new Color(0.9f, 0.15f, 0.15f), 0.9f);
            if (vida <= 0) { StartCoroutine(Morir()); yield break; }
        }
    }

    IEnumerator Parpadeo()
    {
        for (int i = 0; i < 3; i++)
        {
            foreach (var r in rends) r.enabled = false;
            yield return new WaitForSeconds(0.05f);
            foreach (var r in rends) r.enabled = true;
            yield return new WaitForSeconds(0.05f);
        }
    }
}
