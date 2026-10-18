using System.Collections.Generic;
using UnityEngine;

// Sonidos del juego. No hay que ponerlo en ningun objeto: se crea solo.
// Los sonidos se cargan desde Assets/Resources/Sonidos/ por nombre (si hay "nombre_2", "nombre_3"... elige uno al azar):
//   ambiente        -> mar, viento y gaviotas de fondo (en bucle)
//   espada_aire     -> Kai blande el arma
//   espada_golpe    -> el arma le pega a un enemigo
//   punio_golpe     -> golpe a punos
//   bloqueo         -> Kai bloquea un golpe
//   kai_dolor       -> Kai recibe dano
//   enemigo_dolor   -> un enemigo recibe dano
//   enemigo_cae     -> un enemigo muere y cae
//   paso            -> pisadas de Kai (paso, paso_2, paso_3, paso_4)
//   aterrizaje      -> Kai cae de un salto alto
//   monedas         -> recoger o gastar doblones
//   recoger         -> recoger madera, chatarra, cocos o un arma
//   cofre           -> abrir un cofre
//   nivel           -> subir de nivel
//   musica_jefe     -> musica de pelea con jefe (en bucle)
//   victoria        -> fanfarria al derrotar un jefe
// Para cambiar un sonido por uno mejor, reemplaza el archivo con el mismo nombre (wav, mp3 u ogg).
// El volumen general es el de Opciones.
public class Sonidos : MonoBehaviour
{
    public static float VolumenAmbiente = 0.45f;
    public static float VolumenEfectos = 0.9f;
    public static float VolumenMusica = 0.55f;
    public static float TonoMusica = 1f;   // la fase 2 de un jefe la acelera un poco

    static Sonidos instancia;
    readonly Dictionary<string, AudioClip[]> clips = new Dictionary<string, AudioClip[]>();
    readonly Dictionary<string, float> ultimaVez = new Dictionary<string, float>();
    AudioSource[] fuentes;
    AudioSource ambiente, musica;
    string musicaPedida;          // la que debe sonar (null = ninguna)
    AudioClip clipMusica;
    int siguiente;

    static Sonidos I
    {
        get
        {
            if (!instancia)
            {
                var go = new GameObject("Sonidos");
                DontDestroyOnLoad(go);
                instancia = go.AddComponent<Sonidos>();
            }
            return instancia;
        }
    }

    void Awake()
    {
        fuentes = new AudioSource[10];
        for (int i = 0; i < fuentes.Length; i++)
        {
            var hijo = new GameObject("Fuente" + i);   // cada fuente en su propio objeto (para ponerla donde suena)
            hijo.transform.SetParent(transform, false);
            fuentes[i] = hijo.AddComponent<AudioSource>();
            fuentes[i].playOnAwake = false;
        }
    }

    // ------------------------------------------------------------ uso desde otros scripts
    // Sonidos.Tocar("espada_golpe", posicion)  (posicion opcional; sin posicion suena igual en los dos oidos)
    public static void Tocar(string nombre, Vector3? posicion = null, float volumen = 1f, float variacionTono = 0.08f, float minimoEntre = 0.05f)
    {
        var s = I;
        float ahora = Time.unscaledTime;
        if (s.ultimaVez.TryGetValue(nombre, out float t) && ahora - t < minimoEntre) return;   // que no se amontonen
        var clip = s.Clip(nombre);
        if (!clip) return;
        s.ultimaVez[nombre] = ahora;

        var f = s.fuentes[s.siguiente]; s.siguiente = (s.siguiente + 1) % s.fuentes.Length;
        f.Stop();
        f.clip = clip;
        f.volume = volumen * VolumenEfectos;
        f.pitch = 1f + Random.Range(-variacionTono, variacionTono);
        if (posicion.HasValue)
        {
            // sonido en el mundo: se oye mas fuerte cerca (pero siempre se oye un poco)
            f.transform.position = posicion.Value;
            f.spatialBlend = 0.6f; f.minDistance = 4f; f.maxDistance = 40f; f.rolloffMode = AudioRolloffMode.Linear;
        }
        else f.spatialBlend = 0f;
        f.Play();
    }

    // Sonido de fondo en bucle (mar y viento). Lo llama el HUD al empezar cada escena.
    public static void Ambiente(string nombre = "ambiente")
    {
        var s = I;
        var clip = s.Clip(nombre);
        if (!clip) return;
        if (!s.ambiente) { s.ambiente = s.gameObject.AddComponent<AudioSource>(); s.ambiente.loop = true; s.ambiente.spatialBlend = 0f; s.ambiente.playOnAwake = false; }
        if (s.ambiente.clip == clip && s.ambiente.isPlaying) return;
        s.ambiente.clip = clip; s.ambiente.volume = 0f; s.ambiente.Play();
    }

    // Musica en bucle con fundido: Sonidos.Musica("musica_jefe") la empieza, Sonidos.Musica(null) la apaga.
    // Mientras suena, el sonido ambiente baja un poco.
    public static void Musica(string nombre)
    {
        var s = I;
        if (s.musicaPedida == nombre) return;
        s.musicaPedida = nombre;
        s.clipMusica = nombre == null ? null : s.Clip(nombre);
        if (!s.musica) { s.musica = s.gameObject.AddComponent<AudioSource>(); s.musica.loop = true; s.musica.spatialBlend = 0f; s.musica.playOnAwake = false; s.musica.volume = 0f; }
    }

    public static bool HayMusica => instancia && instancia.musicaPedida != null;
    public static bool SuenaMusica => instancia && instancia.musica && instancia.musica.isPlaying;

    void Update()
    {
        float dt = Time.unscaledDeltaTime;
        // musica: si cambia, primero baja la que suena y luego entra la nueva
        if (musica)
        {
            AudioClip quiero = clipMusica;
            if (musica.clip != quiero && musica.isPlaying)
            {
                musica.volume = Mathf.MoveTowards(musica.volume, 0f, dt * 0.8f);
                if (musica.volume <= 0f) musica.Stop();
            }
            else if (quiero)
            {
                if (!musica.isPlaying) { musica.clip = quiero; musica.volume = 0f; musica.pitch = 1f; musica.Play(); }
                musica.volume = Mathf.MoveTowards(musica.volume, VolumenMusica, dt * 0.6f);
                musica.pitch = Mathf.MoveTowards(musica.pitch, TonoMusica, dt * 0.2f);
            }
        }
        // el ambiente sube suave al empezar (y baja a la mitad cuando hay musica)
        if (ambiente && ambiente.isPlaying)
        {
            float meta = SuenaMusica && musicaPedida != null ? VolumenAmbiente * 0.45f : VolumenAmbiente;
            ambiente.volume = Mathf.MoveTowards(ambiente.volume, meta, dt * 0.3f);
        }
    }

    AudioClip Clip(string nombre)
    {
        if (!clips.TryGetValue(nombre, out var lista))
        {
            var l = new List<AudioClip>();
            var c = Resources.Load<AudioClip>("Sonidos/" + nombre);
            if (c) l.Add(c);
            for (int i = 2; i <= 6; i++) { c = Resources.Load<AudioClip>("Sonidos/" + nombre + "_" + i); if (c) l.Add(c); }
            if (l.Count == 0) Debug.LogWarning("[Sonidos] Falta el sonido \"" + nombre + "\" en Assets/Resources/Sonidos/");
            lista = l.ToArray();
            clips[nombre] = lista;
        }
        return lista.Length == 0 ? null : lista[Random.Range(0, lista.Length)];
    }
}
