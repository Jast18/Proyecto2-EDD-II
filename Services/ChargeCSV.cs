using System;
using System.IO;

// Persistencia CSV. Solo se guardan los DATOS FUENTE: usuarios y relaciones.
// Trie y Union-Find se reconstruyen con Reconstruir() (son índices derivados, no datos).
//
// user.csv      : username,name,career,year,music,courses,visits   (courses separados por ';')
// relations.csv : user1,user2,affinity                              (affinity opcional: si falta se calcula con AC)
static class ChargeCSV
{
    private const string ENC_USR = "username,name,career,year,music,courses,visits";
    private const string ENC_REL = "user1,user2,affinity";

    private static string Limpiar(string s)
    {
        return (s ?? "").Replace(",", " ").Replace("\r", " ").Replace("\n", " ").Trim();
    }

    private static string FilaUsuario(User u)
    {
        return Limpiar(u.Username) + "," + Limpiar(u.Name) + "," + Limpiar(u.Career) + "," + u.Year + ","
             + Limpiar(u.Music) + "," + Limpiar(string.Join(";", u.Courses)) + "," + u.Visits;
    }

    private static (bool ok, User usuario) ParsearUsuario(string linea)
    {
        string[] f = linea.Split(',');
        if (f.Length < 2 || string.IsNullOrWhiteSpace(f[0])) return (false, null);

        User u = new User { Username = Util.Norm(f[0]), Name = f[1].Trim() };
        if (f.Length > 2) u.Career = f[2].Trim();
        if (f.Length > 3) { int y; if (int.TryParse(f[3].Trim(), out y)) u.Year = y; }
        if (f.Length > 4) u.Music = f[4].Trim();
        if (f.Length > 5)
            u.Courses = f[5].Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
        if (f.Length > 6) { int v; if (int.TryParse(f[6].Trim(), out v)) u.Visits = v; }
        return (true, u);
    }

    // Un encabezado es la línea EXACTA de columnas. No se compara por prefijo: un usuario real
    // llamado "user1" no debe confundirse con el encabezado.
    private static bool EsEncabezado(string linea)
    {
        string l = linea.Trim().ToLowerInvariant();
        return l == ENC_USR || l == ENC_REL || l == "user,name,career,year,music,courses,visits"
            || l == "user1,user2,afinidad";
    }

    // ---------- Usuarios ----------

    public static (bool ok, int filas, string error) GuardarUsuarios(string ruta, HashTable tabla)
    {
        try
        {
            User[] todos = tabla.ObtenerTodos();
            string[] lineas = new string[todos.Length + 1];
            lineas[0] = ENC_USR;
            for (int i = 0; i < todos.Length; i++) lineas[i + 1] = FilaUsuario(todos[i]);
            File.WriteAllLines(ruta, lineas);
            return (true, todos.Length, null);
        }
        catch (Exception ex) { return (false, 0, ex.Message); }
    }

    public static (int cargados, int duplicados, int invalidos, string error)
        CargarUsuarios(string ruta, HashTable tabla)
    {
        if (!File.Exists(ruta)) return (0, 0, 0, "No existe el archivo: " + ruta);
        int cargados = 0, duplicados = 0, invalidos = 0;
        try
        {
            foreach (string linea in File.ReadAllLines(ruta))
            {
                if (string.IsNullOrWhiteSpace(linea) || EsEncabezado(linea)) continue;
                var p = ParsearUsuario(linea);
                if (!p.ok) { invalidos++; continue; }
                if (tabla.Insertar(p.usuario).insertado) cargados++; else duplicados++;
            }
            return (cargados, duplicados, invalidos, null);
        }
        catch (Exception ex) { return (cargados, duplicados, invalidos, ex.Message); }
    }

    // Actualiza la fila del usuario en el archivo (o la agrega si no existe), sin recargar todo.
    public static (bool actualizado, bool agregado, string error) ActualizarUsuario(string ruta, User u)
    {
        try
        {
            string key = Util.Norm(u.Username);
            string[] lineas = File.Exists(ruta) ? File.ReadAllLines(ruta) : new string[0];
            string nueva = FilaUsuario(u);

            if (lineas.Length == 0 || !EsEncabezado(lineas[0]))
            {
                string[] con = new string[lineas.Length + 1];
                con[0] = ENC_USR;
                Array.Copy(lineas, 0, con, 1, lineas.Length);
                lineas = con;
            }

            for (int i = 1; i < lineas.Length; i++)
            {
                string[] f = lineas[i].Split(',');
                if (f.Length > 0 && Util.Norm(f[0]) == key)
                {
                    lineas[i] = nueva;
                    File.WriteAllLines(ruta, lineas);
                    return (true, false, null);
                }
            }

            Array.Resize(ref lineas, lineas.Length + 1);
            lineas[lineas.Length - 1] = nueva;
            File.WriteAllLines(ruta, lineas);
            return (false, true, null);
        }
        catch (Exception ex) { return (false, false, ex.Message); }
    }

    // ---------- Relaciones (aristas) ----------

    public static (bool ok, int filas, string error) GuardarRelaciones(string ruta, Graph grafo)
    {
        try
        {
            var ar = grafo.ObtenerAristas();
            string[] lineas = new string[ar.a.Length + 1];
            lineas[0] = ENC_REL;
            for (int i = 0; i < ar.a.Length; i++)
                lineas[i + 1] = ar.a[i] + "," + ar.b[i] + "," + ar.afinidades[i];
            File.WriteAllLines(ruta, lineas);
            return (true, ar.a.Length, null);
        }
        catch (Exception ex) { return (false, 0, ex.Message); }
    }

    // Cada extremo debe existir en la HashTable (referencia por llave primaria).
    // Si no viene la afinidad en el archivo, se calcula con los datos de los dos perfiles.
    public static (int cargadas, int omitidas, string error)
        CargarRelaciones(string ruta, Graph grafo, HashTable tabla)
    {
        if (!File.Exists(ruta)) return (0, 0, "No existe el archivo: " + ruta);
        int cargadas = 0, omitidas = 0;
        try
        {
            foreach (string linea in File.ReadAllLines(ruta))
            {
                if (string.IsNullOrWhiteSpace(linea) || EsEncabezado(linea)) continue;
                string[] f = linea.Split(',');
                if (f.Length < 2) { omitidas++; continue; }

                var ea = tabla.Buscar(f[0]);
                var eb = tabla.Buscar(f[1]);
                if (!ea.encontrado || !eb.encontrado) { omitidas++; continue; }

                int af;
                if (f.Length < 3 || !int.TryParse(f[2].Trim(), out af))
                    af = AC.Calcular(ea.usuario, eb.usuario).puntaje;

                grafo.AgregarVertice(ea.usuario.Username);
                grafo.AgregarVertice(eb.usuario.Username);
                if (grafo.AgregarArista(ea.usuario.Username, eb.usuario.Username, af).exito) cargadas++;
                else omitidas++;
            }
            return (cargadas, omitidas, null);
        }
        catch (Exception ex) { return (cargadas, omitidas, ex.Message); }
    }

    // ---------- Sincronización ----------

    // Deja Trie, Graph y Union-Find coherentes con lo que hay en la HashTable y en las aristas del Graph.
    // Llamar después de cargar usuarios y relaciones, o tras eliminar algo (el UF no deshace uniones).
    public static (int enTrie, int vertices, int comunidades)
        Reconstruir(HashTable tabla, Trie trie, Graph grafo, UnionFind uf)
    {
        uf.Reiniciar();
        int enTrie = 0;
        foreach (User u in tabla.ObtenerTodos())
        {
            if (trie.Insertar(u.Username).insertada) enTrie++;
            grafo.AgregarVertice(u.Username);
            uf.Agregar(u.Username);
        }
        var ar = grafo.ObtenerAristas();
        for (int i = 0; i < ar.a.Length; i++) uf.Unir(ar.a[i], ar.b[i]);
        return (enTrie, grafo.CantidadVertices, uf.Componentes);
    }
}