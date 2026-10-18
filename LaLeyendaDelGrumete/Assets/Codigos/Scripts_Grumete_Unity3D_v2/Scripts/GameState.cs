using UnityEngine;
using UnityEngine.SceneManagement;

// Estado global del juego. Es estatico, asi que se conserva al cambiar de escena.
public static class GameState
{
    public static int Agilidad, Combate, Sigilo, Navegacion, Fragmentos, PruebasHechas;
    public static int Tripulacion;   // bits: 0 Mara, 1 Pico, 2 Sombra, 3 Bruno (ver Tripulacion.cs)
    // compatibilidad: Mara es el bit 0 de la tripulacion
    public static int TieneMara { get => Tripulacion & 1; set => Tripulacion = value != 0 ? Tripulacion | 1 : Tripulacion & ~1; }
    public static int IslaActual = 1;
    public static int VidaKai = 100;
    public static float Aguante = 100f;   // estamina: correr, saltar, atacar y bloquear la gastan
    public static string PrimeraPrueba = "";
    public static float Deteccion;
    public static bool MostrarDeteccion;
    public static bool RutaRevelada;   // la animacion de la tinta solo se ve la primera vez
    public static int IslaDestino = 2; // isla de la ruta trazada en el mapa
    public static bool RutaTrazada;    // el jugador eligio un destino y puede zarpar desde un muelle
    public static Transform Kai;       // lo registra PlayerController (para muelles y mapa)
    public static bool LlegandoEnBarco; // true al desembarcar: Kai aparece en el muelle de la isla
    public static int IslasVistas = 1; // bits: islas cuya revelacion en el mapa ya se mostro (bit 1 = Caleta)
    public static bool IslaVista(int id) => (IslasVistas & (1 << id)) != 0;
    public static void MarcarVista(int id) { IslasVistas |= 1 << id; }
    public static int IslasPisadas = 1 << 1; // bits: islas donde Kai ya desembarco (para dar experiencia una sola vez)
    public static bool IslaPisada(int id) => (IslasPisadas & (1 << id)) != 0;
    public static void MarcarPisada(int id) { IslasPisadas |= 1 << id; }

    static string aviso = "";
    static string hablante = "";
    static float avisoHasta;

    // Reinicia todo (al presionar Play y al empezar una partida nueva desde el menu)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void Reiniciar()
    {
        hablante = "";
        Agilidad = Combate = Sigilo = Navegacion = Fragmentos = PruebasHechas = Tripulacion = 0;
        IslaActual = 1; VidaKai = 100; Aguante = 100f; PrimeraPrueba = "";
        Deteccion = 0; MostrarDeteccion = false; RutaRevelada = false; IslaDestino = 2; RutaTrazada = false; IslasVistas = 1 << 1; aviso = ""; avisoHasta = 0;
        IslasPisadas = 1 << 1; LlegandoEnBarco = false;
        Progreso.Reiniciar();
        Mejoras.Reiniciar();
    }

    // Mensaje del juego (sin nombre)
    public static void Avisar(string texto, float segundos = 3f)
    {
        aviso = texto;
        hablante = "";
        avisoHasta = Time.unscaledTime + segundos;
    }

    // Dialogo de un personaje: se muestra con su nombre arriba
    public static void Hablar(string quien, string texto, float segundos = 4f)
    {
        aviso = texto;
        hablante = quien;
        avisoHasta = Time.unscaledTime + segundos;
    }

    public static string AvisoActual => Time.unscaledTime < avisoHasta ? aviso : "";
    public static string HablanteActual => Time.unscaledTime < avisoHasta ? hablante : "";

    public static void CompletarPrueba(string tipo)
    {
        if (tipo == "agilidad") { if (Agilidad == 1) return; Agilidad = 1; }
        else if (tipo == "combate") { if (Combate == 1) return; Combate = 1; }
        else if (tipo == "sigilo") { if (Sigilo == 1) return; Sigilo = 1; }
        else return;

        PruebasHechas++;
        if (PrimeraPrueba == "") PrimeraPrueba = tipo;
        Avisar("¡Prueba de " + tipo + " superada! (" + PruebasHechas + "/3)");
        Progreso.Ganar(Progreso.XpPrueba);
    }

    // Dano a Kai con numero flotante rojo sobre su cabeza
    public static void DaniarKai(int dano, Vector3 posicion)
    {
        if (global::Tripulacion.FactorDano < 1f && dano > 0)   // Bruno invocado: la mitad del dano
        {
            dano = Mathf.Max(1, Mathf.RoundToInt(dano * global::Tripulacion.FactorDano));
            Efectos.Anillo(posicion, 1.1f, new Color(1f, 0.45f, 0.3f), 0.25f);
        }
        VidaKai -= dano;
        if (dano > 0)
        {
            Sonidos.Tocar("kai_dolor", null, 0.9f, 0.08f, 0.35f);   // quejido de Kai
            Efectos.Chispas(posicion + Vector3.up * 1.1f, Vector3.up, new Color(0.85f, 0.1f, 0.08f), 7, 3.5f, 0.35f, 1f);
            HUD.Herida(Mathf.Clamp(dano / 25f, 0.35f, 1f));   // bordes rojos, mas fuertes si el golpe es grande
        }
        HUD.Numero(posicion + Vector3.up * 1.4f, "-" + dano, new Color(1f, 0.35f, 0.3f), 1f);
    }

    // ---------------------------------------------------------------- partida guardada
    // Se guarda sola cada vez que se cambia de escena (no hace falta un boton de guardar)
    public static bool HayPartida => PlayerPrefs.HasKey("pg_escena");

    public static void Guardar(string escena)
    {
        if (escena == "Menu") return;
        if (escena == "Mapa") escena = "Isla";
        PlayerPrefs.SetString("pg_escena", escena);
        PlayerPrefs.SetString("pg_datos", string.Join(",", new[] {
            Agilidad, Combate, Sigilo, Navegacion, Fragmentos, PruebasHechas, TieneMara,
            IslaActual, VidaKai, RutaRevelada ? 1 : 0, Equipo.Doblones, IslaDestino, IslasVistas, RutaTrazada ? 1 : 0,
            Progreso.Nivel, Progreso.Xp, Equipo.ArmasObtenidas, IslasPisadas, Equipo.arma,
            Mejoras.NivelArmaPor[1], Mejoras.NivelArmaPor[2], Mejoras.NivelArmaPor[3], Mejoras.NivelVela, Tripulacion }));
        PlayerPrefs.SetString("pg_hechos", string.Join("|", RecompensaIsla.Hechas));
        string cant = "";
        foreach (var c in Equipo.Pociones) cant += c.cantidad + ",";
        foreach (var c in Equipo.Objetos) cant += c.cantidad + ",";
        cant += Equipo.Madera + "," + Equipo.Chatarra;   // recursos al final
        PlayerPrefs.SetString("pg_equipo", cant);
        PlayerPrefs.SetString("pg_primera", PrimeraPrueba);
        PlayerPrefs.Save();
    }

    public static void Continuar()
    {
        if (!HayPartida) return;
        Reiniciar(); Equipo.Reiniciar();
        var d = PlayerPrefs.GetString("pg_datos").Split(',');
        int i = 0;
        Agilidad = Leer(d, ref i); Combate = Leer(d, ref i); Sigilo = Leer(d, ref i); Navegacion = Leer(d, ref i);
        Fragmentos = Leer(d, ref i); PruebasHechas = Leer(d, ref i); TieneMara = Leer(d, ref i);
        IslaActual = Mathf.Max(1, Leer(d, ref i)); VidaKai = Leer(d, ref i);
        RutaRevelada = Leer(d, ref i) == 1; Equipo.Doblones = Leer(d, ref i);
        IslaDestino = Mathf.Max(2, Leer(d, ref i)); IslasVistas = Leer(d, ref i) | (1 << 1);
        RutaTrazada = Leer(d, ref i) == 1;
        Progreso.Nivel = Mathf.Max(1, Leer(d, ref i)); Progreso.Xp = Leer(d, ref i);
        Equipo.ArmasObtenidas = Leer(d, ref i) | 1;
        if (Combate > 0 || PruebasHechas > 0) Equipo.ArmasObtenidas |= 1 << (int)Equipo.IdArma.Sable;   // partidas viejas
        IslasPisadas = Leer(d, ref i) | (1 << 1) | (1 << IslaActual);
        Equipo.arma = Leer(d, ref i);
        for (int k = 1; k <= 3; k++) Mejoras.NivelArmaPor[k] = Mathf.Clamp(Leer(d, ref i), 0, Mejoras.MaxArma);
        Mejoras.NivelVela = Mathf.Clamp(Leer(d, ref i), 0, Mejoras.MaxVela);
        Tripulacion |= Leer(d, ref i);
        RecompensaIsla.Hechas.Clear();
        foreach (var h in PlayerPrefs.GetString("pg_hechos", "").Split('|')) if (h != "") RecompensaIsla.Hechas.Add(h);
        if (VidaKai <= 0) VidaKai = Progreso.VidaMax;
        var e = PlayerPrefs.GetString("pg_equipo").Split(',');
        int j = 0;
        foreach (var c in Equipo.Pociones) if (j < e.Length) c.cantidad = Leer(e, ref j);
        foreach (var c in Equipo.Objetos) if (j < e.Length) c.cantidad = Leer(e, ref j);
        Equipo.Madera = Leer(e, ref j); Equipo.Chatarra = Leer(e, ref j);
        PrimeraPrueba = PlayerPrefs.GetString("pg_primera", "");
        CargarEscena(PlayerPrefs.GetString("pg_escena", "Isla"));
    }

    static int Leer(string[] d, ref int i)
    {
        int v = 0;
        if (i < d.Length) int.TryParse(d[i], out v);
        i++;
        return v;
    }

    public static void NuevaPartida()
    {
        PlayerPrefs.DeleteKey("pg_escena"); PlayerPrefs.DeleteKey("pg_datos"); PlayerPrefs.DeleteKey("pg_equipo"); PlayerPrefs.DeleteKey("pg_hechos");
        Reiniciar(); Equipo.Reiniciar(); RecompensaIsla.Hechas.Clear();
        CargarEscena("Isla");
    }

    // Cambia de escena con fundido a negro (si hay HUD en la escena)
    public static void CargarEscena(string nombre)
    {
        Guardar(nombre == "Menu" ? SceneManager.GetActiveScene().name : nombre);
        MostrarDeteccion = false;
        Time.timeScale = 1f;
        if (HUD.Actual != null) HUD.Actual.Transicion(nombre);
        else SceneManager.LoadScene(nombre);
    }
}

// Compatibilidad: Unity 6 cambio rb.velocity por rb.linearVelocity
public static class RbExt
{
    public static Vector3 GetVel(this Rigidbody rb)
    {
#if UNITY_6000_0_OR_NEWER
        return rb.linearVelocity;
#else
        return rb.velocity;
#endif
    }

    public static void SetVel(this Rigidbody rb, Vector3 v)
    {
#if UNITY_6000_0_OR_NEWER
        rb.linearVelocity = v;
#else
        rb.velocity = v;
#endif
    }
}
