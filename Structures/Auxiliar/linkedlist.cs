//<List> Porhibida por eso la implementaremos
//// Archivo: linkedlist.cs | Responsable: [tu nombre]
// Lista enlazada simple y genérica hecha desde cero.
// PROHIBIDO usar List<T>, LinkedList<T>, Dictionary, HashSet ni LINQ (using System.Linq).
// Solo se permiten arreglos T[] como apoyo.
// La usan HashTable (cadena de colisiones) y Graph (lista de adyacencia).

////Clase genérica LinkedList<T> c

class LinkedList<T>
{
    private class Node
    {
        public T Data;
        public Node Next;

        public Node(T data)
        {
            Data = data;
            Next = null;
        }
    }

    private Node head;
    private Node tail;
    public int Count { get; private set; }

    public LinkedList()
    {
        head = null;
        tail = null;
        Count = 0;
    }

    // Implementa los métodos necesarios para agregar, eliminar y buscar elementos en la lista.

    //// Método AddLast(T data): crea un Node nuevo, y si la lista está vacía
// head y tail apuntan a él; si no, tail.Next apunta al nuevo y tail se actualiza.
// Incrementa Count. Complejidad O(1).
    public void AddLast(T data)
    {
        Node newNode = new Node(data);
        if (head == null)
        {
            head = newNode;
            tail = newNode;
        }
        else
        {
            tail.Next = newNode;
            tail = newNode;
        }
        Count++;
    }

    //// Método RemoveFirst(): si la lista está vacía lanza InvalidOperationException.
    // Método Find(Predicate<T> criteria) que devuelve el primer elemento T que cumpla
// el criterio, recorriendo desde head. Si no hay ninguno devuelve default(T).
// Complejidad O(n).
    public void RemoveFirst()
    {
        if (head == null)
        {
            throw new InvalidOperationException("La lista está vacía.");
        }
        head = head.Next;
        if (head == null)
        {
            tail = null;
        }
        Count--;
    }

    public T Find(Predicate<T> criteria)
    {
        Node current = head;
        while (current != null)
        {
            if (criteria(current.Data))
            {
                return current.Data;
            }
            current = current.Next;
        }
        return default(T);
    }

    // Método Remove(Predicate<T> criteria) que elimina el primer nodo que cumpla el criterio
// y devuelve true si lo eliminó. Hay que manejar tres casos:
// 1) eliminar head, 2) eliminar un nodo intermedio, 3) eliminar tail (actualizar tail).
// Mantén un puntero previous mientras recorres. Decrementa Count. Complejidad O(n).
    public bool Remove(Predicate<T> criteria)
    {
        Node current = head;
        Node previous = null;

        while (current != null)
        {
            if (criteria(current.Data))
            {
                if (previous == null) // Caso 1: eliminar head
                {
                    head = current.Next;
                    if (head == null) // Si la lista queda vacía, actualizar tail
                    {
                        tail = null;
                    }
                }
                else // Caso 2 y 3: eliminar nodo intermedio o tail
                {
                    previous.Next = current.Next;
                    if (current.Next == null) // Caso 3: eliminar tail
                    {
                        tail = previous;
                    }
                }
                Count--;
                return true; // Nodo eliminado
            }
            previous = current;
            current = current.Next;
        }
        return false; // No se encontró el nodo a eliminar
    }

    // Método ToArray() que devuelve un T[] del tamaño de Count con todos los elementos
// en orden, recorriendo desde head. Complejidad O(n).
    public T[] ToArray()
    {
        T[] array = new T[Count];
        Node current = head;
        int index = 0;

        while (current != null)
        {
            array[index++] = current.Data;
            current = current.Next;
        }

        return array;
    }

    // Método Clear() que deja la lista vacía: head y tail en null y Count en 0.

    public void Clear()
    {
        head = null;
        tail = null;
        Count = 0;
    }
}