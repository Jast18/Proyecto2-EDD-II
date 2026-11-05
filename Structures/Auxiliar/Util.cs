using System;

// Utilidades compartidas: hash de cadenas e índice auxiliar llave -> posición.
static class Util
{
    // djb2 modificado (xor). Mucho mejor distribuido que sumar los caracteres.
    public static int Hash(string s)
    {
        unchecked
        {
            uint h = 5381;
            foreach (char c in s) h = (h * 33) ^ c;
            return (int)(h & 0x7FFFFFFF);
        }
    }

    // Todas las llaves (usernames) se normalizan igual en TODAS las estructuras.
    public static string Norm(string s)
    {
        return s == null ? null : s.Trim().ToLowerInvariant();
    }
}

// Mapa propio llave(string) -> índice entero consecutivo (0,1,2...).
// Lo usan UnionFind y Grafo para trabajar con arreglos de enteros sin Dictionary.
// Direccionamiento abierto con sondeo lineal; no soporta borrado (no hace falta).
class IndiceLlaves
{
    private int[] tabla;       // -1 = vacío, otro = índice dentro de nombres[]
    private string[] nombres;
    private int cantidad;

    public int Cantidad { get { return cantidad; } }

    public IndiceLlaves(int capacidad = 16)
    {
        tabla = new int[capacidad];
        for (int i = 0; i < tabla.Length; i++) tabla[i] = -1;
        nombres = new string[capacidad];
    }

    public string Nombre(int indice) { return nombres[indice]; }

    public (bool encontrado, int indice) Buscar(string llave)
    {
        if (llave == null) return (false, -1);
        int i = Util.Hash(llave) % tabla.Length;
        while (tabla[i] != -1)
        {
            if (nombres[tabla[i]] == llave) return (true, tabla[i]);
            i = (i + 1) % tabla.Length;
        }
        return (false, -1);
    }

    public (bool nuevo, int indice) Agregar(string llave)
    {
        var r = Buscar(llave);
        if (r.encontrado) return (false, r.indice);

        if ((cantidad + 1) * 2 > tabla.Length) Crecer();
        if (cantidad == nombres.Length) Array.Resize(ref nombres, nombres.Length * 2);

        nombres[cantidad] = llave;
        Colocar(cantidad);
        cantidad++;
        return (true, cantidad - 1);
    }

    private void Colocar(int indiceNombre)
    {
        int i = Util.Hash(nombres[indiceNombre]) % tabla.Length;
        while (tabla[i] != -1) i = (i + 1) % tabla.Length;
        tabla[i] = indiceNombre;
    }

    private void Crecer()
    {
        tabla = new int[tabla.Length * 2];
        for (int i = 0; i < tabla.Length; i++) tabla[i] = -1;
        for (int k = 0; k < cantidad; k++) Colocar(k);
    }
}