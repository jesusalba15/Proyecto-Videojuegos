using UnityEngine;

// Camara del menu principal: gira despacio alrededor de un punto (por ejemplo Kai en el muelle)
// y se mece un poco, como si estuviera en un barco.
public class MenuCamara : MonoBehaviour
{
    public Transform objetivo;
    public float distancia = 7f;
    public float altura = 2.5f;
    public float velocidadGiro = 4f;       // grados por segundo
    public float alturaMirada = 1f;
    public float vaiven = 0.25f;           // balanceo suave

    float angulo;

    void Start()
    {
        if (objetivo) angulo = objetivo.eulerAngles.y + 180f;
    }

    void LateUpdate()
    {
        if (!objetivo) return;
        angulo += velocidadGiro * Time.deltaTime;
        Vector3 dir = Quaternion.Euler(0f, angulo, 0f) * Vector3.forward;
        float meneo = Mathf.Sin(Time.time * 0.8f) * vaiven;
        transform.position = objetivo.position + dir * distancia + Vector3.up * (altura + meneo);
        transform.LookAt(objetivo.position + Vector3.up * alturaMirada);
    }
}
