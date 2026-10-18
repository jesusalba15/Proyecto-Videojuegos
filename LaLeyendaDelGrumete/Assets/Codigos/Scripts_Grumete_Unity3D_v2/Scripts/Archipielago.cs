using UnityEngine;

// Datos de las islas del archipielago (mapa del mundo, estilo Albion).
// Para cambiar nombres, requisitos, posiciones en el mapa o activar una isla nueva, edita esta tabla.
public static class Archipielago
{
    public enum Tipo { Libre, Corona }

    public class Isla
    {
        public int id; public string nombre; public Tipo tipo;
        public int peligro;                // 1 bajo (verde), 2 medio (amarillo), 3 alto (rojo), 4 maximo (negro)
        public int fragmentos;             // fragmentos de mapa para que la tinta la revele
        public int agi, com, sig, nav;     // destrezas necesarias para zarpar hacia ella
        public int nivel;                  // nivel de Kai necesario (0 = cualquiera)
        public Tripulacion.Miembro tripulante = Tripulacion.Miembro.Ninguno;   // tripulante necesario (Pico: sin el no se llega a islas lejanas)
        public int origen;                 // isla desde la que sale su ruta (0 = ninguna)
        public Vector2 posicion;           // posicion en el mapa del mundo (0 a 1)
        public bool disponible;            // true = forzar disponible. Si no, se activa sola cuando hay una ZonaIsla con su id en la escena
        public bool Disponible => disponible || ZonaIsla.Existe(id);
        public string descripcion, recompensa;
        public string escena = "Isla";    // escena de Unity donde esta construida (varias islas pueden compartir escena)

        public bool Revelada => id == 1 || GameState.Fragmentos >= fragmentos;
        public bool EsActual => GameState.IslaActual == id;
        public string Falta()
        {
            string f = "";
            if (Progreso.Nivel < nivel) f += "Nivel " + nivel + "  ";
            if (GameState.Agilidad < agi) f += "Agilidad " + agi + "  ";
            if (GameState.Combate < com) f += "Combate " + com + "  ";
            if (GameState.Sigilo < sig) f += "Sigilo " + sig + "  ";
            if (GameState.Navegacion < nav) f += "Navegación " + nav + "  ";
            if (tripulante != Tripulacion.Miembro.Ninguno && !Tripulacion.Tiene(tripulante)) f += "Tripulante " + Tripulacion.Nombres[(int)tripulante] + "  ";
            return f.Trim();
        }
        public bool Cumple => Falta() == "";
    }

    public static readonly Isla[] Islas =
    {
        new Isla { id = 1, nombre = "Caleta Gris", tipo = Tipo.Libre, peligro = 1, fragmentos = 0, origen = 0,
                   posicion = new Vector2(0.14f, 0.40f), disponible = true,
                   descripcion = "Tu aldea natal, pobre pero honrada. Aquí empezó todo.", recompensa = "Primer nudo del mapa" },
        new Isla { id = 2, nombre = "Isla del Faro Roto", tipo = Tipo.Libre, peligro = 1, fragmentos = 1, origen = 1,
                   posicion = new Vector2(0.34f, 0.66f), disponible = true,
                   descripcion = "Un viejo faro apagado. Dicen que alguien espera atrapada en un cofre.", recompensa = "Tripulante: Mara \"Nudos\"" },
        new Isla { id = 3, nombre = "Astillero Hundido", tipo = Tipo.Libre, peligro = 2, fragmentos = 2, com = 1, nivel = 3, origen = 2,
                   posicion = new Vector2(0.52f, 0.38f), disponible = false,
                   descripcion = "Cascos podridos y un carpintero gruñón entre la madera.", recompensa = "Tripulante: Pico · vela nueva · pistola" },
        new Isla { id = 4, nombre = "Bahía de la Corona", tipo = Tipo.Corona, peligro = 3, fragmentos = 3, sig = 1, nav = 1, nivel = 5, origen = 3, tripulante = Tripulacion.Miembro.Pico,
                   posicion = new Vector2(0.66f, 0.72f), disponible = false,
                   descripcion = "Puerto de la Armada. De noche los Cazadores patrullan con faroles.", recompensa = "Tripulante: Sombra · ganzúa" },
        new Isla { id = 5, nombre = "Fuerte de Roca", tipo = Tipo.Libre, peligro = 2, fragmentos = 4, com = 1, agi = 1, nivel = 6, origen = 3, tripulante = Tripulacion.Miembro.Pico,
                   posicion = new Vector2(0.72f, 0.26f), disponible = false,
                   descripcion = "Un fuerte de piedra tomado por rufianes. Solo los fuertes pasan.", recompensa = "Tripulante: Bruno · fragmento" },
        new Isla { id = 6, nombre = "Fortaleza de Voss", tipo = Tipo.Corona, peligro = 4, fragmentos = 5, sig = 1, com = 1, nivel = 8, origen = 4, tripulante = Tripulacion.Miembro.Sombra,
                   posicion = new Vector2(0.88f, 0.55f), disponible = false,
                   descripcion = "La fortaleza de la Almirante Renata Voss. El Corazón de las Mareas espera.", recompensa = "El Corazón de las Mareas" },
    };

    public static Isla Buscar(int id) { foreach (var i in Islas) if (i.id == id) return i; return null; }

    public static Color ColorPeligro(int p)
    {
        switch (p)
        {
            case 1: return new Color(0.35f, 0.75f, 0.35f);
            case 2: return new Color(0.95f, 0.78f, 0.25f);
            case 3: return new Color(0.88f, 0.25f, 0.2f);
            default: return new Color(0.2f, 0.12f, 0.22f);
        }
    }

    public static string NombrePeligro(int p)
    {
        switch (p) { case 1: return "Bajo"; case 2: return "Medio"; case 3: return "Alto"; default: return "Extremo"; }
    }

    // Camino de islas entre dos islas siguiendo las rutas (para resaltar la ruta trazada)
    public static System.Collections.Generic.List<int> Camino(int desde, int hasta)
    {
        var padres = new System.Collections.Generic.Dictionary<int, int>();
        var cola = new System.Collections.Generic.Queue<int>();
        cola.Enqueue(desde); padres[desde] = 0;
        while (cola.Count > 0)
        {
            int a = cola.Dequeue();
            if (a == hasta) break;
            foreach (var i in Islas)
            {
                int b = i.id == a ? i.origen : (i.origen == a ? i.id : 0);
                if (b != 0 && !padres.ContainsKey(b)) { padres[b] = a; cola.Enqueue(b); }
            }
        }
        var camino = new System.Collections.Generic.List<int>();
        if (!padres.ContainsKey(hasta)) return camino;
        for (int x = hasta; x != 0; x = padres[x]) camino.Insert(0, x);
        return camino;
    }
}
