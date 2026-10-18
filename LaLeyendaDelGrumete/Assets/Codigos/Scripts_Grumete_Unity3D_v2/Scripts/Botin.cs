using System.Collections.Generic;
using UnityEngine;

// Bolsa de botin que suelta un enemigo al morir (estilo Albion). Kai la recoge con F.
// Se crea sola desde Rival; no hace falta ponerla en la escena.
public class Botin : MonoBehaviour
{
    public static readonly List<Botin> Todos = new List<Botin>();
    public const float Radio = 2f;

    public int doblones, madera, chatarra, bombas;
    public int pocion = -1, cantidadPocion;   // indice en Equipo.Pociones (-1 = ninguna)
    float nace, baseY;
    static Material material;
    Transform modelo;

    public static Botin Crear(Vector3 pos, int doblones, int madera, int chatarra, int pocion, int cantidadPocion, int bombas = 0)
    {
        var go = new GameObject("Botin");
        go.transform.position = pos + Vector3.up * 0.05f;
        var b = go.AddComponent<Botin>();
        b.bombas = bombas; b.doblones = doblones; b.madera = madera; b.chatarra = chatarra; b.pocion = pocion; b.cantidadPocion = cantidadPocion;
        b.Modelo();
        MarcadorMinimapa.Agregar(go, MarcadorMinimapa.Tipo.Cofre, false, "$", "Botín");
        return b;
    }

    // saco de cuero con una moneda encima y una lucecita dorada (piezas basicas, sin assets)
    void Modelo()
    {
        modelo = new GameObject("Modelo").transform;
        modelo.SetParent(transform, false);
        Pieza(PrimitiveType.Sphere, new Vector3(0, 0.25f, 0), new Vector3(0.5f, 0.45f, 0.5f), new Color(0.45f, 0.28f, 0.12f));
        Pieza(PrimitiveType.Cylinder, new Vector3(0, 0.5f, 0), new Vector3(0.18f, 0.06f, 0.18f), new Color(0.35f, 0.2f, 0.08f));
        Pieza(PrimitiveType.Cylinder, new Vector3(0.12f, 0.58f, 0), new Vector3(0.22f, 0.02f, 0.22f), new Color(1f, 0.8f, 0.25f)).localRotation = Quaternion.Euler(0, 0, 70f);
        var luz = new GameObject("Brillo").AddComponent<Light>();
        luz.transform.SetParent(transform, false); luz.transform.localPosition = Vector3.up * 0.8f;
        luz.type = LightType.Point; luz.color = new Color(1f, 0.8f, 0.35f); luz.range = 2.5f; luz.intensity = 1.5f;
        baseY = 0f; nace = Time.time;
    }

    Transform Pieza(PrimitiveType tipo, Vector3 pos, Vector3 escala, Color color)
    {
        var p = GameObject.CreatePrimitive(tipo);
        DestroyImmediate(p.GetComponent<Collider>());
        p.transform.SetParent(modelo, false);
        p.transform.localPosition = pos; p.transform.localScale = escala;
        var rend = p.GetComponent<Renderer>();
        if (!material) { var sh = Shader.Find("Universal Render Pipeline/Lit"); material = sh ? new Material(sh) : rend.sharedMaterial; }
        rend.sharedMaterial = material;   // material de URP (evita que salga rosado)
        var mpb = new MaterialPropertyBlock();
        mpb.SetColor("_BaseColor", color); mpb.SetColor("_Color", color);
        p.GetComponent<Renderer>().SetPropertyBlock(mpb);
        return p.transform;
    }

    void OnEnable() { Todos.Add(this); }
    void OnDisable() { Todos.Remove(this); }

    void Update()
    {
        // sale con un saltito y luego flota y gira despacio
        float t = Time.time - nace;
        float salto = t < 0.4f ? Mathf.Sin(t / 0.4f * Mathf.PI) * 0.6f : 0f;
        modelo.localPosition = Vector3.up * (baseY + salto + Mathf.Sin(Time.time * 2.5f) * 0.05f);
        modelo.Rotate(0f, 60f * Time.deltaTime, 0f);
        if (t > 120f) Destroy(gameObject);   // desaparece a los 2 minutos

        var kai = GameState.Kai;
        if (kai && !HUD.Pausado && t > 0.4f && Vector3.Distance(kai.position, transform.position) < Radio && Input.GetKeyDown(KeyCode.F)) Recoger();
    }

    public string Contenido()
    {
        var partes = new List<string>();
        if (doblones > 0) partes.Add(doblones + " doblones");
        if (pocion >= 0 && cantidadPocion > 0) partes.Add(cantidadPocion + " " + Equipo.Pociones[pocion].nombre);
        if (madera > 0) partes.Add(madera + " madera");
        if (chatarra > 0) partes.Add(chatarra + " chatarra");
        if (bombas > 0) partes.Add(bombas + (bombas == 1 ? " bomba de humo" : " bombas de humo"));
        return partes.Count > 0 ? string.Join(", ", partes) : "nada";
    }

    public void Recoger()
    {
        Equipo.Doblones += doblones; Equipo.Madera += madera; Equipo.Chatarra += chatarra;
        var humo = System.Array.Find(Equipo.Objetos, o => o.efecto == "humo");
        if (humo != null && bombas > 0) { humo.cantidad += bombas; HUD.Numero(transform.position + Vector3.up * 1.9f, "+" + bombas + " bomba de humo", new Color(0.7f, 0.7f, 0.78f), 0.8f); }
        if (pocion >= 0 && pocion < Equipo.Pociones.Length) Equipo.Pociones[pocion].cantidad += cantidadPocion;
        Vector3 p = transform.position + Vector3.up * 1.2f;
        if (doblones > 0) HUD.Numero(p, "+" + doblones + " doblones", new Color(1f, 0.82f, 0.3f), 0.9f);
        if (madera > 0) HUD.Numero(p + Vector3.up * 0.3f, "+" + madera + " madera", new Color(0.8f, 0.6f, 0.4f), 0.8f);
        if (chatarra > 0) HUD.Numero(p + Vector3.up * 0.3f, "+" + chatarra + " chatarra", new Color(0.75f, 0.78f, 0.8f), 0.8f);
        if (pocion >= 0 && cantidadPocion > 0) HUD.Numero(p + Vector3.up * 0.5f, "+" + cantidadPocion + " " + Equipo.Pociones[pocion].nombre, new Color(0.45f, 1f, 0.5f), 0.8f);
        GameState.Avisar("Botín: " + Contenido(), 2f);
        Sonidos.Tocar(doblones > 0 ? "monedas" : "recoger", transform.position, 0.8f);
        Efectos.Columna(transform.position, new Color(1f, 0.82f, 0.3f), 12, 0.7f);
        Destroy(gameObject);
    }
}
