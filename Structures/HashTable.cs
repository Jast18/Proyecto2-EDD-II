//Almacena los perfiles por username en base a su propia cadena en funcion hash redimensionado y recorrido
class Student
{
    public string User { get; set; } // Llave primaria, debe ser única
    public string Name { get; set; } // nombre completo del estudiante
    public List<(string FriendNick, int Score)> Affinity { get; set; } = new List<(string, int)>(); // Lista de
    public List<string> Courses { get; set; } = new List<string>();
    public int Year { get; set; }
    public string Career { get; set; }
}

class Node
{
    public Student Value { get; set; }
    public Node Next { get; set; }

    public Node(Student value)
    {
        Value = value;
        Next = null;
    }
}

class HashTable
{
    private Node[] table;
    private int size;

    public HashTable(int size)
    {
        this.size = size;
        table = new Node[size];
    }

    private void resizeTable()
    {
        int newSize = size * 2;
        Node[] newTable = new Node[newSize];

        foreach (var node in table)
        {
            if (node != null)
            {
                int index = Hash(node.Value.User);
                while (newTable[index] != null)
                {
                    index += 1;
                }
                newTable[index] = node;
            }
        }

        table = newTable;
        size = newSize;
    }

    private int CountNull()
    {
        int countNull = 0;
        foreach (var valor in table)
        {
            if (valor == null) countNull += 1;
        }
        return countNull / size;
    }

    // Función hash simple basada en el username
    private int Hash(string key)
    {
        int hashValue = 0;
        foreach (char c in key)
        {
            hashValue += c;
        }
        return hashValue % size;
    }

    // Inserta un estudiante en la tabla hash
    public bool Insert(Student student)
    {
        int index = Hash(student.User);
        for (int i = 0; i < size; i++)
        {
            if (table[index] != null && table[index].Value.User == student.User) return false;
            if (table[index] == null)
            {
                table[index] = new Node(student);
                if (CountNull() <= 0.3) resizeTable();
                return true;
            }
            index += 1;
        }
        return false;
    }

    public Student Search(string user)
    {
        int index = Hash(user);
        for (int i = 0; i < size; i++)
        {
            if (table[index] == null) return null;
            if (table[index].Value.User == user) return table[index].Value;
            index += 1;
        }
        return null;
    }

    public bool Delete(string user)
    {
        int index = Hash(user);
        for (int i = 0; i < size; i++)
        {
            if (table[index] == null) return false;
            if (table[index].Value.User == user)
            {
                table[index] = null;
                return true;
            }
            index += 1;
        }
        return false;
    }

    public List<Student> GetAllStudents()
    {
        List<Student> students = new List<Student>();
        foreach (var node in table)
        {
            if (node != null)
            {
                students.Add(node.Value);
            }
        }
        return students;
    }

    public bool EditStudent(string user, Student updatedStudent)
    {
        int index = Hash(user);
        for (int i = 0; i < size; i++)
        {
            if (table[index] == null) return false;
            if (table[index].Value.User == user)
            {
                table[index].Value = updatedStudent;
                return true;
            }
            index += 1;
        }
        return false;
    }


    class StudentComparer : IEqualityComparer<Student>
    /*
    Función auxiliar para comparar estudiantes por su propiedad User, utilizada para obtener estudiantes únicos y comunes entre listas
    y para filtrar estudiantes con afinidad por encima de un umbral específico.
    */
    {
        public bool Equals(Student x, Student y)
        {
            return x.User == y.User;
        }

        public int GetHashCode(Student obj)
        {
            return obj.User.GetHashCode();
        }

        public bool Contains(List<Student> list, Student student)
        {
            foreach (var item in list)
            {
                if (Equals(item, student))
                {
                    return true;
                }
            }
            return false;
        }

        public List<Student> GetCommonStudents(List<Student> list1, List<Student> list2)
        {
            List<Student> commonStudents = new List<Student>();
            foreach (var student in list1)
            {
                if (Contains(list2, student))
                {
                    commonStudents.Add(student);
                }
            }
            return commonStudents;
        }

        public List<Student> GetStudentsNotInList(List<Student> list1, List<Student> list2)
        {
            List<Student> notInList = new List<Student>();
            foreach (var student in list1)
            {
                if (!Contains(list2, student))
                {
                    notInList.Add(student);
                }
            }
            return notInList;
        }

        public List<Student> GetFriendsWithAffinityAboveThreshold(List<Student> students, int threshold)
        {
            List<Student> result = new List<Student>();
            foreach (var student in students)
            {
                foreach (var affinity in student.Affinity)
                {
                    if (affinity.Score > threshold)
                    {
                        result.Add(student);
                        break;
                    }
                }
            }
            return result;
        }
    }
}