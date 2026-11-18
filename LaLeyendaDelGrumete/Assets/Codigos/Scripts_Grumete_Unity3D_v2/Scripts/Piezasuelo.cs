using UnityEngine;

// Pieza de barco en el suelo. Kai la recoge con F.
//  - Ponla a mano en el mapa: objeto vacio + este script, y escribe el Id Pieza.
//  - O la crea Sueltapieza cuando muere un enemigo.
//  - "Altura": cuanto flota el modelo sobre el suelo.
// Si Kai ya la tiene, desaparece sola (no se repite al cargar la partida).
// Modelo: si existe Assets/Resources/Pieza_<id> (ej. Pieza_vela) se usa ese prefab; si no, una cajita brillante.
public class Piezasuelo : MonoBehaviour
{
    [Tooltip("Id de la pieza en PiezasBarco.Catalogo (vela, timon, casco, canon, brujula)")]
    public string idPieza = "vela";
    public float radio = 2f;
    [Tooltip("Altura a la que flota el modelo sobre el suelo (las que sueltan los enemigos usan el valor por defecto)")]
    public float altura = 0.35f;
    [Tooltip("Tamano maximo del modelo en metros: si el prefab es mas grande se achica solo (0 = no tocarlo)")]
    public float tamanoMaximo = 0.8f;
    [Tooltip("Desmarcalo para piezas escondidas que el jugador debe descubrir")]
    public bool mostrarEnMinimapa = true;

    Transform modelo;
    float nace;
    bool avisado;
    static Material material;

    public static Piezasuelo Crear(Vector3 pos, string id)
    {
        var go = new GameObject("Pieza_" + id);
        go.transform.position = pos + Vector3.up * 0.05f;
        var p = go.AddComponent<Piezasuelo>();
        p.idPieza = id;   // Start corre despues, ya con el id puesto
        return p;
    }

    void Start()
    {
        var pieza = PiezasBarco.Buscar(idPieza);
        if (pieza == null) { Debug.LogWarning("[PiezaEnSuelo] '" + idPieza + "' no esta en PiezasBarco.Catalogo.", this); enabled = false; return; }
        if (PiezasBarco.Tiene(idPieza)) { Destroy(gameObject); return; }
        if (GetComponent<Renderer>() || GetComponentInChildren<Renderer>())
            Debug.LogWarning("[Piezasuelo] '" + name + "' ya tiene un modelo. Pon este script en un OBJETO VACIO: el modelo lo carga solo desde Resources (Pieza_" + idPieza + ").", this);
        nace = Time.time;
        Modelo(pieza);
        if (mostrarEnMinimapa) MarcadorMinimapa.Agregar(gameObject, MarcadorMinimapa.Tipo.Cofre, false, "P", pieza.nombre);
    }

    void Modelo(PiezasBarco.Pieza pieza)
    {
        var prefab = Resources.Load<GameObject>("Pieza_" + idPieza);
        if (prefab)
        {
            modelo = new GameObject("Modelo").transform;   // contenedor: el que flota y gira
            modelo.SetParent(transform, false);
            var inst = Instantiate(prefab, modelo, false).transform;
            inst.localPosition = Vector3.zero;
            Limpiar(inst.gameObject);
            Ajustar(inst);
        }
        else
        {
            // sin prefab: cajita de madera con bandas de hierro y una gema del color de la pieza
            modelo = new GameObject("Modelo").transform;
            modelo.SetParent(transform, false);
            var madera = new Color(0.45f, 0.28f, 0.13f);
            var hierro = new Color(0.22f, 0.22f, 0.25f);
            Cubo(new Vector3(0, 0.22f, 0), new Vector3(0.42f, 0.34f, 0.42f), madera);
            Cubo(new Vector3(0, 0.12f, 0), new Vector3(0.44f, 0.05f, 0.44f), hierro);
            Cubo(new Vector3(0, 0.34f, 0), new Vector3(0.44f, 0.05f, 0.44f), hierro);
            Cubo(new Vector3(0, 0.52f, 0), new Vector3(0.16f, 0.16f, 0.16f), pieza.color).localRotation = Quaternion.Euler(45f, 0f, 45f);
        }
        // brillo suave (antes era muy fuerte y quemaba el modelo en blanco)
        var luz = new GameObject("Brillo").AddComponent<Light>();
        luz.transform.SetParent(transform, false);
        luz.transform.localPosition = Vector3.up * (altura + 0.9f);
        luz.type = LightType.Point; luz.color = pieza.color; luz.range = 2.5f; luz.intensity = 0.8f;
    }

    // Quita del modelo lo que da lag o estorba: colisiones, fisicas, luces propias (faroles), sombras y "Static"
    static void Limpiar(GameObject go)
    {
        // si el prefab trae este mismo script, se crearia una pieza dentro de otra sin parar (lag enorme)
        foreach (var ps in go.GetComponentsInChildren<Piezasuelo>(true)) DestroyImmediate(ps);
        foreach (var rb in go.GetComponentsInChildren<Rigidbody>(true)) Destroy(rb);
        foreach (var c in go.GetComponentsInChildren<Collider>(true)) Destroy(c);
        foreach (var l in go.GetComponentsInChildren<Light>(true)) Destroy(l);
        foreach (var r in go.GetComponentsInChildren<Renderer>(true))
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        foreach (var t in go.GetComponentsInChildren<Transform>(true)) t.gameObject.isStatic = false;
    }

    // Achica el modelo si es muy grande y lo centra con la base en el suelo (asi gira sobre si mismo)
    void Ajustar(Transform inst)
    {
        if (!Medir(inst, out Bounds b)) return;
        float mayor = Mathf.Max(b.size.x, b.size.y, b.size.z);
        if (tamanoMaximo > 0f && mayor > tamanoMaximo)
        {
            inst.localScale *= tamanoMaximo / mayor;
            Medir(inst, out b);
        }
        inst.position += modelo.position - new Vector3(b.center.x, b.min.y, b.center.z);
    }

    static bool Medir(Transform t, out Bounds b)
    {
        b = new Bounds();
        bool hay = false;
        foreach (var r in t.GetComponentsInChildren<Renderer>())
        {
            if (r is ParticleSystemRenderer) continue;
            if (!hay) { b = r.bounds; hay = true; } else b.Encapsulate(r.bounds);
        }
        return hay;
    }

    Transform Cubo(Vector3 pos, Vector3 escala, Color color)
    {
        var p = GameObject.CreatePrimitive(PrimitiveType.Cube);
        DestroyImmediate(p.GetComponent<Collider>());
        p.transform.SetParent(modelo, false);
        p.transform.localPosition = pos; p.transform.localScale = escala;
        var rend = p.GetComponent<Renderer>();
        if (!material) { var sh = Shader.Find("Universal Render Pipeline/Lit"); material = sh ? new Material(sh) : rend.sharedMaterial; }
        rend.sharedMaterial = material;
        var mpb = new MaterialPropertyBlock();
        mpb.SetColor("_BaseColor", color); mpb.SetColor("_Color", color);
        rend.SetPropertyBlock(mpb);
        return p.transform;
    }

    void Update()
    {
        if (!modelo) return;
        // sale con un saltito y luego flota y gira despacio
        float t = Time.time - nace;
        float salto = t < 0.4f ? Mathf.Sin(t / 0.4f * Mathf.PI) * 0.6f : 0f;
        modelo.localPosition = Vector3.up * (altura + salto + Mathf.Sin(Time.time * 2f) * 0.08f);
        modelo.Rotate(0f, 50f * Time.deltaTime, 0f);

        var kai = GameState.Kai;
        bool cerca = kai && Vector3.Distance(kai.position, transform.position) < radio;
        if (cerca && !avisado) { avisado = true; GameState.Avisar("F: recoger " + PiezasBarco.Buscar(idPieza).nombre, 2f); }
        if (!cerca) avisado = false;
        if (cerca && !HUD.Pausado && t > 0.4f && Input.GetKeyDown(KeyCode.F)) Recoger();
    }

    void Recoger()
    {
        var pieza = PiezasBarco.Buscar(idPieza);
        PiezasBarco.Obtener(idPieza);
        Sonidos.Tocar("recoger", transform.position, 0.9f);
        Efectos.Columna(transform.position, pieza.color, 16, 0.9f);
        Destroy(gameObject);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.8f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, radio);
    }
}