using UnityEngine;

// Muestra en la mano de Kai SOLO el arma que tiene equipada (con los punos, la mano queda vacia).
// Ponlo en Kai (el objeto raiz, el mismo que tiene PlayerController).
//
//  Sable   -> arrastra aqui la espada que ya trae el modelo de Quaternius (el objeto hijo "Sword", "Cutlass"...).
//  Hacha   -> arrastra un modelo de hacha puesto como hijo del hueso de la mano (opcional).
//  Pistola -> igual (opcional).
// Si dejas vacia el Hacha o la Pistola (o el Sable), se arma un modelo sencillo con cubos en la mano.
// Para acomodar los modelos sencillos: dale Play, selecciona Kai y mueve "Ajuste Posicion / Rotacion"
// hasta que se vean bien; copia los valores (clic derecho en el componente > Copy) y pegalos al salir de Play.
public class ArmasVisuales : MonoBehaviour
{
    [Header("Modelos del arma en la mano (vacio = modelo sencillo)")]
    public GameObject sable;
    public GameObject hacha;
    public GameObject pistola;

    [Header("Hueso de la mano derecha (vacio = se busca solo)")]
    public Transform mano;

    [Header("Modelos sencillos")]
    public bool crearModelosSimples = true;
    public Vector3 ajustePosicion = new Vector3(0f, 0.05f, 0f);
    public Vector3 ajusteRotacion = new Vector3(90f, 0f, 0f);
    public float ajusteEscala = 1f;

    GameObject[] modelos;
    Transform[] generados;
    int mostrado = -2;

    void Start()
    {
        if (!mano && sable) mano = sable.transform.parent;   // la espada del modelo ya cuelga del hueso de la mano
        if (!mano) mano = BuscarMano(transform);
        if (!mano) Debug.LogWarning("[ArmasVisuales] No encontre el hueso de la mano. Arrastralo al campo 'Mano'.");

        modelos = new GameObject[] { null, sable, hacha, pistola };
        generados = new Transform[4];
        if (crearModelosSimples && mano)
            for (int i = 1; i < 4; i++)
                if (!modelos[i])
                {
                    var g = new GameObject("ArmaSimple_" + (Equipo.IdArma)i).transform;
                    g.SetParent(mano, false);
                    ArmaEnSuelo.ConstruirModelo((Equipo.IdArma)i, g);
                    generados[i] = g;
                    modelos[i] = g.gameObject;
                }
        Refrescar(true);
    }

    void LateUpdate() => Refrescar(false);

    void Refrescar(bool forzar)
    {
        if (modelos == null) return;
        int actual = Equipo.Tiene((Equipo.IdArma)Equipo.arma) ? Equipo.arma : 0;
        if (actual != mostrado || forzar)
        {
            mostrado = actual;
            for (int i = 1; i < modelos.Length; i++)
                if (modelos[i]) modelos[i].SetActive(i == actual);
        }
        // los ajustes se pueden mover en Play para acomodar el arma
        foreach (var g in generados)
            if (g) { g.localPosition = ajustePosicion; g.localRotation = Quaternion.Euler(ajusteRotacion); g.localScale = Vector3.one * ajusteEscala; }
    }

    // Busca un hueso con "hand" / "mano" / "wrist" del lado derecho
    static Transform BuscarMano(Transform raiz)
    {
        Transform cualquiera = null;
        foreach (var t in raiz.GetComponentsInChildren<Transform>(true))
        {
            string n = t.name.ToLower().Replace(" ", "").Replace("_", ".").Replace(":", ".");
            bool esMano = n.Contains("hand") || n.Contains("mano") || n.Contains("wrist");
            if (!esMano || n.Contains("thumb") || n.Contains("index") || n.Contains("finger")) continue;
            bool izquierda = n.Contains("left") || n.EndsWith(".l") || n.Contains("izq");
            bool derecha = !izquierda && (n.Contains("right") || n.EndsWith(".r") || n.EndsWith("r") || n.Contains("derech"));
            if (derecha) return t;
            if (!cualquiera) cualquiera = t;
        }
        return cualquiera;
    }
}
