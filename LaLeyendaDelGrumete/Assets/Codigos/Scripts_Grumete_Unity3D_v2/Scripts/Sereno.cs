using UnityEngine;

// Guardia que patrulla entre puntos con un farol (Spot Light).
// Ve a Kai si esta dentro del cono, en rango, con linea de vision libre y sin esconderse.
// Agacharse detras de cajas bloquea la vision.
public class Sereno : MonoBehaviour
{
    public PlayerController jugador;
    public Transform reaparicion;
    public Transform[] puntos;
    public Light farol;

    public float velocidad = 2.5f;
    public float pausa = 0.8f;
    public float alcance = 8f;
    public float anguloVision = 60f;
    public float llenado = 80f, vaciado = 35f;

    int indice;
    float det, espera;

    void Start()
    {
        if (farol)
        {
            farol.type = LightType.Spot;
            farol.spotAngle = anguloVision;
            farol.range = alcance + 2f;
        }
    }

    void Update()
    {
        Patrullar();
        Vigilar();
    }

    void Patrullar()
    {
        if (puntos == null || puntos.Length == 0) return;
        if (espera > 0f) { espera -= Time.deltaTime; return; }

        Vector3 destino = puntos[indice].position;
        destino.y = transform.position.y;
        Vector3 d = destino - transform.position;

        if (d.magnitude < 0.1f)
        {
            indice = (indice + 1) % puntos.Length;
            espera = pausa;
            return;
        }

        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(d), 360f * Time.deltaTime);
        transform.position = Vector3.MoveTowards(transform.position, destino, velocidad * Time.deltaTime);
    }

    void Vigilar()
    {
        if (!jugador) return;

        Vector3 ojos = transform.position + Vector3.up * 0.6f;
        Vector3 blanco = jugador.transform.position + Vector3.up * (jugador.agachado ? -0.5f : 0.3f);
        Vector3 d = blanco - ojos;
        Vector3 plano = d; plano.y = 0f;

        bool viendo = false;
        if (!jugador.oculto && d.magnitude < alcance && Vector3.Angle(transform.forward, plano) < anguloVision * 0.5f)
        {
            if (Physics.Linecast(ojos, blanco, out RaycastHit hit, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                viendo = hit.collider.GetComponentInParent<PlayerController>() != null;
            else
                viendo = true;
        }

        det = Mathf.Clamp(det + (viendo ? llenado : -vaciado) * Time.deltaTime, 0f, 100f);
        GameState.Deteccion = det;
        GameState.MostrarDeteccion = Vector3.Distance(jugador.transform.position, transform.position) < alcance + 5f;

        if (det >= 100f)
        {
            det = 0f;
            GameState.Avisar("¡Te vio el sereno! Inténtalo de nuevo");
            jugador.Reaparecer(reaparicion);
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 izq = Quaternion.Euler(0, -anguloVision / 2f, 0) * transform.forward * alcance;
        Vector3 der = Quaternion.Euler(0, anguloVision / 2f, 0) * transform.forward * alcance;
        Gizmos.DrawLine(transform.position, transform.position + izq);
        Gizmos.DrawLine(transform.position, transform.position + der);
    }
}
