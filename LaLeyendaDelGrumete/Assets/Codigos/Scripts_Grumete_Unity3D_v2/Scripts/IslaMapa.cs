using UnityEngine;

// Una isla del mapa vivo (escena Mapa). Ponlo en cada modelo de isla del pergamino.
// La isla aparece cuando tienes los fragmentos necesarios, y se puede zarpar a ella
// cuando cumples los requisitos de destreza (se muestran como sellos de cera).
public class IslaMapa : MonoBehaviour
{
    public enum TipoIsla { Libre, Corona }

    [Header("Identidad")]
    [Tooltip("1 = Caleta Gris, 2 = Faro Roto, 3 = Astillero, 4 = Bahia de la Corona, 5 = Fuerte de Roca, 6 = Fortaleza de Voss")]
    public int id = 2;
    public string nombre = "Isla del Faro Roto";
    public TipoIsla tipo = TipoIsla.Libre;
    [TextArea] public string descripcion = "Un viejo faro apagado. Dicen que alguien espera atrapada en un cofre.";
    public string recompensa = "Tripulante: Mara \"Nudos\"";

    [Header("Cuando aparece y que pide")]
    [Tooltip("Fragmentos de mapa necesarios para que la tinta la revele")]
    public int fragmentosParaRevelar = 1;
    public int requiereAgilidad, requiereCombate, requiereSigilo, requiereNavegacion;
    [Tooltip("La ruta de tinta se dibuja desde esta isla")]
    public IslaMapa origen;
    [Tooltip("Desmarcado: se ve en el mapa pero dice 'Proximamente' (todavia no esta construida)")]
    public bool disponibleEnDemo = true;

    public bool Revelada => id == 1 || GameState.Fragmentos >= fragmentosParaRevelar;
    public bool EsActual => GameState.IslaActual == id;
    public bool CumpleRequisitos => Falta() == "";

    public string Falta()
    {
        string f = "";
        if (GameState.Agilidad < requiereAgilidad) f += "Agilidad " + requiereAgilidad + "  ";
        if (GameState.Combate < requiereCombate) f += "Combate " + requiereCombate + "  ";
        if (GameState.Sigilo < requiereSigilo) f += "Sigilo " + requiereSigilo + "  ";
        if (GameState.Navegacion < requiereNavegacion) f += "Navegación " + requiereNavegacion + "  ";
        return f.Trim();
    }

    // --- visual: niebla sobre las islas que la tinta aun no revela
    [HideInInspector] public Transform niebla;
    [HideInInspector] public Renderer[] modelo;
    [HideInInspector] public Vector3 escalaOriginal;

    void Awake()
    {
        escalaOriginal = transform.localScale;
        modelo = GetComponentsInChildren<Renderer>();
        if (!GetComponent<Collider>()) AjustarCollider();
        CrearNiebla();
    }

    void AjustarCollider()
    {
        var b = new Bounds(transform.position, Vector3.zero); bool primero = true;
        foreach (var r in modelo) { if (primero) { b = r.bounds; primero = false; } else b.Encapsulate(r.bounds); }
        var c = gameObject.AddComponent<BoxCollider>();
        c.center = transform.InverseTransformPoint(b.center);
        Vector3 s = transform.lossyScale;
        c.size = new Vector3(b.size.x / Mathf.Max(0.001f, s.x), Mathf.Max(0.5f, b.size.y / Mathf.Max(0.001f, s.y)), b.size.z / Mathf.Max(0.001f, s.z));
    }

    void CrearNiebla()
    {
        niebla = new GameObject("Niebla").transform;
        niebla.position = transform.position;
        var sh = Shader.Find("Sprites/Default");
        var mat = new Material(sh) { color = new Color(0.92f, 0.9f, 0.86f, 0.92f) };
        Vector3[] puff = { new Vector3(0, 0.4f, 0), new Vector3(0.9f, 0.3f, 0.3f), new Vector3(-0.8f, 0.3f, -0.2f), new Vector3(0.2f, 0.35f, -0.8f), new Vector3(-0.3f, 0.3f, 0.8f) };
        float[] tam = { 1.6f, 1.2f, 1.1f, 1f, 1f };
        for (int i = 0; i < puff.Length; i++)
        {
            var p = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            Destroy(p.GetComponent<Collider>());
            p.transform.SetParent(niebla, false);
            p.transform.localPosition = puff[i];
            p.transform.localScale = new Vector3(tam[i], tam[i] * 0.45f, tam[i]);
            p.GetComponent<Renderer>().sharedMaterial = mat;
        }
        // tamano segun lo que ocupe el modelo de la isla
        float k = 1f;
        if (modelo.Length > 0)
        {
            var b = modelo[0].bounds; foreach (var r in modelo) b.Encapsulate(r.bounds);
            k = Mathf.Max(0.6f, Mathf.Max(b.extents.x, b.extents.z));
            niebla.position = new Vector3(b.center.x, b.max.y, b.center.z);
        }
        niebla.localScale = Vector3.one * k;
    }

    void OnMouseEnter() { if (MapaVivo.Actual) MapaVivo.Actual.Sobre(this); }
    void OnMouseExit() { if (MapaVivo.Actual) MapaVivo.Actual.Fuera(this); }
    void OnMouseDown() { if (MapaVivo.Actual) MapaVivo.Actual.Clic(this); }
}
