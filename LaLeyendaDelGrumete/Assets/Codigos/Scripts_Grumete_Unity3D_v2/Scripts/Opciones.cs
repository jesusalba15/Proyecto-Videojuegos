using UnityEngine;

// Opciones del jugador. Se guardan en el computador (PlayerPrefs) y se aplican al iniciar el juego.
public static class Opciones
{
    public static float Volumen = 0.8f;          // 0 a 1
    public static float Sensibilidad = 1f;       // multiplicador de la camara (0.3 a 2)
    public static bool InvertirY = false;
    public static bool PantallaCompleta = true;
    public static int Calidad = -1;              // -1 = la que traiga el proyecto

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Cargar()
    {
        Volumen = PlayerPrefs.GetFloat("op_volumen", 0.8f);
        Sensibilidad = PlayerPrefs.GetFloat("op_sensibilidad", 1f);
        InvertirY = PlayerPrefs.GetInt("op_invertirY", 0) == 1;
        PantallaCompleta = PlayerPrefs.GetInt("op_completa", Screen.fullScreen ? 1 : 0) == 1;
        Calidad = PlayerPrefs.GetInt("op_calidad", QualitySettings.GetQualityLevel());
        Aplicar();
    }

    public static void Aplicar()
    {
        AudioListener.volume = Volumen;
        if (Screen.fullScreen != PantallaCompleta) Screen.fullScreen = PantallaCompleta;
        if (Calidad >= 0 && Calidad < QualitySettings.names.Length && Calidad != QualitySettings.GetQualityLevel())
            QualitySettings.SetQualityLevel(Calidad, true);
    }

    public static void Guardar()
    {
        PlayerPrefs.SetFloat("op_volumen", Volumen);
        PlayerPrefs.SetFloat("op_sensibilidad", Sensibilidad);
        PlayerPrefs.SetInt("op_invertirY", InvertirY ? 1 : 0);
        PlayerPrefs.SetInt("op_completa", PantallaCompleta ? 1 : 0);
        PlayerPrefs.SetInt("op_calidad", Calidad);
        PlayerPrefs.Save();
        Aplicar();
    }
}
