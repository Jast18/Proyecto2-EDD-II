using System;

// Grafo no dirigido ponderado. Lista de adyacencia: un LinkedList<Edge> (Auxiliar/) por vértice.
// Los vértices son llaves primarias (Username) que existen en la HashTable.
// Dijkstra usa MinMound (Auxiliar/). Peso: costo = 101 - afinidad => mayor afinidad = camino más corto.
// Nota: Queue<T> se usa SOLO en el BFS (permitido por el enunciado); va con nombre completo
// para no chocar con el LinkedList<T> propio del proyecto.
class Graph
{
    public const int AFINIDAD_MAX = 100;

    private IndiceLlaves indice = new IndiceLlaves();
    private LinkedList<Edge>[] adj = new LinkedList<Edge>[16];
    private bool[] activo = new bool[16];
    private int aristas;
    private int vertices;

    public int CantidadVertices { get { return vertices; } }
    public int CantidadAristas { get { return aristas; } }

    // Costo de Dijkstra: el peso de la arista (fórmula de AC) invertido, siempre > 0.
    public static int Costo(int afinidad) { return AFINIDAD_MAX + 1 - AC.PesoArista(afinidad); }

    private (bool ok, int i) Obt(string llave)
    {
        var r = indice.Buscar(Util.Norm(llave));
        return (r.encontrado && activo[r.indice], r.indice);
    }

    public bool ExisteVertice(string llave) { return Obt(llave).ok; }

    public (bool nuevo, int indice) AgregarVertice(string llave)
    {
        var r = indice.Agregar(Util.Norm(llave));
        if (r.indice >= adj.Length)
        {
            Array.Resize(ref adj, adj.Length * 2);
            Array.Resize(ref activo, activo.Length * 2);
        }
        if (!activo[r.indice])
        {
            activo[r.indice] = true;
            if (adj[r.indice] == null) adj[r.indice] = new LinkedList<Edge>();
            else adj[r.indice].Clear();
            vertices++;
            return (true, r.indice);
        }
        return (false, r.indice);
    }

    private Edge BuscarArista(int u, int v)
    {
        return adj[u].Find(e => e.Destination == v);
    }

    // exito=false si falta algún vértice o es un lazo; actualizada=true si ya existía y solo cambió el peso.
    public (bool exito, bool actualizada) AgregarArista(string a, string b, int afinidad)
    {
        var x = Obt(a); var y = Obt(b);
        if (!x.ok || !y.ok || x.i == y.i) return (false, false);
        afinidad = Math.Max(0, Math.Min(AFINIDAD_MAX, afinidad));

        Edge e = BuscarArista(x.i, y.i);
        if (e != null)
        {
            e.Affinity = afinidad;
            BuscarArista(y.i, x.i).Affinity = afinidad;
            return (true, true);
        }
        adj[x.i].AddLast(new Edge { Destination = y.i, Affinity = afinidad });
        adj[y.i].AddLast(new Edge { Destination = x.i, Affinity = afinidad });
        aristas++;
        return (true, false);
    }

    // Quita la arista u->v; devuelve su afinidad o -1 si no existía.
    private int QuitarDe(int u, int v)
    {
        Edge e = BuscarArista(u, v);
        if (e == null) return -1;
        adj[u].Remove(x => x.Destination == v);
        return e.Affinity;
    }

    public (bool eliminada, int afinidad) EliminarArista(string a, string b)
    {
        var x = Obt(a); var y = Obt(b);
        if (!x.ok || !y.ok) return (false, -1);
        int af = QuitarDe(x.i, y.i);
        if (af < 0) return (false, -1);
        QuitarDe(y.i, x.i);
        aristas--;
        return (true, af);
    }

    public (bool eliminado, int aristasRemovidas) EliminarVertice(string llave)
    {
        var x = Obt(llave);
        if (!x.ok) return (false, 0);
        Edge[] vecinos = adj[x.i].ToArray();
        foreach (Edge e in vecinos) QuitarDe(e.Destination, x.i);
        adj[x.i].Clear();
        activo[x.i] = false;
        vertices--;
        aristas -= vecinos.Length;
        return (true, vecinos.Length);
    }

    public (string[] vecinos, int[] afinidades) Vecinos(string llave)
    {
        var x = Obt(llave);
        if (!x.ok) return (new string[0], new int[0]);
        Edge[] es = adj[x.i].ToArray();
        string[] v = new string[es.Length];
        int[] a = new int[es.Length];
        for (int k = 0; k < es.Length; k++)
        {
            v[k] = indice.Nombre(es[k].Destination);
            a[k] = es[k].Affinity;
        }
        return (v, a);
    }

    // ---------- Recorridos ----------

    // BFS: orden de visita y nivel (distancia en saltos) de cada vértice alcanzado.
    public (string[] orden, int[] niveles) BFS(string origen)
    {
        var s = Obt(origen);
        if (!s.ok) return (new string[0], new int[0]);

        int n = indice.Cantidad;
        bool[] visto = new bool[n];
        int[] nivel = new int[n];
        string[] orden = new string[n];
        int[] niveles = new int[n];
        int c = 0;

        System.Collections.Generic.Queue<int> cola = new System.Collections.Generic.Queue<int>();
        visto[s.i] = true;
        cola.Enqueue(s.i);
        while (cola.Count > 0)
        {
            int u = cola.Dequeue();
            orden[c] = indice.Nombre(u);
            niveles[c] = nivel[u];
            c++;
            foreach (Edge e in adj[u].ToArray())
            {
                if (visto[e.Destination]) continue;
                visto[e.Destination] = true;
                nivel[e.Destination] = nivel[u] + 1;
                cola.Enqueue(e.Destination);
            }
        }
        Array.Resize(ref orden, c);
        Array.Resize(ref niveles, c);
        return (orden, niveles);
    }

    // DFS en preorden: orden de visita y profundidad.
    public (string[] orden, int[] profundidades) DFS(string origen)
    {
        var s = Obt(origen);
        if (!s.ok) return (new string[0], new int[0]);

        int n = indice.Cantidad;
        bool[] visto = new bool[n];
        string[] orden = new string[n];
        int[] prof = new int[n];
        int c = 0;
        DFSRec(s.i, 0, visto, orden, prof, ref c);
        Array.Resize(ref orden, c);
        Array.Resize(ref prof, c);
        return (orden, prof);
    }

    private void DFSRec(int u, int profundidad, bool[] visto, string[] orden, int[] prof, ref int c)
    {
        visto[u] = true;
        orden[c] = indice.Nombre(u);
        prof[c] = profundidad;
        c++;
        foreach (Edge e in adj[u].ToArray())
            if (!visto[e.Destination]) DFSRec(e.Destination, profundidad + 1, visto, orden, prof, ref c);
    }

    // ---------- Dijkstra (camino de conexión más fuerte) ----------

    // existe=false si no hay camino. afinidadPromedio = promedio de afinidad de las aristas del camino.
    public (bool existe, string[] camino, int costoTotal, int afinidadPromedio, int saltos)
        Dijkstra(string origen, string destino)
    {
        var s = Obt(origen); var t = Obt(destino);
        if (!s.ok || !t.ok) return (false, new string[0], 0, 0, 0);
        if (s.i == t.i) return (true, new string[] { indice.Nombre(s.i) }, 0, AFINIDAD_MAX, 0);

        int n = indice.Cantidad;
        int[] dist = new int[n];
        int[] previo = new int[n];
        bool[] listo = new bool[n];
        for (int i = 0; i < n; i++) { dist[i] = int.MaxValue; previo[i] = -1; }

        MinMound heap = new MinMound();
        dist[s.i] = 0;
        heap.Insert(s.i, 0);

        while (heap.Count > 0)
        {
            int d;
            int u = heap.ExtractMin(out d);
            if (listo[u]) continue;          // entrada obsoleta (borrado perezoso)
            listo[u] = true;
            if (u == t.i) break;
            foreach (Edge e in adj[u].ToArray())
            {
                int nd = d + Costo(e.Affinity);
                if (nd < dist[e.Destination])
                {
                    dist[e.Destination] = nd;
                    previo[e.Destination] = u;
                    heap.Insert(e.Destination, nd);
                }
            }
        }

        if (dist[t.i] == int.MaxValue) return (false, new string[0], 0, 0, 0);

        int largo = 1;
        for (int v = t.i; previo[v] != -1; v = previo[v]) largo++;
        string[] camino = new string[largo];
        int sumaAf = 0;
        int pos = largo - 1;
        for (int v = t.i; v != -1; v = previo[v])
        {
            camino[pos--] = indice.Nombre(v);
            if (previo[v] != -1) sumaAf += BuscarArista(previo[v], v).Affinity;
        }
        int saltos = largo - 1;
        return (true, camino, dist[t.i], sumaAf / saltos, saltos);
    }

    // ---------- Sugerencias de amistad ----------

    // Amigos de amigos puntuados por afinidad (min de las dos aristas). Si el usuario no tiene
    // conexiones, devuelve los usuarios más conectados ("sugerencias múltiples").
    public (string[] candidatos, int[] puntajes) SugerirAmigos(string llave, int max = 5)
    {
        var x = Obt(llave);
        if (!x.ok || max <= 0) return (new string[0], new int[0]);

        int n = indice.Cantidad;
        int[] punt = new int[n];
        bool[] excluido = new bool[n];
        excluido[x.i] = true;
        Edge[] directos = adj[x.i].ToArray();
        foreach (Edge e in directos) excluido[e.Destination] = true;

        if (directos.Length > 0)
        {
            foreach (Edge e1 in directos)
                foreach (Edge e2 in adj[e1.Destination].ToArray())
                    if (!excluido[e2.Destination]) punt[e2.Destination] += Math.Min(e1.Affinity, e2.Affinity);
        }
        else
        {
            for (int i = 0; i < n; i++)
                if (activo[i] && !excluido[i]) punt[i] = adj[i].Count;
        }

        string[] cand = new string[max];
        int[] pts = new int[max];
        bool[] tomado = new bool[n];
        int c = 0;
        for (; c < max; c++)
        {
            int mejor = -1;
            for (int i = 0; i < n; i++)
                if (!tomado[i] && punt[i] > 0 && (mejor == -1 || punt[i] > punt[mejor])) mejor = i;
            if (mejor == -1) break;
            tomado[mejor] = true;
            cand[c] = indice.Nombre(mejor);
            pts[c] = punt[mejor];
        }
        Array.Resize(ref cand, c);
        Array.Resize(ref pts, c);
        return (cand, pts);
    }

    // Lista de aristas (una sola vez cada una) para guardar en CSV.
    public (string[] a, string[] b, int[] afinidades) ObtenerAristas()
    {
        string[] a = new string[aristas];
        string[] b = new string[aristas];
        int[] w = new int[aristas];
        int k = 0;
        for (int u = 0; u < indice.Cantidad; u++)
        {
            if (!activo[u]) continue;
            foreach (Edge e in adj[u].ToArray())
            {
                if (u >= e.Destination) continue;
                a[k] = indice.Nombre(u);
                b[k] = indice.Nombre(e.Destination);
                w[k] = e.Affinity;
                k++;
            }
        }
        return (a, b, w);
    }
}