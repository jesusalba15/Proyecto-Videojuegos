using System.Collections;
using UnityEngine;

// Escena Mapa.
// - Sin fragmentos: solo se ve Caleta Gris.
// - Con el primer fragmento: la tinta "dibuja" la ruta y emerge la Isla del Faro Roto (solo la primera vez).
// - Despues: todo aparece directamente.
// M, Esc o clic en la isla actual = cerrar el mapa y volver a la isla.
public class MapaVivo : MonoBehaviour
{
    public Transform ruta;
    public Transform islaFaro;
    public float duracion = 1.5f;

    public bool Revelada { get; private set; }

    IEnumerator Start()
    {
        Vector3 escalaRuta = ruta ? ruta.localScale : Vector3.one;
        Vector3 escalaIsla = islaFaro ? islaFaro.localScale : Vector3.one;

        if (GameState.Fragmentos < 1)
        {
            if (ruta) ruta.localScale = Vector3.zero;
            if (islaFaro) islaFaro.localScale = Vector3.zero;
            GameState.Avisar("La tinta solo muestra Caleta Gris. Demuestra tu destreza pirata. (M para cerrar)", 999f);
            yield break;
        }

        if (GameState.RutaRevelada)
        {
            Revelada = true;
            GameState.Avisar("Clic en el Faro Roto o Enter para zarpar. M para cerrar el mapa.", 999f);
            yield break;
        }

        if (ruta) ruta.localScale = Vector3.zero;
        if (islaFaro) islaFaro.localScale = Vector3.zero;

        GameState.Avisar("La Tinta de Marea reacciona a tus hazañas...", 3f);
        yield return new WaitForSeconds(0.8f);
        yield return Crecer(ruta, escalaRuta);
        yield return Crecer(islaFaro, escalaIsla);

        Revelada = true;
        GameState.RutaRevelada = true;
        GameState.Avisar("Nueva ruta: Isla del Faro Roto. Clic en la isla o Enter para zarpar. M para cerrar.", 999f);
    }

    IEnumerator Crecer(Transform tr, Vector3 final)
    {
        if (!tr) yield break;
        for (float t = 0f; t < duracion; t += Time.deltaTime)
        {
            float k = 1f - Mathf.Pow(1f - t / duracion, 3f); // suavizado
            tr.localScale = Vector3.Lerp(Vector3.zero, final, k);
            yield return null;
        }
        tr.localScale = final;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M) || Input.GetKeyDown(KeyCode.Escape)) Cerrar();
        if (Revelada && (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))) Zarpar();
    }

    public void Zarpar()
    {
        if (Revelada) GameState.CargarEscena("Navegacion");
    }

    // Vuelve a la isla donde esta Kai (Caleta o Faro Roto, segun IslaActual)
    public void Cerrar()
    {
        GameState.Avisar("", 0f);
        GameState.CargarEscena("Isla");
    }
}