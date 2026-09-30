using UnityEngine;
using UnityEngine.SceneManagement;

// Estado global del juego. Es estatico, asi que se conserva al cambiar de escena.
public static class GameState
{
    public static int Agilidad, Combate, Sigilo, Navegacion, Fragmentos, PruebasHechas, TieneMara;
    public static int IslaActual = 1;
    public static int VidaKai = 100;
    public static string PrimeraPrueba = "";
    public static float Deteccion;
    public static bool MostrarDeteccion;
    public static bool RutaRevelada;   // la animacion de la tinta solo se ve la primera vez

    static string aviso = "";
    static float avisoHasta;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reiniciar()
    {
        Agilidad = Combate = Sigilo = Navegacion = Fragmentos = PruebasHechas = TieneMara = 0;
        IslaActual = 1; VidaKai = 100; PrimeraPrueba = "";
        Deteccion = 0; MostrarDeteccion = false; RutaRevelada = false; aviso = ""; avisoHasta = 0;
    }

    public static void Avisar(string texto, float segundos = 3f)
    {
        aviso = texto;
        avisoHasta = Time.time + segundos;
    }

    public static string AvisoActual => Time.time < avisoHasta ? aviso : "";

    public static void CompletarPrueba(string tipo)
    {
        if (tipo == "agilidad") { if (Agilidad == 1) return; Agilidad = 1; }
        else if (tipo == "combate") { if (Combate == 1) return; Combate = 1; }
        else if (tipo == "sigilo") { if (Sigilo == 1) return; Sigilo = 1; }
        else return;

        PruebasHechas++;
        if (PrimeraPrueba == "") PrimeraPrueba = tipo;
        Avisar("¡Prueba de " + tipo + " superada! (" + PruebasHechas + "/3)");
    }

    public static void CargarEscena(string nombre)
    {
        MostrarDeteccion = false;
        SceneManager.LoadScene(nombre);
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