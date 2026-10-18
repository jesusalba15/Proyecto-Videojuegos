using UnityEngine;

// Collider con Is Trigger. tipo = "agilidad" o "sigilo" (no importan mayusculas ni espacios)
public class Meta : MonoBehaviour
{
    public string tipo = "agilidad";
    [Tooltip("Sigilo: solo vale si Kai entro por la puerta correcta (objeto con EntradaSigilo). Si no hay EntradaSigilo en la escena, vale como antes.")]
    public bool soloPorLaEntrada = true;
    float proximoAviso;

    void Start()
    {
        // si la prueba ya estaba hecha (al volver a la isla), se muestra completada y sin icono
        if (Hecha()) Marcar();
    }

    public bool Hecha()
    {
        string t = tipo.Trim().ToLower();
        return (t == "agilidad" && GameState.Agilidad == 1) || (t == "sigilo" && GameState.Sigilo == 1);
    }

    void Marcar()
    {
        Colorear.Poner(gameObject, new Color(0.4f, 1f, 0.4f));
        var m = GetComponent<MarcadorMinimapa>(); if (m) m.Activo = false;
    }

    void OnTriggerEnter(Collider otro)
    {
        if (!otro.GetComponentInParent<PlayerController>()) return;
        string t = tipo.Trim().ToLower();
        if (t == "sigilo" && soloPorLaEntrada && EntradaSigilo.HayEntrada && !EntradaSigilo.Entro && !Hecha())
        {
            if (Time.time > proximoAviso)
            {
                proximoAviso = Time.time + 3f;
                GameState.Avisar("Así no vale: tienes que colarte por la puerta del castillo sin que te vean.", 3f);
            }
            return;
        }
        GameState.CompletarPrueba(t);
        Marcar();
    }
}
