using UnityEngine;

// Bloqueo que solo abre un tripulante (GDD: "los tripulantes desbloquean acciones nuevas que abren rutas").
//   Bruno  -> rompe bloqueos pesados (rocas, portones atrancados)
//   Sombra -> abre cerraduras con su ganzua
// Kai se acerca, invoca al tripulante con G y presiona E. Queda abierto para siempre (se guarda).
// Ponlo en el porton / la reja (con su collider). Al abrirse se desactiva; "Al Abrir" activa otras cosas.
public class Bloqueo : MonoBehaviour
{
    public Tripulacion.Miembro quien = Tripulacion.Miembro.Bruno;
    public string nombre = "Portón atrancado";
    [Tooltip("Nombre unico para guardarlo (vacio = nombre del objeto)")]
    public string clave = "";
    public float radio = 3f;
    public GameObject[] alAbrir;

    public static readonly System.Collections.Generic.List<Bloqueo> Todos = new System.Collections.Generic.List<Bloqueo>();
    string Clave => "bloq:" + (string.IsNullOrEmpty(clave) ? gameObject.name : clave);
    public bool Abierto => RecompensaIsla.Hechas.Contains(Clave);
    public bool Cerca(Vector3 p) => !Abierto && Vector3.Distance(p, transform.position) < radio;
    bool PuedeAbrir => Tripulacion.Invocado(quien);
    string Accion => quien == Tripulacion.Miembro.Sombra ? "Forzar la cerradura" : "Romper el bloqueo";

    void OnEnable() { if (!Todos.Contains(this)) Todos.Add(this); }
    void OnDisable() { Todos.Remove(this); }

    void Start()
    {
        if (Abierto) Abrir(false);
        else MarcadorMinimapa.Agregar(gameObject, MarcadorMinimapa.Tipo.Objetivo, false, "", nombre);
    }

    // texto del boton E en el HUD
    public string Prompt() => PuedeAbrir ? Accion + " (" + Tripulacion.Nombres[(int)quien] + ")" : "Examinar: " + nombre;

    void Update()
    {
        var kai = GameState.Kai;
        if (!kai || HUD.Pausado || !Cerca(kai.position) || !Input.GetKeyDown(KeyCode.E)) return;
        if (!Tripulacion.Tiene(quien))
        { GameState.Avisar(nombre + ": no puedes abrirlo solo. Hace falta alguien de la tripulación.", 2.5f); return; }
        if (!PuedeAbrir)
        { GameState.Avisar(nombre + ": invoca a " + Tripulacion.Nombres[(int)quien] + " (G, y H para elegirlo) y vuelve a intentarlo.", 3f); return; }
        Abrir(true);
    }

    void Abrir(bool animado)
    {
        RecompensaIsla.Hechas.Add(Clave);
        if (animado)
        {
            bool fuerza = quien == Tripulacion.Miembro.Bruno;
            Sonidos.Tocar(fuerza ? "enemigo_cae" : "cofre", transform.position, 1f, 0f);
            Efectos.Polvo(transform.position, new Color(0.7f, 0.65f, 0.58f, 0.6f), fuerza ? 14 : 5, fuerza ? 1.6f : 0.8f);
            if (fuerza) Efectos.Sacudir(0.25f, 0.35f);
            GameState.Avisar(fuerza ? "¡Bruno lo hizo pedazos!" : "Sombra abrió la cerradura sin un ruido.", 2.5f);
        }
        if (alAbrir != null) foreach (var g in alAbrir) if (g) g.SetActive(true);
        gameObject.SetActive(false);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.9f, 0.6f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, radio);
    }
}
