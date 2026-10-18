using UnityEngine;

// Kai en 3D: WASD/flechas moverse, Shift correr, Espacio saltar, C agacharse (junto a barril = oculto),
// J o clic izquierdo atacar, K o clic derecho bloquear.
// 1/2/3 cambiar arma / pocion / objeto, Q usar pocion, R usar objeto (ver Equipo.cs).
// El objeto raiz esta a la altura del CENTRO del personaje (Y = 1 sobre el suelo).
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public float velocidad = 6f;
    public float velocidadCorrer = 9f;
    public float esperaAtaque = 0.5f;
    public float tiempoMuerte = 2.5f;
    [Tooltip("Si esta desmarcado, al agacharse (C) Kai se queda quieto")]
    public bool puedeMoverseAgachado = false;
    [Header("Suavidad del movimiento")]
    [Tooltip("Que tan rapido arranca (m/s por segundo). Mas bajo = arranque mas suave")]
    public float aceleracion = 28f;
    [Tooltip("Que tan rapido frena (m/s por segundo)")]
    public float frenado = 34f;
    [Tooltip("Grados por segundo que gira Kai. Mas bajo = giros mas lentos y naturales")]
    public float velocidadGiro = 600f;
    [Header("Mar")]
    [Tooltip("Cuanto se pueden meter los pies bajo el agua antes de que cuente como tocar el mar (0 = apenas lo toca)")]
    public float toleranciaAgua = 0.05f;
    [Tooltip("Vida que pierde cada vez que toca el mar (luego vuelve a la orilla)")]
    public int danoMar = 15;
    public float fuerzaSalto = 8f;
    public float gravedad = -20f;

    [Header("Pisadas")]
    [Tooltip("Metros entre paso y paso caminando (corriendo se alarga un poco)")]
    public float largoPaso = 1.6f;
    public float volumenPasos = 0.35f;

    [Header("Aguante (estamina)")]
    public float gastoCorrer = 22f;       // por segundo
    public float gastoSalto = 12f;
    public float recuperacion = 28f;      // por segundo
    public float esperaRecuperar = 0.7f;  // segundos sin gastar antes de recuperar
    public Transform spawnInicio;
    public Transform spawnFaro;

    [HideInInspector] public bool oculto, bloqueando, agachado;
    public bool EstaMuerto { get; private set; }

    CharacterController cc;
    Vector3 velPlana;   // velocidad horizontal suavizada
    Vector3 ultimaOrilla; bool hayOrilla; float proximaOrilla, pisoAgua = -10f, graciaAgua;
    void OnEnable() { velPlana = Vector3.zero; }   // al bajar del barco arranca quieto
    float vy;
    int estadoVisual = -1;
    Transform ultimoSpawn;
    AnimacionSimple anim;
    float proximoAtaque, ultimoGasto, humoHasta;
    HabilidadesKai hab;
    float trechoPaso, tiempoEnAire, caidaMax;   // pisadas y aterrizajes
    bool ladoTajo;                              // el tajo alterna de derecha a izquierda

    void Start()
    {
        cc = GetComponent<CharacterController>();
        anim = GetComponentInChildren<AnimacionSimple>();
        GameState.Kai = transform;
        hab = GetComponent<HabilidadesKai>();
        if (!hab) hab = gameObject.AddComponent<HabilidadesKai>();   // barra de habilidades 1-4

        Reaparecer(PuntoInicial());
        Mar.Buscar();
        if (GameState.IslaActual != 1)
            GameState.Avisar("Llegaste a " + (Archipielago.Buscar(GameState.IslaActual)?.nombre ?? "una isla nueva"), 4f);
        GameState.LlegandoEnBarco = false;
        if (!GameState.IslaPisada(GameState.IslaActual))   // primera vez en esta isla: experiencia
        {
            GameState.MarcarPisada(GameState.IslaActual);
            if (GameState.IslaActual != 1) Progreso.Ganar(Progreso.XpIslaNueva);
        }
    }

    void Update()
    {
        if (EstaMuerto || HUD.Pausado) return;
        if (GameState.VidaKai <= 0) { StartCoroutine(Morir()); return; }

        float h = Input.GetAxisRaw("Horizontal");
        float vIn = Input.GetAxisRaw("Vertical");
        Vector3 entrada = new Vector3(h, 0f, vIn);

        // Movimiento relativo a la camara (W = hacia donde mira la camara)
        Camera cam = Camera.main;
        if (cam)
        {
            Vector3 f = cam.transform.forward; f.y = 0f; f.Normalize();
            Vector3 r = cam.transform.right; r.y = 0f; r.Normalize();
            entrada = f * vIn + r * h;
        }
        if (entrada.sqrMagnitude > 1f) entrada.Normalize();

        agachado = Input.GetKey(KeyCode.C) || Input.GetKey(KeyCode.LeftControl);
        bloqueando = Input.GetKey(KeyCode.K) || Input.GetMouseButton(1);
        oculto = (agachado && CercaDeBarril()) || Time.time < humoHasta;
        if (anim) anim.Agachado = agachado;

        bool corriendo = Input.GetKey(KeyCode.LeftShift) && !agachado && GameState.Aguante > 1f && entrada.sqrMagnitude > 0.01f;
        if (corriendo) Gastar(gastoCorrer * Time.deltaTime);
        float v = bloqueando ? 0f
                : agachado ? (puedeMoverseAgachado ? velocidad * 0.4f : 0f)
                : (corriendo ? velocidadCorrer : velocidad) * Tripulacion.BonoPie;   // Mara invocada: +15%

        bool enHabilidad = hab && hab.Ocupado;   // estocada / abordaje: la habilidad mueve a Kai
        if (enHabilidad) { v = 0f; entrada = Vector3.zero; }

        var camGTA = CamaraGTA.Actual;
        bool primeraPersona = camGTA && camGTA.EnPrimeraPersona;
        Vector3 deseada = entrada * v;
        if (!primeraPersona && entrada.sqrMagnitude > 0.01f && v > 0f)
        {
            // gira de forma pareja hacia donde quiere ir (no de golpe)
            Quaternion meta = Quaternion.LookRotation(entrada);
            float angulo = Quaternion.Angle(transform.rotation, meta);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, meta, velocidadGiro * Time.deltaTime);
            // en un giro cerrado (por ejemplo dar media vuelta) frena un poco mientras se voltea
            if (angulo > 100f) deseada *= 0.35f;
            else if (angulo > 45f) deseada *= Mathf.Lerp(1f, 0.6f, (angulo - 45f) / 55f);
        }
        if (primeraPersona) transform.rotation = Quaternion.Euler(0f, camGTA.Yaw, 0f);   // en primera persona mira con la camara

        // la velocidad sube y baja poco a poco (arranca y frena suave, sin tirones)
        float tasa = deseada.sqrMagnitude > velPlana.sqrMagnitude ? aceleracion : frenado;
        if (enHabilidad) velPlana = Vector3.zero;
        else velPlana = Vector3.MoveTowards(velPlana, deseada, tasa * Time.deltaTime);

        if (cc.isGrounded)
        {
            if (vy < 0f) vy = -2f;
            if (Input.GetKeyDown(KeyCode.Space) && !agachado && GameState.Aguante >= gastoSalto)
            {
                vy = fuerzaSalto;
                Gastar(gastoSalto);
            }
        }
        vy += gravedad * Time.deltaTime;

        Vector3 mov = velPlana;
        mov.y = vy;
        cc.Move(mov * Time.deltaTime);
        Pisadas(corriendo);

        if (!enHabilidad && (Input.GetKeyDown(KeyCode.J) || Input.GetMouseButtonDown(0))) Atacar();

        // Equipo rapido (1-4 son ahora las habilidades del arma)
        if (Input.GetKeyDown(KeyCode.Z)) Equipo.SiguienteArma();
        if (Input.GetKeyDown(KeyCode.X)) Equipo.Siguiente(ref Equipo.pocion, Equipo.Pociones.Length);
        if (Input.GetKeyDown(KeyCode.T)) Equipo.Siguiente(ref Equipo.objeto, Equipo.Objetos.Length);
        if (Input.GetKeyDown(KeyCode.Q)) UsarPocion();
        if (Input.GetKeyDown(KeyCode.R)) UsarObjeto();

        // Recuperar aguante
        if (Time.time - ultimoGasto > esperaRecuperar)
            GameState.Aguante = Mathf.Min(100f, GameState.Aguante + recuperacion * (bloqueando ? 0.35f : 1f) * Time.deltaTime);

        // M = abrir el mapa vivo en cualquier momento
        if (Input.GetKeyDown(KeyCode.M)) MapaMundo.Abrir(MapaMundo.Pestana.Isla);

        if (transform.position.y < -5f && ultimoSpawn) Reaparecer(ultimoSpawn);
        RevisarMar();

        ActualizarColor();
    }

    // ================================================================ ahogarse en el mar
    void RevisarMar()
    {
        if (EstaMuerto) return;
        if (!Mar.Existe && Time.time - pisoAgua > 0.2f) return;
        float pies = cc.bounds.min.y;
        float profundidad = Mar.Existe ? Mar.NivelEn(transform.position) - pies : 0f;

        // recuerda la ultima orilla segura (en tierra seca, un poco por encima del agua)
        if (cc.isGrounded && profundidad < -0.15f && Time.time - pisoAgua > 0.3f && Time.time > proximaOrilla)
        {
            ultimaOrilla = transform.position; hayOrilla = true; proximaOrilla = Time.time + 0.3f;
        }

        // apenas toca el agua: se hace dano y vuelve a la orilla (no se puede estar en el mar)
        if (Time.time < graciaAgua) return;
        if (profundidad > toleranciaAgua || Time.time - pisoAgua < 0.1f) TocarAgua();
    }

    // Toca el suelo: si lo que pisa es el agua, ese collider deja de ser solido (asi nunca se para sobre el mar)
    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.normal.y >= 0.5f) { var viga = hit.collider.GetComponent<VigaInestable>(); if (viga) viga.Pisar(); }
        if (!EsAgua(hit.collider)) return;
        pisoAgua = Time.time;
        if (!Mar.Existe) hit.collider.gameObject.AddComponent<Mar>();   // usa este objeto como el mar
        hit.collider.isTrigger = true;
    }

    static bool EsAgua(Collider c)
    {
        if (!c) return false;
        if (c.GetComponentInParent<Mar>()) return true;
        for (var t = c.transform; t; t = t.parent)
        {
            string n = t.name.ToLower();
            if (n.Contains("agua") || n.Contains("water") || n.Contains("ocean") || n.Contains("oceano") || n == "mar" || n.StartsWith("mar ") || n.StartsWith("mar_") || n == "sea") return true;
        }
        return false;
    }

    void TocarAgua()
    {
        float nivel = Mar.Existe ? Mar.NivelEn(transform.position) : transform.position.y;
        Vector3 donde = new Vector3(transform.position.x, nivel, transform.position.z);
        Efectos.Anillo(donde, 1.4f, new Color(0.8f, 0.95f, 1f), 0.45f);   // chapuzon
        Sonidos.Tocar("chapuzon", transform.position);
        GameState.DaniarKai(danoMar, transform.position);
        graciaAgua = Time.time + 0.6f;
        if (GameState.VidaKai <= 0) return;   // sin vida: Update se encarga de la muerte
        if (hayOrilla) ReaparecerEn(ultimaOrilla);
        else Reaparecer(ultimoSpawn ? ultimoSpawn : PuntoInicial());
        GameState.Avisar("¡El mar es peligroso! -" + danoMar + " de vida", 1.5f);
    }

    public void ReaparecerEn(Vector3 p)
    {
        if (!cc) cc = GetComponent<CharacterController>();
        cc.enabled = false;
        transform.position = p + Vector3.up * 0.2f;
        cc.enabled = true;
        vy = 0f; velPlana = Vector3.zero;
    }

    void ActualizarColor()
    {
        int estado = oculto ? 1 : bloqueando ? 2 : 0;
        if (estado == estadoVisual) return;
        estadoVisual = estado;
        if (estado == 1) Colorear.Poner(gameObject, new Color(0.15f, 0.15f, 0.25f));
        else if (estado == 2) Colorear.Poner(gameObject, new Color(0.6f, 0.6f, 1f));
        else Colorear.Restaurar(gameObject);
    }

    bool CercaDeBarril()
    {
        foreach (var col in Physics.OverlapSphere(transform.position, 1.3f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide))
            if (col.GetComponentInParent<Barril>()) return true;
        return false;
    }

    // Gasta aguante y reinicia la espera de recuperacion
    public void Gastar(float cantidad)
    {
        GameState.Aguante = Mathf.Max(0f, GameState.Aguante - cantidad);
        ultimoGasto = Time.time;
    }

    // un paso cada tantos metros recorridos en el suelo; al caer de un salto, golpe y polvo
    void Pisadas(bool corriendo)
    {
        Vector3 pies = transform.position + Vector3.down * 1f;
        if (!cc.isGrounded)
        {
            tiempoEnAire += Time.deltaTime;
            caidaMax = Mathf.Min(caidaMax, vy);
            return;
        }
        if (tiempoEnAire > 0.3f)
        {
            float fuerte = Mathf.InverseLerp(4f, 16f, -caidaMax);
            Sonidos.Tocar("aterrizaje", pies, Mathf.Lerp(0.35f, 0.9f, fuerte), 0.08f, 0.15f);
            if (fuerte > 0.3f) Efectos.Polvo(pies, new Color(0.8f, 0.74f, 0.62f, 0.45f), 6, 0.8f);
            trechoPaso = 0f;
        }
        tiempoEnAire = 0f; caidaMax = 0f;

        float rapidez = new Vector3(velPlana.x, 0f, velPlana.z).magnitude;
        if (rapidez < 0.6f) { trechoPaso = largoPaso * 0.6f; return; }   // quieto: el primer paso suena enseguida
        trechoPaso += rapidez * Time.deltaTime;
        float largo = corriendo ? largoPaso * 1.35f : largoPaso;
        if (trechoPaso < largo) return;
        trechoPaso = 0f;
        float vol = volumenPasos * (corriendo ? 1.4f : agachado ? 0.35f : 1f);   // agachado casi no suena (sigilo)
        Sonidos.Tocar("paso", pies, vol, 0.1f, 0.12f);
        if (corriendo) Efectos.Polvo(pies, new Color(0.8f, 0.74f, 0.62f, 0.25f), 2, 0.35f);
    }

    void Atacar()
    {
        var a = Equipo.ArmaActual;
        if (Time.time < proximoAtaque) return;
        if (ZonaCorona.ProhibeCombate(transform.position)) { proximoAtaque = Time.time + 0.5f; return; }   // islas de la Corona: no se pelea
        if (GameState.Aguante < a.costoAguante * 0.5f) { GameState.Avisar("¡Sin aliento!", 0.8f); return; }
        proximoAtaque = Time.time + a.espera;
        Gastar(a.costoAguante);
        if (anim) anim.Atacar();
        if (a.aDistancia) { if (hab) hab.DisparoBasico(a); return; }   // pistola: dispara en vez de golpear
        bool punos = Equipo.arma == (int)Equipo.IdArma.Punos;
        Sonidos.Tocar("espada_aire", transform.position, punos ? 0.45f : 0.75f);
        // el filo del arma deja un arco (alterna de lado en cada golpe)
        Vector3 pecho = transform.position + Vector3.up * 0.15f;
        if (punos) Efectos.Tajo(pecho, transform.forward, 0.9f, new Color(1f, 0.97f, 0.9f), 60f, 0.14f, ladoTajo);
        else Efectos.Tajo(pecho, transform.forward, Mathf.Max(1.2f, a.alcance * 0.8f), new Color(0.85f, 0.92f, 1f), 150f, 0.24f, ladoTajo);
        ladoTajo = !ladoTajo;

        // golpea al enemigo mas cercano que este delante (no depende de que el collider este bien puesto)
        Rival mejor = null; float mejorD = float.MaxValue;
        foreach (var r in Rival.Todos)
        {
            if (!r || r.Muerto || !r.isActiveAndEnabled) continue;
            Vector3 d = r.transform.position - transform.position; d.y = 0f;
            float borde = d.magnitude - r.Radio;                       // distancia hasta el borde del enemigo
            if (borde > a.alcance + 0.6f) continue;
            if (d.magnitude > 0.5f && Vector3.Angle(transform.forward, d) > 75f) continue;   // tiene que estar al frente
            if (Mathf.Abs(r.transform.position.y - transform.position.y) > 3f) continue;
            if (borde < mejorD) { mejorD = borde; mejor = r; }
        }
        if (mejor)
        {
            int dano = Equipo.TirarDano(out bool critico);
            mejor.RecibirGolpe(dano, critico);
        }
    }

    void UsarPocion()
    {
        var p = Equipo.PocionActual;
        if (p.cantidad <= 0) { GameState.Avisar("No te queda " + p.nombre, 1.2f); return; }
        p.cantidad--;
        if (p.efecto == "vida")
        {
            int antes = GameState.VidaKai;
            GameState.VidaKai = Mathf.Min(Progreso.VidaMax, GameState.VidaKai + p.valor);
            HUD.Numero(transform.position + Vector3.up * 1.4f, "+" + (GameState.VidaKai - antes), new Color(0.4f, 1f, 0.45f), 1f);
        }
        else if (p.efecto == "aguante")
        {
            GameState.Aguante = 100f;
            HUD.Numero(transform.position + Vector3.up * 1.4f, "¡Estamina!", new Color(0.55f, 0.9f, 0.4f), 0.9f);
        }
        GameState.Avisar(p.nombre + " (" + p.cantidad + " restantes)", 1.5f);
    }

    void UsarObjeto()
    {
        var o = Equipo.ObjetoActual;
        if (o.efecto == "nada") { GameState.Avisar("Tienes " + Equipo.Doblones + " doblones.", 1.5f); return; }
        if (o.cantidad <= 0) { GameState.Avisar("No te queda " + o.nombre, 1.2f); return; }
        o.cantidad--;
        if (o.efecto == "humo")
        {
            humoHasta = Time.time + o.valor;
            GameState.Avisar("¡Bomba de humo! Nadie te ve durante " + o.valor + " segundos.", 2f);
        }
    }

    System.Collections.IEnumerator Morir()
    {
        EstaMuerto = true;
        oculto = bloqueando = agachado = false;
        if (anim) anim.Agachado = false;
        Colorear.Restaurar(gameObject);
        estadoVisual = 0;
        GameState.Avisar("¡Kai cayó! Volviendo al inicio...", tiempoMuerte);

        float duracion = anim ? anim.Morir() : 0f;
        yield return new WaitForSeconds(Mathf.Max(tiempoMuerte, duracion + 0.5f));

        GameState.VidaKai = Progreso.VidaMax;
        GameState.Aguante = 100f;
        Reaparecer(PuntoInicial() ?? ultimoSpawn);   // revive en la isla donde esta, no en Caleta Gris
        if (anim) anim.Revivir();
        EstaMuerto = false;
    }

    // Donde empieza Kai en la isla actual:
    //  Caleta Gris (al empezar o al morir) -> spawnInicio
    //  cualquier otra isla, o al volver en barco -> el muelle de esa isla (Muelle con idIsla)
    Transform PuntoInicial()
    {
        int id = GameState.IslaActual;
        if (id != 1 || GameState.LlegandoEnBarco)
        {
            var mu = Muelle.De(id);
            if (mu) return mu.PuntoLlegada;
            if (id == 2 && spawnFaro) return spawnFaro;   // compatibilidad con la escena vieja
        }
        return spawnInicio;
    }

    public void Reaparecer(Transform punto)
    {
        if (punto == null) return;
        ultimoSpawn = punto;
        if (!cc) cc = GetComponent<CharacterController>();
        cc.enabled = false;
        transform.position = punto.position;
        cc.enabled = true;
        vy = 0f;
        velPlana = Vector3.zero;
    }
}
