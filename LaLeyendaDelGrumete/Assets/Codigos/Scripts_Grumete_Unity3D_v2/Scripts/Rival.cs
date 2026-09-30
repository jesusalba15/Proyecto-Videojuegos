using System.Collections;
using UnityEngine;

// Rival del duelo: se acerca y ataca apenas esta cerca (sin aviso en rojo).
// El dano se aplica en la mitad de la animacion de ataque.
// Al morir hace su animacion de muerte y desaparece.
public class Rival : MonoBehaviour
{
    public PlayerController jugador;
    public Transform centroZona;
    public float radioZona = 7f;
    public float velocidad = 2f;
    public float distanciaGolpe = 1.8f;
    public float esperaEntreAtaques = 1.2f;
    [Range(0f, 1f)] public float momentoImpacto = 0.5f; // fraccion de la animacion de ataque
    public int danoGolpe = 35;
    public int danoBloqueado = 5;
    public int vidaMax = 3;
    public float esperaAlDesaparecer = 1f;

    int vida;
    bool atacando, golpeAplicado, muerto, jugadorEstabaMuerto;
    float t, proximoAtaque;
    Vector3 inicio;
    Quaternion rotInicio;
    Renderer[] rends;
    AnimacionSimple anim;
    Collider col;

    void Start()
    {
        inicio = transform.position;
        rotInicio = transform.rotation;
        vida = vidaMax;
        rends = GetComponentsInChildren<Renderer>();
        anim = GetComponentInChildren<AnimacionSimple>();
        col = GetComponent<Collider>();

        // Si ya se gano el duelo (por ejemplo al volver del Faro Roto), no aparece
        if (GameState.Combate == 1) gameObject.SetActive(false);
    }

    void Update()
    {
        if (muerto || !jugador) return;

        // Mientras Kai esta muerto el rival espera; cuando Kai revive, el duelo se reinicia
        if (jugador.EstaMuerto) { jugadorEstabaMuerto = true; atacando = false; return; }
        if (jugadorEstabaMuerto) { Reiniciar(); jugadorEstabaMuerto = false; }

        Vector3 d = jugador.transform.position - transform.position; d.y = 0f;
        float dist = d.magnitude;

        Vector3 centro = centroZona ? centroZona.position : inicio;
        Vector3 aCentro = jugador.transform.position - centro; aCentro.y = 0f;
        bool enZona = aCentro.magnitude < radioZona;

        if (dist > 0.01f) transform.rotation = Quaternion.LookRotation(d);

        if (atacando)
        {
            t += Time.deltaTime;
            float duracion = anim ? anim.DuracionAtaque : 0.6f;

            if (!golpeAplicado && t >= duracion * momentoImpacto)
            {
                golpeAplicado = true;
                if (dist < distanciaGolpe + 0.5f)
                {
                    GameState.VidaKai -= jugador.bloqueando ? danoBloqueado : danoGolpe;
                    GameState.Avisar(jugador.bloqueando ? "¡Bloqueado! -" + danoBloqueado : "¡Golpe! -" + danoGolpe, 1.2f);
                }
            }

            if (t >= duracion)
            {
                atacando = false;
                proximoAtaque = Time.time + esperaEntreAtaques;
            }
            return;
        }

        if (!enZona) return;

        if (dist > distanciaGolpe)
        {
            transform.position += d.normalized * velocidad * Time.deltaTime;
        }
        else if (Time.time >= proximoAtaque)
        {
            atacando = true;
            golpeAplicado = false;
            t = 0f;
            if (anim) anim.Atacar();
        }
    }

    void Reiniciar()
    {
        vida = vidaMax;
        atacando = false;
        transform.SetPositionAndRotation(inicio, rotInicio);
        proximoAtaque = Time.time + 1f;
    }

    public void RecibirGolpe()
    {
        if (muerto || GameState.Combate == 1) return;
        vida--;

        if (vida <= 0)
        {
            StartCoroutine(Morir());
            return;
        }

        if (anim) anim.Golpeado();
        StartCoroutine(Parpadeo());
        GameState.Avisar("¡Le diste! Al rival le quedan " + vida, 1f);
    }

    IEnumerator Morir()
    {
        muerto = true;
        if (col) col.enabled = false;
        GameState.CompletarPrueba("combate");

        float duracion = anim ? anim.Morir() : 0f;
        yield return new WaitForSeconds(duracion + esperaAlDesaparecer);
        gameObject.SetActive(false);
    }

    IEnumerator Parpadeo()
    {
        for (int i = 0; i < 3; i++)
        {
            foreach (var r in rends) r.enabled = false;
            yield return new WaitForSeconds(0.05f);
            foreach (var r in rends) r.enabled = true;
            yield return new WaitForSeconds(0.05f);
        }
    }
}