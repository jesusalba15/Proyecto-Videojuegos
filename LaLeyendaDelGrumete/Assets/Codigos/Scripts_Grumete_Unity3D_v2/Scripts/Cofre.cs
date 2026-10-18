using System.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

// Cofre de tripulante: al abrirlo con E se une Mara "Nudos" a la tripulacion.
// Tres formas de animar la apertura (usa la que tengas, se pueden combinar):
//  A) Clip Abrir: si el modelo del cofre trae una animacion de abrir (necesita Animator).
//  B) Tapa: si la tapa es una pieza separada, la gira hacia atras.
//  C) Modelo Cerrado + Modelo Abierto: cambia un modelo por otro con un pequeno "salto".
public class Cofre : MonoBehaviour
{
    public Transform jugador;
    public float radio = 2f;
    public GameObject aparecerAlAbrir;
    [TextArea] public string mensaje = "¡Por fin alguien me saca de aquí! Cuenta conmigo, capitán. Con mis nudos, tu barco irá más rápido.";

    [Header("A) Animacion del modelo")]
    public Animator animador;
    public AnimationClip clipAbrir;

    [Header("B) Tapa separada")]
    public Transform tapa;
    public Vector3 giroTapa = new Vector3(-110f, 0f, 0f);

    [Header("C) Cambio de modelo")]
    public GameObject modeloCerrado;
    public GameObject modeloAbierto;

    [Header("Ajustes")]
    public float duracion = 0.6f;
    public float esperaAntesDeMara = 0.4f;

    bool abierto;
    PlayableGraph grafo;

    void Start()
    {
        if (modeloAbierto) modeloAbierto.SetActive(false);
        // Si Mara ya estaba reclutada (por ejemplo al recargar la escena), mostrar el cofre abierto
        if (GameState.TieneMara == 1) StartCoroutine(Abrir(false));
    }

    void Update()
    {
        if (abierto || GameState.TieneMara == 1 || !jugador || HUD.Pausado) return;
        if (Vector3.Distance(jugador.position, transform.position) < radio && Input.GetKeyDown(KeyCode.E))
            StartCoroutine(Abrir(true));
    }

    IEnumerator Abrir(bool animado)
    {
        abierto = true;
        if (animado && GameState.TieneMara == 0)
        {
            Sonidos.Tocar("cofre", transform.position, 1f, 0f);
            Efectos.Columna(transform.position + Vector3.up * 0.5f, new Color(1f, 0.85f, 0.35f), 20, 1.1f);
            Progreso.Ganar(Progreso.XpCofre);
        }
        GameState.TieneMara = 1;
        var marca = GetComponent<MarcadorMinimapa>(); if (marca) marca.Activo = false;
        float d = animado ? duracion : 0f;

        // A) Clip de animacion
        if (animador && clipAbrir)
        {
            var clip = AnimationPlayableUtilities.PlayClip(animador, clipAbrir, out grafo);
            if (!animado) clip.SetTime(clipAbrir.length);
        }

        // C) Cambio de modelo con salto
        if (modeloAbierto)
        {
            if (modeloCerrado) modeloCerrado.SetActive(false);
            modeloAbierto.SetActive(true);
            if (animado) StartCoroutine(Salto(modeloAbierto.transform));
        }

        // B) Girar la tapa
        if (tapa)
        {
            Quaternion ini = tapa.localRotation, fin = ini * Quaternion.Euler(giroTapa);
            for (float t = 0f; t < d; t += Time.deltaTime)
            {
                float k = 1f - Mathf.Pow(1f - t / d, 3f);
                tapa.localRotation = Quaternion.Slerp(ini, fin, k);
                yield return null;
            }
            tapa.localRotation = fin;
        }

        if (animado)
        {
            yield return new WaitForSeconds(esperaAntesDeMara);
            GameState.Hablar("Mara \"Nudos\"", mensaje, 4f);
        }
        if (aparecerAlAbrir) aparecerAlAbrir.SetActive(true);
    }

    IEnumerator Salto(Transform tr)
    {
        Vector3 final = tr.localScale;
        for (float t = 0f; t < duracion; t += Time.deltaTime)
        {
            float k = t / duracion;
            float s = 1f + Mathf.Sin(k * Mathf.PI) * 0.25f;   // crece un 25% y vuelve
            tr.localScale = final * Mathf.Lerp(0.6f, 1f, Mathf.Min(1f, k * 2f)) * s;
            yield return null;
        }
        tr.localScale = final;
    }

    void OnDestroy()
    {
        if (grafo.IsValid()) grafo.Destroy();
    }
}
