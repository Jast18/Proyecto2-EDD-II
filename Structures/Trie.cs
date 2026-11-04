using System;

// Trie de usernames para autocompletado. Solo guarda llaves (strings); el perfil completo
// se pide después a la HashTable. Sin Dictionary: hijos en arreglo de 128 (ASCII).
// Cada nodo lleva Conteo = cantidad de palabras en su subárbol (permite total y poda O(L)).
class NodoTrie
{
    public NodoTrie[] Hijos = new NodoTrie[128];
    public bool EsFinPalabra;
    public int Conteo;
}

class Trie
{
    private readonly NodoTrie raiz = new NodoTrie();

    public int Cantidad { get { return raiz.Conteo; } }

    private static bool Valida(string p)
    {
        foreach (char c in p) if (c >= 128) return false;
        return true;
    }

    // Devuelve null si el camino no existe.
    private NodoTrie ObtenerNodo(string texto)
    {
        NodoTrie actual = raiz;
        foreach (char c in texto)
        {
            if (c >= 128) return null;
            actual = actual.Hijos[c];
            if (actual == null) return null;
        }
        return actual;
    }

    // Devuelve si se insertó (false si ya existía o es inválida) y cuántos nodos nuevos creó.
    public (bool insertada, int nodosCreados) Insertar(string palabra)
    {
        string p = Util.Norm(palabra);
        if (string.IsNullOrEmpty(p) || !Valida(p)) return (false, 0);
        if (Buscar(p).existe) return (false, 0);

        int creados = 0;
        NodoTrie actual = raiz;
        raiz.Conteo++;
        foreach (char c in p)
        {
            if (actual.Hijos[c] == null) { actual.Hijos[c] = new NodoTrie(); creados++; }
            actual = actual.Hijos[c];
            actual.Conteo++;
        }
        actual.EsFinPalabra = true;
        return (true, creados);
    }

    // existe = palabra completa; esPrefijo = hay al menos una palabra que empieza así.
    public (bool existe, bool esPrefijo) Buscar(string palabra)
    {
        NodoTrie n = ObtenerNodo(Util.Norm(palabra) ?? "");
        return (n != null && n.EsFinPalabra, n != null);
    }

    // Cuántas palabras empiezan con el prefijo.
    public (bool hayCoincidencias, int cantidad) EmpiezaCon(string prefijo)
    {
        NodoTrie n = ObtenerNodo(Util.Norm(prefijo) ?? "");
        return n == null ? (false, 0) : (n.Conteo > 0, n.Conteo);
    }

    // Sugerencias en orden alfabético (máx. 'max'); 'total' es cuántas coinciden en realidad.
    public (string[] sugerencias, int total) Autocompletar(string prefijo, int max = 10)
    {
        string p = Util.Norm(prefijo) ?? "";
        NodoTrie nodo = ObtenerNodo(p);
        if (nodo == null || max <= 0) return (new string[0], 0);

        string[] res = new string[Math.Min(max, nodo.Conteo)];
        int n = 0;
        Recolectar(nodo, p, res, ref n);
        return (res, nodo.Conteo);
    }

    private void Recolectar(NodoTrie nodo, string actual, string[] res, ref int n)
    {
        if (n >= res.Length) return;
        if (nodo.EsFinPalabra) res[n++] = actual;
        for (int i = 0; i < 128; i++)
        {
            if (nodo.Hijos[i] != null) Recolectar(nodo.Hijos[i], actual + (char)i, res, ref n);
            if (n >= res.Length) return;
        }
    }

    // Elimina la palabra y poda las ramas que quedan sin palabras.
    public (bool eliminada, int nodosLiberados) Eliminar(string palabra)
    {
        string p = Util.Norm(palabra);
        if (string.IsNullOrEmpty(p) || !Buscar(p).existe) return (false, 0);

        NodoTrie[] camino = new NodoTrie[p.Length + 1];
        camino[0] = raiz;
        for (int i = 0; i < p.Length; i++) camino[i + 1] = camino[i].Hijos[p[i]];

        for (int i = 0; i < camino.Length; i++) camino[i].Conteo--;
        camino[p.Length].EsFinPalabra = false;

        int liberados = 0;
        for (int i = p.Length; i >= 1; i--)
        {
            if (camino[i].Conteo == 0)
            {
                camino[i - 1].Hijos[p[i - 1]] = null;
                liberados++;
            }
        }
        return (true, liberados);
    }
}