using System.Collections.Generic;
using UnityEngine;

// Pon este componente en cualquier objeto que quieras ver como icono en el minimapa.
// (Enemigos, cofres, NPCs y metas reciben uno automaticamente; este es para lo demas.)
public class MarcadorMinimapa : MonoBehaviour
{
    public enum Tipo { Enemigo, Jefe, Cofre, Personaje, Objetivo, Recurso, Puerto }

    public Tipo tipo = Tipo.Objetivo;
    [Tooltip("Si esta lejos, se queda pegado al borde del minimapa senalando la direccion")]
    public bool siempreVisible = false;
    [Tooltip("Texto corto opcional dentro del icono (por ejemplo '!' o '?')")]
    public string letra = "";
    [Tooltip("Nombre que sale en el mapa al pasar el mouse por el pin (vacio = nombre del objeto)")]
    public string etiqueta = "";

    public static readonly List<MarcadorMinimapa> Todos = new List<MarcadorMinimapa>();

    // El icono deja de verse cuando el objeto se desactiva (enemigo muerto, cofre abierto, etc.)
    public bool Activo { get; set; } = true;

    void OnEnable() { if (!Todos.Contains(this)) Todos.Add(this); }
    void OnDisable() { Todos.Remove(this); }

    public static MarcadorMinimapa Agregar(GameObject go, Tipo tipo, bool siempreVisible = false, string letra = "", string etiqueta = "")
    {
        var m = go.GetComponent<MarcadorMinimapa>();
        if (!m)
        {
            m = go.AddComponent<MarcadorMinimapa>();
            m.tipo = tipo; m.siempreVisible = siempreVisible; m.letra = letra;
        }
        if (string.IsNullOrEmpty(m.etiqueta)) m.etiqueta = etiqueta;
        return m;
    }

    public Color ColorIcono() => ColorDe(tipo);

    public static Color ColorDe(Tipo tipo)
    {
        switch (tipo)
        {
            case Tipo.Enemigo: return new Color(0.9f, 0.25f, 0.2f);
            case Tipo.Jefe: return new Color(0.75f, 0.05f, 0.1f);
            case Tipo.Cofre: return new Color(0.98f, 0.78f, 0.25f);
            case Tipo.Personaje: return new Color(0.35f, 0.7f, 0.95f);
            case Tipo.Recurso: return new Color(0.45f, 0.85f, 0.4f);
            case Tipo.Puerto: return new Color(0.95f, 0.95f, 0.95f);
            default: return new Color(1f, 0.9f, 0.3f);
        }
    }

    public float Tamano()
    {
        switch (tipo)
        {
            case Tipo.Jefe: return 26f;
            case Tipo.Objetivo: return 24f;
            case Tipo.Cofre: return 18f;
            default: return 15f;
        }
    }
}
