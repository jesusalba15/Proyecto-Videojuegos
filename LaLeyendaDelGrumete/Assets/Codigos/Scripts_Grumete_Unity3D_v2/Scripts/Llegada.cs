using UnityEngine;

// Costa del Faro Roto en la escena Navegacion. Collider con Is Trigger.
public class Llegada : MonoBehaviour
{
    void OnTriggerEnter(Collider otro)
    {
        if (!otro.GetComponentInParent<BarcoController>()) return;
        GameState.Navegacion = 1;
        GameState.IslaActual = 2;
        GameState.CargarEscena("Isla");
    }
}
