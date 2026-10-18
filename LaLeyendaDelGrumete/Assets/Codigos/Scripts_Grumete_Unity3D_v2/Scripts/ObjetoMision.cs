using System.Collections.Generic;
using UnityEngine;

// Objeto de un encargo (por ejemplo las herramientas de Pico). Se recoge con F y queda guardado con la partida.
//  - "Grupo" lo une con su Encargo (el NPC que los pide) y con el paso del objetivo de la ZonaIsla.
//  - Si el objeto no tiene modelo, se dibuja una caja dorada que flota y gira.
//  - "Solo Con Encargo": no aparece hasta que Kai hable con quien lo pide.
public class ObjetoMision : MonoBehaviour
{
    public string grupo = "herramientas_pico";
    public string nombre = "Martillo de Pico";
    [Tooltip("Nombre unico para guardarlo (vacio = nombre del objeto en la escena)")]
    public string clave = "";
    public float radio = 1.8f;
    public Color color = new Color(1f, 0.8f, 0.35f);
    [Tooltip("No aparece hasta aceptar el encargo de su grupo")]
    public bool soloConEncargo = true;

    public static readonly List<ObjetoMision> Todos = new List<ObjetoMision>();
    public string Clave => "obj:" + grupo + ":" + (string.IsNullOrEmpty(clave) ? gameObject.name : clave);
    public bool Recogido => RecompensaIsla.Hechas.Contains(Clave);
    public bool Visible => !Recogido && (!soloConEncargo || Encargo.Aceptado(grupo));
    public bool Cerca(Vector3 p) => Visible && Vector3.Distance(p, transform.position) < radio;

    // cuantos objetos de un grupo ya recogio Kai (cuenta tambien los de otras escenas)
    public static int Contar(string grupo)
    {
        int n = 0; string pre = "obj:" + grupo + ":";
        foreach (var h in RecompensaIsla.Hechas) if (h.StartsWith(pre)) n++;
        return n;
    }

    Transform modelo;
    Renderer[] rends;
    Light luz;
    MarcadorMinimapa marca;
    bool visto = true;

    void OnEnable() { if (!Todos.Contains(this)) Todos.Add(this); }
    void OnDisable() { Todos.Remove(this); }

    void Start()
    {
        rends = GetComponentsInChildren<Renderer>(true);
        if (rends.Length == 0)
        {
            var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
            var col = c.GetComponent<Collider>(); if (col) Destroy(col);
            c.name = "Modelo";
            c.transform.SetParent(transform, false);
            c.transform.localScale = new Vector3(0.45f, 0.3f, 0.3f);
            Colorear.Poner(c, color);
            rends = c.GetComponentsInChildren<Renderer>();
        }
        modelo = transform.childCount > 0 ? transform.GetChild(0) : null;
        var lg = new GameObject("Brillo");
        lg.transform.SetParent(transform, false); lg.transform.localPosition = Vector3.up * 0.8f;
        luz = lg.AddComponent<Light>(); luz.type = LightType.Point; luz.color = color; luz.range = 3.5f; luz.intensity = 1.5f; luz.shadows = LightShadows.None;
        marca = MarcadorMinimapa.Agregar(gameObject, MarcadorMinimapa.Tipo.Objetivo, false, "", nombre);
        Mostrar(Visible);
    }

    void Mostrar(bool si)
    {
        if (si == visto) return;
        visto = si;
        foreach (var r in rends) if (r) r.enabled = si;
        if (luz) luz.enabled = si;
        if (marca) marca.Activo = si;
    }

    void Update()
    {
        Mostrar(Visible);
        if (!visto) return;
        if (modelo)
        {
            modelo.localPosition = Vector3.up * (0.6f + Mathf.Sin(Time.time * 2f) * 0.1f);
            modelo.Rotate(0f, 70f * Time.deltaTime, 0f, Space.World);
        }
        var kai = GameState.Kai;
        if (kai && !HUD.Pausado && Cerca(kai.position) && Input.GetKeyDown(KeyCode.F)) Recoger();
    }

    public void Recoger()
    {
        if (Recogido) return;
        RecompensaIsla.Hechas.Add(Clave);
        Mostrar(false);
        Sonidos.Tocar("recoger", transform.position, 1f, 0f);
        Efectos.Columna(transform.position, color, 16, 1f);
        var enc = Encargo.De(grupo);
        int n = Contar(grupo), total = enc ? enc.cantidad : 0;
        string cuenta = total > 0 ? " (" + Mathf.Min(n, total) + "/" + total + ")" : "";
        if (GameState.Kai) HUD.Numero(GameState.Kai.position + Vector3.up * 2.2f, "+ " + nombre, color, 1f);
        GameState.Avisar("Encontraste: " + nombre + cuenta + (enc && total > 0 && n >= total ? ". Llévaselo a " + enc.nombre + "." : ""), 3f);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = color;
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.6f, 0.35f);
    }
}
