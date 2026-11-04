//Agrupa los elementos en conjuntos disjuntos y permite operaciones de unión y búsqueda eficiente en base as comprension de caminos y union por rango
using System;

// Conjuntos disjuntos sobre llaves (usernames): comunidades / validación de conectividad.
// Compresión de caminos (iterativa) + unión por rango. Casi O(1) amortizado (α(n)).
// Union-Find no soporta "deshacer" uniones: si se elimina una relación, se usa Reiniciar()
// y se reconstruye desde el Grafo (ver Persistencia.Reconstruir).
class UnionFind
{
    private IndiceLlaves indice = new IndiceLlaves();
    private int[] padre = new int[16];
    private int[] rango = new int[16];
    private int[] tam = new int[16];
    private int componentes;

    public int Componentes { get { return componentes; } }
    public int Cantidad { get { return indice.Cantidad; } }

    public void Reiniciar()
    {
        indice = new IndiceLlaves();
        padre = new int[16]; rango = new int[16]; tam = new int[16];
        componentes = 0;
    }

    public (bool nuevo, int indice) Agregar(string llave)
    {
        var r = indice.Agregar(Util.Norm(llave));
        if (r.nuevo)
        {
            if (r.indice >= padre.Length)
            {
                Array.Resize(ref padre, padre.Length * 2);
                Array.Resize(ref rango, rango.Length * 2);
                Array.Resize(ref tam, tam.Length * 2);
            }
            padre[r.indice] = r.indice;
            rango[r.indice] = 0;
            tam[r.indice] = 1;
            componentes++;
        }
        return r;
    }

    // Find con compresión de caminos: dos pasadas, sin recursión.
    private int Raiz(int i)
    {
        int r = i;
        while (padre[r] != r) r = padre[r];
        while (padre[i] != r) { int sig = padre[i]; padre[i] = r; i = sig; }
        return r;
    }

    public (bool existe, string raiz, int tamano) Buscar(string llave)
    {
        var r = indice.Buscar(Util.Norm(llave));
        if (!r.encontrado) return (false, null, 0);
        int raiz = Raiz(r.indice);
        return (true, indice.Nombre(raiz), tam[raiz]);
    }

    // exito=false solo si alguna llave no existe. yaConectados=true si ya estaban en el mismo conjunto.
    public (bool exito, bool yaConectados, string raiz, int tamano) Unir(string a, string b)
    {
        var ra = indice.Buscar(Util.Norm(a));
        var rb = indice.Buscar(Util.Norm(b));
        if (!ra.encontrado || !rb.encontrado) return (false, false, null, 0);

        int x = Raiz(ra.indice), y = Raiz(rb.indice);
        if (x == y) return (true, true, indice.Nombre(x), tam[x]);

        if (rango[x] < rango[y]) { int t = x; x = y; y = t; }   // x = raíz de mayor rango
        padre[y] = x;
        tam[x] += tam[y];
        if (rango[x] == rango[y]) rango[x]++;
        componentes--;
        return (true, false, indice.Nombre(x), tam[x]);
    }

    // Validación previa a Dijkstra: ¿están en la misma comunidad?
    public (bool conectados, bool ambosExisten, string raiz) Conectados(string a, string b)
    {
        var ra = indice.Buscar(Util.Norm(a));
        var rb = indice.Buscar(Util.Norm(b));
        if (!ra.encontrado || !rb.encontrado) return (false, false, null);
        int x = Raiz(ra.indice), y = Raiz(rb.indice);
        return (x == y, true, x == y ? indice.Nombre(x) : null);
    }

    // Todas las comunidades como arreglos de usernames.
    public (string[][] grupos, int cantidad) Comunidades()
    {
        int n = indice.Cantidad;
        int[] grupoDeRaiz = new int[n];
        for (int i = 0; i < n; i++) grupoDeRaiz[i] = -1;
        int[] miGrupo = new int[n];
        int[] conteo = new int[Math.Max(componentes, 1)];
        int g = 0;

        for (int i = 0; i < n; i++)
        {
            int r = Raiz(i);
            if (grupoDeRaiz[r] == -1) grupoDeRaiz[r] = g++;
            miGrupo[i] = grupoDeRaiz[r];
            conteo[miGrupo[i]]++;
        }

        string[][] grupos = new string[g][];
        for (int k = 0; k < g; k++) grupos[k] = new string[conteo[k]];
        int[] pos = new int[g];
        for (int i = 0; i < n; i++) grupos[miGrupo[i]][pos[miGrupo[i]]++] = indice.Nombre(i);
        return (grupos, g);
    }
}