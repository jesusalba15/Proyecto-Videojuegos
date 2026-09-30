using UnityEngine;

// Muestra un mensaje cuando Kai entra en la zona. Collider con Is Trigger.
public class Cartel : MonoBehaviour
{
    [TextArea] public string mensaje = "Escribe aquí el mensaje";
    public float duracion = 3f;

    void OnTriggerEnter(Collider otro)
    {
        if (otro.GetComponentInParent<PlayerController>()) GameState.Avisar(mensaje, duracion);
    }
}
