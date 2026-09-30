using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

// Va en el MODELO 3D (el hijo que tiene el Animator). No necesita Animator Controller.
// Elige sola entre quieto / caminar / correr / saltar segun el movimiento,
// y reproduce atacar o golpeado cuando otro script lo pide.
// Deja vacio cualquier clip que el personaje no tenga.
[RequireComponent(typeof(Animator))]
public class AnimacionSimple : MonoBehaviour
{
    [Header("Movimiento")]
    public AnimationClip quieto;
    public AnimationClip caminar;
    public AnimationClip correr;
    public AnimationClip saltar;
    public AnimationClip agacharse;   // opcional: si no hay clip, el modelo se "encoge"

    [Header("Acciones")]
    public AnimationClip atacar;
    public AnimationClip golpeado;
    public AnimationClip morir;

    [Header("Ajustes")]
    public float velocidadParaCaminar = 0.5f;
    public float velocidadParaCorrer = 7.5f;
    public float suavizado = 8f;
    [Tooltip("En que punto del clip de agacharse se congela la pose (0 = inicio, 1 = final)")]
    [Range(0f, 1f)] public float poseAgachado = 0.5f;

    const int Q = 0, C = 1, R = 2, S = 3, A = 4, G = 5, M = 6, K = 7, N = 8;

    PlayableGraph grafo;
    AnimationMixerPlayable mezcla;
    readonly AnimationClipPlayable[] clips = new AnimationClipPlayable[N];
    readonly float[] pesos = new float[N];

    CharacterController cc;
    Vector3 ultimaPos;
    float tiempoAire;
    bool enAire;
    int accion = -1;
    float accionHasta;
    bool muerto;
    Vector3 escalaBase;
    public bool Agachado { get; set; }
    bool estabaAgachado;

    public float DuracionAtaque => atacar ? atacar.length : 0.6f;

    void Start()
    {
        var anim = GetComponent<Animator>();
        anim.applyRootMotion = false;
        cc = GetComponentInParent<CharacterController>();

        grafo = PlayableGraph.Create("AnimacionSimple");
        grafo.SetTimeUpdateMode(DirectorUpdateMode.GameTime);
        var salida = AnimationPlayableOutput.Create(grafo, "Salida", anim);
        mezcla = AnimationMixerPlayable.Create(grafo, N);
        salida.SetSourcePlayable(mezcla);

        Conectar(Q, quieto);
        Conectar(C, caminar);
        Conectar(R, correr);
        Conectar(S, saltar);
        Conectar(A, atacar);
        Conectar(G, golpeado);
        Conectar(M, morir);
        Conectar(K, agacharse);
        escalaBase = transform.localScale;

        pesos[Q] = 1f;
        Aplicar();
        grafo.Play();
        ultimaPos = transform.position;
    }

    void Conectar(int i, AnimationClip clip)
    {
        if (!clip) return;
        clips[i] = AnimationClipPlayable.Create(grafo, clip);
        grafo.Connect(clips[i], 0, mezcla, i);
    }

    public void Atacar() { Disparar(A, atacar); }
    public void Golpeado() { Disparar(G, golpeado); }

    // Reproduce la muerte y se queda en el ultimo cuadro. Devuelve cuanto dura.
    public float Morir()
    {
        muerto = true;
        accion = -1;
        if (clips[M].IsValid()) clips[M].SetTime(0);
        return morir ? morir.length : 0f;
    }

    // Vuelve de golpe a la pose de reposo
    public void Revivir()
    {
        muerto = false;
        accion = -1;
        for (int i = 0; i < N; i++) pesos[i] = i == Q ? 1f : 0f;
        Aplicar();
    }

    void Disparar(int i, AnimationClip clip)
    {
        if (muerto || !clip || !clips[i].IsValid()) return;
        accion = i;
        accionHasta = Time.time + clip.length;
        clips[i].SetTime(0);
    }

    void Update()
    {
        Vector3 d = transform.position - ultimaPos; d.y = 0f;
        ultimaPos = transform.position;
        float vel = d.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);

        bool suelo = cc ? cc.isGrounded : true;
        tiempoAire = suelo ? 0f : tiempoAire + Time.deltaTime;
        bool aire = tiempoAire > 0.1f;
        if (aire && !enAire && clips[S].IsValid()) clips[S].SetTime(0);
        enAire = aire;

        // Agacharse: reproduce el clip hasta la pose mas baja y se queda congelado ahi
        if (clips[K].IsValid())
        {
            if (Agachado && !estabaAgachado)
            {
                clips[K].SetTime(0);
                clips[K].SetSpeed(1);
            }
            if (Agachado)
            {
                double pose = agacharse.length * poseAgachado;
                if (clips[K].GetTime() >= pose)
                {
                    clips[K].SetTime(pose);
                    clips[K].SetSpeed(0);
                }
            }
            else if (estabaAgachado)
            {
                clips[K].SetSpeed(1);
            }
        }
        estabaAgachado = Agachado;

        int objetivo;
        if (muerto)
        {
            objetivo = morir ? M : Q;
        }
        else if (accion >= 0 && Time.time < accionHasta)
        {
            objetivo = accion;
        }
        else
        {
            accion = -1;
            if (Agachado && agacharse && !enAire) objetivo = K;
            else if (enAire && saltar) objetivo = S;
            else if (vel > velocidadParaCorrer && correr) objetivo = R;
            else if (vel > velocidadParaCaminar && caminar) objetivo = C;
            else objetivo = Q;
        }

        float paso = suavizado * Time.deltaTime;
        for (int i = 0; i < N; i++)
            pesos[i] = Mathf.MoveTowards(pesos[i], i == objetivo ? 1f : 0f, paso);

        Aplicar();

        // Sin clip de agacharse: el modelo se achata un poco para que se note
        float factorY = (Agachado && !agacharse && !muerto) ? 0.7f : 1f;
        Vector3 objetivoEscala = new Vector3(escalaBase.x, escalaBase.y * factorY, escalaBase.z);
        transform.localScale = Vector3.Lerp(transform.localScale, objetivoEscala, 12f * Time.deltaTime);
    }

    void Aplicar()
    {
        float total = 0f;
        for (int i = 0; i < N; i++) if (clips[i].IsValid()) total += pesos[i];
        for (int i = 0; i < N; i++)
        {
            if (!clips[i].IsValid()) continue;
            float w = total > 0f ? pesos[i] / total : (i == Q ? 1f : 0f);
            mezcla.SetInputWeight(i, w);
        }
    }

    void OnDestroy()
    {
        if (grafo.IsValid()) grafo.Destroy();
    }
}