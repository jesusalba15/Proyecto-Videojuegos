using UnityEngine;

// Tripulacion de Kai: cada tripulante se recluta en una isla y se INVOCA con G (H cambia de tripulante),
// como los Elegidos de Zelda BotW. Su bonus solo funciona mientras esta invocado (ver Invocacion.cs):
//   Mara "Nudos"  (Faro Roto)          -> barco +50% y Kai +15% a pie
//   Pico          (Astillero Hundido)  -> el Taller cobra 25% menos (y al unirse regala una vela nueva;
//                                         sin el, el barco no llega a las islas lejanas 4 y 5)
//   Sombra        (Bahia de la Corona) -> los guardias tardan mucho mas en verte
//   Bruno         (Fuerte de Roca)     -> recibes la mitad del dano
// Se recluta con un RecompensaIsla (campo "Tripulante") o con el Cofre de Mara.
public static class Tripulacion
{
    public enum Miembro { Ninguno = -1, Mara = 0, Pico = 1, Sombra = 2, Bruno = 3 }

    public static readonly string[] Nombres = { "Mara \"Nudos\"", "Pico", "Sombra", "Bruno" };
    public static readonly string[] Bonus =
    {
        "barco +50% de velocidad y tú +15% a pie",
        "el taller cobra 25% menos",
        "los guardias tardan mucho más en verte",
        "recibes la mitad del daño",
    };

    public static bool Tiene(Miembro m) => m != Miembro.Ninguno && (GameState.Tripulacion & (1 << (int)m)) != 0;
    public static int Cuantos { get { int n = 0; for (int i = 0; i < 4; i++) if ((GameState.Tripulacion & (1 << i)) != 0) n++; return n; } }

    // bonus de cada tripulante: solo mientras esta invocado (los usan otros scripts)
    public static bool Invocado(Miembro m) => Tiene(m) && Invocacion.Actual && Invocacion.Actual.Activo((int)m);
    public static float BonoBarco => Invocado(Miembro.Mara) ? 1.5f : 1f;
    public static float BonoPie => Invocado(Miembro.Mara) ? 1.15f : 1f;
    public static float DescuentoTaller => Invocado(Miembro.Pico) ? 0.75f : 1f;
    public static float FactorDeteccion => Invocado(Miembro.Sombra) ? 0.35f : 1f;
    public static float FactorDano => Invocado(Miembro.Bruno) ? 0.5f : 1f;
    public static int VidaExtra => 0;   // Bruno ya no da vida fija: ahora protege al invocarlo

    public static void Reclutar(Miembro m)
    {
        if (m == Miembro.Ninguno || Tiene(m)) return;
        GameState.Tripulacion |= 1 << (int)m;
        if (GameState.Kai)
        {
            HUD.Numero(GameState.Kai.position + Vector3.up * 2.6f, "¡" + Nombres[(int)m] + " se une!", new Color(0.35f, 0.85f, 0.85f), 1.3f);
            Efectos.Anillo(GameState.Kai.position, 2.2f, new Color(0.35f, 0.85f, 0.85f), 0.7f);
        }
        GameState.Avisar(Nombres[(int)m] + " se unió a la tripulación. Invócalo con G: " + Bonus[(int)m] + ".", 4f);
    }
}
