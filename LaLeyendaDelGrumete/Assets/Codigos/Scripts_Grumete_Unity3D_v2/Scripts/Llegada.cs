using UnityEngine;

// Costa de una isla en la escena Navegacion. Collider con Is Trigger.
// Al tocarla con el barco, Kai desembarca en la isla elegida en el mapa y aparece en su muelle.
// Si hay varias costas en la escena, ponle a cada una su idIsla: solo funciona la de la ruta trazada.
public class Llegada : MonoBehaviour
{
    [Tooltip("Isla de esta costa (0 = sirve para cualquier destino)")]
    public int idIsla = 0;
    [Tooltip("Solo se usa si la isla no tiene escena en Archipielago.cs")]
    public string escenaIsla = "Isla";

    public bool EsDestino => idIsla == 0 || idIsla == GameState.IslaDestino;

    void OnTriggerEnter(Collider otro)
    {
        if (!otro.GetComponentInParent<BarcoController>()) return;
        if (!EsDestino)
        {
            var esta = Archipielago.Buscar(idIsla);
            GameState.Avisar((esta != null ? esta.nombre : "Esta isla") + " no es tu destino. Sigue la ruta.", 2.5f);
            return;
        }
        var destino = Archipielago.Buscar(GameState.IslaDestino);
        GameState.Navegacion = Mathf.Max(GameState.Navegacion, 1);
        GameState.IslaActual = GameState.IslaDestino;
        GameState.LlegandoEnBarco = true;
        GameState.CargarEscena(destino != null && !string.IsNullOrEmpty(destino.escena) ? destino.escena : escenaIsla);
    }
}
