//Utilizara la lista de adyacencia podnerada con BFS, DFS y Dijkstra's algorithm para encontrar el camino mas corto entre dos vertices
using System;
using System.Collections.Generic; // SOLO para Queue<T> en el BFS (permitido por el enunciado)

class Arista
{
    public int Destino;
    public int Afinidad;     // 0..100 (mayor = más fuerte)
    public Arista Siguiente;
}

// Grafo no dirigido ponderado con lista de adyacencia (lista enlazada propia por vértice).
// Los vértices son llaves primarias (usernames) que existen en la HashTable.
// Peso de Dijkstra: costo = 101 - afinidad  => mayor afinidad = camino más corto (siempre > 0).
class Grafo
{
    public const int AFINIDAD_MAX = 100;

    private IndiceLlaves indice = new IndiceLlaves();
    private Arista[] adj = new Arista[16];
    private bool[] activo = new bool[16];
    private int[] grado = new int[16];
    private int aristas;
    private int vertices;

    public int CantidadVertices { get { return vertices; } }
    public int CantidadAristas { get { return aristas; } }

    public static int Costo(int afinidad) { return AFINIDAD_MAX + 1 - afinidad; }

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
            Array.Resize(ref grado, grado.Length * 2);
        }
        if (!activo[r.indice])
        {
            activo[r.indice] = true;
            adj[r.indice] = null;
            grado[r.indice] = 0;
            vertices++;
            return (true, r.indice);
        }
        return (false, r.indice);
    }

    private Arista BuscarArista(int u, int v)
    {
        for (Arista e = adj[u]; e != null; e = e.Siguiente)
            if (e.Destino == v) return e;
        return null;
    }

    // exito=false si falta algún vértice o es un lazo; actualizada=true si ya existía y solo cambió el peso.
    public (bool exito, bool actualizada) AgregarArista(string a, string b, int afinidad)
    {
        var x = Obt(a); var y = Obt(b);
        if (!x.ok || !y.ok || x.i == y.i) return (false, false);
        afinidad = Math.Max(0, Math.Min(AFINIDAD_MAX, afinidad));

        Arista e = BuscarArista(x.i, y.i);
        if (e != null)
        {
            e.Afinidad = afinidad;
            BuscarArista(y.i, x.i).Afinidad = afinidad;
            return (true, true);
        }
        adj[x.i] = new Arista { Destino = y.i, Afinidad = afinidad, Siguiente = adj[x.i] };
        adj[y.i] = new Arista { Destino = x.i, Afinidad = afinidad, Siguiente = adj[y.i] };
        grado[x.i]++; grado[y.i]++;
        aristas++;
        return (true, false);
    }

    // Quita la arista u->v; devuelve su afinidad o -1 si no existía.
    private int QuitarDe(int u, int v)
    {
        Arista previo = null;
        for (Arista e = adj[u]; e != null; previo = e, e = e.Siguiente)
        {
            if (e.Destino != v) continue;
            if (previo == null) adj[u] = e.Siguiente; else previo.Siguiente = e.Siguiente;
            grado[u]--;
            return e.Afinidad;
        }
        return -1;
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
        int removidas = 0;
        while (adj[x.i] != null)
        {
            int v = adj[x.i].Destino;
            QuitarDe(x.i, v);
            QuitarDe(v, x.i);
            removidas++;
        }
        activo[x.i] = false;
        vertices--;
        aristas -= removidas;
        return (true, removidas);
    }

    public (string[] vecinos, int[] afinidades) Vecinos(string llave)
    {
        var x = Obt(llave);
        if (!x.ok) return (new string[0], new int[0]);
        string[] v = new string[grado[x.i]];
        int[] a = new int[grado[x.i]];
        int n = 0;
        for (Arista e = adj[x.i]; e != null; e = e.Siguiente)
        {
            v[n] = indice.Nombre(e.Destino);
            a[n] = e.Afinidad;
            n++;
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

        Queue<int> cola = new Queue<int>();
        visto[s.i] = true;
        cola.Enqueue(s.i);
        while (cola.Count > 0)
        {
            int u = cola.Dequeue();
            orden[c] = indice.Nombre(u);
            niveles[c] = nivel[u];
            c++;
            for (Arista e = adj[u]; e != null; e = e.Siguiente)
            {
                if (visto[e.Destino]) continue;
                visto[e.Destino] = true;
                nivel[e.Destino] = nivel[u] + 1;
                cola.Enqueue(e.Destino);
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
        for (Arista e = adj[u]; e != null; e = e.Siguiente)
            if (!visto[e.Destino]) DFSRec(e.Destino, profundidad + 1, visto, orden, prof, ref c);
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

        MinHeap heap = new MinHeap();
        dist[s.i] = 0;
        heap.Insertar(s.i, 0);

        while (heap.Cantidad > 0)
        {
            var (u, d) = heap.Extraer();
            if (listo[u]) continue;          // entrada obsoleta (borrado perezoso)
            listo[u] = true;
            if (u == t.i) break;
            for (Arista e = adj[u]; e != null; e = e.Siguiente)
            {
                int nd = d + Costo(e.Afinidad);
                if (nd < dist[e.Destino])
                {
                    dist[e.Destino] = nd;
                    previo[e.Destino] = u;
                    heap.Insertar(e.Destino, nd);
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
            if (previo[v] != -1) sumaAf += BuscarArista(previo[v], v).Afinidad;
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
        for (Arista e = adj[x.i]; e != null; e = e.Siguiente) excluido[e.Destino] = true;

        if (grado[x.i] > 0)
        {
            for (Arista e1 = adj[x.i]; e1 != null; e1 = e1.Siguiente)
                for (Arista e2 = adj[e1.Destino]; e2 != null; e2 = e2.Siguiente)
                    if (!excluido[e2.Destino]) punt[e2.Destino] += Math.Min(e1.Afinidad, e2.Afinidad);
        }
        else
        {
            for (int i = 0; i < n; i++) if (activo[i] && !excluido[i]) punt[i] = grado[i];
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
            for (Arista e = adj[u]; e != null; e = e.Siguiente)
            {
                if (u >= e.Destino) continue;
                a[k] = indice.Nombre(u);
                b[k] = indice.Nombre(e.Destino);
                w[k] = e.Afinidad;
                k++;
            }
        }
        return (a, b, w);
    }

    // Min-heap binario propio (PriorityQueue<T> está prohibida por el enunciado).
    private class MinHeap
    {
        private int[] vert = new int[16];
        private int[] prio = new int[16];
        private int n;

        public int Cantidad { get { return n; } }

        public void Insertar(int v, int p)
        {
            if (n == vert.Length) { Array.Resize(ref vert, n * 2); Array.Resize(ref prio, n * 2); }
            int i = n++;
            while (i > 0)
            {
                int padre = (i - 1) / 2;
                if (prio[padre] <= p) break;
                vert[i] = vert[padre]; prio[i] = prio[padre];
                i = padre;
            }
            vert[i] = v; prio[i] = p;
        }

        public (int vertice, int prioridad) Extraer()
        {
            int rv = vert[0], rp = prio[0];
            n--;
            if (n > 0)
            {
                int v = vert[n], p = prio[n];
                int i = 0;
                while (true)
                {
                    int h = 2 * i + 1;
                    if (h >= n) break;
                    if (h + 1 < n && prio[h + 1] < prio[h]) h++;
                    if (prio[h] >= p) break;
                    vert[i] = vert[h]; prio[i] = prio[h];
                    i = h;
                }
                vert[i] = v; prio[i] = p;
            }
            return (rv, rp);
        }
    }
}