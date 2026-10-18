using System.Collections;
using UnityEngine;

// Derrumbe (Isla 5: la cantera). Mientras Kai esta dentro del area, cada pocos segundos aparece
// un circulo rojo en el suelo cerca de el y luego cae una roca: si no se mueve, recibe dano.
// Ponlo en un objeto vacio en la cantera.
public class Derrumbe : MonoBehaviour
{
    public float radioArea = 14f;
    public float cadaSegundos = 2.2f;
    public float radioRoca = 1.6f;
    public float aviso = 1.1f;
    public int dano = 20;
    [Tooltip("Modelo de roca (opcional). Si no, una esfera gris")]
    public GameObject modeloRoca;

    float proxima;

    void Update()
    {
        var kai = GameState.Kai;
        if (!kai || HUD.Pausado) return;
        Vector3 d = kai.position - transform.position; d.y = 0f;
        if (d.magnitude > radioArea) { proxima = Time.time + 1f; return; }
        if (Time.time < proxima) return;
        proxima = Time.time + cadaSegundos * Random.Range(0.8f, 1.2f);
        // cae cerca de Kai (a veces justo encima, para obligarlo a moverse)
        Vector2 r = Random.insideUnitCircle * 2.5f;
        Vector3 p = kai.position + new Vector3(r.x, 0f, r.y);
        if (Physics.Raycast(p + Vector3.up * 30f, Vector3.down, out var hit, 60f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
            && !hit.collider.GetComponentInParent<PlayerController>()) p = hit.point;
        else p.y = kai.position.y - 1f;
        StartCoroutine(Roca(p));
    }

    IEnumerator Roca(Vector3 suelo)
    {
        Efectos.AvisoCirculo(suelo, radioRoca, aviso, new Color(1f, 0.3f, 0.15f));
        GameObject roca = modeloRoca ? Instantiate(modeloRoca) : GameObject.CreatePrimitive(PrimitiveType.Sphere);
        var col = roca.GetComponent<Collider>(); if (col) Destroy(col);
        if (!modeloRoca) { roca.transform.localScale = Vector3.one * radioRoca * 1.1f; Colorear.Poner(roca, new Color(0.45f, 0.42f, 0.38f)); }
        float alto = 18f;
        for (float t = 0f; t < aviso; t += Time.deltaTime)
        {
            float k = t / aviso;
            roca.transform.position = suelo + Vector3.up * (alto * (1f - k * k) + radioRoca * 0.5f);
            roca.transform.Rotate(200f * Time.deltaTime, 90f * Time.deltaTime, 0f);
            yield return null;
        }
        roca.transform.position = suelo + Vector3.up * radioRoca * 0.4f;
        Sonidos.Tocar("aterrizaje", suelo, 1f, 0.15f, 0.05f);
        Efectos.Polvo(suelo, new Color(0.7f, 0.66f, 0.6f, 0.6f), 10, 1.4f);
        Efectos.Sacudir(0.12f, 0.2f);
        var kai = GameState.Kai;
        if (kai)
        {
            Vector3 d = kai.position - suelo; d.y = 0f;
            if (d.magnitude < radioRoca + 0.4f) GameState.DaniarKai(dano, kai.position);
        }
        yield return new WaitForSeconds(1.5f);
        Destroy(roca);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.8f, 0.5f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, radioArea);
    }
}
