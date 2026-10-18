using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;

// Menu Grumete > Islas: arma el ESQUELETO jugable de una isla donde esta mirando la vista Scene.
// Crea todo lo que hace funcionar la isla (zona, objetivo con pasos, muelle, NPC, objetos, jefe, enemigos...)
// con bloques de colores provisionales. Despues tu cambias los bloques por los modelos de verdad.
// Si en la escena ya hay un jefe o enemigos, los COPIA (asi reusan sus modelos y animaciones).
public static class ConstruirIslas
{
    // ================================================================ Isla 3: Astillero Hundido
    [MenuItem("Grumete/Islas/Crear Isla 3 - Astillero Hundido")]
    static void Isla3()
    {
        if (YaExiste(3)) return;
        Vector3 c = CentroVista();
        var raiz = new GameObject("Isla3_AstilleroHundido");
        raiz.transform.position = c;
        Undo.RegisterCreatedObjectUndo(raiz, "Crear Isla 3");

        // ---------- zona de la isla
        var zona = raiz.AddComponent<ZonaIsla>();
        zona.idIsla = 3;
        zona.tamano = new Vector2(90f, 70f);
        zona.objetivo = "<b>Astillero Hundido</b>: los carroñeros del capataz Brea saquean el astillero.";

        // ---------- muelle (punto de llegada y de guardado)
        var muelleGO = Bloque(raiz, "Muelle_Astillero", PrimitiveType.Cube, new Vector3(0f, 0f, -32f), new Vector3(3f, 0.3f, 8f), new Color(0.45f, 0.32f, 0.2f));
        var muelle = muelleGO.AddComponent<Muelle>();
        muelle.idIsla = 3;
        var llegada = Vacio(muelleGO, "PuntoLlegada", new Vector3(0f, 1f, 3f));
        muelle.puntoLlegada = llegada.transform;

        // ---------- Pico y su encargo
        var pico = Bloque(raiz, "Pico", PrimitiveType.Capsule, new Vector3(-8f, 1f, -20f), new Vector3(0.8f, 1f, 0.8f), new Color(0.6f, 0.42f, 0.25f));
        var encargo = pico.AddComponent<Encargo>();
        encargo.nombre = "Pico"; encargo.grupo = "herramientas_pico"; encargo.cantidad = 3;
        var recPico = Vacio(pico, "Recompensa_Pico", Vector3.zero).AddComponent<RecompensaIsla>();
        recPico.cuando = RecompensaIsla.Cuando.AlLlamar;
        recPico.clave = "astillero_pico";
        recPico.fragmentos = 0; recPico.experiencia = 150; recPico.mejoraVela = 1;
        recPico.tripulante = Tripulacion.Miembro.Pico;
        recPico.abrirMapa = false;
        encargo.recompensa = recPico;

        // ---------- herramientas (una en el casco hundido: solo se llega con marea baja)
        Herramienta(raiz, "Martillo", "Martillo de Pico", new Vector3(12f, 0f, -10f), new Color(0.85f, 0.6f, 0.3f));
        Herramienta(raiz, "Serrucho", "Serrucho de Pico", new Vector3(24f, 0f, 14f), new Color(0.75f, 0.8f, 0.85f));

        // ---------- laguna con marea: de dia inunda el casco, de noche se seca
        var laguna = GameObject.CreatePrimitive(PrimitiveType.Plane);
        laguna.name = "LagunaMarea";
        Undo.RegisterCreatedObjectUndo(laguna, "Laguna");
        laguna.transform.SetParent(raiz.transform, false);
        Vector3 pl = Suelo(c + new Vector3(-22f, 0f, 12f));
        laguna.transform.position = pl + Vector3.up * 0.6f;   // de dia el agua tapa el suelo 60 cm
        laguna.transform.localScale = new Vector3(2.4f, 1f, 2.4f);   // 24 x 24 m
        var col = laguna.GetComponent<Collider>(); if (col) Object.DestroyImmediate(col);
        laguna.AddComponent<Colorear>().color = new Color(0.2f, 0.5f, 0.7f);
        var marea = laguna.AddComponent<Marea>();
        marea.bajada = 3f; marea.idIsla = 3;
        var mar = laguna.AddComponent<Mar>(); mar.soloEnSuArea = true;

        // casco hundido: paredes bajas alrededor del cepillo
        var casco = new GameObject("CascoHundido");
        Undo.RegisterCreatedObjectUndo(casco, "Casco");
        casco.transform.SetParent(raiz.transform, false);
        casco.transform.position = pl;
        casco.transform.rotation = Quaternion.Euler(0f, 20f, 0f);
        Color madera = new Color(0.35f, 0.25f, 0.17f);
        Bloque(casco, "Costado_Izq", PrimitiveType.Cube, new Vector3(-2f, 0.5f, 0f), new Vector3(0.4f, 1f, 10f), madera, true);
        Bloque(casco, "Costado_Der", PrimitiveType.Cube, new Vector3(2f, 0.5f, 0f), new Vector3(0.4f, 1f, 10f), madera, true);
        Bloque(casco, "Popa", PrimitiveType.Cube, new Vector3(0f, 0.5f, -5f), new Vector3(4.4f, 1f, 0.4f), madera, true);
        Bloque(casco, "Mastil_Roto", PrimitiveType.Cylinder, new Vector3(0f, 1.5f, 1f), new Vector3(0.35f, 1.5f, 0.35f), madera, true);
        Herramienta(casco, "Cepillo", "Cepillo de Pico", new Vector3(0f, 0f, 2.5f), new Color(0.9f, 0.75f, 0.45f), true);

        // ---------- jefe: el capataz Brea (copia del jefe que ya tengas, o un bloque provisional)
        Vector3 pJefe = new Vector3(18f, 0f, 22f);
        var recJefe = Vacio(raiz, "Recompensa_Jefe", pJefe).AddComponent<RecompensaIsla>();
        recJefe.cuando = RecompensaIsla.Cuando.AlLlamar;
        recJefe.clave = "astillero_jefe";
        recJefe.fragmentos = 1; recJefe.experiencia = 250; recJefe.doblones = 120;
        recJefe.mensaje = "Entre las cosas del capataz encuentras un trozo del mapa de tu padre. La tinta se mueve...";
        var jefe = CopiarJefe(raiz, "Jefe_CapatazBrea", pJefe);
        var rj = jefe.GetComponent<Rival>();
        rj.nombre = "Capataz Brea"; rj.vidaMaxima = 650; rj.danoGolpe = 34; rj.radioZona = 10f;
        rj.armaQueSuelta = Equipo.IdArma.Pistola;   // la pistola de chispa (GDD: arma del Astillero)
        rj.experiencia = 0; rj.doblonesMin = 40; rj.doblonesMax = 60;
        var j = jefe.GetComponent<Jefe>();
        j.fraseInicio = "¿Vienes por la madera del astillero? Aquí todo lo que flota es mío, grumete.";
        j.fraseFuria = "¡Muchachos, a él! ¡Que no salga de aquí!";
        j.fraseDerrota = "Llévate el astillero... y que se te hunda con todo.";
        j.usarBombardeo = true; j.bombas = 4;
        j.alMorir = new UnityEvent();
        UnityEventTools.AddPersistentListener(j.alMorir, recJefe.Dar);
        // secuaces que entran en la fase 2
        j.secuaces = new[] {
            CopiarEnemigo(raiz, "Secuaz_1", pJefe + new Vector3(-5f, 0f, 3f), "Carroñero", 70),
            CopiarEnemigo(raiz, "Secuaz_2", pJefe + new Vector3(5f, 0f, 3f), "Carroñero", 70) };
        foreach (var s in j.secuaces) s.SetActive(false);

        // ---------- campamento de carroneros
        CopiarEnemigo(raiz, "Carronero_1", new Vector3(14f, 0f, 10f), "Carroñero", 90);
        CopiarEnemigo(raiz, "Carronero_2", new Vector3(26f, 0f, 6f), "Carroñero", 90);
        CopiarEnemigo(raiz, "Carronero_3", new Vector3(8f, 0f, 2f), "Carroñero", 90);

        // ---------- chatarra para el taller (copia un Recurso si ya tienes)
        var rec = Object.FindFirstObjectByType<Recurso>();
        if (rec)
            for (int i = 0; i < 4; i++)
            {
                var r = (GameObject)Object.Instantiate(rec.gameObject, raiz.transform);
                Undo.RegisterCreatedObjectUndo(r, "Recurso");
                r.name = "Chatarra_" + (i + 1);
                r.transform.position = Suelo(c + new Vector3(-30f + i * 9f, 0f, -6f + (i % 2) * 8f));
                r.GetComponent<Recurso>().tipo = Recurso.Tipo.Chatarra;
            }

        // ---------- objetivo con casillas
        zona.pasos = new[] {
            new ZonaIsla.Paso { texto = "Habla con <b>Pico</b>, el carpintero", encargo = encargo },
            new ZonaIsla.Paso { texto = "Recupera sus herramientas (una está en el casco hundido: ve de noche, con marea baja)", grupoObjetos = "herramientas_pico", cantidad = 3, encargo = encargo, ocultoHastaAnterior = true },
            new ZonaIsla.Paso { texto = "Derrota al <b>capataz Brea</b>", recompensa = recJefe },
            new ZonaIsla.Paso { texto = "Lleva las herramientas a Pico", recompensa = recPico, ocultoHastaAnterior = false },
        };
        zona.recompensaPrincipal = recPico;

        Terminar(raiz, "Isla 3 creada. Mueve el grupo \"Isla3_AstilleroHundido\" a su lugar y cambia los bloques por modelos." +
                       (Object.FindFirstObjectByType<CicloDia>() ? "" : "\n\nOJO: no hay CicloDia en la escena. Sin él la marea no baja nunca."));
        Selection.activeObject = raiz;
    }

    // ================================================================ Isla 4: Bahia de la Corona (sigilo)
    [MenuItem("Grumete/Islas/Crear Isla 4 - Bahía de la Corona")]
    static void Isla4()
    {
        if (YaExiste(4)) return;
        Vector3 c = CentroVista();
        var raiz = new GameObject("Isla4_BahiaCorona");
        raiz.transform.position = c;
        Undo.RegisterCreatedObjectUndo(raiz, "Crear Isla 4");

        var zona = raiz.AddComponent<ZonaIsla>();
        zona.idIsla = 4; zona.tamano = new Vector2(80f, 80f);
        zona.objetivo = "<b>Bahía de la Corona</b>: puerto de la Armada. Aquí no se pelea: si te ven, vuelves a la cala.";
        var corona = Vacio(raiz, "ZonaCorona", new Vector3(0f, 0f, 4f)).AddComponent<ZonaCorona>();
        corona.tamano = new Vector3(76f, 40f, 66f);   // la cala de llegada queda fuera

        // ---------- cala secreta (llegada, guardado y punto de reaparicion)
        var muelleGO = Bloque(raiz, "Muelle_CalaSecreta", PrimitiveType.Cube, new Vector3(-30f, 0f, -36f), new Vector3(2.5f, 0.3f, 6f), new Color(0.4f, 0.3f, 0.2f));
        var muelle = muelleGO.AddComponent<Muelle>(); muelle.idIsla = 4;
        var llegada = Vacio(muelleGO, "PuntoLlegada", new Vector3(0f, 1f, 3f));
        muelle.puntoLlegada = llegada.transform;
        Transform reaparicion = llegada.transform;

        // ---------- Sombra y su encargo
        var sombra = Bloque(raiz, "Sombra", PrimitiveType.Capsule, new Vector3(-28f, 1f, -22f), new Vector3(0.7f, 0.95f, 0.7f), new Color(0.25f, 0.18f, 0.35f));
        var enc = sombra.AddComponent<Encargo>();
        enc.nombre = "Sombra"; enc.grupo = "ganzua_sombra"; enc.cantidad = 1;
        enc.fraseInicio = "Shh. Los Cazadores me quitaron mi ganzúa y la guardaron en la Comandancia. Tráemela sin que te vean y te enseño a moverte como una sombra. Entra por la puerta de atrás.";
        enc.fraseFalta = "Sigue en la Comandancia. Pasa por los callejones y quédate lejos de los faroles.";
        enc.fraseFin = "Ni un rasguño y nadie te vio. Me gustas, grumete. Voy contigo: invócame y los guardias ni te notarán.";
        enc.fraseDespues = "Invócame con G cuando haya guardias cerca.";
        var recSombra = Vacio(sombra, "Recompensa_Sombra", Vector3.zero).AddComponent<RecompensaIsla>();
        recSombra.cuando = RecompensaIsla.Cuando.AlLlamar; recSombra.clave = "bahia_sombra";
        recSombra.fragmentos = 0; recSombra.experiencia = 200; recSombra.tripulante = Tripulacion.Miembro.Sombra; recSombra.abrirMapa = false;
        enc.recompensa = recSombra;

        // ---------- edificios: comandancia con puerta trasera, casas de los callejones, torre de vigia
        Color piedra = new Color(0.62f, 0.6f, 0.55f), madera = new Color(0.45f, 0.32f, 0.2f);
        var comandancia = Edificio(raiz, "Comandancia", new Vector3(-24f, 0f, 30f), new Vector3(12f, 5f, 9f), piedra, true);
        Objeto(comandancia, "Ganzua", "Ganzúa de Sombra", "ganzua_sombra", new Vector3(-3f, 0f, 1f), new Color(0.75f, 0.6f, 1f), true);
        var despacho = Vacio(comandancia, "Despacho_Gobernador", new Vector3(3f, 1f, 2f));
        Bloque(despacho, "Escritorio", PrimitiveType.Cube, new Vector3(0f, -0.6f, 0f), new Vector3(2f, 0.8f, 1f), madera, true);
        var recDespacho = despacho.AddComponent<RecompensaIsla>();
        recDespacho.cuando = RecompensaIsla.Cuando.AlHablar; recDespacho.clave = "bahia_despacho"; recDespacho.radio = 2.2f;
        recDespacho.fragmentos = 1; recDespacho.experiencia = 200; recDespacho.doblones = 150;
        recDespacho.mensaje = "Sobre el escritorio del gobernador: un trozo de mapa con el sello de la Almirante Voss. La tinta de tu mapa lo reconoce...";
        for (int i = 0; i < 4; i++)
            Edificio(raiz, "Casa_" + (i + 1), new Vector3(-34f + (i % 2) * 18f, 0f, -12f + (i / 2) * 10f), new Vector3(7f, 4f, 6f), new Color(0.75f, 0.68f, 0.55f), false);
        var torre = Bloque(raiz, "TorreVigia", PrimitiveType.Cube, new Vector3(20f, 0f, 30f), new Vector3(4f, 14f, 4f), piedra);
        var farol = Vacio(torre, "FarolGiratorio", Vector3.zero);
        farol.transform.position = torre.transform.position + Vector3.up * 7.5f;
        var fg = farol.AddComponent<FarolGiratorio>(); fg.reaparicion = reaparicion; fg.alcance = 34f;
        Bloque(raiz, "Fuente_Plaza", PrimitiveType.Cylinder, new Vector3(10f, 0.5f, 8f), new Vector3(3f, 0.5f, 3f), piedra);
        Bloque(raiz, "Muelle_Armada", PrimitiveType.Cube, new Vector3(14f, 0f, -34f), new Vector3(4f, 0.3f, 12f), madera);

        // ---------- escondites (barriles: agachado junto a ellos = oculto)
        Vector3[] barriles = { new Vector3(-24f, 0f, -8f), new Vector3(-20f, 0f, 2f), new Vector3(-30f, 0f, 10f), new Vector3(-18f, 0f, 16f),
                               new Vector3(-28f, 0f, 20f), new Vector3(4f, 0f, 2f), new Vector3(16f, 0f, 14f), new Vector3(-14f, 0f, 26f) };
        for (int i = 0; i < barriles.Length; i++) BarrilEn(raiz, "Barril_" + (i + 1), barriles[i]);

        // ---------- Cazadores (guardias con farol)
        Cazador(raiz, "Cazador_Callejon", new Vector3(-25f, 0f, -2f), reaparicion, new Vector3(0f, 0f, 8f));
        Cazador(raiz, "Cazador_Patio_1", new Vector3(-30f, 0f, 14f), reaparicion, new Vector3(10f, 0f, 0f));
        Cazador(raiz, "Cazador_Patio_2", new Vector3(-20f, 0f, 18f), reaparicion, new Vector3(-10f, 0f, 0f));
        Cazador(raiz, "Cazador_Patio_3", new Vector3(-26f, 0f, 22f), reaparicion, new Vector3(0f, 0f, -8f));
        Cazador(raiz, "Cazador_Comandancia", new Vector3(-24f, 0f, 24f), reaparicion, new Vector3(8f, 0f, 0f));
        Cazador(raiz, "Cazador_Plaza_1", new Vector3(4f, 0f, 4f), reaparicion, new Vector3(12f, 0f, 0f));
        Cazador(raiz, "Cazador_Plaza_2", new Vector3(14f, 0f, 16f), reaparicion, new Vector3(0f, 0f, -12f));

        zona.pasos = new[] {
            new ZonaIsla.Paso { texto = "Encuentra a <b>Sombra</b> en su escondite", encargo = enc },
            new ZonaIsla.Paso { texto = "Recupera su ganzúa en la Comandancia (entra por atrás)", grupoObjetos = "ganzua_sombra", cantidad = 1, encargo = enc, ocultoHastaAnterior = true },
            new ZonaIsla.Paso { texto = "Roba el mapa del despacho del gobernador", recompensa = recDespacho },
            new ZonaIsla.Paso { texto = "Vuelve con Sombra sin que te atrapen", recompensa = recSombra },
        };
        zona.recompensaPrincipal = recSombra;
        Terminar(raiz, "Isla 4 creada. Los Cazadores ven menos de noche: pon la llegada a esta isla de noche (F4 adelanta 2 horas)." +
                       (Object.FindFirstObjectByType<CicloDia>() ? "" : "\n\nOJO: no hay CicloDia: el farol de la torre queda siempre prendido."));
    }

    // ================================================================ Isla 5: Fuerte de Roca (combate y agilidad)
    [MenuItem("Grumete/Islas/Crear Isla 5 - Fuerte de Roca")]
    static void Isla5()
    {
        if (YaExiste(5)) return;
        Vector3 c = CentroVista();
        var raiz = new GameObject("Isla5_FuerteRoca");
        raiz.transform.position = c;
        Undo.RegisterCreatedObjectUndo(raiz, "Crear Isla 5");
        Color piedra = new Color(0.55f, 0.52f, 0.48f), hierro = new Color(0.3f, 0.3f, 0.33f);

        var zona = raiz.AddComponent<ZonaIsla>();
        zona.idIsla = 5; zona.tamano = new Vector2(90f, 70f);
        zona.objetivo = "<b>Fuerte de Roca</b>: los rufianes de El Mazo mandan aquí. Solo los fuertes pasan.";

        // ---------- rompeolas
        var muelleGO = Bloque(raiz, "Rompeolas", PrimitiveType.Cube, new Vector3(10f, 0f, -33f), new Vector3(4f, 0.6f, 10f), piedra);
        var muelle = muelleGO.AddComponent<Muelle>(); muelle.idIsla = 5;
        muelle.puntoLlegada = Vacio(muelleGO, "PuntoLlegada", new Vector3(0f, 1f, 0.4f)).transform;

        // ---------- mercado de roca: rufianes y un taller (copia del que tengas)
        CopiarEnemigo(raiz, "Rufian_1", new Vector3(4f, 0f, -14f), "Rufián", 110);
        CopiarEnemigo(raiz, "Rufian_2", new Vector3(16f, 0f, -10f), "Rufián", 110);
        CopiarEnemigo(raiz, "Rufian_3", new Vector3(10f, 0f, -4f), "Rufián", 110);
        var taller = Object.FindFirstObjectByType<Taller>();
        if (taller)
        {
            var t = (GameObject)Object.Instantiate(taller.gameObject, raiz.transform);
            Undo.RegisterCreatedObjectUndo(t, "Taller");
            t.name = "Taller_Fuerte"; t.transform.position = Suelo(c + new Vector3(18f, 0f, -18f));
            t.GetComponent<Taller>().nombreTaller = "Herrería del Fuerte";
        }

        // ---------- cantera con derrumbes
        var cantera = Vacio(raiz, "Cantera_Derrumbes", new Vector3(-26f, 0f, -8f));
        cantera.transform.position = Suelo(c + new Vector3(-26f, 0f, -8f));
        cantera.AddComponent<Derrumbe>();
        var rec = Object.FindFirstObjectByType<Recurso>();
        if (rec)
            for (int i = 0; i < 5; i++)
            {
                var r = (GameObject)Object.Instantiate(rec.gameObject, raiz.transform);
                Undo.RegisterCreatedObjectUndo(r, "Recurso");
                r.name = "Chatarra_Cantera_" + (i + 1);
                r.transform.position = Suelo(c + new Vector3(-32f + i * 3.5f, 0f, -14f + (i % 2) * 9f));
                r.GetComponent<Recurso>().tipo = Recurso.Tipo.Chatarra;
            }

        // ---------- muralla rota: camino de vigas inestables (agilidad) hasta el patio
        var muralla = Vacio(raiz, "MurallaRota", new Vector3(-20f, 0f, 10f));
        muralla.transform.position = Suelo(c + new Vector3(-20f, 0f, 10f));
        for (int i = 0; i < 7; i++)
        {
            var v = Bloque(muralla, "Viga_" + (i + 1), PrimitiveType.Cube, new Vector3(i * 2.6f, 2f + i * 0.5f, i * 1.6f), new Vector3(1.4f, 0.3f, 1.4f), new Color(0.5f, 0.36f, 0.22f), true);
            v.AddComponent<VigaInestable>();
        }

        // ---------- el foso: Bruno encadenado y 3 oleadas
        Vector3 pFoso = new Vector3(24f, 0f, 8f);
        var foso = Vacio(raiz, "ElFoso", pFoso);
        foso.transform.position = Suelo(c + pFoso);
        var ol = foso.AddComponent<Oleadas>();
        ol.radio = 9f; ol.porOleada = new[] { 2, 3, 4 }; ol.nombreArena = "El foso";
        var molde1 = CopiarEnemigo(foso, "Molde_Rufian", new Vector3(0f, 0f, -3f), "Rufián del foso", 80);
        var molde2 = CopiarEnemigo(foso, "Molde_Bruto", new Vector3(3f, 0f, -3f), "Bruto del foso", 130);
        molde2.GetComponent<Rival>().danoGolpe = 28;
        ol.modelos = new[] { molde1.GetComponent<Rival>(), molde2.GetComponent<Rival>() };
        molde1.SetActive(false); molde2.SetActive(false);
        var rejas = new GameObject[4];
        for (int i = 0; i < 4; i++)
        {
            float a = i * Mathf.PI / 2f;
            rejas[i] = Bloque(foso, "Reja_" + (i + 1), PrimitiveType.Cube, new Vector3(Mathf.Cos(a) * 9.5f, 1.5f, Mathf.Sin(a) * 9.5f), new Vector3(i % 2 == 0 ? 0.4f : 6f, 3f, i % 2 == 0 ? 6f : 0.4f), hierro, true);
            rejas[i].SetActive(false);
        }
        ol.rejas = rejas;
        var recFoso = Vacio(foso, "Recompensa_Foso", Vector3.zero).AddComponent<RecompensaIsla>();
        recFoso.cuando = RecompensaIsla.Cuando.AlLlamar; recFoso.clave = "fuerte_foso";
        recFoso.fragmentos = 0; recFoso.experiencia = 200; recFoso.doblones = 80; recFoso.abrirMapa = false;
        ol.recompensa = recFoso;

        var bruno = Bloque(raiz, "Bruno", PrimitiveType.Capsule, pFoso + new Vector3(0f, 1.2f, 12f), new Vector3(1.2f, 1.2f, 1.2f), new Color(0.55f, 0.35f, 0.25f));
        var enc = bruno.AddComponent<Encargo>();
        enc.nombre = "Bruno"; enc.grupo = "foso_bruno"; enc.cantidad = 0; enc.requisito = recFoso;
        enc.fraseInicio = "¿Vienes a mirar al grandote encadenado? El Mazo me tiene aquí para divertir a sus rufianes. Gana el foso y rompo estas cadenas con mis propias manos.";
        enc.fraseFalta = "Primero gana el foso, grumete. Entra al círculo cuando estés listo.";
        enc.fraseFin = "¡JA! Peleas como un pirata de verdad. Cuenta conmigo: ningún portón ni ningún golpe te va a parar.";
        enc.fraseDespues = "Invócame con G y recibes la mitad del daño. También rompo portones.";
        var recBruno = Vacio(bruno, "Recompensa_Bruno", Vector3.zero).AddComponent<RecompensaIsla>();
        recBruno.cuando = RecompensaIsla.Cuando.AlLlamar; recBruno.clave = "fuerte_bruno";
        recBruno.fragmentos = 0; recBruno.experiencia = 150; recBruno.tripulante = Tripulacion.Miembro.Bruno; recBruno.abrirMapa = false;
        enc.recompensa = recBruno;

        // ---------- porton de hierro: se abre al ganar el foso
        var porton = Bloque(raiz, "PortonHierro", PrimitiveType.Cube, new Vector3(0f, 0f, 18f), new Vector3(8f, 5f, 0.6f), hierro);
        recFoso.desactivar = new[] { porton };
        recFoso.mensaje = "El portón de hierro del fuerte se abre con un chirrido.";

        // ---------- patio del fuerte: El Mazo
        Vector3 pJefe = new Vector3(0f, 0f, 28f);
        var recJefe = Vacio(raiz, "Recompensa_Jefe", pJefe).AddComponent<RecompensaIsla>();
        recJefe.cuando = RecompensaIsla.Cuando.AlLlamar; recJefe.clave = "fuerte_jefe";
        recJefe.fragmentos = 1; recJefe.experiencia = 300; recJefe.doblones = 160;
        recJefe.mensaje = "En el cinturón de El Mazo hay un trozo de mapa enrollado...";
        var jefe = CopiarJefe(raiz, "Jefe_ElMazo", pJefe);
        var rj = jefe.GetComponent<Rival>();
        rj.nombre = "El Mazo"; rj.vidaMaxima = 850; rj.danoGolpe = 40; rj.velocidad = Mathf.Max(1.6f, rj.velocidad * 0.9f); rj.radioZona = 11f; rj.experiencia = 0;
        var j = jefe.GetComponent<Jefe>();
        j.fraseInicio = "¿Ganaste mi foso y crees que eres alguien? ¡Ven, que te aplasto!";
        j.fraseFuria = "¡Ahora sí vas a conocer mi mazo!";
        j.fraseDerrota = "El fuerte... es tuyo...";
        j.usarGolpeArea = true; j.radioArea = 5.5f; j.danoArea = 40; j.usarBombardeo = false;
        j.alMorir = new UnityEvent();
        UnityEventTools.AddPersistentListener(j.alMorir, recJefe.Dar);
        j.secuaces = new[] { CopiarEnemigo(raiz, "Secuaz_1", pJefe + new Vector3(-6f, 0f, 2f), "Rufián", 90),
                             CopiarEnemigo(raiz, "Secuaz_2", pJefe + new Vector3(6f, 0f, 2f), "Rufián", 90) };
        foreach (var s in j.secuaces) s.SetActive(false);

        zona.pasos = new[] {
            new ZonaIsla.Paso { texto = "Habla con <b>Bruno</b> en el foso", encargo = enc },
            new ZonaIsla.Paso { texto = "Gana las 3 oleadas del foso", recompensa = recFoso },
            new ZonaIsla.Paso { texto = "Libera a Bruno", recompensa = recBruno },
            new ZonaIsla.Paso { texto = "Derrota a <b>El Mazo</b> en el patio del fuerte", recompensa = recJefe },
        };
        zona.recompensaPrincipal = recJefe;
        Terminar(raiz, "Isla 5 creada. Los dos \"Molde\" del foso deben quedar desactivados: las oleadas los copian.");
    }

    // ================================================================ Isla 6: Fortaleza de Voss (final)
    [MenuItem("Grumete/Islas/Crear Isla 6 - Fortaleza de Voss")]
    static void Isla6()
    {
        if (YaExiste(6)) return;
        Vector3 c = CentroVista();
        var raiz = new GameObject("Isla6_FortalezaVoss");
        raiz.transform.position = c;
        Undo.RegisterCreatedObjectUndo(raiz, "Crear Isla 6");
        Color piedra = new Color(0.4f, 0.4f, 0.45f), hierro = new Color(0.28f, 0.28f, 0.32f);

        var zona = raiz.AddComponent<ZonaIsla>();
        zona.idIsla = 6; zona.tamano = new Vector2(70f, 90f);
        zona.objetivo = "<b>Fortaleza de Voss</b>: el Corazón de las Mareas está en lo alto. Usa todo lo que aprendiste.";

        // ---------- cala de los naufragos
        var muelleGO = Bloque(raiz, "Muelle_Naufragos", PrimitiveType.Cube, new Vector3(0f, 0f, -42f), new Vector3(3f, 0.4f, 7f), new Color(0.35f, 0.26f, 0.18f));
        var muelle = muelleGO.AddComponent<Muelle>(); muelle.idIsla = 6;
        muelle.puntoLlegada = Vacio(muelleGO, "PuntoLlegada", new Vector3(0f, 1f, 3f)).transform;

        // ---------- rocas del acantilado (subir saltando)
        var rocas = Vacio(raiz, "RocasAcantilado", new Vector3(0f, 0f, -34f));
        rocas.transform.position = Suelo(c + new Vector3(0f, 0f, -34f));
        for (int i = 0; i < 6; i++)
            Bloque(rocas, "Roca_" + (i + 1), PrimitiveType.Cube, new Vector3((i % 2 == 0 ? -2f : 2f), 0.6f + i * 1.1f, i * 2.2f), new Vector3(2.4f, 1.2f + i * 0.2f, 2.2f), new Color(0.45f, 0.42f, 0.4f), true);
        var reaparicion = Vacio(rocas, "Reaparicion_Sigilo", new Vector3(0f, 8f, 13f)).transform;

        // ---------- muralla exterior (sigilo): no se pelea, Cazadores, puerta con cerradura de Sombra
        var corona = Vacio(raiz, "ZonaCorona_Muralla", new Vector3(0f, 0f, -12f)).AddComponent<ZonaCorona>();
        corona.tamano = new Vector3(66f, 40f, 16f);
        corona.alEntrar = "La muralla de Voss: aquí todavía no te han visto. Que siga así.";
        Bloque(raiz, "Muralla_Izq", PrimitiveType.Cube, new Vector3(-20f, 0f, -4f), new Vector3(26f, 6f, 1.5f), piedra);
        Bloque(raiz, "Muralla_Der", PrimitiveType.Cube, new Vector3(20f, 0f, -4f), new Vector3(26f, 6f, 1.5f), piedra);
        var puertaMuralla = Bloque(raiz, "Puerta_Muralla", PrimitiveType.Cube, new Vector3(0f, 0f, -4f), new Vector3(14f, 6f, 1.2f), hierro);
        var cerradura = puertaMuralla.AddComponent<Bloqueo>();
        cerradura.quien = Tripulacion.Miembro.Sombra; cerradura.nombre = "Puerta con candado"; cerradura.clave = "voss_cerradura"; cerradura.radio = 4f;
        for (int i = 0; i < 4; i++) BarrilEn(raiz, "Barril_" + (i + 1), new Vector3(-18f + i * 12f, 0f, -16f + (i % 2) * 5f));
        Cazador(raiz, "Cazador_Muralla_1", new Vector3(-14f, 0f, -12f), reaparicion, new Vector3(12f, 0f, 0f));
        Cazador(raiz, "Cazador_Muralla_2", new Vector3(14f, 0f, -12f), reaparicion, new Vector3(-12f, 0f, 0f));
        Cazador(raiz, "Cazador_Muralla_3", new Vector3(0f, 0f, -18f), reaparicion, new Vector3(0f, 0f, 8f));

        // ---------- patio de armas: suena la alarma y llegan oleadas de soldados
        Vector3 pPatio = new Vector3(0f, 0f, 6f);
        var patio = Vacio(raiz, "PatioDeArmas", pPatio);
        patio.transform.position = Suelo(c + pPatio);
        var ol = patio.AddComponent<Oleadas>();
        ol.radio = 11f; ol.porOleada = new[] { 3, 4 }; ol.nombreArena = "La alarma del patio";
        var m1 = CopiarEnemigo(patio, "Molde_Soldado", new Vector3(0f, 0f, -3f), "Soldado de la Corona", 110);
        var m2 = CopiarEnemigo(patio, "Molde_Cazador", new Vector3(3f, 0f, -3f), "Cazador de élite", 140);
        m2.GetComponent<Rival>().danoGolpe = 32;
        ol.modelos = new[] { m1.GetComponent<Rival>(), m2.GetComponent<Rival>() };
        m1.SetActive(false); m2.SetActive(false);
        var recPatio = Vacio(patio, "Recompensa_Patio", Vector3.zero).AddComponent<RecompensaIsla>();
        recPatio.cuando = RecompensaIsla.Cuando.AlLlamar; recPatio.clave = "voss_patio";
        recPatio.fragmentos = 0; recPatio.experiencia = 250; recPatio.abrirMapa = false;
        ol.recompensa = recPatio;

        // ---------- porton interior: solo Bruno lo rompe
        var porton = Bloque(raiz, "Porton_Interior", PrimitiveType.Cube, new Vector3(0f, 0f, 18f), new Vector3(7f, 6f, 1.2f), hierro);
        var bq = porton.AddComponent<Bloqueo>();
        bq.quien = Tripulacion.Miembro.Bruno; bq.nombre = "Portón atrancado"; bq.clave = "voss_porton"; bq.radio = 4f;
        Bloque(raiz, "Muro_Interior_Izq", PrimitiveType.Cube, new Vector3(-14f, 0f, 18f), new Vector3(21f, 6f, 1.2f), piedra);
        Bloque(raiz, "Muro_Interior_Der", PrimitiveType.Cube, new Vector3(14f, 0f, 18f), new Vector3(21f, 6f, 1.2f), piedra);

        // ---------- cima de la torre: la Almirante Voss
        Vector3 pJefe = new Vector3(0f, 0f, 30f);
        var recVoss = Vacio(raiz, "Recompensa_Voss", pJefe).AddComponent<RecompensaIsla>();
        recVoss.cuando = RecompensaIsla.Cuando.AlLlamar; recVoss.clave = "voss_jefe";
        recVoss.fragmentos = 1; recVoss.experiencia = 500; recVoss.doblones = 300; recVoss.abrirMapa = false;
        recVoss.hablante = "Almirante Voss"; recVoss.mensaje = "Llévatelo... pero el mar no perdona a los que lo roban.";
        var jefe = CopiarJefe(raiz, "Jefe_AlmiranteVoss", pJefe);
        var rj = jefe.GetComponent<Rival>();
        rj.nombre = "Almirante Renata Voss"; rj.vidaMaxima = 1300; rj.danoGolpe = 42; rj.radioZona = 12f; rj.experiencia = 0;
        rj.velocidad = Mathf.Max(2.6f, rj.velocidad * 1.15f);
        var j = jefe.GetComponent<Jefe>();
        j.fraseInicio = "Así que tú eres el grumete del mapa. La piratería termina hoy, aquí, conmigo.";
        j.fraseFuria = "¡Artilleros! ¡Fuego sobre el patio!";
        j.fraseDerrota = "Imposible... un simple grumete...";
        j.usarGolpeArea = true; j.usarEmbestida = true; j.usarBombardeo = true; j.bombas = 6; j.esperaEspecialFase2 = 3f;
        j.alMorir = new UnityEvent();
        UnityEventTools.AddPersistentListener(j.alMorir, recVoss.Dar);
        j.secuaces = new[] { CopiarEnemigo(raiz, "Escolta_1", pJefe + new Vector3(-6f, 0f, 0f), "Escolta de Voss", 120),
                             CopiarEnemigo(raiz, "Escolta_2", pJefe + new Vector3(6f, 0f, 0f), "Escolta de Voss", 120) };
        foreach (var s in j.secuaces) s.SetActive(false);

        // ---------- camara del Corazon (se abre al vencer a Voss)
        var puertaCamara = Bloque(raiz, "Puerta_Camara", PrimitiveType.Cube, new Vector3(0f, 0f, 37f), new Vector3(4f, 4f, 0.8f), new Color(0.6f, 0.5f, 0.2f));
        recVoss.desactivar = new[] { puertaCamara };
        var corazon = Bloque(raiz, "CorazonDeLasMareas", PrimitiveType.Sphere, new Vector3(0f, 1.2f, 41f), new Vector3(0.9f, 0.9f, 0.9f), new Color(0.2f, 0.85f, 0.9f));
        var luz = Vacio(corazon, "Brillo", Vector3.zero).AddComponent<Light>();
        luz.type = LightType.Point; luz.color = new Color(0.3f, 0.9f, 1f); luz.range = 8f; luz.intensity = 3f;
        var recCorazon = corazon.AddComponent<RecompensaIsla>();
        recCorazon.cuando = RecompensaIsla.Cuando.AlEntrar; recCorazon.clave = "voss_corazon"; recCorazon.radio = 2f;
        recCorazon.fragmentos = 0; recCorazon.experiencia = 0; recCorazon.esFinal = true;
        recCorazon.mensaje = "El Corazón de las Mareas late en tus manos. El mapa de tu padre por fin está completo.";

        zona.pasos = new[] {
            new ZonaIsla.Paso { texto = "Abre la puerta de la muralla con <b>Sombra</b> sin que te vean", bloqueo = cerradura },
            new ZonaIsla.Paso { texto = "Resiste la alarma del patio de armas", recompensa = recPatio },
            new ZonaIsla.Paso { texto = "Rompe el portón interior con <b>Bruno</b>", bloqueo = bq },
            new ZonaIsla.Paso { texto = "Derrota a la <b>Almirante Voss</b>", recompensa = recVoss },
            new ZonaIsla.Paso { texto = "Toma el Corazón de las Mareas", recompensa = recCorazon },
        };
        zona.recompensaPrincipal = recCorazon;
        Terminar(raiz, "Isla 6 creada. Los dos \"Molde\" del patio deben quedar desactivados. Para la cerradura y el portón hay que invocar a Sombra y a Bruno (G / H).");
    }

    // ================================================================ ayudas de las islas 4 a 6
    static GameObject Objeto(GameObject padre, string nombre, string visible, string grupo, Vector3 pos, Color color, bool local = false)
    {
        var go = Herramienta(padre, nombre, visible, pos, color, local);
        go.GetComponent<ObjetoMision>().grupo = grupo;
        return go;
    }

    // edificio hueco de 4 paredes con una puerta (atras = lado norte, para la Comandancia)
    static GameObject Edificio(GameObject padre, string nombre, Vector3 pos, Vector3 t, Color color, bool puertaAtras)
    {
        var go = new GameObject(nombre);
        Undo.RegisterCreatedObjectUndo(go, nombre);
        go.transform.SetParent(padre.transform, false);
        go.transform.position = Suelo(padre.transform.position + pos);
        float g = 0.4f, puerta = 2.2f;
        Bloque(go, "Pared_Izq", PrimitiveType.Cube, new Vector3(-t.x / 2f, t.y / 2f, 0f), new Vector3(g, t.y, t.z), color, true);
        Bloque(go, "Pared_Der", PrimitiveType.Cube, new Vector3(t.x / 2f, t.y / 2f, 0f), new Vector3(g, t.y, t.z), color, true);
        // la pared con puerta se parte en dos con un hueco en medio
        float zp = puertaAtras ? t.z / 2f : -t.z / 2f, zl = puertaAtras ? -t.z / 2f : t.z / 2f;
        float lado = (t.x - puerta) / 2f;
        Bloque(go, "Pared_Puerta_A", PrimitiveType.Cube, new Vector3(-(puerta + lado) / 2f, t.y / 2f, zp), new Vector3(lado, t.y, g), color, true);
        Bloque(go, "Pared_Puerta_B", PrimitiveType.Cube, new Vector3((puerta + lado) / 2f, t.y / 2f, zp), new Vector3(lado, t.y, g), color, true);
        Bloque(go, "Pared_Llena", PrimitiveType.Cube, new Vector3(0f, t.y / 2f, zl), new Vector3(t.x, t.y, g), color, true);
        Bloque(go, "Techo", PrimitiveType.Cube, new Vector3(0f, t.y + 0.15f, 0f), new Vector3(t.x + 0.6f, 0.3f, t.z + 0.6f), color * 0.8f, true);
        return go;
    }

    static void BarrilEn(GameObject padre, string nombre, Vector3 pos)
    {
        var b = Bloque(padre, nombre, PrimitiveType.Cylinder, pos + Vector3.up * 0.6f, new Vector3(0.9f, 0.6f, 0.9f), new Color(0.5f, 0.35f, 0.2f));
        b.AddComponent<Barril>();
    }

    // guardia de la Corona: copia un Sereno que ya tengas (o un bloque), con 2 puntos de patrulla
    static GameObject Cazador(GameObject padre, string nombre, Vector3 pos, Transform reaparicion, Vector3 recorrido)
    {
        var original = Object.FindFirstObjectByType<Sereno>(FindObjectsInactive.Include);
        GameObject go;
        if (original)
        {
            go = (GameObject)Object.Instantiate(original.gameObject, padre.transform);
            go.SetActive(true);
        }
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.transform.SetParent(padre.transform, false);
            go.AddComponent<Colorear>().color = new Color(0.2f, 0.25f, 0.5f);
            var l = new GameObject("Farol").AddComponent<Light>();
            l.transform.SetParent(go.transform, false); l.transform.localPosition = new Vector3(0.4f, 0.4f, 0.4f);
            l.type = LightType.Point; l.color = new Color(1f, 0.75f, 0.4f); l.range = 6f; l.intensity = 2f;
            go.AddComponent<Sereno>().farol = l;
        }
        Undo.RegisterCreatedObjectUndo(go, nombre);
        go.name = nombre;
        go.transform.position = Suelo(padre.transform.position + pos) + Vector3.up * 1f;
        var s = go.GetComponent<Sereno>();
        s.reaparicion = reaparicion;
        if (!s.jugador) s.jugador = Object.FindFirstObjectByType<PlayerController>();
        // puntos de patrulla (fuera del guardia, para que no se muevan con el)
        var ruta = new GameObject(nombre + "_Ruta");
        Undo.RegisterCreatedObjectUndo(ruta, "Ruta");
        ruta.transform.SetParent(padre.transform, false);
        var a = Vacio(ruta, "Punto_A", Vector3.zero); a.transform.position = Suelo(padre.transform.position + pos);
        var b = Vacio(ruta, "Punto_B", Vector3.zero); b.transform.position = Suelo(padre.transform.position + pos + recorrido);
        s.puntos = new[] { a.transform, b.transform };
        return go;
    }

    // ================================================================ ayudas
    static bool YaExiste(int id)
    {
        foreach (var z in Object.FindObjectsByType<ZonaIsla>(FindObjectsSortMode.None))
            if (z.idIsla == id)
            {
                EditorUtility.DisplayDialog("Grumete", "Ya hay una ZonaIsla con id " + id + " en la escena (" + z.name + ").", "OK");
                Selection.activeObject = z.gameObject;
                return true;
            }
        return false;
    }

    static Vector3 CentroVista()
    {
        var v = SceneView.lastActiveSceneView;
        return Suelo(v ? v.pivot : Vector3.zero);
    }

    // baja el punto hasta el suelo que haya debajo (terreno o colliders)
    static Vector3 Suelo(Vector3 p)
    {
        if (Physics.Raycast(p + Vector3.up * 300f, Vector3.down, out var hit, 600f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            return hit.point;
        return p;
    }

    static GameObject Vacio(GameObject padre, string nombre, Vector3 local)
    {
        var go = new GameObject(nombre);
        Undo.RegisterCreatedObjectUndo(go, nombre);
        go.transform.SetParent(padre.transform, false);
        go.transform.localPosition = local;
        return go;
    }

    // bloque provisional de color. "local" = relativo al padre sin buscar suelo (para piezas de un grupo)
    static GameObject Bloque(GameObject padre, string nombre, PrimitiveType tipo, Vector3 pos, Vector3 escala, Color color, bool local = false)
    {
        var go = GameObject.CreatePrimitive(tipo);
        go.name = nombre;
        Undo.RegisterCreatedObjectUndo(go, nombre);
        go.transform.SetParent(padre.transform, false);
        if (local) go.transform.localPosition = pos;
        else go.transform.position = Suelo(padre.transform.position + new Vector3(pos.x, 0f, pos.z)) + Vector3.up * (pos.y + escala.y * 0.5f * (tipo == PrimitiveType.Cube ? 1f : 0f));
        go.transform.localScale = escala;
        go.AddComponent<Colorear>().color = color;
        return go;
    }

    static GameObject Herramienta(GameObject padre, string nombre, string visible, Vector3 pos, Color color, bool local = false)
    {
        var go = new GameObject(nombre);
        Undo.RegisterCreatedObjectUndo(go, nombre);
        go.transform.SetParent(padre.transform, false);
        if (local) go.transform.localPosition = pos;
        else go.transform.position = Suelo(padre.transform.position + pos);
        var om = go.AddComponent<ObjetoMision>();
        om.grupo = "herramientas_pico"; om.nombre = visible; om.clave = nombre.ToLower(); om.color = color;
        return go;
    }

    static GameObject CopiarJefe(GameObject padre, string nombre, Vector3 pos)
    {
        var original = Object.FindFirstObjectByType<Jefe>();
        GameObject go;
        if (original)
        {
            go = (GameObject)Object.Instantiate(original.gameObject, padre.transform);
            go.SetActive(true);
            // la copia no se lleva las recompensas del original (darian lo de la otra isla)
            foreach (var rc in go.GetComponentsInChildren<RecompensaIsla>(true)) Object.DestroyImmediate(rc);
        }
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.transform.SetParent(padre.transform, false);
            go.transform.localScale = new Vector3(1.4f, 1.4f, 1.4f);
            go.AddComponent<Colorear>().color = new Color(0.55f, 0.15f, 0.12f);
            var rv = go.AddComponent<Rival>(); rv.esDuelo = false;
            go.AddComponent<Jefe>();
        }
        Undo.RegisterCreatedObjectUndo(go, nombre);
        go.name = nombre;
        go.transform.position = Suelo(padre.transform.position + pos) + Vector3.up * 1.4f;
        var r = go.GetComponent<Rival>();
        r.centroZona = null; r.esDuelo = false; r.esJefe = true;
        if (!r.jugador) r.jugador = Object.FindFirstObjectByType<PlayerController>();
        return go;
    }

    static GameObject CopiarEnemigo(GameObject padre, string nombre, Vector3 pos, string nombreRival, int vida)
    {
        Rival original = null;
        foreach (var rv in Object.FindObjectsByType<Rival>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (!rv.esJefe && !rv.GetComponent<Jefe>() && !rv.esDuelo) { original = rv; break; }
        if (!original)
            foreach (var rv in Object.FindObjectsByType<Rival>(FindObjectsInactive.Include, FindObjectsSortMode.None))
                if (!rv.GetComponent<Jefe>()) { original = rv; break; }
        GameObject go;
        if (original)
        {
            go = (GameObject)Object.Instantiate(original.gameObject, padre.transform);
            go.SetActive(true);
            // la copia no se lleva las recompensas del original (darian lo de la otra isla)
            foreach (var rc in go.GetComponentsInChildren<RecompensaIsla>(true)) Object.DestroyImmediate(rc);
        }
        else
        {
            go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.transform.SetParent(padre.transform, false);
            go.AddComponent<Colorear>().color = new Color(0.45f, 0.4f, 0.3f);
            go.AddComponent<Rival>();
        }
        Undo.RegisterCreatedObjectUndo(go, nombre);
        go.name = nombre;
        go.transform.position = Suelo(padre.transform.position + pos) + Vector3.up * 1f;
        var r = go.GetComponent<Rival>();
        r.nombre = nombreRival; r.vidaMaxima = vida; r.esDuelo = false; r.esJefe = false; r.centroZona = null;
        r.armaQueSuelta = Equipo.IdArma.Punos; r.chatarraMax = 2;
        if (!r.jugador) r.jugador = Object.FindFirstObjectByType<PlayerController>();
        return go;
    }

    static void Terminar(GameObject raiz, string mensaje)
    {
        EditorSceneManager.MarkSceneDirty(raiz.scene);
        Debug.Log("[Grumete] " + mensaje.Replace("\n", " "), raiz);
        EditorUtility.DisplayDialog("Grumete", mensaje, "OK");
    }
}
