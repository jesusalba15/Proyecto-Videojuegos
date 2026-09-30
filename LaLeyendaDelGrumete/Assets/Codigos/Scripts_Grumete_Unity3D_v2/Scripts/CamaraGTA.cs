using UnityEngine;

// Camara estilo GTA: orbita con el mouse, sobre el hombro, varias distancias y primera persona.
// V = cambiar de distancia (lejos -> media -> cerca -> primera persona).
// Rueda del mouse = acercar / alejar. Esc = liberar el cursor, clic = volver a capturarlo.
// Va en la Main Camera de la escena Isla (en lugar de CameraFollow).
public class CamaraGTA : MonoBehaviour
{
    public Transform objetivo;

    [Header("Distancias (la ultima en 0 = primera persona)")]
    public float[] distancias = { 7f, 4.5f, 2.8f, 0f };
    public int modoInicial = 1;
    public KeyCode teclaCambiar = KeyCode.V;

    [Header("Mouse")]
    public float sensibilidad = 3f;
    public float anguloMin = -30f, anguloMax = 70f;

    [Header("Encuadre")]
    public float alturaMirada = 0.6f;       // desde el centro del personaje
    public float alturaPrimeraPersona = 0.75f;
    public float hombro = 0.45f;            // desplazamiento a la derecha, estilo GTA
    public float suavizadoZoom = 10f;

    [Header("Colision con paredes")]
    public float radioColision = 0.25f;
    public LayerMask capas = ~0;

    public static CamaraGTA Actual;
    public bool EnPrimeraPersona => distancias[modo] <= 0.01f;
    public float Yaw => yaw;

    int modo;
    float yaw, pitch = 20f, distActual;
    Renderer[] rendsObjetivo;
    bool modeloOculto;

    void Awake() { Actual = this; }

    void Start()
    {
        modo = Mathf.Clamp(modoInicial, 0, distancias.Length - 1);
        distActual = distancias[modo];
        if (objetivo)
        {
            yaw = objetivo.eulerAngles.y;
            rendsObjetivo = objetivo.GetComponentsInChildren<Renderer>();
        }
        Capturar(true);
    }

    void Capturar(bool si)
    {
        Cursor.lockState = si ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !si;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) Capturar(false);
        if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked) Capturar(true);

        if (Input.GetKeyDown(teclaCambiar)) modo = (modo + 1) % distancias.Length;

        float rueda = Input.GetAxis("Mouse ScrollWheel");
        if (rueda > 0.01f) modo = Mathf.Min(modo + 1, distancias.Length - 1);
        else if (rueda < -0.01f) modo = Mathf.Max(modo - 1, 0);

        if (Cursor.lockState == CursorLockMode.Locked)
        {
            yaw += Input.GetAxis("Mouse X") * sensibilidad;
            pitch -= Input.GetAxis("Mouse Y") * sensibilidad;
        }
        float min = EnPrimeraPersona ? -80f : anguloMin;
        float max = EnPrimeraPersona ? 80f : anguloMax;
        pitch = Mathf.Clamp(pitch, min, max);
    }

    void LateUpdate()
    {
        if (!objetivo) return;

        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        distActual = Mathf.Lerp(distActual, distancias[modo], suavizadoZoom * Time.deltaTime);

        float altura = Mathf.Lerp(alturaPrimeraPersona, alturaMirada, Mathf.Clamp01(distActual / 1.5f));
        Vector3 pivote = objetivo.position + Vector3.up * altura;

        float factorHombro = Mathf.Clamp01(distActual / 2f);
        Vector3 deseada = pivote + rot * (Vector3.right * hombro * factorHombro + Vector3.back * distActual);
        Vector3 dir = deseada - pivote;
        float largo = dir.magnitude;

        Vector3 final = deseada;
        if (largo > 0.05f &&
            Physics.SphereCast(pivote, radioColision, dir / largo, out RaycastHit hit, largo, capas, QueryTriggerInteraction.Ignore))
        {
            final = pivote + dir / largo * Mathf.Max(0f, hit.distance - 0.05f);
        }

        transform.SetPositionAndRotation(final, rot);

        // En primera persona el modelo solo proyecta sombra (no tapa la vista)
        bool ocultar = distActual < 0.6f;
        if (ocultar != modeloOculto && rendsObjetivo != null)
        {
            modeloOculto = ocultar;
            foreach (var r in rendsObjetivo)
                r.shadowCastingMode = ocultar ? UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly
                                              : UnityEngine.Rendering.ShadowCastingMode.On;
        }
    }

    void OnDisable()
    {
        // Al cambiar de escena (por ejemplo al Mapa) el cursor vuelve a quedar libre
        Capturar(false);
        if (Actual == this) Actual = null;
    }
}
