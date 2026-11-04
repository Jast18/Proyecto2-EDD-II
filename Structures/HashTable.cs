using System;

// Estructura NUCLEAR: guarda el Student completo y lo localiza por su llave primaria (User).
// Direccionamiento abierto, sondeo lineal con módulo (circular), marcas de borrado (tombstones)
// y redimensionado cuando el factor de carga (ocupados + borrados) supera 0.7.
class HashTable
{
    private const byte VACIO = 0, OCUPADO = 1, BORRADO = 2;

    private Student[] tabla;
    private byte[] estado;
    private int capacidad;
    private int cantidad;   // elementos vivos
    private int usados;     // vivos + tombstones (lo que realmente ensucia la tabla)

    public int Cantidad { get { return cantidad; } }
    public int Capacidad { get { return capacidad; } }
    public double FactorCarga { get { return (double)usados / capacidad; } }

    public HashTable(int capacidadInicial = 16)
    {
        capacidad = Math.Max(8, capacidadInicial);
        tabla = new Student[capacidad];
        estado = new byte[capacidad];
    }

    // Devuelve la posición (o -1) y cuántos sondeos hicieron falta.
    private (int pos, int sondeos) Localizar(string user)
    {
        if (user == null) return (-1, 0);
        int i = Util.Hash(user) % capacidad;
        int sondeos = 0;
        for (int n = 0; n < capacidad; n++)
        {
            sondeos++;
            if (estado[i] == VACIO) return (-1, sondeos);
            if (estado[i] == OCUPADO && tabla[i].User == user) return (i, sondeos);
            i = (i + 1) % capacidad;
        }
        return (-1, sondeos);
    }

    private void Redimensionar(int nuevaCapacidad)
    {
        Student[] viejaTabla = tabla;
        byte[] viejoEstado = estado;

        capacidad = nuevaCapacidad;
        tabla = new Student[capacidad];
        estado = new byte[capacidad];
        cantidad = 0;
        usados = 0;

        for (int k = 0; k < viejaTabla.Length; k++)
        {
            if (viejoEstado[k] != OCUPADO) continue;
            int i = Util.Hash(viejaTabla[k].User) % capacidad;
            while (estado[i] != VACIO) i = (i + 1) % capacidad;
            tabla[i] = viejaTabla[k];
            estado[i] = OCUPADO;
            cantidad++;
            usados++;
        }
    }

    // Inserta. Si la llave ya existe NO sobrescribe (devuelve insertado=false y la posición existente).
    public (bool insertado, int indice, int sondeos, bool redimensionado) Insertar(Student s)
    {
        if (s == null || string.IsNullOrWhiteSpace(s.User)) return (false, -1, 0, false);
        s.User = Util.Norm(s.User);

        var existente = Localizar(s.User);
        if (existente.pos >= 0) return (false, existente.pos, existente.sondeos, false);

        bool redim = false;
        if ((usados + 1) * 10 > capacidad * 7)
        {
            // Si hay muchos tombstones basta con limpiar; si hay muchos vivos, se duplica.
            Redimensionar((cantidad + 1) * 2 > capacidad ? capacidad * 2 : capacidad);
            redim = true;
        }

        int i = Util.Hash(s.User) % capacidad;
        int sondeos = 0;
        int primerBorrado = -1;
        while (true)
        {
            sondeos++;
            if (estado[i] == VACIO)
            {
                int pos = primerBorrado >= 0 ? primerBorrado : i;
                if (primerBorrado < 0) usados++;   // reutilizar un tombstone no ensucia más
                tabla[pos] = s;
                estado[pos] = OCUPADO;
                cantidad++;
                return (true, pos, sondeos, redim);
            }
            if (estado[i] == BORRADO && primerBorrado < 0) primerBorrado = i;
            i = (i + 1) % capacidad;
        }
    }

    public (bool encontrado, Student estudiante, int sondeos) Buscar(string user)
    {
        var r = Localizar(Util.Norm(user));
        return r.pos >= 0 ? (true, tabla[r.pos], r.sondeos) : (false, null, r.sondeos);
    }

    public bool Existe(string user)
    {
        return Localizar(Util.Norm(user)).pos >= 0;
    }

    public (bool eliminado, Student estudiante) Eliminar(string user)
    {
        var r = Localizar(Util.Norm(user));
        if (r.pos < 0) return (false, null);
        Student quitado = tabla[r.pos];
        tabla[r.pos] = null;
        estado[r.pos] = BORRADO;   // tombstone: no rompe las cadenas de sondeo de otras llaves
        cantidad--;
        return (true, quitado);
    }

    // Reemplaza los datos manteniendo la llave primaria. Devuelve el registro anterior.
    public (bool editado, Student anterior) Editar(string user, Student nuevo)
    {
        user = Util.Norm(user);
        var r = Localizar(user);
        if (r.pos < 0 || nuevo == null) return (false, null);
        Student anterior = tabla[r.pos];
        nuevo.User = user;                 // la llave primaria no se cambia
        tabla[r.pos] = nuevo;
        return (true, anterior);
    }

    // Contador de visitas (insumo de la recomendación por visitas frecuentes).
    public (bool exito, int visitas) RegistrarVisita(string user)
    {
        var r = Localizar(Util.Norm(user));
        if (r.pos < 0) return (false, 0);
        tabla[r.pos].Visits++;
        return (true, tabla[r.pos].Visits);
    }

    public Student[] ObtenerTodos()
    {
        Student[] res = new Student[cantidad];
        int n = 0;
        for (int i = 0; i < capacidad; i++)
            if (estado[i] == OCUPADO) res[n++] = tabla[i];
        return res;
    }
}