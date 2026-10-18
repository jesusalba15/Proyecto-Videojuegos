using System.Collections.Generic;
using UnityEngine;

// Arma tirada que Kai recoge con F (por ejemplo el Hacha en el Faro Roto o la Pistola en el Astillero).
// Puedes ponerla a mano en la escena (Empty + este componente) o la suelta un Rival con "Arma Que Suelta".
// Si Kai ya tiene esa arma, no aparece.
// Si no le pones un modelo, se arma uno sencillo con piezas basicas.
public class ArmaEnSuelo : MonoBehaviour
{
    public Equipo.IdArma arma = Equipo.IdArma.Hacha;
    [Tooltip("Modelo 3D del arma (opcional). Vacio = modelo simple hecho con cubos")]
    public GameObject modelo;
    public float radio = 2f;

    public static readonly List<ArmaEnSuelo> Todos = new List<ArmaEnSuelo>();
    public string Nombre => Equipo.Armas[(int)arma].nombre;

    Transform giro;
    static Material material;

    public static ArmaEnSuelo Crear(Vector3 pos, Equipo.IdArma arma)
    {
        var go = new GameObject("Arma_" + arma);
        go.transform.position = pos;
        var a = go.AddComponent<ArmaEnSuelo>();
        a.arma = arma;
        return a;
    }

    void OnEnable() { Todos.Add(this); }
    void OnDisable() { Todos.Remove(this); }

    void Start()
    {
        if (Equipo.Tiene(arma)) { gameObject.SetActive(false); return; }

        giro = new GameObject("Giro").transform;
        giro.SetParent(transform, false);
        giro.localPosition = Vector3.up * 0.5f;
        if (modelo) modelo.transform.SetParent(giro, true);
        else ModeloSimple();

        var luz = new GameObject("Brillo").AddComponent<Light>();
        luz.transform.SetParent(transform, false); luz.transform.localPosition = Vector3.up * 1.2f;
        luz.type = LightType.Point; luz.color = new Color(1f, 0.85f, 0.4f); luz.range = 3.5f; luz.intensity = 2.5f;

        MarcadorMinimapa.Agregar(gameObject, MarcadorMinimapa.Tipo.Objetivo, true, "", Nombre);
    }

    void ModeloSimple() { ConstruirModelo(arma, giro); }

    // Modelo sencillo de cada arma hecho con cubos y cilindros (lo usa tambien ArmasVisuales en la mano de Kai).
    // El mango queda en el origen y la hoja/canon apunta hacia +Y.
    public static void ConstruirModelo(Equipo.IdArma arma, Transform padre)
    {
        Color madera = new Color(0.45f, 0.28f, 0.14f), metal = new Color(0.72f, 0.74f, 0.78f), oro = new Color(0.95f, 0.75f, 0.25f);
        switch (arma)
        {
            case Equipo.IdArma.Sable:
                Pieza(padre, PrimitiveType.Cube, new Vector3(0, 0.6f, 0), new Vector3(0.06f, 0.9f, 0.02f), metal);
                Pieza(padre, PrimitiveType.Cube, new Vector3(0, 0.13f, 0), new Vector3(0.3f, 0.05f, 0.06f), oro);
                Pieza(padre, PrimitiveType.Cube, new Vector3(0, 0f, 0), new Vector3(0.05f, 0.22f, 0.05f), madera);
                break;
            case Equipo.IdArma.Hacha:
                Pieza(padre, PrimitiveType.Cylinder, new Vector3(0, 0.3f, 0), new Vector3(0.06f, 0.45f, 0.06f), madera);
                Pieza(padre, PrimitiveType.Cube, new Vector3(0.13f, 0.63f, 0), new Vector3(0.28f, 0.22f, 0.04f), metal);
                break;
            case Equipo.IdArma.Pistola:
                Pieza(padre, PrimitiveType.Cube, new Vector3(0, 0f, 0), new Vector3(0.07f, 0.2f, 0.12f), madera).localRotation = Quaternion.Euler(-20, 0, 0);
                Pieza(padre, PrimitiveType.Cylinder, new Vector3(0, 0.12f, 0.15f), new Vector3(0.06f, 0.16f, 0.06f), metal).localRotation = Quaternion.Euler(90, 0, 0);
                Pieza(padre, PrimitiveType.Cube, new Vector3(0, 0.12f, 0f), new Vector3(0.06f, 0.06f, 0.08f), oro);
                break;
        }
    }

    static Transform Pieza(Transform padre, PrimitiveType tipo, Vector3 pos, Vector3 escala, Color color)
    {
        var p = GameObject.CreatePrimitive(tipo);
        DestroyImmediate(p.GetComponent<Collider>());   // al instante: si no, empuja a Kai o choca con la camara
        p.transform.SetParent(padre, false);
        p.transform.localPosition = pos; p.transform.localScale = escala;
        var rend = p.GetComponent<Renderer>();
        if (!material) { var sh = Shader.Find("Universal Render Pipeline/Lit"); material = sh ? new Material(sh) : rend.sharedMaterial; }
        rend.sharedMaterial = material;
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
        var mpb = new MaterialPropertyBlock();
        mpb.SetColor("_BaseColor", color); mpb.SetColor("_Color", color);
        rend.SetPropertyBlock(mpb);
        return p.transform;
    }

    public bool Cerca(Vector3 p) => Vector3.Distance(p, transform.position) < radio;

    void Update()
    {
        if (giro)
        {
            giro.Rotate(0f, 90f * Time.deltaTime, 0f, Space.World);
            giro.localPosition = Vector3.up * (0.5f + Mathf.Sin(Time.time * 2f) * 0.1f);
        }
        var kai = GameState.Kai;
        if (kai && !HUD.Pausado && Cerca(kai.position) && Input.GetKeyDown(KeyCode.F))
        {
            Equipo.Desbloquear(arma);
            Sonidos.Tocar("recoger", transform.position, 0.9f, 0f);
            Efectos.Columna(transform.position, new Color(1f, 0.85f, 0.3f), 16, 1f);
            gameObject.SetActive(false);
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = new Color(1f, 0.85f, 0.3f);
        Gizmos.DrawWireSphere(transform.position + Vector3.up * 0.9f, 0.4f);
    }
}
