using UnityEngine;

// Equipo rapido de Kai (el "triangulo" de abajo a la derecha, estilo Dark Souls version pirata).
//   Arriba: ARMA       -> Z cambia de arma.        Clic / J ataca.   1 2 3 4 habilidades del arma (estilo Albion)
//   Abajo izq: POCION  -> X cambia de pocion.      Q la usa.
//   Abajo der: OBJETO  -> T cambia de objeto.      R lo usa.
// Kai empieza con los PUNOS. Las armas se consiguen en la aventura:
//   Sable   -> te lo da Barbagris (Caleta Gris)
//   Hacha   -> Isla del Faro Roto (un enemigo con "Arma Que Suelta" o un ArmaEnSuelo)
//   Pistola -> Astillero Hundido (igual)
// Los iconos se cargan solos desde Assets/Resources/Iconos/<icono>.png (si no estan, se usa un color).
// Los de las habilidades desde Assets/Resources/Habilidades/<icono>.png.
public static class Equipo
{
    public enum IdArma { Punos = 0, Sable = 1, Hacha = 2, Pistola = 3 }

    public class Arma
    {
        public string nombre, icono; public int danoMin, danoMax; public float espera, costoAguante, alcance;
        public bool aDistancia; public float rango;   // armas de fuego: disparan en linea recta
        public Habilidad[] habilidades;               // las habilidades de la barra (teclas 1 a 4)
        public Arma(string n, string i, int min, int max, float esp, float aguante, float alc, params Habilidad[] h)
        { nombre = n; icono = i; danoMin = min; danoMax = max; espera = esp; costoAguante = aguante; alcance = alc; habilidades = h; }
    }

    public enum TipoHabilidad { Estocada, Area, Cono, Grito, Furia, Enganche, Rafaga, Sangrado, Abordaje, Disparo, Explosivo }

    // Habilidad del arma. mult = multiplicador del dano del arma.
    public class Habilidad
    {
        public string nombre, icono, descripcion; public TipoHabilidad tipo;
        public float espera, aguante, mult = 1f, radio = 2f, alcance, aturdir, empuje; public int golpes = 1;
        public Color color = new Color(0.95f, 0.75f, 0.3f);
    }

    // Nivel de Kai necesario para cada casilla de habilidad (tecla 1, 2, 3, 4). Pon todo en 1 para tenerlas desde el inicio.
    public static readonly int[] NivelHabilidad = { 1, 2, 4, 6 };

    public static readonly Arma[] Armas =
    {
        new Arma("Puños", "punos", 5, 8, 0.45f, 10f, 1.0f),   // sin habilidades

        new Arma("Sable de madera", "sable", 14, 20, 0.50f, 18f, 1.2f,
            new Habilidad { nombre = "Estocada", icono = "estocada", tipo = TipoHabilidad.Estocada, espera = 4f, aguante = 15f, mult = 1.4f, alcance = 4f, radio = 1.4f,
                            descripcion = "Te lanzas hacia adelante y atraviesas al enemigo." },
            new Habilidad { nombre = "Torbellino", icono = "torbellino", tipo = TipoHabilidad.Area, espera = 8f, aguante = 25f, mult = 0.9f, radio = 3f,
                            descripcion = "Giras con el sable y golpeas a todos a tu alrededor." },
            new Habilidad { nombre = "Grito pirata", icono = "grito", tipo = TipoHabilidad.Grito, espera = 12f, aguante = 10f, radio = 5f, aturdir = 1.8f,
                            descripcion = "Un grito que deja aturdidos a los enemigos cercanos.", color = new Color(0.55f, 0.8f, 1f) },
            new Habilidad { nombre = "Furia del grumete", icono = "furia", tipo = TipoHabilidad.Furia, espera = 25f, golpes = 3,
                            descripcion = "Tus 3 siguientes golpes son críticos.", color = new Color(1f, 0.4f, 0.2f) }),

        new Arma("Hacha", "hacha", 24, 34, 0.90f, 30f, 1.6f,
            new Habilidad { nombre = "Hachazo", icono = "hachazo", tipo = TipoHabilidad.Cono, espera = 5f, aguante = 20f, mult = 1.4f, radio = 2.6f, empuje = 3f,
                            descripcion = "Golpe de frente que hace retroceder al enemigo." },
            new Habilidad { nombre = "Giro del leñador", icono = "giro", tipo = TipoHabilidad.Area, espera = 9f, aguante = 25f, mult = 1f, radio = 3.2f, aturdir = 0.8f,
                            descripcion = "Giras con el hacha y aturdes a todos a tu alrededor." },
            new Habilidad { nombre = "Hacha arrojadiza", icono = "arrojadiza", tipo = TipoHabilidad.Disparo, espera = 10f, aguante = 20f, mult = 1.2f, alcance = 12f, aturdir = 0.6f,
                            descripcion = "Lanzas el hacha al enemigo de enfrente y vuelve a tu mano.", color = new Color(0.75f, 0.55f, 0.35f) },
            new Habilidad { nombre = "Partetroncos", icono = "partetroncos", tipo = TipoHabilidad.Area, espera = 25f, aguante = 35f, mult = 2.2f, radio = 3.5f, aturdir = 1.5f,
                            descripcion = "Un golpe enorme contra el suelo. Mucho daño y aturde.", color = new Color(1f, 0.5f, 0.2f) }),

        new Arma("Pistola de chispa", "pistola", 18, 26, 1.10f, 8f, 1.0f,
            new Habilidad { nombre = "Disparo certero", icono = "certero", tipo = TipoHabilidad.Disparo, espera = 5f, aguante = 15f, mult = 2f, alcance = 22f,
                            descripcion = "Apuntas con calma: un disparo de mucho daño a gran distancia.", color = new Color(1f, 0.9f, 0.5f) },
            new Habilidad { nombre = "Doble cañón", icono = "doble", tipo = TipoHabilidad.Disparo, espera = 7f, aguante = 20f, mult = 0.9f, alcance = 16f, golpes = 2,
                            descripcion = "Dos disparos seguidos al mismo enemigo.", color = new Color(1f, 0.9f, 0.5f) },
            new Habilidad { nombre = "Bala de pólvora", icono = "polvora", tipo = TipoHabilidad.Explosivo, espera = 12f, aguante = 20f, mult = 1.2f, alcance = 16f, radio = 2.8f,
                            descripcion = "La bala explota y daña a todos los que estén cerca.", color = new Color(1f, 0.5f, 0.15f) },
            new Habilidad { nombre = "Culatazo", icono = "culatazo", tipo = TipoHabilidad.Cono, espera = 10f, aguante = 15f, mult = 0.8f, radio = 2f, aturdir = 1.5f, empuje = 3f,
                            descripcion = "Golpe con la culata para alejar a quien se acerque demasiado." }),
    };

    // la pistola dispara en linea recta en vez de golpear de cerca
    static Equipo()
    {
        var p = Armas[(int)IdArma.Pistola];
        p.aDistancia = true; p.rango = 16f;
    }

    public static readonly Consumible[] Pociones =
    {
        // Kai empieza con 0 de todo: se consiguen en botines (enemigos), palmeras (cocos) y mas adelante en la tienda.
        // El ultimo numero de cada fila es lo que cura / dura; el penultimo es la cantidad inicial.
        new Consumible("Grog de curación", "grog",  "vida",    0, 40),
        new Consumible("Ron añejo",        "ron",   "vida",    0, 100),
        new Consumible("Agua de coco",     "coco",  "aguante", 0, 100),
    };

    public static readonly Consumible[] Objetos =
    {
        new Consumible("Bomba de humo", "humo",      "humo",   0, 4),    // valor = segundos oculto
        new Consumible("Doblón",        "doblon",    "nada",   0, 0),
    };

    public class Consumible
    {
        public string nombre, icono, efecto; public int cantidad, cantidadInicial, valor;
        public Consumible(string n, string i, string e, int cant, int val)
        { nombre = n; icono = i; efecto = e; cantidad = cantidadInicial = cant; valor = val; }
    }

    public static int arma, pocion, objeto;
    public static int ArmasObtenidas = 1;      // bits por IdArma (bit 0 = puños, siempre)
    public static int Doblones;
    public static int Madera, Chatarra;        // recursos para mejoras (se recogen o salen de botin)
    public static int CriticosGarantizados;    // "Furia del grumete": los siguientes golpes son criticos
    public static int CriticoProb = 15;        // % de golpe critico (dano x1.5)

    public static Arma ArmaActual => Armas[Tiene((IdArma)arma) ? arma : 0];
    public static Consumible PocionActual => Pociones[pocion];
    public static Consumible ObjetoActual => Objetos[objeto];

    public static bool Tiene(IdArma a) => (ArmasObtenidas & (1 << (int)a)) != 0;
    public static bool HabilidadDesbloqueada(int casilla) => casilla < NivelHabilidad.Length && Progreso.Nivel >= NivelHabilidad[casilla];

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void Reiniciar()
    {
        arma = pocion = objeto = 0; Doblones = 0; Madera = Chatarra = 0; CriticosGarantizados = 0;
        ArmasObtenidas = 1;
        foreach (var p in Pociones) p.cantidad = p.cantidadInicial;
        foreach (var o in Objetos) o.cantidad = o.cantidadInicial;
    }

    // Da un arma nueva a Kai y se la pone en la mano
    public static void Desbloquear(IdArma a)
    {
        if (Tiene(a)) return;
        ArmasObtenidas |= 1 << (int)a;
        arma = (int)a;
        var w = Armas[(int)a];
        if (GameState.Kai)
        {
            HUD.Numero(GameState.Kai.position + Vector3.up * 2.4f, "¡" + w.nombre + "!", new Color(1f, 0.85f, 0.3f), 1.4f);
            Efectos.Anillo(GameState.Kai.position, 2f, new Color(1f, 0.85f, 0.3f), 0.6f);
        }
        GameState.Avisar("Nueva arma: " + w.nombre + ".  Clic para atacar, 1 a 4 para sus habilidades, Z para cambiar de arma.", 4f);
    }

    // Z: pasa a la siguiente arma que Kai tenga (los puños solo si no tiene ninguna otra)
    public static void SiguienteArma()
    {
        bool tieneOtra = ArmasObtenidas > 1;
        for (int k = 1; k <= Armas.Length; k++)
        {
            int i = (arma + k) % Armas.Length;
            if (i == 0 && tieneOtra) continue;
            if (Tiene((IdArma)i)) { arma = i; return; }
        }
    }

    public static void Siguiente(ref int i, int largo) { i = (i + 1) % largo; }

    // Calcula el dano de un golpe con el arma actual (incluye el bono de nivel). critico = si fue golpe critico
    public static int TirarDano(out bool critico)
    {
        float d = Random.Range(ArmaActual.danoMin, ArmaActual.danoMax + 1) * Progreso.BonoDano * Mejoras.BonoArmaActual;   // nivel de Kai y mejoras del taller
        critico = Random.Range(0, 100) < CriticoProb;
        if (CriticosGarantizados > 0) { CriticosGarantizados--; critico = true; }
        return Mathf.RoundToInt(critico ? d * 1.5f : d);
    }
}
