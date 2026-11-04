using System;

// Estructura NUCLEAR: guarda el User completo y lo localiza por su llave primaria (Username).
// Encadenamiento separado: cada casilla es una LinkedList<User> (la del proyecto, en Auxiliar/).
// Se redimensiona (x2) cuando el factor de carga supera 0.75.
class HashTable
{
    private LinkedList<User>[] tabla;
    private int capacidad;
    private int cantidad;
    private int siguienteId;   // contador de Id únicos (nunca se reutilizan)

    public int Cantidad { get { return cantidad; } }
    public int Capacidad { get { return capacidad; } }
    public double FactorCarga { get { return (double)cantidad / capacidad; } }

    public HashTable(int capacidadInicial = 16)
    {
        capacidad = Math.Max(8, capacidadInicial);
        tabla = new LinkedList<User>[capacidad];
    }

    private void Redimensionar()
    {
        LinkedList<User>[] vieja = tabla;
        capacidad *= 2;
        tabla = new LinkedList<User>[capacidad];
        foreach (LinkedList<User> cadena in vieja)
        {
            if (cadena == null) continue;
            foreach (User u in cadena.ToArray())
            {
                int i = Util.Hash(u.Username) % capacidad;
                if (tabla[i] == null) tabla[i] = new LinkedList<User>();
                tabla[i].AddLast(u);
            }
        }
    }

    // Inserta. Si la llave ya existe NO sobrescribe. largoCadena = tamaño de la cadena donde quedó.
    public (bool insertado, int indice, int largoCadena, bool redimensionado) Insertar(User u)
    {
        if (u == null || string.IsNullOrWhiteSpace(u.Username)) return (false, -1, 0, false);
        string key = Util.Norm(u.Username);
        u.Username = key;

        int i = Util.Hash(key) % capacidad;
        if (tabla[i] != null && tabla[i].Find(x => x.Username == key) != null)
            return (false, i, tabla[i].Count, false);

        bool redim = false;
        if ((cantidad + 1) * 4 > capacidad * 3)
        {
            Redimensionar();
            redim = true;
            i = Util.Hash(key) % capacidad;
        }

        if (tabla[i] == null) tabla[i] = new LinkedList<User>();
        u.Id = siguienteId++;
        tabla[i].AddLast(u);
        cantidad++;
        return (true, i, tabla[i].Count, redim);
    }

    public (bool encontrado, User usuario, int largoCadena) Buscar(string username)
    {
        string key = Util.Norm(username);
        if (key == null) return (false, null, 0);
        int i = Util.Hash(key) % capacidad;
        if (tabla[i] == null) return (false, null, 0);
        User u = tabla[i].Find(x => x.Username == key);
        return (u != null, u, tabla[i].Count);
    }

    public bool Existe(string username)
    {
        return Buscar(username).encontrado;
    }

    public (bool eliminado, User usuario) Eliminar(string username)
    {
        string key = Util.Norm(username);
        var r = Buscar(key);
        if (!r.encontrado) return (false, null);
        tabla[Util.Hash(key) % capacidad].Remove(x => x.Username == key);
        cantidad--;
        return (true, r.usuario);
    }

    // Reemplaza los datos manteniendo la llave primaria. Devuelve el registro anterior.
    public (bool editado, User anterior) Editar(string username, User nuevo)
    {
        string key = Util.Norm(username);
        var r = Buscar(key);
        if (!r.encontrado || nuevo == null) return (false, null);
        LinkedList<User> cadena = tabla[Util.Hash(key) % capacidad];
        cadena.Remove(x => x.Username == key);
        nuevo.Username = key;                 // la llave primaria no se cambia
        nuevo.Id = r.usuario.Id;              // ni el Id del vértice
        cadena.AddLast(nuevo);
        return (true, r.usuario);
    }

    // Contador de visitas (insumo de la recomendación por visitas frecuentes).
    public (bool exito, int visitas) RegistrarVisita(string username)
    {
        var r = Buscar(username);
        if (!r.encontrado) return (false, 0);
        r.usuario.Visits++;
        return (true, r.usuario.Visits);
    }

    public User[] ObtenerTodos()
    {
        User[] res = new User[cantidad];
        int n = 0;
        foreach (LinkedList<User> cadena in tabla)
        {
            if (cadena == null) continue;
            foreach (User u in cadena.ToArray()) res[n++] = u;
        }
        return res;
    }
}