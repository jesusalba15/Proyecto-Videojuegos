using System.Collections.Generic;
using UnityEngine;

// Farol giratorio de la torre de vigia (Isla 4, Bahia de la Corona).
// Un foco que barre la plaza dando vueltas. Si Kai queda dentro del haz (y no esta escondido),
// la barra de sigilo se llena; si se llena, lo atrapan y vuelve al punto de reaparicion.
// Ponlo en lo alto de la torre. Si no tiene una luz Spot de hijo, la crea.
public class FarolGiratorio : MonoBehaviour
{
    public Transform reaparicion;
    [Tooltip("Segundos que tarda en dar una vuelta completa")]
    public float segundosPorVuelta = 8f;
    [Tooltip("Hacia abajo: cuanto se inclina el foco (grados)")]
    public float inclinacion = 35f;
    public float alcance = 30f;
    [Tooltip("Ancho del haz en grados")]
    public float anguloHaz = 18f;
    [Tooltip("Barra de deteccion por segundo dentro del haz (0 a 100)")]
    public float llenado = 160f;
    public float vaciado = 60f;
    public Color color = new Color(1f, 0.92f, 0.7f);
    [Tooltip("Solo funciona de noche (de dia el farol esta apagado)")]
    public bool soloDeNoche = true;

    static readonly List<FarolGiratorio> todos = new List<FarolGiratorio>();
    public static void ReiniciarTodos() { foreach (var f in todos) if (f) f.det = 0f; }

    Light foco;
    float det, angulo, gracia;
    bool mostraba;

    void OnEnable() { if (!todos.Contains(this)) todos.Add(this); }
    void OnDisable() { todos.Remove(this); }

    void Start()
    {
        foco = GetComponentInChildren<Light>();
        if (!foco || foco.type != LightType.Spot)
        {
            var go = new GameObject("Foco");
            go.transform.SetParent(transform, false);
            foco = go.AddComponent<Light>();
            foco.type = LightType.Spot;
        }
        foco.color = color; foco.range = alcance * 1.2f; foco.spotAngle = anguloHaz * 1.6f; foco.intensity = 6f;
        foco.shadows = LightShadows.Soft;
    }

    void Update()
    {
        bool prendido = !soloDeNoche || !CicloDia.Actual || CicloDia.Luz < 0.6f;
        if (foco) foco.enabled = prendido;
        angulo = Mathf.Repeat(angulo + 360f / Mathf.Max(1f, segundosPorVuelta) * Time.deltaTime, 360f);
        if (foco) foco.transform.rotation = Quaternion.Euler(inclinacion, angulo, 0f);

        var kai = GameState.Kai ? GameState.Kai.GetComponent<PlayerController>() : null;
        bool viendo = prendido && kai && Time.time > gracia && Ve(kai);
        det = Mathf.Clamp(det + (viendo ? llenado * Tripulacion.FactorDeteccion : -vaciado) * Time.deltaTime, 0f, 100f);
        if (det >= 100f)
        {
            det = 0f; gracia = Time.time + 2f;
            Sereno.AtraparA(kai, reaparicion, "¡El farol de la torre te descubrió! Vuelves a la cala.");
        }
    }

    void LateUpdate()
    {
        // la barra de sigilo del HUD muestra lo que mas te esta viendo (guardias o farol)
        if (det > 0.5f) { GameState.Deteccion = Mathf.Max(GameState.Deteccion, det); GameState.MostrarDeteccion = true; mostraba = true; }
        else if (mostraba) { mostraba = false; if (Sereno.Todos.Count == 0) GameState.MostrarDeteccion = false; }
    }

    bool Ve(PlayerController kai)
    {
        if (kai.oculto || kai.EstaMuerto) return false;
        Vector3 desde = foco ? foco.transform.position : transform.position;
        Vector3 haciaKai = kai.transform.position - desde;
        if (haciaKai.magnitude > alcance) return false;
        Vector3 eje = foco ? foco.transform.forward : transform.forward;
        if (Vector3.Angle(eje, haciaKai) > anguloHaz * 0.5f) return false;
        // algo en medio (una pared, un techo): no lo ve
        if (Physics.Raycast(desde, haciaKai.normalized, out var hit, haciaKai.magnitude - 0.6f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
            && !hit.collider.GetComponentInParent<PlayerController>()) return false;
        return true;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = color;
        Gizmos.DrawWireSphere(transform.position, alcance);
    }
}
