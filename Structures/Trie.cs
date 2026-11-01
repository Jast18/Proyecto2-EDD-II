//Trie data structure for efficient string searching
class NodoTrie
{
    public Dictionary<char, NodoTrie> Hijos { get; } = new Dictionary<char, NodoTrie>();
    public bool EsFinPalabra { get; set; } = false;
}

class Trie
{
    private readonly NodoTrie raiz = new NodoTrie();

    // Inserta una palabra en el Trie
    public void Insertar(string palabra)
    {
        NodoTrie actual = raiz;

        foreach (char c in palabra)
        {
            if (!actual.Hijos.ContainsKey(c))
            {
                actual.Hijos[c] = new NodoTrie();
            }
            actual = actual.Hijos[c];
        }

        actual.EsFinPalabra = true;
    }

    // Devuelve true solo si la palabra completa existe en el Trie
    public bool Buscar(string palabra)
    {
        NodoTrie nodo = ObtenerNodo(palabra);
        return nodo != null && nodo.EsFinPalabra;
    }

    // Devuelve true si existe al menos una palabra que empiece con el prefijo
    public bool EmpiezaCon(string prefijo)
    {
        return ObtenerNodo(prefijo) != null;
    }

    // Recorre el Trie siguiendo los caracteres; devuelve null si el camino no existe
    private NodoTrie ObtenerNodo(string texto)
    {
        NodoTrie actual = raiz;

        foreach (char c in texto)
        {
            if (!actual.Hijos.TryGetValue(c, out actual))
            {
                return null;
            }
        }

        return actual;
    }
}
