using UnityEngine;

// Barco con controles de timon: W acelera hacia adelante, S frena / reversa,
// A y D giran el timon. Mara da bonus de velocidad.
// La proa del modelo debe apuntar hacia la flecha azul (eje Z) de la raiz.
[RequireComponent(typeof(Rigidbody))]
public class BarcoController : MonoBehaviour
{
    public float velocidad = 6f;
    public float bonusMara = 1.5f;
    public float velocidadGiro = 90f;     // grados por segundo
    public float aceleracion = 0.8f;      // que tan rapido llega a la velocidad maxima
    [Range(0f, 1f)] public float factorReversa = 0.4f;
    [HideInInspector] public Vector3 corriente;

    Rigidbody rb;
    float velActual;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        rb.constraints = RigidbodyConstraints.FreezePositionY | RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.sleepThreshold = 0f;
        GameState.Avisar("W avanzar, S frenar, A/D girar el timón. Llega a la Isla del Faro Roto.", 4f);
    }

    void FixedUpdate()
    {
        float avance = Input.GetAxisRaw("Vertical");
        float giro = Input.GetAxisRaw("Horizontal");
        float max = velocidad * (GameState.TieneMara == 1 ? bonusMara : 1f);

        float objetivo = avance >= 0f ? avance * max : avance * max * factorReversa;
        velActual = Mathf.MoveTowards(velActual, objetivo, aceleracion * max * Time.fixedDeltaTime);

        // Girar el timon (en reversa el giro se invierte, como en un bote real)
        float sentido = velActual < -0.1f ? -1f : 1f;
        rb.angularVelocity = Vector3.zero; // que los choques no lo dejen girando solo
        rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, giro * velocidadGiro * sentido * Time.fixedDeltaTime, 0f));

        Vector3 deseada = transform.forward * velActual + corriente;
        rb.SetVel(Vector3.Lerp(rb.GetVel(), deseada, 0.2f));
        corriente = Vector3.zero; // las corrientes la vuelven a llenar en este paso de fisica
    }

    void OnCollisionEnter(Collision c)
    {
        // Al chocar con una roca pierde velocidad
        velActual *= 0.3f;
    }
}