using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// Mision del barco: Kai junta las piezas (PiezasBarco) y se las lleva a un personaje.
// Al hablar con el teniendo TODAS hay una conversacion y al final el barco mejora de una sola vez.
//  - Ponlo en el personaje (ej. el carpintero del Taller). Kai habla con el con la tecla Hablar (E).
//  - Sin todas las piezas: dice las frases de "Sin Piezas" ({n} = las que llevas, {total} = las que pide).
//  - Despues de la mejora: dice las frases de "Despues".
//  - Mejoras: multiplicadores sobre el BarcoController. 1 = igual, 1.3 = +30 %, 0.5 = la mitad.
//  - Queda guardado: al cargar partida el barco ya viene mejorado.
public class Misionpiezasbarco : MonoBehaviour
{
    [Header("Personaje")]
    public string nombre = "Carpintero";
    public float radio = 3f;
    public KeyCode teclaHablar = KeyCode.E;

    [Header("Mision")]
    [Tooltip("Ids de las piezas que pide (vacio = todas las del Catalogo de PiezasBarco)")]
    public string[] piezas;
    [Tooltip("Segundos que dura cada frase")]
    public float duracionFrase = 3.5f;
    [Tooltip("Tecla para pasar a la siguiente frase. None = las frases pasan solas (no uses E: tambien habla con Barbagris)")]
    public KeyCode teclaAvanzar = KeyCode.None;
    [TextArea]
    public string[] sinPiezas =
    {
        "Ese barco tuyo es un cascarón, grumete. Tráeme las piezas perdidas y lo dejo como nuevo.",
        "Llevas {n} de {total}. Búscalas en las islas... y en los bolsillos de los piratas."
    };
    [TextArea]
    public string[] entrega =
    {
        "¡Por todos los mares! Las trajiste todas.",
        "Dame un momento... martillo, clavos, un poco de brea...",
        "¡Listo! Tu barco ahora es más rápido y resistente que nunca."
    };
    [TextArea]
    public string[] despues =
    {
        "¿Qué tal navega? Mejor que nunca, ¿verdad?"
    };

    [Header("Mejoras al barco (multiplicadores, 1 = igual)")]
    public BarcoController barco;
    public float velocidad = 1.3f;
    public float velocidadGiro = 1.2f;
    public float aceleracion = 1.3f;
    public float factorReversa = 1f;
    public float bonusMara = 1f;
    public float radioSubir = 1f;
    public float radioBajar = 1f;
    [Tooltip("Menos de 1 = las corrientes de las islas bloqueadas empujan menos")]
    public float fuerzaCorriente = 1f;

    [Header("Extras")]
    [Tooltip("Objetos que aparecen con la mejora (ej. vela nueva o canon en el modelo del barco)")]
    public GameObject[] activar;
    [Tooltip("Objetos que se quitan con la mejora (ej. la vela vieja)")]
    public GameObject[] desactivar;
    [Tooltip("Cualquier otra cosa que quieras hacer al terminar la mision")]
    public UnityEvent alMejorar;

    public const string Clave = "MisionBarcoEntregada";
    public static bool Entregada => PlayerPrefs.GetInt(Clave, 0) == 1;

    bool hablando, avisado, aplicada;
    // true mientras este personaje habla (otros scripts, como el de Barbagris, pueden revisarlo para no hablar encima)
    public static bool EnConversacion { get; private set; }

    void OnEnable() { PiezasBarco.AlObtener += AlObtener; }
    void OnDisable() { PiezasBarco.AlObtener -= AlObtener; if (hablando) EnConversacion = false; }

    void Start()
    {
        if (!barco) barco = FindAnyObjectByType<BarcoController>(FindObjectsInactive.Include);
        if (!barco) Debug.LogWarning("[MisionPiezasBarco] No encuentro el barco: arrastralo al campo Barco.", this);
        if (piezas == null || piezas.Length == 0) piezas = System.Array.ConvertAll(PiezasBarco.Catalogo, p => p.id);
        if (Entregada) Aplicar();   // partida cargada: el barco ya estaba mejorado
    }

    int Conseguidas() { int n = 0; foreach (var id in piezas) if (PiezasBarco.Tiene(id)) n++; return n; }
    bool TieneTodas => piezas != null && piezas.Length > 0 && Conseguidas() == piezas.Length;

    // Al recoger la ultima pieza, avisa a quien hay que llevarselas
    void AlObtener(PiezasBarco.Pieza p)
    {
        if (Entregada || piezas == null || System.Array.IndexOf(piezas, p.id) < 0) return;
        if (TieneTodas) GameState.Avisar("¡Tienes todas las piezas! Llévaselas a " + nombre + ".", 4f);
        else GameState.Avisar("¡" + p.nombre + "! Piezas del barco: " + Conseguidas() + "/" + piezas.Length, 3f);
    }

    void Update()
    {
        var kai = GameState.Kai;
        if (!kai || hablando || HUD.Pausado) return;
        if (BarcoController.Actual && BarcoController.Actual.Manejando) return;   // desde el barco no se habla
        bool cerca = Vector3.Distance(kai.position, transform.position) < radio;
        if (cerca && !avisado) { avisado = true; GameState.Avisar("Parece que " + nombre + " tiene algo para decirte.", 2f); }
        if (!cerca) avisado = false;
        if (cerca && Input.GetKeyDown(teclaHablar)) StartCoroutine(Conversacion());
    }

    IEnumerator Conversacion()
    {
        hablando = true;
        EnConversacion = true;
        if (Entregada) yield return Decir(despues);
        else if (!TieneTodas) yield return Decir(sinPiezas);
        else
        {
            PlayerPrefs.SetInt(Clave, 1); PlayerPrefs.Save();   // se guarda ya, por si cierran el juego a mitad
            yield return Decir(entrega);
            Aplicar();
            var dorado = new Color(1f, 0.85f, 0.4f);
            Efectos.Anillo(transform.position, 4f, dorado, 0.8f);
            if (barco && barco.gameObject.activeInHierarchy) Efectos.Anillo(barco.transform.position, 6f, dorado, 0.8f);
            Sonidos.Tocar("recoger", transform.position, 1f);
            GameState.Avisar("¡Barco mejorado!", 3f);
            alMejorar?.Invoke();
        }
        hablando = false;
        EnConversacion = false;
    }

    IEnumerator Decir(string[] frases)
    {
        if (frases == null) yield break;
        foreach (var f in frases)
        {
            string texto = f.Replace("{n}", Conseguidas().ToString()).Replace("{total}", piezas.Length.ToString());
            // el cuadro dura un poco mas que la espera: asi nunca se cierra entre una frase y la siguiente
            GameState.Hablar(nombre, texto, duracionFrase + 1f);
            yield return null;   // que la misma pulsacion no salte la frase
            float fin = Time.time + duracionFrase;
            while (Time.time < fin && !(teclaAvanzar != KeyCode.None && Input.GetKeyDown(teclaAvanzar))) yield return null;
        }
    }

    void Aplicar()
    {
        if (aplicada) return;
        aplicada = true;
        if (barco)
        {
            barco.velocidad *= velocidad;
            barco.velocidadGiro *= velocidadGiro;
            barco.aceleracion *= aceleracion;
            barco.factorReversa = Mathf.Clamp01(barco.factorReversa * factorReversa);
            barco.bonusMara *= bonusMara;
            barco.radioSubir *= radioSubir;
            barco.radioBajar *= radioBajar;
            barco.fuerzaCorriente *= fuerzaCorriente;
        }
        if (activar != null) foreach (var g in activar) if (g) g.SetActive(true);
        if (desactivar != null) foreach (var g in desactivar) if (g) g.SetActive(false);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.85f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, radio);
    }
}