using UnityEngine;

// Islas del mapa. Necesita un Collider para detectar el clic.
// Es Destino: zarpa hacia la Navegacion.
// Cerrar Mapa: vuelve a la isla (usalo en la Isla de Caleta Gris).
// Ninguno marcado: muestra el mensaje de isla bloqueada.
public class IslaClick : MonoBehaviour
{
    public MapaVivo mapa;
    public bool esDestino = true;
    public bool cerrarMapa = false;
    [TextArea] public string mensajeBloqueada = "Requisitos: Sigilo 2 y Combate 2. Tus destrezas aún no bastan.";

    void OnMouseDown()
    {
        if (!mapa) return;
        if (cerrarMapa) mapa.Cerrar();
        else if (esDestino) mapa.Zarpar();
        else GameState.Avisar(mensajeBloqueada);
    }
}