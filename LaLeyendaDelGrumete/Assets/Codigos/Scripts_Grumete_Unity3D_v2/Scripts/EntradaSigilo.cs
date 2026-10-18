using UnityEngine;

// Puerta correcta de la prueba de sigilo.
// Ponlo en un objeto con Box Collider marcado "Is Trigger" justo en la puerta del castillo, con la
// flecha azul (Z) apuntando HACIA ADENTRO del castillo.
// La Meta de sigilo solo cuenta si Kai entro cruzando esta puerta. Si entra por otro lado (saltando un
// muro, por otra puerta...) llegar a la Meta no vale.
// Si atrapan a Kai, o si sale de vuelta por la puerta, tiene que volver a entrar.
[RequireComponent(typeof(Collider))]
public class EntradaSigilo : MonoBehaviour
{
    public static bool Entro;              // Kai cruzo la puerta hacia adentro
    public static bool HayEntrada;         // existe una puerta en la escena (si no, la Meta funciona como antes)

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Limpiar() { Entro = false; HayEntrada = false; }

    public static void Reiniciar() { Entro = false; }

    void OnEnable() { HayEntrada = true; }

    void Start()
    {
        var c = GetComponent<Collider>();
        if (!c.isTrigger) { c.isTrigger = true; Debug.LogWarning("[EntradaSigilo] " + name + ": marque el collider como Is Trigger.", this); }
    }

    // al salir del trigger se mira de que lado quedo Kai: adentro (lado de la flecha azul) o afuera
    void OnTriggerExit(Collider otro)
    {
        var kai = otro.GetComponentInParent<PlayerController>();
        if (!kai) return;
        Vector3 d = kai.transform.position - transform.position; d.y = 0f;
        bool adentro = Vector3.Dot(d, transform.forward) > 0f;
        if (adentro && !Entro) GameState.Avisar("Entraste al castillo a escondidas. ¡Llega a la bodega sin que te vean!", 2.5f);
        Entro = adentro;
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Entro ? Color.green : new Color(0.3f, 0.8f, 1f);
        Gizmos.DrawRay(transform.position + Vector3.up, transform.forward * 2f);   // hacia adentro
        Gizmos.DrawWireCube(transform.position + Vector3.up, new Vector3(1.5f, 2f, 0.3f));
    }
}
