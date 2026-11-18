using System.Collections.Generic;
using UnityEngine;

// Piezas especiales para mejorar el barco.
//  - Se consiguen de enemigos (componente SueltaPieza) o tiradas por el mapa (componente PiezaEnSuelo).
//  - Quedan registradas como obtenidas y se guardan (PlayerPrefs): no vuelven a aparecer al cargar.
//  - Para agregar una pieza nueva: anadela al Catalogo con un id unico (sin espacios).
//  - Desde el Taller / Mejoras / BarcoController:   if (PiezasBarco.Tiene("vela")) { ... }
public static class PiezasBarco
{
    public class Pieza
    {
        public string id, nombre, descripcion;
        public Color color;
        public Pieza(string id, string nombre, string descripcion, Color color)
        { this.id = id; this.nombre = nombre; this.descripcion = descripcion; this.color = color; }
    }

    // Catalogo de piezas (cambia nombres, descripciones o agrega las tuyas)
    public static readonly Pieza[] Catalogo =
    {
        new Pieza("vela",    "Vela reforzada",     "El barco navega más rápido",        new Color(0.95f, 0.92f, 0.80f)),
        new Pieza("timon",   "Timón de bronce",    "El barco gira mejor",               new Color(0.85f, 0.60f, 0.30f)),
        new Pieza("casco",   "Casco alivianado", "El casco ha reducido su peso",       new Color(0.60f, 0.65f, 0.70f)),
        new Pieza("mastil",   "Mastil de barco pirata",      "Aprovecha mejor las corrientes de viento",           new Color(0.35f, 0.35f, 0.40f)),
        new Pieza("bandera", "Bandera de Cuervo",    "Detalle pirata",   new Color(1.00f, 0.80f, 0.30f)),
    };

    const string Clave = "PiezasBarco";
    static HashSet<string> obtenidas;

    // Se dispara al conseguir una pieza (por si el HUD o el Taller quieren reaccionar)
    public static event System.Action<Pieza> AlObtener;

    static HashSet<string> Lista { get { if (obtenidas == null) Cargar(); return obtenidas; } }

    public static Pieza Buscar(string id) => System.Array.Find(Catalogo, p => p.id == id);
    public static bool Tiene(string id) => Lista.Contains(id);
    public static int Cantidad => Lista.Count;

    public static void Obtener(string id)
    {
        var p = Buscar(id);
        if (p == null) { Debug.LogWarning("[PiezasBarco] No existe la pieza '" + id + "' en el Catalogo."); return; }
        if (!Lista.Add(id)) return;   // ya la tenia
        Guardar();
        GameState.Avisar("¡Pieza de barco: " + p.nombre + "! (" + Cantidad + "/" + Catalogo.Length + ")", 3f);
        AlObtener?.Invoke(p);
    }

    public static void Guardar()
    {
        PlayerPrefs.SetString(Clave, string.Join(",", Lista));
        PlayerPrefs.Save();
    }

    public static void Cargar()
    {
        obtenidas = new HashSet<string>();
        foreach (var id in PlayerPrefs.GetString(Clave, "").Split(','))
            if (id != "") obtenidas.Add(id);
    }

    // Llamar al empezar una "Nueva partida"
    public static void Reiniciar()
    {
        obtenidas = new HashSet<string>();
        PlayerPrefs.DeleteKey(Clave);
        PlayerPrefs.DeleteKey("MisionBarcoEntregada");   // la mision de MisionPiezasBarco
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Limpiar()
    {
        obtenidas = null; AlObtener = null;
#if UNITY_EDITOR
        if (UnityEditor.EditorPrefs.GetBool(ClaveEditor, false)) { Reiniciar(); Debug.Log("[PiezasBarco] Piezas y mision reiniciadas al dar Play."); }
#endif
    }

#if UNITY_EDITOR
    // Para probar en el editor (menu superior Grumete):
    //  - "Borrar piezas de barco": las borra una vez.
    //  - "Reiniciar piezas al dar Play": si esta marcado, cada Play empieza sin piezas y sin la mision hecha.
    const string ClaveEditor = "PiezasBarco_ReiniciarAlPlay";
    const string MenuReiniciar = "Grumete/Reiniciar piezas al dar Play";

    [UnityEditor.MenuItem("Grumete/Borrar piezas de barco")]
    static void BorrarEditor() { Reiniciar(); Debug.Log("[PiezasBarco] Piezas borradas."); }

    [UnityEditor.MenuItem(MenuReiniciar)]
    static void AlternarReiniciar()
    {
        bool v = !UnityEditor.EditorPrefs.GetBool(ClaveEditor, false);
        UnityEditor.EditorPrefs.SetBool(ClaveEditor, v);
        Debug.Log("[PiezasBarco] Reiniciar piezas al dar Play: " + (v ? "SI" : "NO"));
    }

    [UnityEditor.MenuItem(MenuReiniciar, true)]
    static bool AlternarReiniciarCheck()
    {
        UnityEditor.Menu.SetChecked(MenuReiniciar, UnityEditor.EditorPrefs.GetBool(ClaveEditor, false));
        return true;
    }
#endif
}