using UnityEngine;

public class Barbagris : MonoBehaviour
{
    public Transform jugador;
    public float radio = 2.5f;

    System.Collections.IEnumerator AbrirMapa()
    {
        yield return new WaitForSeconds(2f);
        MapaMundo.Abrir(MapaMundo.Pestana.Mundo);
    }

    void Update()
    {
        if (!jugador || HUD.Pausado) return;
        if (Vector3.Distance(jugador.position, transform.position) < radio && Input.GetKeyDown(KeyCode.E))
        {
            // la primera vez entrega el sable: Kai empieza la aventura con los punos
            if (!Equipo.Tiene(Equipo.IdArma.Sable))
            {
                GameState.Hablar("Barbagrís", "¿Pelear con los puños? Ja. Toma este sable de madera, grumete: era mío cuando tenía tu edad. Ahora demuéstrame lo que vales.", 5f);
                Equipo.Desbloquear(Equipo.IdArma.Sable);
                return;
            }
            if (GameState.PruebasHechas < 3)
            {
                GameState.Hablar("Barbagrís", GameState.PruebasHechas == 0
                    ? "Así que quieres ser pirata, grumete... Demuéstralo: trepa el mástil, gana un duelo y cuélate en la bodega del alcalde."
                    : "Bien hecho, pero aún te faltan " + (3 - GameState.PruebasHechas) + " pruebas. ¡Ánimo, grumete!", 5f);
            }
            else
            {
                if (GameState.Fragmentos < 1)
                {
                    GameState.Fragmentos = 1;
                    Progreso.Ganar(Progreso.XpFragmento);
                    GameState.Hablar("Barbagrís", "Tienes madera de pirata. Toma este primer nudo... ¡Mira cómo reacciona tu mapa!", 3f);
                    StartCoroutine(AbrirMapa());
                }
                else
                {
                    GameState.Hablar("Barbagrís", "Tu mapa ya te muestra el camino. Ábrelo con M, traza la ruta al Faro Roto y ve al muelle.");
                }
            }
        }
    }
}
