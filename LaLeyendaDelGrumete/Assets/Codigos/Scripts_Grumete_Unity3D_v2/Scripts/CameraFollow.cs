using UnityEngine;

// Camara que sigue a un objetivo.
// Seguir Rotacion desmarcado: angulo fijo visto desde arriba.
// Seguir Rotacion marcado: se queda detras del objetivo y gira con el (ideal para el barco).
public class CameraFollow : MonoBehaviour
{
    public Transform objetivo;
    public Vector3 desfase = new Vector3(0f, 9f, -8f);
    public float suavizado = 6f;
    public bool seguirRotacion = false;
    public float alturaMirada = 1f;

    void Start()
    {
        if (!objetivo) return;
        transform.position = PosicionDeseada();
        transform.rotation = RotacionDeseada(transform.position);
    }

    Vector3 PosicionDeseada()
    {
        if (!seguirRotacion) return objetivo.position + desfase;
        Quaternion giro = Quaternion.Euler(0f, objetivo.eulerAngles.y, 0f);
        return objetivo.position + giro * desfase;
    }

    Quaternion RotacionDeseada(Vector3 desde)
    {
        if (!seguirRotacion) return Quaternion.LookRotation(-desfase);
        return Quaternion.LookRotation(objetivo.position + Vector3.up * alturaMirada - desde);
    }

    void LateUpdate()
    {
        if (!objetivo) return;
        transform.position = Vector3.Lerp(transform.position, PosicionDeseada(), suavizado * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, RotacionDeseada(transform.position), suavizado * Time.deltaTime);
    }
}