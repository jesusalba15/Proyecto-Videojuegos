using UnityEngine;

// Collider con Is Trigger. tipo = "agilidad" o "sigilo" (no importan mayusculas ni espacios)
public class Meta : MonoBehaviour
{
    public string tipo = "agilidad";

    void OnTriggerEnter(Collider otro)
    {
        if (!otro.GetComponentInParent<PlayerController>()) return;
        GameState.CompletarPrueba(tipo.Trim().ToLower());
        Colorear.Poner(gameObject, new Color(0.4f, 1f, 0.4f));
    }
}