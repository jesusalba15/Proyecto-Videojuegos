using UnityEngine;

// Experiencia y nivel de Kai (estilo RPG).
//   Progreso.Ganar(40)  -> suma experiencia; al llenar la barra sube de nivel.
// Cada nivel: +10 de vida maxima y +6% de dano. Al subir se cura por completo.
// Las habilidades del arma se desbloquean por nivel (ver Equipo.NivelHabilidad).
// De donde sale la experiencia (los valores se cambian aqui):
public static class Progreso
{
    public const int XpEnemigo = 40;      // por defecto; cada Rival tiene su campo "experiencia"
    public const int XpPrueba = 60;
    public const int XpFragmento = 80;
    public const int XpCofre = 100;
    public const int XpIslaNueva = 100;
    public const int XpRecurso = 4;
    public const int NivelMaximo = 20;

    public static int Nivel = 1, Xp;

    public static int XpParaSubir => 80 + 60 * (Nivel - 1) + 20 * (Nivel - 1) * (Nivel - 1);
    public static float Fraccion => Nivel >= NivelMaximo ? 1f : Mathf.Clamp01((float)Xp / XpParaSubir);
    public static int VidaMax => 100 + 10 * (Nivel - 1) + Tripulacion.VidaExtra;   // Bruno: +30
    public static float BonoDano => 1f + 0.06f * (Nivel - 1);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    public static void Reiniciar() { Nivel = 1; Xp = 0; }

    public static void Ganar(int cantidad)
    {
        if (cantidad <= 0 || Nivel >= NivelMaximo) return;
        Xp += cantidad;
        Vector3 p = GameState.Kai ? GameState.Kai.position + Vector3.up * 2.3f : Vector3.zero;
        if (GameState.Kai) HUD.Numero(p, "+" + cantidad + " EXP", new Color(0.72f, 0.58f, 1f), 0.8f);

        while (Nivel < NivelMaximo && Xp >= XpParaSubir)
        {
            Xp -= XpParaSubir;
            Nivel++;
            GameState.VidaKai = VidaMax;
            GameState.Aguante = 100f;
            if (GameState.Kai)
            {
                HUD.Numero(p + Vector3.up * 0.5f, "¡NIVEL " + Nivel + "!", new Color(1f, 0.85f, 0.3f), 1.6f);
                Efectos.Anillo(GameState.Kai.position, 2.5f, new Color(1f, 0.85f, 0.3f), 0.7f);
                Efectos.Anillo(GameState.Kai.position, 1.4f, new Color(0.72f, 0.58f, 1f), 0.5f);
                Efectos.Columna(GameState.Kai.position, new Color(1f, 0.85f, 0.3f), 26, 1.4f);
                Efectos.Destello(GameState.Kai.position + Vector3.up * 1.5f, new Color(1f, 0.85f, 0.4f), 3f, 6f, 0.5f);
                Sonidos.Tocar("nivel", null, 0.9f, 0f, 0.5f);
            }
            string extra = "";
            for (int i = 0; i < Equipo.NivelHabilidad.Length; i++)
                if (Equipo.NivelHabilidad[i] == Nivel) extra = "  Nueva habilidad: tecla " + (i + 1) + ".";
            GameState.Avisar("¡Subiste a nivel " + Nivel + "! Vida máxima " + VidaMax + ", daño +" + Mathf.RoundToInt((BonoDano - 1f) * 100f) + "%." + extra, 3f);
        }
        if (Nivel >= NivelMaximo) Xp = 0;
    }
}
