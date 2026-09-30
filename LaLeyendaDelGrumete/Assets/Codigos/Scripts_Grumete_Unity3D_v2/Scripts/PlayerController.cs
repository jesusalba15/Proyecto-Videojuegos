using UnityEngine;

// Kai en 3D: WASD/flechas moverse, Shift correr, Espacio saltar, C agacharse (junto a barril = oculto),
// J o clic izquierdo atacar, K o clic derecho bloquear.
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
    public float fuerzaSalto = 8f;
    public float gravedad = -20f;
    public Transform spawnInicio;
    public Transform spawnFaro;

    [HideInInspector] public bool oculto, bloqueando, agachado;
    public bool EstaMuerto { get; private set; }

    CharacterController cc;
    float vy;
    int estadoVisual = -1;
    Transform ultimoSpawn;
    AnimacionSimple anim;
    float proximoAtaque;

    void Start()
    {
        cc = GetComponent<CharacterController>();
        anim = GetComponentInChildren<AnimacionSimple>();

        if (GameState.IslaActual == 2 && spawnFaro != null)
        {
            Reaparecer(spawnFaro);
            GameState.Avisar("Llegaste a la Isla del Faro Roto", 4f);
        }
        else if (spawnInicio != null)
        {
            Reaparecer(spawnInicio);
        }
    }

    void Update()
    {
        if (EstaMuerto) return;
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
        oculto = agachado && CercaDeBarril();
        if (anim) anim.Agachado = agachado;

        bool corriendo = Input.GetKey(KeyCode.LeftShift) && !agachado;
        float v = bloqueando ? 0f
                : agachado ? (puedeMoverseAgachado ? velocidad * 0.4f : 0f)
                : corriendo ? velocidadCorrer : velocidad;

        var camGTA = CamaraGTA.Actual;
        if (camGTA && camGTA.EnPrimeraPersona)
            transform.rotation = Quaternion.Euler(0f, camGTA.Yaw, 0f);   // en primera persona mira con la camara
        else if (entrada.sqrMagnitude > 0.01f && v > 0f)
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(entrada), 15f * Time.deltaTime);

        if (cc.isGrounded)
        {
            if (vy < 0f) vy = -2f;
            if (Input.GetKeyDown(KeyCode.Space) && !agachado) vy = fuerzaSalto;
        }
        vy += gravedad * Time.deltaTime;

        Vector3 mov = entrada * v;
        mov.y = vy;
        cc.Move(mov * Time.deltaTime);

        if (Input.GetKeyDown(KeyCode.J) || Input.GetMouseButtonDown(0)) Atacar();

        // M = abrir el mapa vivo en cualquier momento
        if (Input.GetKeyDown(KeyCode.M)) GameState.CargarEscena("Mapa");

        if (transform.position.y < -5f && ultimoSpawn) Reaparecer(ultimoSpawn);

        ActualizarColor();
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

    void Atacar()
    {
        if (Time.time < proximoAtaque) return;
        proximoAtaque = Time.time + esperaAtaque;
        if (anim) anim.Atacar();

        Vector3 centro = transform.position + transform.forward * 1.2f;
        foreach (var col in Physics.OverlapSphere(centro, 1f))
        {
            var r = col.GetComponentInParent<Rival>();
            if (r) { r.RecibirGolpe(); break; }
        }
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.normal.y < 0.5f) return;
        var viga = hit.collider.GetComponent<VigaInestable>();
        if (viga) viga.Pisar();
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

        GameState.VidaKai = 100;
        Reaparecer(spawnInicio ? spawnInicio : ultimoSpawn);
        if (anim) anim.Revivir();
        EstaMuerto = false;
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
    }
}