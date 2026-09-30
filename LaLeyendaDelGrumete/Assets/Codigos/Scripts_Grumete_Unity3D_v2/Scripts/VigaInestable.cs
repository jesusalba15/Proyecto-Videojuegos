using System.Collections;
using UnityEngine;

// Viga del mastil: al pisarla se pone roja, desaparece y vuelve a los pocos segundos.
public class VigaInestable : MonoBehaviour
{
    public float tiempoAntes = 0.7f;
    public float tiempoVolver = 3f;

    Collider col;
    Renderer[] rends;
    bool activa;

    void Awake()
    {
        col = GetComponent<Collider>();
        rends = GetComponentsInChildren<Renderer>();
    }

    public void Pisar()
    {
        if (!activa) StartCoroutine(Caer());
    }

    IEnumerator Caer()
    {
        activa = true;
        Colorear.Poner(gameObject, new Color(1f, 0.35f, 0.3f));
        yield return new WaitForSeconds(tiempoAntes);
        col.enabled = false;
        foreach (var r in rends) r.enabled = false;
        yield return new WaitForSeconds(tiempoVolver);
        col.enabled = true;
        foreach (var r in rends) r.enabled = true;
        Colorear.Restaurar(gameObject);
        activa = false;
    }
}
