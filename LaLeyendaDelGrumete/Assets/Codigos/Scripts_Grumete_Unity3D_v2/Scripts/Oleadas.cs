using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Arena de oleadas (Isla 5: el foso del Fuerte de Roca; Isla 6: el patio de armas cuando suena la alarma).
// Kai entra al circulo: se cierran las rejas y van saliendo oleadas de enemigos. Al ganar la ultima
// se abren las rejas y se da la Recompensa (queda guardado). Si Kai cae, la arena vuelve a empezar.
//  - "Modelos": enemigos de la escena (desactivados) que sirven de molde; se copian en cada oleada.
//  - "Puntos Salida": desde donde aparecen (vacio = alrededor del centro).
public class Oleadas : MonoBehaviour
{
    public float radio = 9f;
    [Tooltip("Enemigos por oleada (por ejemplo 2, 3, 4)")]
    public int[] porOleada = { 2, 3, 4 };
    [Tooltip("Enemigos molde (desactivados en la escena). Se elige uno al azar para cada enemigo")]
    public Rival[] modelos;
    public Transform[] puntosSalida;
    [Tooltip("Rejas o puertas que se cierran durante la pelea (se activan)")]
    public GameObject[] rejas;
    [Tooltip("Se da al ganar la ultima oleada (y queda guardada)")]
    public RecompensaIsla recompensa;
    public string nombreArena = "El foso";
    public float esperaEntreOleadas = 2.5f;

    public bool Hecha => recompensa ? recompensa.Hecha : hecha;
    public int OleadaActual { get; private set; }
    public bool EnCurso { get; private set; }

    readonly List<Rival> vivos = new List<Rival>();
    bool hecha;

    void Start()
    {
        foreach (var m in modelos) if (m) m.gameObject.SetActive(false);
        Rejas(false);
        if (recompensa) recompensa.cuando = RecompensaIsla.Cuando.AlLlamar;
    }

    void Rejas(bool cerradas) { if (rejas != null) foreach (var r in rejas) if (r) r.SetActive(cerradas); }

    void Update()
    {
        if (Hecha || EnCurso || HUD.Pausado) return;
        var kai = GameState.Kai;
        if (kai && Plano(kai.position - transform.position).magnitude < radio * 0.8f) StartCoroutine(Pelea());
    }

    IEnumerator Pelea()
    {
        EnCurso = true;
        Rejas(true);
        Sonidos.Musica("musica_jefe");
        GameState.Avisar("¡" + nombreArena + "! Aguanta " + porOleada.Length + " oleadas.", 2.5f);
        var pc = GameState.Kai ? GameState.Kai.GetComponent<PlayerController>() : null;
        for (OleadaActual = 1; OleadaActual <= porOleada.Length; OleadaActual++)
        {
            yield return new WaitForSeconds(OleadaActual == 1 ? 1.5f : esperaEntreOleadas);
            GameState.Avisar("Oleada " + OleadaActual + " de " + porOleada.Length, 1.8f);
            for (int i = 0; i < porOleada[OleadaActual - 1]; i++) Sacar(i);
            // espera a que caigan todos (o a que caiga Kai)
            while (true)
            {
                if (pc && pc.EstaMuerto) { Reiniciar(); yield break; }
                vivos.RemoveAll(r => !r || r.Muerto);
                if (vivos.Count == 0) break;
                yield return null;
            }
        }
        EnCurso = false;
        hecha = true;
        Rejas(false);
        Sonidos.Musica(null);
        Sonidos.Tocar("victoria", null, 1f, 0f, 1f);
        GameState.Avisar("¡Ganaste " + nombreArena.ToLower() + "!", 3f);
        Efectos.Anillo(transform.position, radio, new Color(1f, 0.85f, 0.3f), 1f);
        if (recompensa) recompensa.Dar();
    }

    void Sacar(int i)
    {
        if (modelos == null || modelos.Length == 0) { Debug.LogWarning("[Oleadas] " + name + " no tiene Modelos de enemigo.", this); return; }
        var molde = modelos[Random.Range(0, modelos.Length)];
        if (!molde) return;
        Vector3 p;
        if (puntosSalida != null && puntosSalida.Length > 0 && puntosSalida[i % puntosSalida.Length])
            p = puntosSalida[i % puntosSalida.Length].position;
        else
        {
            float a = (i / (float)Mathf.Max(1, porOleada[OleadaActual - 1])) * Mathf.PI * 2f + OleadaActual;
            p = transform.position + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radio * 0.7f + Vector3.up;
        }
        var go = Instantiate(molde.gameObject, p, Quaternion.LookRotation(Plano(transform.position - p) + Vector3.forward * 0.001f));
        go.name = molde.name + "_oleada" + OleadaActual;
        var r = go.GetComponent<Rival>();
        r.esDuelo = false; r.centroZona = transform; r.radioZona = radio; r.radioPersecucion = radio * 1.5f;
        go.SetActive(true);
        Efectos.Polvo(p - Vector3.up * 0.8f, new Color(0.8f, 0.74f, 0.62f, 0.5f), 8, 1f);
        vivos.Add(r);
    }

    // Kai cayo: se borran los enemigos y la arena queda lista para empezar otra vez
    void Reiniciar()
    {
        foreach (var r in vivos) if (r) Destroy(r.gameObject);
        vivos.Clear();
        Rejas(false);
        Sonidos.Musica(null);
        EnCurso = false;
        OleadaActual = 0;
    }

    static Vector3 Plano(Vector3 v) { v.y = 0f; return v; }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.4f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, radio);
    }
}
