using UnityEngine;

// OBSOLETO: el mapa nuevo usa IslaMapa. Quita este componente de las islas del mapa.
public class IslaClick : MonoBehaviour
{
    public MapaVivo mapa;
    public bool esDestino = true;
    public bool cerrarMapa = false;
    [TextArea] public string mensajeBloqueada = "";

    void Start()
    {
        Debug.LogWarning("IslaClick ya no se usa: en '" + name + "' reemplazalo por IslaMapa.");
        enabled = false;
    }
}
