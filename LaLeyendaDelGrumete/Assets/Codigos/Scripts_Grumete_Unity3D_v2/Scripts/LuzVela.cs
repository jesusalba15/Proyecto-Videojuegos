using UnityEngine;

// Ponlo en una Point Light (o en una vela con una Point Light hija): la luz titila como una llama.
public class LuzVela : MonoBehaviour
{
    public float intensidadBase = 2.5f;
    public float variacion = 0.6f;
    public float velocidad = 7f;
    public float temblor = 0.015f;   // la llama se mueve un poquito
    [HideInInspector] public float factor = 1f;   // lo usa Farol para prenderla de noche

    Light luz;
    Vector3 posBase;
    float semilla;

    void Start()
    {
        luz = GetComponentInChildren<Light>();
        if (!luz) { luz = gameObject.AddComponent<Light>(); luz.type = LightType.Point; luz.range = 6f; luz.color = new Color(1f, 0.7f, 0.4f); }
        posBase = luz.transform.localPosition;
        semilla = Random.Range(0f, 100f);
    }

    void Update()
    {
        if (!luz) return;
        float t = Time.time * velocidad + semilla;
        float ruido = Mathf.PerlinNoise(t, semilla) - 0.5f;
        luz.intensity = (intensidadBase + ruido * 2f * variacion) * factor;
        luz.enabled = factor > 0.01f;
        luz.transform.localPosition = posBase + new Vector3(Mathf.PerlinNoise(semilla, t) - 0.5f, 0f, Mathf.PerlinNoise(t, t) - 0.5f) * temblor * 2f;
    }
}
