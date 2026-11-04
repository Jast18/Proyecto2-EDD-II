// Dijkstra's algorithm
// La arista guarda lo que el algoritmo de Dijkstra necesita (ver Graph.Dijkstra): el vértice destino y la
// afinidad, de la que sale el costo = 101 - AC.PesoArista(afinidad) (más afinidad = camino más corto).
// Arista de la lista de adyacencia del Graph.
// Destination es el índice interno del vértice destino; Affinity va de 0 a 100 (mayor = más fuerte).
// Es class (no struct) para que LinkedList<Edge>.Find devuelva null cuando no encuentra nada.
class Edge
{
    public int Destination;
    public int Affinity;
}