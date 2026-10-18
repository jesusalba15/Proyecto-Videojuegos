using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Barra de habilidades estilo Albion: cada arma tiene 4 habilidades en las teclas 1, 2, 3 y 4.
// Cada una gasta aguante y tiene su propio enfriamiento (se ve como un reloj oscuro en la barra del HUD).
// Las habilidades estan definidas en Equipo.cs (dentro de cada arma).
// PlayerController lo agrega solo a Kai; no hace falta ponerlo a mano.
public class HabilidadesKai : MonoBehaviour
{
    public static HabilidadesKai Actual;

    // momento en que cada habilidad vuelve a estar lista: [arma, casilla]
    readonly float[,] listoEn = new float[8, 4];
    PlayerController pc;
    CharacterController cc;
    AnimacionSimple anim;

    // true mientras Kai hace una estocada o un abordaje (no puede moverse ni atacar)
    public bool Ocupado { get; private set; }

    void Awake() { Actual = this; }
    void OnDestroy() { if (Actual == this) Actual = null; }

    void Start()
    {
        pc = GetComponent<PlayerController>();
        cc = GetComponent<CharacterController>();
        anim = GetComponentInChildren<AnimacionSimple>();
    }

    // ---------------- consultas para el HUD
    public float Restante(int casilla) => Mathf.Max(0f, listoEn[Equipo.arma, casilla] - Time.time);
    public float Fraccion(int casilla)
    {
        var h = Equipo.ArmaActual.habilidades;
        if (h == null || casilla >= h.Length || h[casilla].espera <= 0f) return 0f;
        return Mathf.Clamp01(Restante(casilla) / h[casilla].espera);
    }

    void Update()
    {
        if (!pc || pc.EstaMuerto || HUD.Pausado || Ocupado) return;
        for (int i = 0; i < 4; i++)
            if (Input.GetKeyDown(KeyCode.Alpha1 + i) || Input.GetKeyDown(KeyCode.Keypad1 + i)) Usar(i);
    }

    public void Usar(int casilla)
    {
        if (ZonaCorona.ProhibeCombate(transform.position)) return;
        var hs = Equipo.ArmaActual.habilidades;
        if (hs == null || hs.Length == 0) { GameState.Avisar("Con los puños no hay habilidades. ¡Consigue un arma!", 1.2f); return; }
        if (casilla >= hs.Length) return;
        if (!Equipo.HabilidadDesbloqueada(casilla)) { GameState.Avisar("Se desbloquea en el nivel " + Equipo.NivelHabilidad[casilla] + ".", 1.2f); return; }
        var h = hs[casilla];
        float falta = Restante(casilla);
        if (falta > 0f) { GameState.Avisar(h.nombre + ": lista en " + Mathf.CeilToInt(falta) + " s", 0.8f); return; }
        if (GameState.Aguante < h.aguante) { GameState.Avisar("¡Sin aliento para " + h.nombre + "!", 0.8f); return; }

        listoEn[Equipo.arma, casilla] = Time.time + h.espera;
        pc.Gastar(h.aguante);
        MirarAlEnemigo(h.alcance > 0 ? h.alcance : h.radio + 2f);
        HUD.Numero(transform.position + Vector3.up * 2.1f, h.nombre, h.color, 0.75f);

        switch (h.tipo)
        {
            case Equipo.TipoHabilidad.Area:
                if (anim) anim.Atacar();
                Efectos.Anillo(transform.position, h.radio, h.color);
                if (h.mult >= 2f) Efectos.Sacudir(0.25f, 0.3f);
                foreach (var r in EnRadio(transform.position, h.radio)) Golpear(r, h);
                break;

            case Equipo.TipoHabilidad.Cono:
                if (anim) anim.Atacar();
                Efectos.Cono(transform.position, transform.forward, h.radio, h.color);
                foreach (var r in EnCono(h.radio, 120f)) Golpear(r, h);
                break;

            case Equipo.TipoHabilidad.Grito:
                Efectos.Anillo(transform.position, h.radio, h.color, 0.6f);
                Efectos.Sacudir(0.08f, 0.25f);
                foreach (var r in EnRadio(transform.position, h.radio)) r.Aturdir(h.aturdir);
                break;

            case Equipo.TipoHabilidad.Furia:
                Equipo.CriticosGarantizados = h.golpes;
                Efectos.Anillo(transform.position, 1.6f, h.color, 0.6f);
                GameState.Avisar("¡Furia! Tus " + h.golpes + " siguientes golpes serán críticos.", 1.5f);
                break;

            case Equipo.TipoHabilidad.Estocada:
                StartCoroutine(Estocada(h));
                break;

            case Equipo.TipoHabilidad.Enganche:
            {
                var r = MasCercanoAdelante(h.alcance, 60f);
                if (!r) { GameState.Avisar("El garfio no alcanza a nadie.", 0.8f); listoEn[Equipo.arma, casilla] = Time.time + 1f; break; }
                if (anim) anim.Atacar();
                Efectos.Linea(transform.position + Vector3.up * 1.1f, r.transform.position + Vector3.up * 1.1f, new Color(0.75f, 0.6f, 0.4f), 0.35f);
                r.AtraerHacia(transform.position + transform.forward * 1.4f);
                Golpear(r, h);
                break;
            }

            case Equipo.TipoHabilidad.Rafaga:
                StartCoroutine(Rafaga(h));
                break;

            case Equipo.TipoHabilidad.Sangrado:
            {
                if (anim) anim.Atacar();
                Efectos.Cono(transform.position, transform.forward, h.radio + 0.4f, h.color, 90f);
                foreach (var r in EnCono(h.radio + 0.4f, 90f))
                {
                    Golpear(r, h);
                    r.Sangrar(Mathf.Max(2, Mathf.RoundToInt(Equipo.ArmaActual.danoMin * 0.4f)), h.golpes);
                }
                break;
            }

            case Equipo.TipoHabilidad.Disparo:
                StartCoroutine(Disparos(h));
                break;

            case Equipo.TipoHabilidad.Explosivo:
            {
                if (anim) anim.Atacar();
                Disparar(h.alcance, h.color, out Vector3 fin);
                Efectos.Anillo(fin, h.radio, h.color, 0.5f);
                Efectos.Sacudir(0.18f, 0.25f);
                foreach (var r in EnRadio(fin, h.radio)) Golpear(r, h);
                break;
            }

            case Equipo.TipoHabilidad.Abordaje:
            {
                var r = MasCercano(h.alcance);
                if (!r) { GameState.Avisar("No hay nadie a quien abordar.", 0.8f); listoEn[Equipo.arma, casilla] = Time.time + 1f; break; }
                StartCoroutine(Abordaje(h, r));
                break;
            }
        }
    }

    // ---------------- habilidades con movimiento
    IEnumerator Estocada(Equipo.Habilidad h)
    {
        Ocupado = true;
        if (anim) anim.Atacar();
        var golpeados = new HashSet<Rival>();
        Vector3 inicio = transform.position, dir = transform.forward;
        float dur = 0.18f;
        for (float t = 0; t < dur; t += Time.deltaTime)
        {
            cc.Move(dir * (h.alcance / dur) * Time.deltaTime + Vector3.down * 2f * Time.deltaTime);
            foreach (var r in EnRadio(transform.position, h.radio))
                if (golpeados.Add(r)) Golpear(r, h);
            yield return null;
        }
        Efectos.Linea(inicio + Vector3.up, transform.position + Vector3.up, h.color, 0.25f);
        Ocupado = false;
    }

    IEnumerator Rafaga(Equipo.Habilidad h)
    {
        Ocupado = true;
        for (int i = 0; i < h.golpes; i++)
        {
            if (anim) anim.Atacar();
            Efectos.Cono(transform.position, transform.forward, h.radio, h.color, 80f, 0.2f);
            foreach (var r in EnCono(h.radio, 90f)) Golpear(r, h);
            yield return new WaitForSeconds(0.17f);
        }
        Ocupado = false;
    }

    IEnumerator Abordaje(Equipo.Habilidad h, Rival r)
    {
        Ocupado = true;
        Vector3 inicio = transform.position;
        Vector3 d = r.transform.position - inicio; d.y = 0f;
        Vector3 destino = r.transform.position - d.normalized * 1.2f;
        float dur = 0.35f;
        for (float t = 0; t < dur; t += Time.deltaTime)
        {
            float k = t / dur;
            Vector3 p = Vector3.Lerp(inicio, destino, k) + Vector3.up * Mathf.Sin(k * Mathf.PI) * 1.6f;   // salto en arco
            cc.Move(p - transform.position);
            if (d.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(d);
            yield return null;
        }
        if (anim) anim.Atacar();
        Efectos.Anillo(transform.position, 2f, h.color);
        Efectos.Sacudir(0.2f, 0.25f);
        if (r && !r.Muerto) Golpear(r, h);
        Ocupado = false;
    }

    // ---------------- armas de fuego y armas arrojadizas
    IEnumerator Disparos(Equipo.Habilidad h)
    {
        for (int i = 0; i < Mathf.Max(1, h.golpes); i++)
        {
            if (anim) anim.Atacar();
            var r = Disparar(h.alcance, h.color, out _);
            if (r) Golpear(r, h);
            if (i < h.golpes - 1) yield return new WaitForSeconds(0.2f);
        }
    }

    // Ataque normal de la pistola (clic): lo llama PlayerController
    public void DisparoBasico(Equipo.Arma a)
    {
        MirarAlEnemigo(a.rango);
        var r = Disparar(a.rango, new Color(1f, 0.9f, 0.5f), out _);
        if (r) r.RecibirGolpe(Equipo.TirarDano(out bool critico), critico);
    }

    // Disparo en linea recta: apunta al enemigo mas cercano frente a Kai; las paredes lo detienen.
    // Devuelve el enemigo alcanzado (o null) y el punto donde pego la bala.
    Rival Disparar(float rango, Color color, out Vector3 fin)
    {
        Vector3 o = transform.position + Vector3.up * 1.3f + transform.forward * 0.5f;
        var r = MasCercanoAdelante(rango, 25f);
        fin = r ? r.transform.position + Vector3.up * 1.1f : o + transform.forward * rango;
        Vector3 dir = fin - o; float largo = dir.magnitude;
        if (largo > 0.1f && Physics.Raycast(o, dir / largo, out RaycastHit hit, largo, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
        {
            var golpeado = hit.collider.GetComponentInParent<Rival>();
            if (!golpeado && !hit.collider.GetComponentInParent<PlayerController>()) { fin = hit.point; r = null; }   // pego en una pared
        }
        Efectos.Linea(o, fin, color, 0.14f);
        Efectos.Anillo(o - Vector3.up * 1.2f, 0.6f, new Color(1f, 0.85f, 0.4f), 0.15f);   // fogonazo
        Efectos.Sacudir(0.04f, 0.08f);
        return r;
    }

    // ---------------- dano
    void Golpear(Rival r, Equipo.Habilidad h)
    {
        if (!r || r.Muerto) return;
        int dano = Mathf.Max(1, Mathf.RoundToInt(Equipo.TirarDano(out bool critico) * (h.mult > 0f ? h.mult : 1f)));
        if (h.mult > 0f) r.RecibirGolpe(dano, critico);
        if (h.aturdir > 0f && !r.Muerto) r.Aturdir(h.aturdir);
        if (h.empuje > 0f && !r.Muerto) r.Empujar(transform.position, h.empuje);
    }

    // ---------------- buscar enemigos
    static List<Rival> Vivos()
    {
        var l = new List<Rival>();
        foreach (var r in FindObjectsByType<Rival>(FindObjectsSortMode.None)) if (!r.Muerto && r.isActiveAndEnabled) l.Add(r);
        return l;
    }

    static List<Rival> EnRadio(Vector3 c, float radio)
    {
        var l = new List<Rival>();
        foreach (var r in Vivos())
        {
            Vector3 d = r.transform.position - c; d.y = 0f;
            if (d.magnitude <= radio) l.Add(r);
        }
        return l;
    }

    List<Rival> EnCono(float radio, float angulo)
    {
        var l = new List<Rival>();
        foreach (var r in EnRadio(transform.position, radio))
        {
            Vector3 d = r.transform.position - transform.position; d.y = 0f;
            if (d.magnitude < 0.8f || Vector3.Angle(transform.forward, d) <= angulo / 2f) l.Add(r);
        }
        return l;
    }

    Rival MasCercano(float alcance)
    {
        Rival mejor = null; float md = alcance;
        foreach (var r in Vivos())
        {
            float d = Vector3.Distance(r.transform.position, transform.position);
            if (d < md) { md = d; mejor = r; }
        }
        return mejor;
    }

    Rival MasCercanoAdelante(float alcance, float angulo)
    {
        Rival mejor = null; float md = alcance;
        foreach (var r in Vivos())
        {
            Vector3 d = r.transform.position - transform.position; d.y = 0f;
            if (d.magnitude < md && Vector3.Angle(transform.forward, d) <= angulo) { md = d.magnitude; mejor = r; }
        }
        return mejor;
    }

    // apunta automaticamente al enemigo mas cercano (como el auto-apuntado suave de Albion)
    void MirarAlEnemigo(float alcance)
    {
        var r = MasCercano(alcance);
        if (!r) return;
        Vector3 d = r.transform.position - transform.position; d.y = 0f;
        if (d.sqrMagnitude > 0.01f) transform.rotation = Quaternion.LookRotation(d);
    }
}
