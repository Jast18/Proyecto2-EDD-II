//Priority Queue prohibida por eso la implementaremos
// Archivo: m_mound.cs | Responsable: [tu nombre]
// Montículo mínimo (min-heap) hecho desde cero con arreglos.
// PROHIBIDO usar PriorityQueue, SortedSet, List<T> ni LINQ.
// Lo usa Graph.Dijkstra para sacar siempre el vértice con menor costo acumulado.
// Se guarda en dos arreglos paralelos: int[] vertices y int[] costs.

// clase MinMound con dos arreglos privados int[] vertices e int[] costs,


class MinMound
{
    private int[] vertices;
    private int[] costs;
    public int Count { get; private set; }

    public MinMound(int capacity = 16)
    {
        vertices = new int[capacity];
        costs = new int[capacity];
        Count = 0;
    }

    // Implementa los métodos necesarios para insertar, eliminar y mantener la propiedad del montículo mínimo.

    private int Parent(int i) => (i - 1) / 2;
    private int LeftChild(int i) => 2 * i + 1;
    private int RightChild(int i) => 2 * i + 2;

    // Métodos privados Parent(int i), LeftChild(int i) y RightChild(int i)
    // // para un árbol binario guardado en arreglo: padre = (i-1)/2, izquierdo = 2*i+1, derecho = 2*i+2.
    // Método privado Swap(int i, int j) que intercambia las posiciones i y j
// en los dos arreglos (vertices y costs) a la vez.
    private void Swap(int i, int j)
    {
        int tempVertex = vertices[i];
        vertices[i] = vertices[j];
        vertices[j] = tempVertex;

        int tempCost = costs[i];
        costs[i] = costs[j];
        costs[j] = tempCost;
    }

    // Método privado Resize() que duplica la capacidad cuando el arreglo está lleno:
// crea arreglos nuevos del doble de tamaño, copia los elementos y reemplaza los viejos.
    private void Resize()
    {
        int newCapacity = vertices.Length * 2;
        int[] newVertices = new int[newCapacity];
        int[] newCosts = new int[newCapacity];

        for (int i = 0; i < Count; i++)
        {
            newVertices[i] = vertices[i];
            newCosts[i] = costs[i];
        }

        vertices = newVertices;
        costs = newCosts;
    }

    // Método Insert(int vertex, int cost): si el arreglo está lleno llama Resize(),
// pone el nuevo elemento al final (posición Count), incrementa Count y llama HeapifyUp.
// Complejidad O(log n).
    public void Insert(int vertex, int cost)
    {
        if (Count == vertices.Length)
        {
            Resize();
        }

        vertices[Count] = vertex;
        costs[Count] = cost;
        Count++;
        HeapifyUp(Count - 1);
    }

    // Método HeapifyUp(int index): mientras el costo del nodo actual sea menor que el de su padre,
    // Método privado HeapifyUp(int i): mientras i > 0 y el costo del hijo sea menor
// que el costo de su padre, hace Swap con el padre y sube i a la posición del padre.
    private void HeapifyUp(int index)
    {
        while (index > 0 && costs[index] < costs[Parent(index)])
        {
            Swap(index, Parent(index));
            index = Parent(index);
        }
    }

    // Método RemoveMin(): si Count == 0 lanza InvalidOperationException,
    // guarda el vértice y costo del mínimo (posición 0), reemplaza la posición 0

    // Método ExtractMin(out int cost) que devuelve el vértice con menor costo (posición 0)
// y deja su costo en el parámetro out. Mueve el último elemento a la posición 0,
// decrementa Count y llama HeapifyDown(0). Complejidad O(log n).
public int ExtractMin(out int cost)
    {
        if (Count == 0)
        {
            throw new InvalidOperationException("El montículo está vacío.");
        }

        int minVertex = vertices[0];
        cost = costs[0];

        vertices[0] = vertices[Count - 1];
        costs[0] = costs[Count - 1];
        Count--;
        HeapifyDown(0);

        return minVertex;
    }

    // Método privado HeapifyDown(int i): mientras el hijo izquierdo exista,
    // encuentra el hijo con menor costo, si el costo del hijo es menor que el del padre,
    // hace Swap con el hijo y baja i a la posición del hijo.
    private void HeapifyDown(int index)
    {
        while (LeftChild(index) < Count)
        {
            int smallestChildIndex = LeftChild(index);
            int rightChildIndex = RightChild(index);

            if (rightChildIndex < Count && costs[rightChildIndex] < costs[smallestChildIndex])
            {
                smallestChildIndex = rightChildIndex;
            }

            if (costs[index] <= costs[smallestChildIndex])
            {
                break;
            }

            Swap(index, smallestChildIndex);
            index = smallestChildIndex;
        }
    }

    private void IsEmpty()
    {
        if (Count == 0)
        {
            throw new InvalidOperationException("El montículo está vacío.");
        }
    }
}
