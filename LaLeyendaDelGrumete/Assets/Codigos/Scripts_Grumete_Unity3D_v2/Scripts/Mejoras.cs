using UnityEngine;

// Mejoras compradas en el Taller (se guardan con la partida).
//   Armas: cada nivel +15% de dano (maximo +4).
//   Vela del barco: cada nivel +15% de velocidad (maximo +3).
// Los precios se cambian aqui.
public static class Mejoras
{
    public const int MaxArma = 4, MaxVela = 3;
    public const float BonoPorNivelArma = 0.15f, BonoPorNivelVela = 0.15f;

    public static readonly int[] NivelArmaPor = new int[4];   // por IdArma
    public static int NivelVela;

    public static int NivelArma(Equipo.IdArma a) => NivelArmaPor[(int)a];
    public static float BonoArmaActual => 1f + BonoPorNivelArma * NivelArmaPor[Mathf.Clamp(Equipo.arma, 0, 3)];
    public static float BonoVela => 1f + BonoPorNivelVela * NivelVela;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void Reiniciar() { for (int i = 0; i < NivelArmaPor.Length; i++) NivelArmaPor[i] = 0; NivelVela = 0; }

    // ---------------- precios
    public struct Precio { public int doblones, madera, chatarra; }

    public static Precio PrecioArma(Equipo.IdArma a)
    {
        int n = NivelArma(a) + 1;   // nivel que se compra
        var p = new Precio { doblones = 30 * n };
        if (a == Equipo.IdArma.Pistola) p.chatarra = 2 * n; else p.madera = 2 * n;
        if (n >= 3) p.chatarra += n - 2;   // los ultimos niveles piden un poco de chatarra
        return ConDescuento(p);
    }

    public static Precio PrecioVela() { int n = NivelVela + 1; return ConDescuento(new Precio { doblones = 50 * n, madera = 4 * n, chatarra = 2 * n }); }

    // Pico (tripulante del Astillero) consigue precios de carpintero: 25% menos
    static Precio ConDescuento(Precio p)
    {
        float f = Tripulacion.DescuentoTaller;
        return new Precio { doblones = Mathf.CeilToInt(p.doblones * f), madera = Mathf.CeilToInt(p.madera * f), chatarra = Mathf.CeilToInt(p.chatarra * f) };
    }

    public static bool Alcanza(Precio p) => Equipo.Doblones >= p.doblones && Equipo.Madera >= p.madera && Equipo.Chatarra >= p.chatarra;
    public static void Pagar(Precio p) { Equipo.Doblones -= p.doblones; Equipo.Madera -= p.madera; Equipo.Chatarra -= p.chatarra; }

    public static string Texto(Precio p)
    {
        string s = "<color=#F2C040>" + p.doblones + " doblones</color>";
        if (p.madera > 0) s += "  <color=#D9A066>" + p.madera + " madera</color>";
        if (p.chatarra > 0) s += "  <color=#BFC4CC>" + p.chatarra + " chatarra</color>";
        return s;
    }
}
