using System.Collections.Generic;
using UnityEngine;

// Guardia del castillo (prueba de sigilo).
//   PATRULLA   -> camina entre sus puntos (o, si no tiene puntos, por puntos al azar dentro de "Radio Patrulla").
//   SOSPECHA   -> te vio un momento o te oyo correr: se detiene, mira y va a revisar donde estabas ( ? amarillo).
//   PERSECUCION-> la barra de sigilo se lleno: te persigue corriendo ( ! rojo) y avisa a los guardias cercanos.
//                Si te alcanza, te atrapa y vuelves al inicio.
//   BUSQUEDA   -> te perdio de vista: busca mirando a los lados donde te vio por ultima vez.
//   REGRESO    -> se rinde y vuelve a su ruta.
// Para escapar: corre (Shift), rompe la linea de vision (paredes, carretas) y escondete agachado junto a un barril (C).
// Si el guardia tiene un NavMeshAgent sobre un NavMesh horneado, lo usa para esquivar todo; si no, esquiva solo con rayos.
public class Sereno : MonoBehaviour
{
    public PlayerController jugador;
    public Transform reaparicion;
    public Transform[] puntos;
    public Light farol;

    [Header("Patrulla")]
    public float velocidad = 2.5f;
    public float pausa = 0.8f;
    [Tooltip("Si no pones puntos, patrulla por puntos al azar dentro de este radio alrededor de donde empieza")]
    public float radioPatrulla = 12f;

    [Header("Vision")]
    public float alcance = 8f;
    public float anguloVision = 60f;
    public float llenado = 80f, vaciado = 35f;
    [Tooltip("Si Kai corre (sin agacharse) dentro de este radio, el guardia lo oye y va a revisar")]
    public float radioOido = 4f;

    [Header("Persecucion")]
    public float velocidadInvestigar = 3.2f;
    public float velocidadPersecucion = 6.8f;   // Kai camina a 6 y corre a 9: corriendo se escapa mientras tenga estamina
    public float radioAtrapar = 1.3f;
    [Tooltip("Segundos sin verte antes de pasar a buscarte")]
    public float tiempoPerderRastro = 3f;
    public float tiempoBuscar = 4f;
    [Tooltip("Cuando empieza a perseguir, los guardias a esta distancia vienen a revisar")]
    public float radioAlerta = 15f;
    [Tooltip("No se aleja mas de esto de su zona (asi no te sigue fuera del castillo)")]
    public float distanciaMaxima = 35f;

    public enum Estado { Patrulla, Sospecha, Persecucion, Busqueda, Regreso }
    public Estado estado { get; private set; }

    public static readonly List<Sereno> Todos = new List<Sereno>();
    public static bool Persiguiendo { get { foreach (var s in Todos) if (s && s.estado == Estado.Persecucion) return true; return false; } }
    static float graciaHasta;                    // tras atraparte, unos segundos sin ver
    static int cuadroReporte = -1; static float maxDet; static bool algunoCerca;

    int indice;
    float det, espera, tiempoSinVer, tiempoEstado, miradaBase, alturaPie, revisarAtasco;
    bool atascado; Vector3 posAtasco; float ultimoAngulo;
    [Tooltip("Solo para ver en el Inspector en que estado va")] public string estadoActual;
    Vector3 inicio, ultimaPos, destinoAzar, posKaiAntes;
    Quaternion rotInicio;
    bool hayAzar;
    Color colorFarol = new Color(1f, 0.85f, 0.55f);
    TextMesh icono;
    UnityEngine.AI.NavMeshAgent agente;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void Reiniciar() { Todos.Clear(); graciaHasta = 0f; cuadroReporte = -1; }

    void OnEnable() { Todos.Add(this); }
    void OnDisable() { Todos.Remove(this); }

    void Start()
    {
        if (!jugador) jugador = FindFirstObjectByType<PlayerController>();
        inicio = transform.position; rotInicio = transform.rotation;
        if (farol)
        {
            farol.type = LightType.Spot;
            farol.spotAngle = anguloVision;
            farol.range = alcance + 2f;
            colorFarol = farol.color;
        }
        agente = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agente && !agente.isOnNavMesh) { agente.enabled = false; agente = null; }   // sin NavMesh horneado no sirve y bloquea
        var cc = GetComponent<CharacterController>();
        if (cc) cc.enabled = false;   // un CharacterController deshace los movimientos del script

        // puntos de patrulla: si son hijos del guardia se moverian con el; se sueltan en el mundo
        int validos = 0;
        if (puntos != null)
            foreach (var pt in puntos)
            {
                if (!pt) continue;
                if (pt.IsChildOf(transform)) pt.SetParent(null, true);
                if (Plano(pt.position - inicio).magnitude > 0.5f) validos++;
            }
        if (validos == 0)
        {
            if (puntos != null && puntos.Length > 0) Debug.LogWarning("[Sereno] " + name + ": sus puntos estan vacios o encima de el. Patrullara al azar (Radio Patrulla).", this);
            puntos = null;
        }

        // altura del pivote sobre el suelo (si el pivote del modelo no esta en los pies, no se entierra)
        alturaPie = 0f;
        if (Suelo(transform.position, 3f, out float y0)) alturaPie = Mathf.Clamp(transform.position.y - y0, 0f, 3f);
        CrearIcono();
        if (jugador) posKaiAntes = jugador.transform.position;
    }

    void Update()
    {
        if (!jugador || HUD.Pausado) return;
        bool viendo = Viendo();
        bool oyendo = Oyendo();
        tiempoEstado += Time.deltaTime;
        float f = Tripulacion.FactorDeteccion;   // Sombra: te ven mas lento

        switch (estado)
        {
            case Estado.Patrulla:
                Patrullar();
                Llenar(viendo, f);
                if (det >= 100f) Perseguir(true);
                else if ((viendo && det > 35f) || oyendo) Sospechar();
                break;

            case Estado.Sospecha:
                Llenar(viendo, f * 1.3f);
                if (viendo || oyendo) ultimaPos = jugador.transform.position;
                if (det >= 100f) { Perseguir(true); break; }
                if (tiempoEstado < 0.7f) Mirar(ultimaPos);                 // se queda mirando un momento
                else if (Mover(ultimaPos, velocidadInvestigar) || atascado) Cambiar(Estado.Busqueda);
                if (det <= 0f && !viendo && tiempoEstado > 2f) Cambiar(Estado.Regreso);
                break;

            case Estado.Persecucion:
                det = 100f;
                if (viendo) { ultimaPos = jugador.transform.position; tiempoSinVer = 0f; }
                else tiempoSinVer += Time.deltaTime;
                if (Plano(jugador.transform.position - transform.position).magnitude < radioAtrapar && !jugador.oculto) { Atrapar(); break; }
                bool llego = Mover(viendo ? jugador.transform.position : ultimaPos, velocidadPersecucion);
                if (tiempoSinVer > tiempoPerderRastro || ((llego || atascado) && !viendo)) Cambiar(Estado.Busqueda);
                if (Plano(transform.position - inicio).magnitude > distanciaMaxima) Cambiar(Estado.Regreso);
                break;

            case Estado.Busqueda:
                // mira a un lado y al otro donde te vio por ultima vez
                transform.rotation = Quaternion.Euler(0f, miradaBase + Mathf.Sin(tiempoEstado * 2.2f) * 70f, 0f);
                det = Mathf.Max(0f, det - vaciado * 0.5f * Time.deltaTime);
                if (viendo) det = Mathf.Min(100f, det + llenado * 2f * f * Time.deltaTime);
                if (viendo && det >= 60f) Perseguir(false);
                else if (tiempoEstado > tiempoBuscar) Cambiar(Estado.Regreso);
                break;

            case Estado.Regreso:
                Llenar(viendo, f);
                if (det >= 100f) { Perseguir(true); break; }
                if (oyendo || (viendo && det > 35f)) { Sospechar(); break; }
                Vector3 vuelta = puntos != null && puntos.Length > 0 && puntos[indice] ? puntos[indice].position : inicio;
                if (Mover(vuelta, velocidad) || atascado) { hayAzar = false; Cambiar(Estado.Patrulla); }
                break;
        }

        Reportar();
        Visuales();
        estadoActual = estado + (estado == Estado.Patrulla && espera > 0f ? " (pausa)" : "") + "  deteccion " + Mathf.RoundToInt(det);
        posKaiAntes = jugador.transform.position;
    }

    // ================================================================ estados
    void Cambiar(Estado e)
    {
        estado = e; tiempoEstado = 0f;
        atascado = false; posAtasco = transform.position; revisarAtasco = Time.time + 1f; ultimoAngulo = 0f;
        if (e == Estado.Busqueda) miradaBase = transform.eulerAngles.y;
        if (e == Estado.Patrulla) espera = 0.3f;
    }

    void Sospechar()
    {
        ultimaPos = jugador.transform.position;
        Cambiar(Estado.Sospecha);
    }

    void Perseguir(bool avisarOtros)
    {
        bool nueva = estado != Estado.Persecucion;
        det = 100f; tiempoSinVer = 0f; ultimaPos = jugador.transform.position;
        Cambiar(Estado.Persecucion);
        if (!nueva) return;
        GameState.Avisar("¡Te descubrieron! ¡Corre y escóndete!", 2f);
        if (!avisarOtros) return;
        foreach (var s in Todos)   // los compañeros cercanos vienen a revisar
            if (s && s != this && s.estado != Estado.Persecucion && Vector3.Distance(s.transform.position, transform.position) < radioAlerta)
            { s.ultimaPos = ultimaPos; s.det = Mathf.Max(s.det, 50f); s.Cambiar(Estado.Sospecha); }
    }

    void Atrapar() { AtraparA(jugador, reaparicion, "¡Te atraparon los guardias! Inténtalo de nuevo."); }

    // Kai atrapado (por un guardia o por el farol de la torre): vuelve al punto de reaparicion
    // y todas las patrullas empiezan de nuevo (falla blanda, como dice el GDD)
    public static void AtraparA(PlayerController kai, Transform reaparicion, string mensaje)
    {
        if (!kai) return;
        GameState.Avisar(mensaje, 2.5f);
        if (reaparicion) kai.Reaparecer(reaparicion);
        graciaHasta = Time.time + 2f;
        EntradaSigilo.Reiniciar();   // tiene que volver a entrar por la puerta
        foreach (var s in Todos.ToArray()) if (s) s.VolverAlInicio();
        FarolGiratorio.ReiniciarTodos();
    }

    void VolverAlInicio()
    {
        if (agente) agente.Warp(inicio); else transform.position = inicio;
        transform.rotation = rotInicio;
        det = 0f; indice = 0; hayAzar = false;
        Cambiar(Estado.Patrulla);
    }

    void Llenar(bool viendo, float f)
    {
        det = Mathf.Clamp(det + (viendo ? llenado * f : -vaciado) * Time.deltaTime, 0f, 100f);
    }

    // ================================================================ sentidos
    bool Viendo()
    {
        if (Time.time < graciaHasta || jugador.oculto || jugador.EstaMuerto) return false;
        Vector3 ojos = transform.position + Vector3.up * 0.6f;
        Vector3 blanco = jugador.transform.position + Vector3.up * (jugador.agachado ? -0.5f : 0.3f);
        Vector3 d = blanco - ojos;
        // persiguiendo ve mas lejos y en un angulo mas amplio
        float rango = (estado == Estado.Persecucion ? alcance * 1.6f : alcance) * CicloDia.FactorVision;   // de noche ve menos
        float angulo = estado == Estado.Persecucion ? 160f : anguloVision;
        if (d.magnitude > rango || Vector3.Angle(transform.forward, Plano(d)) > angulo * 0.5f) return false;
        if (Physics.Linecast(ojos, blanco, out RaycastHit hit, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            return hit.collider.GetComponentInParent<PlayerController>() != null;
        return true;
    }

    // oye a Kai si corre cerca sin agacharse
    bool Oyendo()
    {
        if (Time.time < graciaHasta || jugador.agachado || jugador.oculto) return false;
        float vel = Plano(jugador.transform.position - posKaiAntes).magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
        return vel > 7.5f && Vector3.Distance(jugador.transform.position, transform.position) < radioOido;
    }

    // ================================================================ movimiento
    void Patrullar()
    {
        if (espera > 0f)
        {
            // pausa corta mirando un poco a los lados (no se queda como estatua)
            espera -= Time.deltaTime;
            transform.Rotate(0f, Mathf.Sin(Time.time * 2f) * 45f * Time.deltaTime, 0f);
            return;
        }
        Vector3 destino;
        if (puntos != null && puntos.Length > 0)
        {
            if (!puntos[indice]) { indice = (indice + 1) % puntos.Length; return; }
            destino = puntos[indice].position;
            // al llegar, o si se atasca, sigue con el siguiente punto
            if (Mover(destino, velocidad) || atascado) { indice = (indice + 1) % puntos.Length; espera = atascado ? 0f : Pausa(); atascado = false; }
            return;
        }
        // sin puntos: va a lugares al azar que pueda ver sin paredes en medio
        if (!hayAzar) { destinoAzar = PuntoAzar(); hayAzar = true; tiempoEstado = 0f; }
        if (Mover(destinoAzar, velocidad) || atascado || tiempoEstado > 10f) { hayAzar = false; espera = atascado ? 0f : Pausa(); atascado = false; }
    }

    // pausa al llegar a un punto: corta y un poco distinta cada vez (maximo 1.5 s)
    float Pausa() => Mathf.Min(Random.Range(pausa * 0.5f, pausa), 1.5f);

    // elige un lugar al azar dentro del radio al que se llega en linea recta (sin paredes ni cajas en medio)
    Vector3 PuntoAzar()
    {
        Vector3 aqui = transform.position;
        for (int i = 0; i < 40; i++)
        {
            Vector3 c = inicio + new Vector3(Random.Range(-1f, 1f), 0f, Random.Range(-1f, 1f)) * radioPatrulla;
            c.y = aqui.y;
            float dist = Plano(c - aqui).magnitude;
            if (dist < 2.5f) continue;
            if (!Camino(aqui, c)) continue;
            if (!Suelo(c, 1.2f, out _)) continue;
            return c;
        }
        // nada libre lejos: prueba pasos cortos alrededor (8 direcciones)
        for (int k = 0; k < 8; k++)
        {
            Vector3 c = aqui + Quaternion.Euler(0f, k * 45f + Random.Range(0f, 30f), 0f) * Vector3.forward * Random.Range(2.5f, 5f);
            if (Camino(aqui, c)) return c;
        }
        return inicio;   // vuelve a donde empezo
    }

    // true si se puede caminar en linea recta de a hasta b
    bool Camino(Vector3 a, Vector3 b)
    {
        Vector3 o = Pies(a) + Vector3.up * 0.8f;
        Vector3 d = Plano(b - a);
        foreach (var h in Physics.SphereCastAll(o, 0.4f, d.normalized, d.magnitude + 0.5f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (h.collider.transform.IsChildOf(transform) || h.collider.GetComponentInParent<PlayerController>() || h.collider.GetComponentInParent<Sereno>()) continue;
            return false;
        }
        return true;
    }

    Vector3 Pies(Vector3 pos) => pos - Vector3.up * alturaPie;

    // camina hacia 'destino'; devuelve true al llegar
    bool Mover(Vector3 destino, float vel)
    {
        if (agente)
        {
            agente.speed = vel; agente.angularSpeed = 540f; agente.stoppingDistance = 0.2f;
            agente.SetDestination(destino);
            return !agente.pathPending && agente.remainingDistance < 0.4f;
        }
        Vector3 d = Plano(destino - transform.position);
        if (d.magnitude < 0.35f) { atascado = false; return true; }
        // si en 1 segundo casi no avanzo, esta trabado contra algo
        if (Time.time > revisarAtasco)
        {
            atascado = Plano(transform.position - posAtasco).magnitude < 0.3f * vel / 2.5f && d.magnitude > 1f;
            posAtasco = transform.position; revisarAtasco = Time.time + 1f;
        }
        Vector3 dir = Esquivar(d.normalized);
        if (dir.sqrMagnitude < 0.001f) { atascado = true; return false; }
        transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(dir), 540f * Time.deltaTime);
        transform.position += dir * Mathf.Min(vel * Time.deltaTime, d.magnitude);
        PegarAlSuelo();
        return false;
    }

    void Mirar(Vector3 p)
    {
        if (agente) agente.ResetPath();
        Vector3 d = Plano(p - transform.position);
        if (d.sqrMagnitude > 0.01f) transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(d), 300f * Time.deltaTime);
    }

    // si hay una caja o pared delante, busca un hueco a los lados
    Vector3 Esquivar(Vector3 dir)
    {
        Vector3 o = Pies(transform.position) + Vector3.up * 0.8f;
        if (!Choca(o, dir)) { ultimoAngulo = 0f; return dir; }
        // sigue rodeando por el mismo lado que antes (asi no tiembla de un lado al otro)
        if (ultimoAngulo != 0f)
        {
            Vector3 mismo = Quaternion.Euler(0f, ultimoAngulo, 0f) * dir;
            if (!Choca(o, mismo)) return mismo;
        }
        float[] angulos = { 30f, -30f, 60f, -60f, 90f, -90f, 130f, -130f, 180f };
        foreach (float a in angulos)
        {
            Vector3 prueba = Quaternion.Euler(0f, a, 0f) * dir;
            if (!Choca(o, prueba)) { ultimoAngulo = a; return prueba; }
        }
        return Vector3.zero;   // rodeado: no empuja contra la pared
    }

    bool Choca(Vector3 o, Vector3 dir)
    {
        foreach (var h in Physics.SphereCastAll(o, 0.35f, dir, 1.2f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (h.collider.transform.IsChildOf(transform)) continue;
            if (h.collider.GetComponentInParent<PlayerController>()) continue;
            if (h.distance <= 0f) continue;   // ya lo estaba tocando: deja que se aleje
            return true;
        }
        return false;
    }

    // mantiene la misma altura sobre el suelo que tenia al empezar (sube rampas, no se entierra)
    void PegarAlSuelo()
    {
        Vector3 p = transform.position;
        if (!Suelo(p, 1.2f, out float y)) return;
        p.y = Mathf.Lerp(p.y, y + alturaPie, 15f * Time.deltaTime);
        transform.position = p;
    }

    // busca el suelo debajo de los pies (ignora al guardia, a Kai y a lo que este muy arriba, como mesas)
    bool Suelo(Vector3 pos, float margen, out float y)
    {
        float pies = pos.y - alturaPie;
        y = pies; float mejor = float.MaxValue; bool hay = false;
        foreach (var h in Physics.RaycastAll(new Vector3(pos.x, pies + margen, pos.z), Vector3.down, margen + 3f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            if (h.collider.transform.IsChildOf(transform) || h.collider.GetComponentInParent<PlayerController>() || h.collider.GetComponentInParent<Sereno>()) continue;
            if (h.distance < mejor) { mejor = h.distance; y = h.point.y; hay = true; }
        }
        return hay;
    }

    static Vector3 Plano(Vector3 v) { v.y = 0f; return v; }

    // ================================================================ HUD y visual
    // con varios guardias, la barra de sigilo muestra el que mas te ha visto
    void Reportar()
    {
        if (Time.frameCount != cuadroReporte) { cuadroReporte = Time.frameCount; maxDet = 0f; algunoCerca = false; }
        maxDet = Mathf.Max(maxDet, det);
        algunoCerca |= estado == Estado.Persecucion || Vector3.Distance(jugador.transform.position, transform.position) < alcance + 5f;
        GameState.Deteccion = maxDet;
        GameState.MostrarDeteccion = algunoCerca;
    }

    void CrearIcono()
    {
        var go = new GameObject("Alerta");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.up * 2.5f;
        icono = go.AddComponent<TextMesh>();
        var fuente = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        icono.font = fuente; icono.fontSize = 64; icono.characterSize = 0.08f;
        icono.anchor = TextAnchor.MiddleCenter; icono.alignment = TextAlignment.Center; icono.fontStyle = FontStyle.Bold;
        go.GetComponent<MeshRenderer>().material = fuente.material;
        icono.text = "";
    }

    void Visuales()
    {
        Color c = estado == Estado.Persecucion ? new Color(1f, 0.2f, 0.15f)
                : estado == Estado.Sospecha || estado == Estado.Busqueda || det > 35f ? new Color(1f, 0.6f, 0.15f)
                : colorFarol;
        if (farol) farol.color = Color.Lerp(farol.color, c, 6f * Time.deltaTime);
        if (!icono) return;
        icono.text = estado == Estado.Persecucion ? "!" : estado == Estado.Sospecha || estado == Estado.Busqueda ? "?" : "";
        icono.color = estado == Estado.Persecucion ? new Color(1f, 0.15f, 0.1f) : new Color(1f, 0.85f, 0.2f);
        var cam = Camera.main;
        if (cam) icono.transform.rotation = Quaternion.LookRotation(icono.transform.position - cam.transform.position);
        float s = 1f + Mathf.Sin(Time.time * 10f) * (estado == Estado.Persecucion ? 0.12f : 0.05f);
        icono.transform.localScale = Vector3.one * s;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 izq = Quaternion.Euler(0, -anguloVision / 2f, 0) * transform.forward * alcance;
        Vector3 der = Quaternion.Euler(0, anguloVision / 2f, 0) * transform.forward * alcance;
        Gizmos.DrawLine(transform.position, transform.position + izq);
        Gizmos.DrawLine(transform.position, transform.position + der);
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.6f); Gizmos.DrawWireSphere(transform.position, radioOido);
        if (puntos == null || puntos.Length == 0) { Gizmos.color = Color.cyan; Gizmos.DrawWireSphere(Application.isPlaying ? inicio : transform.position, radioPatrulla); }
        else
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i < puntos.Length; i++)
                if (puntos[i] && puntos[(i + 1) % puntos.Length]) Gizmos.DrawLine(puntos[i].position, puntos[(i + 1) % puntos.Length].position);
        }
    }
}
