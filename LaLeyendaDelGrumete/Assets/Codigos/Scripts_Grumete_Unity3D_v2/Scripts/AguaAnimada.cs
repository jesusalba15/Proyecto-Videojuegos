using UnityEngine;

// Pon este script en el plano del Mar.
// 1) Desliza la textura del agua para que las ondas se muevan.
// 2) (Opcional) Ondula la malla del plano para que el agua suba y baje.
[RequireComponent(typeof(Renderer))]
public class AguaAnimada : MonoBehaviour
{
    [Header("Textura")]
    public Vector2 repeticiones = new Vector2(40f, 40f);     // cuantas veces se repite la textura en el plano
    public Vector2 velocidad = new Vector2(0.02f, 0.01f);    // direccion y rapidez de las ondas

    [Header("Oleaje de la malla (opcional)")]
    public bool ondularMalla = true;
    public float altura = 0.15f;
    public float longitud = 6f;
    public float rapidez = 1.2f;

    Material mat;
    Mesh malla;
    Vector3[] base_, vertices;

    void Start()
    {
        mat = GetComponent<Renderer>().material;   // copia propia del material
        mat.SetTextureScale("_BaseMap", repeticiones);
        mat.mainTextureScale = repeticiones;

        var mf = GetComponent<MeshFilter>();
        if (ondularMalla && mf)
        {
            malla = mf.mesh;
            base_ = malla.vertices;
            vertices = new Vector3[base_.Length];
        }
    }

    void Update()
    {
        Vector2 off = velocidad * Time.time;
        mat.SetTextureOffset("_BaseMap", off);
        mat.mainTextureOffset = off;

        if (malla == null) return;
        float t = Time.time * rapidez;
        for (int i = 0; i < base_.Length; i++)
        {
            Vector3 v = base_[i];
            Vector3 w = transform.TransformPoint(v);
            float y = Mathf.Sin(w.x / longitud + t) + Mathf.Sin(w.z / (longitud * 1.3f) + t * 0.8f);
            v.y = base_[i].y + y * 0.5f * altura / Mathf.Max(0.0001f, transform.lossyScale.y);
            vertices[i] = v;
        }
        malla.vertices = vertices;
        malla.RecalculateNormals();
    }
}
