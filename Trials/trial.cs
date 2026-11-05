using System;

// Pruebas por consola, independientes de la UI. Usa SNS (que orquesta HashTable, Trie, Graph y Union-Find),
// así que prueba exactamente lo mismo que usará la interfaz gráfica.
//   Trials.Run()   -> menú interactivo (carga los CSV de Data/ al iniciar)
//   Trials.Auto()  -> prueba automática en memoria; NO lee ni escribe los CSV
class Trials
{
    // ---------- Menú interactivo ----------

    public static void Run(string rutaUsuarios = "Data/user.csv", string rutaRelaciones = "Data/relations.csv")
    {
        SNS sns = new SNS(rutaUsuarios, rutaRelaciones);
        Console.WriteLine("=== CampusLink - pruebas por consola ===");
        Cargar(sns);

        while (true)
        {
            Console.WriteLine();
            Console.WriteLine(" 1) Cargar CSV              8) Recorridos BFS / DFS");
            Console.WriteLine(" 2) Autocompletar (Trie)    9) Comunidades (Union-Find)");
            Console.WriteLine(" 3) Ver perfil (HashTable) 10) Eliminar usuario");
            Console.WriteLine(" 4) Registrar usuario      11) Guardar CSV");
            Console.WriteLine(" 5) Agregar relación       12) Estadísticas de las estructuras");
            Console.WriteLine(" 6) Camino más fuerte      13) Prueba automática");
            Console.WriteLine(" 7) Sugerencias de amistad 14) Editar usuario");
            Console.WriteLine("                           15) Eliminar relación");
            Console.WriteLine(" 0) Salir");
            string op = Leer("Opción");
            if (op == null || op == "0") break;

            switch (op)
            {
                case "1": Cargar(sns); break;
                case "2": Autocompletar(sns); break;
                case "3": Perfil(sns); break;
                case "4": Registrar(sns); break;
                case "5": Relacionar(sns); break;
                case "6": Camino(sns); break;
                case "7": Sugerencias(sns); break;
                case "8": Recorridos(sns); break;
                case "9": Comunidades(sns); break;
                case "10": Eliminar(sns); break;
                case "11": Guardar(sns); break;
                case "12": Estadisticas(sns); break;
                case "13": Auto(); break;
                case "14": Editar(sns); break;
                case "15": QuitarRelacion(sns); break;
                default: Console.WriteLine("Opción no válida."); break;
            }
        }
        Console.WriteLine("Fin de las pruebas.");
    }

    private static string Leer(string etiqueta)
    {
        Console.Write(etiqueta + ": ");
        string s = Console.ReadLine();
        return s == null ? null : s.Trim();
    }

    private static string Lista(string[] items)
    {
        return items.Length == 0 ? "(vacío)" : string.Join(", ", items);
    }

    private static void Cargar(SNS sns)
    {
        var r = sns.Cargar();
        if (r.error != null) Console.WriteLine("Aviso: " + r.error);
        Console.WriteLine($"Usuarios: {r.usuarios} | Relaciones: {r.relaciones} | Comunidades: {r.comunidades}");
    }

    private static void Autocompletar(SNS sns)
    {
        var r = sns.Buscar(Leer("Prefijo"), 10);
        Console.WriteLine($"{r.total} coincidencia(s): {Lista(r.sugerencias)}");
    }

    private static void Perfil(SNS sns)
    {
        var r = sns.Perfil(Leer("Username"));
        if (!r.encontrado) { Console.WriteLine("No existe."); return; }
        User u = r.perfil;
        Console.WriteLine($"[Id {u.Id}] {u.Username} - {u.Name}");
        Console.WriteLine($"  Carrera: {u.Career} | Año: {u.Year} | Música: {u.Music}");
        Console.WriteLine($"  Cursos: {Lista(u.Courses)} | Visitas: {r.visitas}");
    }

    private static void Registrar(SNS sns)
    {
        User u = new User();
        u.Username = Leer("Username");
        u.Name = Leer("Nombre");
        u.Career = Leer("Carrera");
        int anio; int.TryParse(Leer("Año"), out anio); u.Year = anio;
        u.Music = Leer("Música");
        string cursos = Leer("Cursos (separados por ;)") ?? "";
        u.Courses = cursos.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

        var r = sns.Registrar(u);
        Console.WriteLine(r.ok ? "Registrado." : "No se registró: " + r.motivo);
    }

    private static void Relacionar(SNS sns)
    {
        var r = sns.AgregarRelacion(Leer("Usuario A"), Leer("Usuario B"));
        Console.WriteLine(r.ok ? $"Relación creada. Afinidad: {r.afinidad}%" : "No se creó: " + r.motivo);
    }

    private static void Editar(SNS sns)
    {
        string user = Leer("Username a editar");
        var actual = sns.Usuarios.Buscar(user);
        if (!actual.encontrado) { Console.WriteLine("No existe."); return; }

        User u = new User();
        u.Name = Leer("Nombre");
        u.Career = Leer("Carrera");
        int anio; int.TryParse(Leer("Año"), out anio); u.Year = anio;
        u.Music = Leer("Música");
        string cursos = Leer("Cursos (separados por ;)") ?? "";
        u.Courses = cursos.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);

        var r = sns.EditarUsuario(user, u);
        Console.WriteLine(r.ok ? $"Editado. Afinidades recalculadas: {r.relacionesRecalculadas}" : r.motivo);
    }

    private static void QuitarRelacion(SNS sns)
    {
        var r = sns.EliminarRelacion(Leer("Usuario A"), Leer("Usuario B"));
        Console.WriteLine(r.ok ? $"Relación eliminada (tenía {r.afinidad}%)." : r.motivo);
    }

    private static void Camino(SNS sns)
    {
        var r = sns.CaminoMasFuerte(Leer("Origen"), Leer("Destino"));
        if (!r.ok) { Console.WriteLine(r.motivo); return; }
        Console.WriteLine($"Camino: {string.Join(" -> ", r.camino)}");
        Console.WriteLine($"Afinidad promedio: {r.afinidadPromedio}% | Costo: {r.costo}");
    }

    private static void Sugerencias(SNS sns)
    {
        string user = Leer("Username");
        var r = sns.SugerenciasDeAmistad(user, 5);
        if (r.candidatos.Length == 0) { Console.WriteLine("Sin sugerencias."); return; }
        for (int i = 0; i < r.candidatos.Length; i++)
            Console.WriteLine($"  {r.candidatos[i]} (puntaje {r.puntajes[i]})");
    }

    private static void Recorridos(SNS sns)
    {
        string origen = Leer("Origen");
        var bfs = sns.Grafo.BFS(origen);
        var dfs = sns.Grafo.DFS(origen);
        if (bfs.orden.Length == 0) { Console.WriteLine("El vértice no existe."); return; }
        Console.WriteLine("BFS: " + Lista(bfs.orden));
        Console.WriteLine("DFS: " + Lista(dfs.orden));
    }

    private static void Comunidades(SNS sns)
    {
        var c = sns.Comunidades.Comunidades();
        Console.WriteLine($"{c.cantidad} comunidad(es):");
        for (int i = 0; i < c.grupos.Length; i++)
            Console.WriteLine($"  #{i + 1} ({c.grupos[i].Length}): {Lista(c.grupos[i])}");
    }

    private static void Eliminar(SNS sns)
    {
        var r = sns.Eliminar(Leer("Username"));
        Console.WriteLine(r.ok ? "Eliminado de todas las estructuras." : r.motivo);
    }

    private static void Guardar(SNS sns)
    {
        var r = sns.Guardar();
        Console.WriteLine(r.ok ? "Guardado." : "Error: " + r.error);
    }

    private static void Estadisticas(SNS sns)
    {
        Console.WriteLine($"HashTable : {sns.Usuarios.Cantidad} usuarios | capacidad {sns.Usuarios.Capacidad} | carga {sns.Usuarios.FactorCarga:F2}");
        Console.WriteLine($"Trie      : {sns.Indice.Cantidad} llaves");
        Console.WriteLine($"Graph     : {sns.Grafo.CantidadVertices} vértices | {sns.Grafo.CantidadAristas} aristas");
        Console.WriteLine($"UnionFind : {sns.Comunidades.Cantidad} elementos | {sns.Comunidades.Componentes} comunidades");
    }

    // ---------- Prueba automática (en memoria) ----------

    private static int ok, fallos;

    private static void Check(string nombre, bool condicion)
    {
        if (condicion) ok++; else fallos++;
        Console.WriteLine((condicion ? "  [OK]    " : "  [FALLA] ") + nombre);
    }

    private static User Nuevo(string user, string carrera, int anio, string musica, string cursos)
    {
        return new User
        {
            Username = user, Name = user, Career = carrera, Year = anio, Music = musica,
            Courses = cursos.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries)
        };
    }

    public static void Auto()
    {
        ok = 0; fallos = 0;
        Console.WriteLine("--- Prueba automática (en memoria, no toca los CSV) ---");
        SNS sns = new SNS("__no_existe_u.csv", "__no_existe_r.csv");

        sns.Registrar(Nuevo("ana", "Sistemas", 2, "Pop", "EDD2;Calculo"));
        sns.Registrar(Nuevo("luis", "Sistemas", 2, "Rock", "EDD2;Fisica"));
        sns.Registrar(Nuevo("maria", "Sistemas", 3, "Pop", "EDD2"));
        sns.Registrar(Nuevo("pablo", "Civil", 1, "Jazz", "Estatica"));
        sns.Registrar(Nuevo("sofia", "Civil", 1, "Jazz", "Estatica;Calculo"));
        sns.Registrar(Nuevo("solo", "Medicina", 4, "Clasica", "Anatomia"));

        Check("Registro duplicado se rechaza", !sns.Registrar(Nuevo("ANA", "x", 1, "x", "x")).ok);
        Check("HashTable: 6 usuarios", sns.Usuarios.Cantidad == 6);
        Check("Ids únicos", sns.Usuarios.Buscar("ana").usuario.Id != sns.Usuarios.Buscar("luis").usuario.Id);
        Check("Trie: autocompleta 'so' -> solo, sofia", sns.Buscar("so", 5).total == 2);

        sns.AgregarRelacion("ana", "luis");
        sns.AgregarRelacion("luis", "maria");
        sns.AgregarRelacion("pablo", "sofia");

        Check("UF: ana y maria conectadas (vía luis)", sns.Comunidades.Conectados("ana", "maria").conectados);
        Check("UF: ana y pablo en comunidades distintas", !sns.Comunidades.Conectados("ana", "pablo").conectados);
        Check("UF: 3 comunidades (ana-luis-maria, pablo-sofia, solo)", sns.Comunidades.Componentes == 3);

        var cam = sns.CaminoMasFuerte("ana", "maria");
        Check("Dijkstra: ana -> luis -> maria", cam.ok && string.Join(",", cam.camino) == "ana,luis,maria");
        Check("Dijkstra se omite entre comunidades distintas", !sns.CaminoMasFuerte("ana", "pablo").ok);
        Check("Usuario inexistente se rechaza", !sns.CaminoMasFuerte("ana", "nadie").ok);

        Check("BFS desde ana alcanza 3 vértices", sns.Grafo.BFS("ana").orden.Length == 3);
        Check("DFS desde ana alcanza 3 vértices", sns.Grafo.DFS("ana").orden.Length == 3);
        Check("Sugerencia de amistad: maria para ana", Array.IndexOf(sns.SugerenciasDeAmistad("ana", 3).candidatos, "maria") >= 0);

        sns.Perfil("ana"); sns.Perfil("ana");
        Check("Contador de visitas = 2", sns.Usuarios.Buscar("ana").usuario.Visits == 2);

        // Editar: la afinidad ana-luis se recalcula y las visitas se conservan
        int antes = sns.Grafo.Vecinos("ana").afinidades[0];
        var ed = sns.EditarUsuario("ana", Nuevo("ana", "Civil", 1, "Jazz", "Estatica"));
        int despues = sns.Grafo.Vecinos("ana").afinidades[0];
        Check("Editar: recalcula 1 relación", ed.ok && ed.relacionesRecalculadas == 1);
        Check("Editar: la afinidad cambió", antes != despues);
        Check("Editar: conserva visitas e Id", sns.Usuarios.Buscar("ana").usuario.Visits == 2);
        Check("Editar usuario inexistente se rechaza", !sns.EditarUsuario("nadie", Nuevo("nadie", "x", 1, "x", "x")).ok);

        // Username no ASCII: el alta se revierte y no queda "invisible" para el buscador
        Check("Registrar con ñ se rechaza y se revierte", !sns.Registrar(Nuevo("muñoz", "x", 1, "x", "x")).ok
              && !sns.Usuarios.Existe("muñoz"));

        // Eliminar relación: el Union-Find se reconstruye
        Check("Eliminar relación luis-maria", sns.EliminarRelacion("luis", "maria").ok);
        Check("Eliminar relación inexistente se rechaza", !sns.EliminarRelacion("luis", "maria").ok);
        Check("Tras quitar la relación, luis y maria separados", !sns.Comunidades.Conectados("luis", "maria").conectados);
        sns.AgregarRelacion("luis", "maria");

        var rec = sns.Recorrer("ana");
        Check("Recorrer: BFS y DFS del mismo tamaño", rec.bfs.Length == rec.dfs.Length && rec.bfs.Length == 3);

        sns.Eliminar("luis");
        Check("Eliminar: sale de HashTable y Trie", !sns.Usuarios.Existe("luis") && sns.Buscar("luis", 5).total == 0);
        Check("Eliminar: ana y maria ya no conectadas", !sns.Comunidades.Conectados("ana", "maria").conectados);

        Console.WriteLine($"Resultado: {ok} OK, {fallos} con falla.");
    }
}