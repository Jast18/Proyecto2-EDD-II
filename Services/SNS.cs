using System;

// SNS = Social Network System. Capa de servicio que orquesta las cuatro estructuras con el flujo del enunciado:
//   Trie sugiere -> HashTable localiza -> Union-Find valida conectividad -> Graph (BFS / DFS / Dijkstra)
//
// Regla: todo lo que toca MÁS DE UNA estructura vive aquí, para que ninguna quede desfasada de las otras.
// La UI (PForm / GPanel) y Trials solo deberían llamar a este servicio. Aquí no hay impresión ni controles.
//
//   Estructura   | Responsabilidad dentro del SNS
//   -------------+------------------------------------------------------------------------
//   HashTable    | fuente de verdad: el User completo, por Username (llave primaria)
//   Trie         | autocompletado de usernames (solo llaves)
//   Graph        | relaciones ponderadas por afinidad: BFS, DFS, Dijkstra, sugerencias
//   Union-Find   | comunidades; valida conectividad antes de Dijkstra
class SNS
{
    public readonly HashTable Usuarios = new HashTable();
    public readonly Trie Indice = new Trie();
    public readonly Graph Grafo = new Graph();
    public readonly UnionFind Comunidades = new UnionFind();

    private readonly string rutaUsuarios;
    private readonly string rutaRelaciones;

    public SNS(string rutaUsuarios = "Data/user.csv", string rutaRelaciones = "Data/relations.csv")
    {
        this.rutaUsuarios = rutaUsuarios;
        this.rutaRelaciones = rutaRelaciones;
    }

    // ---------- Persistencia ----------

    public (int usuarios, int relaciones, int comunidades, string error) Cargar()
    {
        var u = ChargeCSV.CargarUsuarios(rutaUsuarios, Usuarios);
        if (u.error != null) return (0, 0, 0, u.error);
        var r = ChargeCSV.CargarRelaciones(rutaRelaciones, Grafo, Usuarios);
        var s = ChargeCSV.Reconstruir(Usuarios, Indice, Grafo, Comunidades);
        return (u.cargados, r.cargadas, s.comunidades, r.error);
    }

    public (bool ok, string error) Guardar()
    {
        var u = ChargeCSV.GuardarUsuarios(rutaUsuarios, Usuarios);
        if (!u.ok) return (false, u.error);
        var r = ChargeCSV.GuardarRelaciones(rutaRelaciones, Grafo);
        return (r.ok, r.error);
    }

    // ---------- Consultas (flujo del enunciado) ----------

    // 1) Trie: sugerencias mientras se escribe (solo llaves).
    public (string[] sugerencias, int total) Buscar(string prefijo, int max = 5)
    {
        return Indice.Autocompletar(prefijo, max);
    }

    // 2) HashTable: perfil completo. Cada consulta cuenta como una visita.
    public (bool encontrado, User perfil, int visitas) Perfil(string username)
    {
        var r = Usuarios.Buscar(username);
        if (!r.encontrado) return (false, null, 0);
        var v = Usuarios.RegistrarVisita(username);
        return (true, r.usuario, v.visitas);
    }

    // Amigos directos con la afinidad de cada relación (para mostrar en el perfil).
    public (string[] usuarios, int[] afinidades) Amigos(string username)
    {
        var v = Grafo.Vecinos(username);
        return (v.vecinos, v.afinidades);
    }

    // 3) Union-Find valida ANTES de ejecutar Dijkstra (4).
    public (bool ok, string motivo, string[] camino, int costo, int afinidadPromedio)
        CaminoMasFuerte(string a, string b)
    {
        if (!Usuarios.Existe(a) || !Usuarios.Existe(b))
            return (false, "Alguno de los usuarios no existe.", new string[0], 0, 0);

        var c = Comunidades.Conectados(a, b);
        if (!c.conectados)
            return (false, "Están en comunidades distintas: no hay camino.", new string[0], 0, 0);

        var d = Grafo.Dijkstra(a, b);
        return (d.existe, d.existe ? null : "Sin camino.", d.camino, d.costoTotal, d.afinidadPromedio);
    }

    public (string[] candidatos, int[] puntajes) SugerenciasDeAmistad(string username, int max = 5)
    {
        return Grafo.SugerirAmigos(username, max);
    }

    // BFS (por niveles) y DFS (en profundidad) desde un usuario. Vacíos si el usuario no existe.
    public (string[] bfs, int[] niveles, string[] dfs, int[] profundidades) Recorrer(string origen)
    {
        if (!Usuarios.Existe(origen)) return (new string[0], new int[0], new string[0], new int[0]);
        var b = Grafo.BFS(origen);
        var d = Grafo.DFS(origen);
        return (b.orden, b.niveles, d.orden, d.profundidades);
    }

    public (string[][] grupos, int cantidad) ObtenerComunidades()
    {
        return Comunidades.Comunidades();
    }

    // ---------- Escrituras coordinadas ----------

    // Alta: HashTable -> Trie -> Graph -> Union-Find. Si el Trie rechaza la llave
    // (vacía o con caracteres no ASCII) se revierte el alta, para no dejar un usuario "invisible" al buscador.
    public (bool ok, string motivo) Registrar(User u)
    {
        var ins = Usuarios.Insertar(u);
        if (!ins.insertado) return (false, "El usuario ya existe o es inválido.");

        if (!Indice.Insertar(u.Username).insertada)
        {
            Usuarios.Eliminar(u.Username);
            return (false, "El username solo puede usar caracteres ASCII (sin tildes ni ñ).");
        }
        Grafo.AgregarVertice(u.Username);
        Comunidades.Agregar(u.Username);
        return (true, null);
    }

    // Reemplaza los datos del perfil (el Username e Id no cambian, las visitas se conservan).
    // Como la afinidad depende del perfil, se recalcula en TODAS las relaciones del usuario.
    public (bool ok, string motivo, int relacionesRecalculadas) EditarUsuario(string username, User nuevo)
    {
        if (nuevo == null) return (false, "Datos inválidos.", 0);
        var actual = Usuarios.Buscar(username);
        if (!actual.encontrado) return (false, "El usuario no existe.", 0);

        string key = actual.usuario.Username;
        nuevo.Visits = actual.usuario.Visits;
        Usuarios.Editar(key, nuevo);

        string[] vecinos = Grafo.Vecinos(key).vecinos;
        int recalculadas = 0;
        foreach (string v in vecinos)
        {
            var otro = Usuarios.Buscar(v);
            if (!otro.encontrado) continue;
            int af = AC.Calcular(nuevo, otro.usuario).puntaje;
            if (Grafo.AgregarArista(key, v, af).exito) recalculadas++;
        }
        return (true, null, recalculadas);
    }

    // Afinidad calculada con los dos perfiles; Union-Find une las comunidades.
    public (bool ok, int afinidad, string motivo) AgregarRelacion(string a, string b)
    {
        var ea = Usuarios.Buscar(a);
        var eb = Usuarios.Buscar(b);
        if (!ea.encontrado || !eb.encontrado) return (false, 0, "Alguno de los usuarios no existe.");
        int af = AC.Calcular(ea.usuario, eb.usuario).puntaje;
        var g = Grafo.AgregarArista(ea.usuario.Username, eb.usuario.Username, af);
        if (!g.exito) return (false, 0, "No se pudo crear la relación (mismo usuario).");
        Comunidades.Unir(ea.usuario.Username, eb.usuario.Username);
        return (true, af, null);
    }

    // Union-Find no deshace uniones: tras quitar una relación se reconstruye desde el grafo.
    public (bool ok, int afinidad, string motivo) EliminarRelacion(string a, string b)
    {
        if (!Usuarios.Existe(a) || !Usuarios.Existe(b))
            return (false, 0, "Alguno de los usuarios no existe.");
        var g = Grafo.EliminarArista(a, b);
        if (!g.eliminada) return (false, 0, "Esa relación no existe.");
        ChargeCSV.Reconstruir(Usuarios, Indice, Grafo, Comunidades);
        return (true, g.afinidad, null);
    }

    // Union-Find no deshace uniones: tras eliminar se reconstruye desde el grafo.
    public (bool ok, string motivo) Eliminar(string username)
    {
        var h = Usuarios.Eliminar(username);
        if (!h.eliminado) return (false, "El usuario no existe.");
        Indice.Eliminar(h.usuario.Username);
        Grafo.EliminarVertice(h.usuario.Username);
        ChargeCSV.Reconstruir(Usuarios, Indice, Grafo, Comunidades);
        return (true, null);
    }
}