using System.Collections.Generic;
using UnityEngine;

// Encargo personal de un tripulante (GDD: "reclutarlo no es automatico: primero hay que completar
// una pequena mision personal suya"). Ponlo en el NPC (Pico, Sombra...).
//   1) Kai habla con el (E): acepta el encargo y aparecen los ObjetoMision de su "Grupo".
//   2) Kai junta los objetos.
//   3) Vuelve a hablar: se entrega y se da la "Recompensa" (un RecompensaIsla con el tripulante).
// Todo queda guardado con la partida.
public class Encargo : MonoBehaviour
{
    public string nombre = "Pico";
    [Tooltip("Grupo de los ObjetoMision que pide")]
    public string grupo = "herramientas_pico";
    public int cantidad = 3;
    public float radio = 2.5f;
    [Tooltip("Que se da al terminar (RecompensaIsla en modo Al Llamar, con el tripulante)")]
    public RecompensaIsla recompensa;
    [Tooltip("En vez de objetos: el encargo se cumple cuando se da esta recompensa (por ejemplo ganar el foso). Deja Cantidad en 0.")]
    public RecompensaIsla requisito;

    [Header("Dialogos ({n} = cuantos faltan)")]
    [TextArea(2, 4)] public string fraseInicio = "¿Un grumete? Sin mis herramientas no arreglo ni una tabla. Los carroñeros me robaron el martillo, el serrucho y el cepillo. Uno quedó en el casco hundido: de noche, con la marea baja, se puede llegar.";
    [TextArea(2, 4)] public string fraseFalta = "Todavía me faltan {n}. Busca bien, grumete.";
    [TextArea(2, 4)] public string fraseFin = "¡Mis herramientas! Está bien, contigo voy. Le pongo una vela nueva a ese cascarón y vas a volar.";
    [TextArea(2, 4)] public string fraseDespues = "Cuando necesites el taller, invócame con G y te cobran menos.";

    public static readonly List<Encargo> Todos = new List<Encargo>();
    public static Encargo De(string grupo) { foreach (var e in Todos) if (e && e.grupo == grupo) return e; return null; }
    public static bool Aceptado(string grupo) => RecompensaIsla.Hechas.Contains("enc:" + grupo);
    public bool EstaAceptado => Aceptado(grupo);
    public bool Terminado => recompensa ? recompensa.Hecha : RecompensaIsla.Hechas.Contains("encfin:" + grupo);
    public int Juntados => Mathf.Min(ObjetoMision.Contar(grupo), cantidad);
    public bool Cumplido => requisito ? requisito.Hecha : Juntados >= cantidad;
    public bool Cerca(Vector3 p) => Vector3.Distance(p, transform.position) < radio;

    MarcadorMinimapa marca;

    void OnEnable() { if (!Todos.Contains(this)) Todos.Add(this); }
    void OnDisable() { Todos.Remove(this); }

    void Start()
    {
        if (recompensa) recompensa.cuando = RecompensaIsla.Cuando.AlLlamar;   // la da este encargo, no la tecla E
        marca = MarcadorMinimapa.Agregar(gameObject, MarcadorMinimapa.Tipo.Personaje, false, "", nombre);
    }

    // texto del boton E en el HUD
    public string Prompt()
    {
        if (Terminado || !EstaAceptado || !Cumplido) return "Hablar con " + nombre;
        return "Entregar a " + nombre;
    }

    void Update()
    {
        var kai = GameState.Kai;
        if (marca) marca.Activo = !Terminado;
        if (!kai || HUD.Pausado || !Cerca(kai.position) || !Input.GetKeyDown(KeyCode.E)) return;
        Hablar();
    }

    public void Hablar()
    {
        if (Terminado) { GameState.Hablar(nombre, fraseDespues, 3.5f); return; }
        if (!EstaAceptado)
        {
            RecompensaIsla.Hechas.Add("enc:" + grupo);
            GameState.Hablar(nombre, fraseInicio, 6f);
            Sonidos.Tocar("recoger", transform.position, 0.6f);
            return;
        }
        if (!Cumplido) { GameState.Hablar(nombre, fraseFalta.Replace("{n}", (cantidad - Juntados).ToString()), 3.5f); return; }

        GameState.Hablar(nombre, fraseFin, 5f);
        Efectos.Columna(transform.position, new Color(0.35f, 0.85f, 0.85f), 20, 1.2f);
        if (recompensa) recompensa.Dar();
        else RecompensaIsla.Hechas.Add("encfin:" + grupo);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.35f, 0.85f, 0.85f);
        Gizmos.DrawWireSphere(transform.position, radio);
    }
}
