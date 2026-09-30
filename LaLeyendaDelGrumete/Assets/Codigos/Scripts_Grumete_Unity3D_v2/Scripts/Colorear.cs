using UnityEngine;

// Pinta un objeto (y sus hijos) de un color sin crear materiales. Funciona en el editor.
// En modelos 3D con sus propios colores NO pongas este componente: los scripts usan
// Poner() para efectos temporales (rojo, oscuro) y Restaurar() para volver al original.
[ExecuteAlways]
public class Colorear : MonoBehaviour
{
    public Color color = Color.white;

    void OnEnable() { Poner(gameObject, color); }
    void OnValidate() { Poner(gameObject, color); }

    public static void Poner(GameObject go, Color c)
    {
        foreach (var r in go.GetComponentsInChildren<Renderer>())
        {
            if (r is ParticleSystemRenderer) continue;
            var mpb = new MaterialPropertyBlock();
            r.GetPropertyBlock(mpb);
            mpb.SetColor("_BaseColor", c); // URP
            mpb.SetColor("_Color", c);     // Built-in
            r.SetPropertyBlock(mpb);
        }
    }

    // Vuelve al color original: el de Colorear si lo tiene, o el del material del modelo
    public static void Restaurar(GameObject go)
    {
        var k = go.GetComponent<Colorear>();
        if (k) { Poner(go, k.color); return; }
        foreach (var r in go.GetComponentsInChildren<Renderer>())
            r.SetPropertyBlock(null);
    }
}
