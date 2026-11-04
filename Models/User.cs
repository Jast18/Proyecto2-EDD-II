// Registro principal: toma toda la información del usuario (username, música, carrera, cursos,
// año y contador de visitas). Es, en la práctica, el valor que se almacena en la HashTable.
// Username es la llave primaria que usan TODAS las demás estructuras.
// Id es un identificador único que describe al vértice para el Union-Find (lo asigna la HashTable
// al insertar; no se reutiliza aunque el usuario se elimine).
// Las relaciones (con su peso) no viven aquí: pertenecen al Graph y referencian por Username.
class User
{
    public int Id { get; set; }
    public string Username { get; set; }
    public string Name { get; set; }
    public string Career { get; set; }
    public int Year { get; set; }
    public string Music { get; set; }
    public string[] Courses { get; set; } = new string[0];
    public int Visits { get; set; }   // contador de visitas al perfil
}