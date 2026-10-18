using System.Collections.Generic;
using UnityEngine;

// Recurso para recolectar (estilo Albion): palmeras, troncos, restos de barcos...
// Ponlo en el objeto (palmera, pila de madera, naufragio). Kai MANTIENE F cerca para recoger.
//   Madera   -> Equipo.Madera (para mejoras)
//   Coco     -> +1 Agua de coco (pocion de aguante)
//   Chatarra -> Equipo.Chatarra (para mejoras)
// Despues de 'usos' veces se agota y reaparece pasado un tiempo.
public class Recurso : MonoBehaviour
{
    public enum Tipo { Madera, Coco, Chatarra }

    public Tipo tipo = Tipo.Madera;
    public int cantidadMin = 1, cantidadMax = 2;
    public int usos = 3;
    public float tiempoRecolectar = 1.5f;
    public float reaparece = 90f;
    public float radio = 2.2f;
    [Tooltip("Lo que se esconde al agotarse (vacio = todos los modelos hijos)")]
    public GameObject modelo;

    public static readonly List<Recurso> Todos = new List<Recurso>();
    public static Recurso Recolectando { get; private set; }   // el que Kai esta recogiendo ahora
    public float Progreso => progreso / Mathf.Max(0.1f, tiempoRecolectar);
    public bool Agotado => restantes <= 0;

    int restantes;
    float progreso, vuelveEn;
    MarcadorMinimapa marcador;
    Renderer[] rends;

    public string NombreRecurso => tipo == Tipo.Madera ? "madera" : tipo == Tipo.Coco ? "cocos" : "chatarra";

    void OnEnable() { Todos.Add(this); }
    void OnDisable() { Todos.Remove(this); if (Recolectando == this) Recolectando = null; }

    void Start()
    {
        restantes = usos;
        rends = modelo ? modelo.GetComponentsInChildren<Renderer>() : GetComponentsInChildren<Renderer>();
        string etiqueta = tipo == Tipo.Madera ? "Madera" : tipo == Tipo.Coco ? "Palmera con cocos" : "Restos de naufragio";
        marcador = MarcadorMinimapa.Agregar(gameObject, MarcadorMinimapa.Tipo.Recurso, false, "", etiqueta);
    }

    public bool Cerca(Vector3 p) => !Agotado && Vector3.Distance(p, transform.position) < radio;

    void Update()
    {
        if (Agotado)
        {
            if (Time.time >= vuelveEn) Mostrar(true);
            return;
        }
        var kai = GameState.Kai;
        bool recogiendo = kai && !HUD.Pausado && Cerca(kai.position) && Input.GetKey(KeyCode.F)
                          && (Recolectando == null || Recolectando == this);
        if (!recogiendo)
        {
            if (Recolectando == this) Recolectando = null;
            progreso = 0f;
            return;
        }
        Recolectando = this;
        // con el hacha se corta madera el doble de rapido
        progreso += Time.deltaTime * (tipo == Tipo.Madera && Equipo.ArmaActual == Equipo.Armas[(int)Equipo.IdArma.Hacha] ? 2f : 1f);
        if (progreso >= tiempoRecolectar) Recoger();
    }

    void Recoger()
    {
        progreso = 0f;
        int n = Random.Range(cantidadMin, cantidadMax + 1);
        Vector3 p = transform.position + Vector3.up * 1.6f;
        switch (tipo)
        {
            case Tipo.Madera: Equipo.Madera += n; HUD.Numero(p, "+" + n + " madera", new Color(0.8f, 0.6f, 0.4f), 0.9f); break;
            case Tipo.Chatarra: Equipo.Chatarra += n; HUD.Numero(p, "+" + n + " chatarra", new Color(0.75f, 0.78f, 0.8f), 0.9f); break;
            case Tipo.Coco:
                var coco = System.Array.Find(Equipo.Pociones, c => c.icono == "coco");
                if (coco != null) coco.cantidad += n;
                HUD.Numero(p, "+" + n + " agua de coco", new Color(0.55f, 0.9f, 0.4f), 0.9f);
                break;
        }
        Sonidos.Tocar("recoger", transform.position, 0.6f, 0.12f);
        global::Progreso.Ganar(global::Progreso.XpRecurso);   // "Progreso" a secas es la barra de recoleccion
        restantes--;
        if (Agotado) { Mostrar(false); vuelveEn = Time.time + reaparece; Recolectando = null; }
    }

    void Mostrar(bool si)
    {
        if (si) restantes = usos;
        foreach (var r in rends) if (r) r.enabled = si;
        if (marcador) marcador.Activo = si;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.4f, 0.9f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, radio);
    }
}
