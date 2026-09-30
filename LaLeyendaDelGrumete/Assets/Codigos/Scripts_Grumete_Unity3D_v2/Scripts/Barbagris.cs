using UnityEngine;

public class Barbagris : MonoBehaviour
{
    public Transform jugador;
    public float radio = 2.5f;

    void Update()
    {
        if (!jugador) return;
        if (Vector3.Distance(jugador.position, transform.position) < radio && Input.GetKeyDown(KeyCode.E))
        {
            if (GameState.PruebasHechas < 3)
            {
                GameState.Avisar("Supera las tres pruebas, grumete. Te faltan " + (3 - GameState.PruebasHechas));
            }
            else
            {
                GameState.Fragmentos = 1;
                GameState.CargarEscena("Mapa");
            }
        }
    }
}
