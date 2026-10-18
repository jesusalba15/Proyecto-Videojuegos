using UnityEngine;

// Camara de la mesa del mapa (escena Mapa). Ponla en la Main Camera.
// - Al abrir el mapa baja suavemente hacia la mesa.
// - Se acerca un poco a la isla elegida y la sigue al zarpar.
// - Se mueve un poquito con el mouse (efecto de profundidad).
public class MapaCamara : MonoBehaviour
{
    [Tooltip("Cuanto se acerca a la isla elegida (0 = nada, 1 = encima)")]
    [Range(0f, 0.8f)] public float acercamiento = 0.35f;
    public float suavizado = 3f;
    public float movimientoMouse = 1.5f;      // grados de giro con el mouse
    public float duracionEntrada = 1.6f;
    public float alturaEntrada = 6f;          // desde cuanto mas arriba empieza al abrir el mapa

    public static MapaCamara Actual;

    Vector3 posBase; Quaternion rotBase;
    Vector3 puntoBase;        // punto de la mesa al que mira la camara en reposo
    Vector3? foco;
    float tiempo;

    void Awake()
    {
        Actual = this;
        posBase = transform.position; rotBase = transform.rotation;
        // punto de la mesa al centro de la vista
        var plano = new Plane(Vector3.up, Vector3.zero);
        var rayo = new Ray(posBase, transform.forward);
        puntoBase = plano.Raycast(rayo, out float d) ? rayo.GetPoint(d) : posBase + transform.forward * 10f;
        transform.position = posBase + Vector3.up * alturaEntrada - transform.forward * alturaEntrada * 0.5f;
    }

    public void Enfocar(Vector3 punto) { foco = punto; }
    public void SinFoco() { foco = null; }

    void LateUpdate()
    {
        tiempo += Time.deltaTime;
        Vector3 objetivo = posBase;
        if (foco.HasValue)
        {
            Vector3 desp = foco.Value - puntoBase; desp.y = 0f;
            objetivo = posBase + desp * acercamiento + transform.forward * Vector3.Distance(posBase, puntoBase) * acercamiento * 0.35f;
        }
        float vel = tiempo < duracionEntrada ? 2.2f : suavizado;
        transform.position = Vector3.Lerp(transform.position, objetivo, 1f - Mathf.Exp(-vel * Time.deltaTime));

        // pequeno giro con el mouse
        Vector2 m = new Vector2(Input.mousePosition.x / Mathf.Max(1, Screen.width) - 0.5f,
                                Input.mousePosition.y / Mathf.Max(1, Screen.height) - 0.5f);
        Quaternion giro = Quaternion.Euler(-m.y * movimientoMouse, m.x * movimientoMouse, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotBase * giro, 1f - Mathf.Exp(-5f * Time.deltaTime));
    }
}
