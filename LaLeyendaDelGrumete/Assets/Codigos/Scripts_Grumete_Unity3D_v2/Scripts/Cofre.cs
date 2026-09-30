using UnityEngine;

public class Cofre : MonoBehaviour
{
    public Transform jugador;
    public float radio = 2f;

    void Update()
    {
        if (GameState.TieneMara == 1 || !jugador) return;
        if (Vector3.Distance(jugador.position, transform.position) < radio && Input.GetKeyDown(KeyCode.E))
        {
            GameState.TieneMara = 1;
            GameState.Avisar("Mara \"Nudos\" se une a la tripulación: +velocidad del barco", 4f);
            Colorear.Poner(gameObject, Color.gray);
        }
    }
}
