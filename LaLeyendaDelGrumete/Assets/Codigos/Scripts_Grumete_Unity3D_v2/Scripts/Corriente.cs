using UnityEngine;

// Zona de corriente marina. Collider con Is Trigger.
public class Corriente : MonoBehaviour
{
    public Vector3 fuerza = new Vector3(0f, 0f, -3f);

    void OnTriggerStay(Collider otro)
    {
        var b = otro.GetComponentInParent<BarcoController>();
        if (b) b.corriente += fuerza;
    }
}
