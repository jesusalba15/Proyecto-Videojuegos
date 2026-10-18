using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Recompensa de una isla (Fase 5). Da UNA sola vez (se guarda con la partida):
//   fragmentos de mapa (revelan la siguiente isla), un tripulante, un arma, experiencia, doblones y un dialogo.
// Cuando se da:
//   Al Llamar  -> desde un evento, por ejemplo el "Al Morir" de un Jefe (arrastra este objeto y elige RecompensaIsla > Dar)
//   Al Entrar  -> cuando Kai llega cerca (por ejemplo al encontrar a un tripulante)
//   Al Hablar  -> cuando Kai presiona E cerca (un NPC que te da algo)
// "Es Final" = la recompensa de la Fortaleza de Voss: muestra la pantalla de victoria.
public class RecompensaIsla : MonoBehaviour
{
    public enum Cuando { AlLlamar, AlEntrar, AlHablar }

    public Cuando cuando = Cuando.AlLlamar;
    [Tooltip("Nombre unico para guardarla (vacio = nombre del objeto). Ej: faro_capitan")]
    public string clave = "";
    public float radio = 2.5f;

    [Header("Que da")]
    public int fragmentos = 1;
    public Tripulacion.Miembro tripulante = Tripulacion.Miembro.Ninguno;
    public Equipo.IdArma arma = Equipo.IdArma.Punos;   // Punos = ninguna
    public int experiencia = 150;
    public int doblones = 0;
    [Tooltip("Niveles de vela del barco que regala (Pico: 1)")]
    public int mejoraVela = 0;

    [Header("Dialogo")]
    public string hablante = "";
    [TextArea(2, 4)] public string mensaje = "";

    [Header("Despues")]
    [Tooltip("Abre el mapa del archipielago para ver la isla nueva que revelo el fragmento")]
    public bool abrirMapa = true;
    [Tooltip("Objetos que aparecen al dar la recompensa (por ejemplo el tripulante en el muelle)")]
    public GameObject[] activar;
    [Tooltip("Objetos que desaparecen (por ejemplo la jaula del tripulante)")]
    public GameObject[] desactivar;
    public bool esFinal = false;

    public static readonly HashSet<string> Hechas = new HashSet<string>();
    public static readonly List<RecompensaIsla> Todas = new List<RecompensaIsla>();
    public string Clave => string.IsNullOrEmpty(clave) ? gameObject.name : clave;
    public bool Hecha => Hechas.Contains(Clave);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reiniciar() { Hechas.Clear(); }

    void OnEnable() { Todas.Add(this); }
    void OnDisable() { Todas.Remove(this); }

    void Start()
    {
        // partida cargada: deja el mundo como quedo
        if (Hecha) { foreach (var g in activar) if (g) g.SetActive(true); foreach (var g in desactivar) if (g) g.SetActive(false); }
        else foreach (var g in activar) if (g) g.SetActive(false);
    }

    public bool Cerca(Vector3 p) => Vector3.Distance(p, transform.position) < radio;
    public bool PideHablar => cuando == Cuando.AlHablar && !Hecha;

    void Update()
    {
        if (Hecha || cuando == Cuando.AlLlamar || HUD.Pausado) return;
        var kai = GameState.Kai;
        if (!kai || !Cerca(kai.position)) return;
        if (cuando == Cuando.AlEntrar || Input.GetKeyDown(KeyCode.E)) Dar();
    }

    public void Dar()
    {
        if (Hecha) return;
        Hechas.Add(Clave);

        if (!string.IsNullOrEmpty(mensaje)) GameState.Hablar(string.IsNullOrEmpty(hablante) ? "" : hablante, mensaje, 4f);
        if (fragmentos > 0)
        {
            GameState.Fragmentos += fragmentos;
            if (GameState.Kai) HUD.Numero(GameState.Kai.position + Vector3.up * 2.2f, "+" + fragmentos + " fragmento de mapa", new Color(0.95f, 0.8f, 0.4f), 1.1f);
        }
        if (doblones > 0) { Sonidos.Tocar("monedas", null, 0.9f); Equipo.Doblones += doblones; if (GameState.Kai) HUD.Numero(GameState.Kai.position + Vector3.up * 1.8f, "+" + doblones + " doblones", new Color(1f, 0.82f, 0.3f), 0.9f); }
        if (arma != Equipo.IdArma.Punos) Equipo.Desbloquear(arma);
        if (mejoraVela > 0 && Mejoras.NivelVela < Mejoras.MaxVela)
        {
            Mejoras.NivelVela = Mathf.Min(Mejoras.MaxVela, Mejoras.NivelVela + mejoraVela);
            if (GameState.Kai) HUD.Numero(GameState.Kai.position + Vector3.up * 3f, "Vela del barco +" + Mejoras.NivelVela, new Color(0.6f, 0.85f, 1f), 1f);
        }
        Tripulacion.Reclutar(tripulante);
        Progreso.Ganar(experiencia);
        foreach (var g in activar) if (g) g.SetActive(true);
        foreach (var g in desactivar) if (g) g.SetActive(false);

        if (esFinal) StartCoroutine(Luego(3.5f, () => PantallaFinal.Mostrar()));
        else if (abrirMapa && fragmentos > 0) StartCoroutine(Luego(3f, () => MapaMundo.Abrir(MapaMundo.Pestana.Mundo)));
    }

    IEnumerator Luego(float s, System.Action accion)
    {
        yield return new WaitForSeconds(s);
        accion();
    }

    void OnDrawGizmosSelected()
    {
        if (cuando == Cuando.AlLlamar) return;
        Gizmos.color = new Color(0.4f, 0.9f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, radio);
    }
}
