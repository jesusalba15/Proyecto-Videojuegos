using UnityEngine;

// Ponlo en la Main Camera (o en un objeto vacio) de cada escena.
// Dibuja el estado, los avisos y el medidor de sigilo sobre paneles oscuros semitransparentes.
// Se adapta solo al tamano de la ventana (referencia: 1080 de alto).
// F1 = ir al mapa con las 3 pruebas hechas. F2 = ir directo al Faro Roto.
public class HUD : MonoBehaviour
{
    [Range(0f, 1f)] public float opacidadPanel = 0.55f;

    GUIStyle estilo, grande, pequeno;
    Texture2D fondo, blanco;
    int alturaPreparada = -1;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F1))
        {
            GameState.Agilidad = GameState.Combate = GameState.Sigilo = 1;
            GameState.PruebasHechas = 3;
            GameState.Fragmentos = 1;
            GameState.CargarEscena("Mapa");
        }
        if (Input.GetKeyDown(KeyCode.F2))
        {
            GameState.IslaActual = 2;
            GameState.CargarEscena("Isla");
        }
    }

    float Escala => Mathf.Max(0.5f, Screen.height / 1080f);

    void Preparar()
    {
        if (estilo != null && alturaPreparada == Screen.height) return;
        alturaPreparada = Screen.height;
        float k = Escala;

        if (!fondo)
        {
            fondo = new Texture2D(1, 1);
            fondo.SetPixel(0, 0, new Color(0f, 0f, 0f, opacidadPanel));
            fondo.Apply();
            blanco = Texture2D.whiteTexture;
        }

        estilo = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(24 * k),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft
        };
        estilo.normal.textColor = Color.white;

        pequeno = new GUIStyle(estilo) { fontSize = Mathf.RoundToInt(18 * k) };

        grande = new GUIStyle(estilo)
        {
            fontSize = Mathf.RoundToInt(32 * k),
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true
        };
    }

    void OnGUI()
    {
        Preparar();
        float k = Escala;
        float margen = 20 * k;
        float relleno = 12 * k;

        // Panel de estado (arriba a la izquierda)
        string estado = "AGI " + GameState.Agilidad + "   COM " + GameState.Combate +
                        "   SIG " + GameState.Sigilo + "   NAV " + GameState.Navegacion +
                        "   Fragmentos " + GameState.Fragmentos +
                        "   Vida " + GameState.VidaKai +
                        "   Mara " + (GameState.TieneMara == 1 ? "Sí" : "No");
        Vector2 tam = estilo.CalcSize(new GUIContent(estado));
        Rect panel = new Rect(margen, margen, tam.x + relleno * 2, tam.y + relleno);
        GUI.DrawTexture(panel, fondo);
        GUI.Label(new Rect(panel.x + relleno, panel.y, tam.x, panel.height), estado, estilo);

        // Medidor de deteccion (debajo del panel)
        if (GameState.MostrarDeteccion)
        {
            float y = panel.yMax + 8 * k;
            float ancho = 320 * k, alto = 20 * k;
            Rect caja = new Rect(margen, y, ancho + relleno * 2 + 130 * k, alto + relleno);
            GUI.DrawTexture(caja, fondo);

            Rect barra = new Rect(caja.x + relleno, caja.y + relleno / 2, ancho, alto);
            GUI.color = new Color(1f, 1f, 1f, 0.25f);
            GUI.DrawTexture(barra, blanco);
            GUI.color = Color.Lerp(Color.yellow, Color.red, GameState.Deteccion / 100f);
            GUI.DrawTexture(new Rect(barra.x, barra.y, ancho * GameState.Deteccion / 100f, alto), blanco);
            GUI.color = Color.white;
            GUI.Label(new Rect(barra.xMax + 10 * k, caja.y, 130 * k, caja.height), "Detección", pequeno);
        }

        // Aviso (abajo al centro)
        string aviso = GameState.AvisoActual;
        if (aviso != "")
        {
            var contenido = new GUIContent(aviso);
            float maxAncho = Screen.width - margen * 4;
            float ancho = Mathf.Min(maxAncho, grande.CalcSize(contenido).x + relleno * 4);
            float alto = grande.CalcHeight(contenido, ancho - relleno * 2) + relleno * 2;
            Rect ra = new Rect((Screen.width - ancho) / 2f, Screen.height - alto - 60 * k, ancho, alto);
            GUI.DrawTexture(ra, fondo);
            GUI.Label(new Rect(ra.x + relleno, ra.y, ra.width - relleno * 2, ra.height), aviso, grande);
        }
    }
}