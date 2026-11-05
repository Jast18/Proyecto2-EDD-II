CampusLink — Red Social Universitaria
CampusLink es una aplicación de escritorio modular desarrollada en C# (.NET 8 con Windows Forms) diseñada para la gestión, análisis e interconexión de miembros dentro de una red social universitaria. El sistema destaca por la implementación de sus estructuras de datos lógicas de manera nativa y completamente desde cero (sin el uso de colecciones o librerías estándar como List<T>, Dictionary, HashSet o LINQ), garantizando una gestión óptima de memoria y un control riguroso sobre la complejidad algorítmica de cada operación.

Tecnologías y Requisitos
Lenguaje: C# 12
Framework: .NET 8.0 (net8.0-windows)
Interfaz Gráfica: Windows Forms (WinForms)
Persistencia: Archivos delimitados por comas (.csv)
Entorno Recomendado: Visual Studio 2022 o VS Code con el SDK de .NET 8

Arquitectura del Sistema
La solución está estructurada bajo una arquitectura por capas que mantiene una estricta separación entre la interfaz gráfica, la lógica del servicio orquestador y los algoritmos fundamentales de las estructuras de datos:
Proyecto2-EDD-II/
├── Models/              # Clases de dominio (User, Edge)
├── Structures/          # Estructuras principales (HashTable, Trie, Graph, UnionFind)
│   └── Auxiliar/        # LinkedList propia, MinMound, Util e IndiceLlaves
├── Services/            # Lógica orquestadora (SNS, AC, ChargeCSV)
├── UI/                  # Capa de presentación (PForm, GPanel, Tema, Dialogocampos)
├── Data/                # Archivos CSV para persistencia (user.csv, relations.csv)
└── Trials/              # Módulo de pruebas por consola (trial.cs)
Regla de Comunicación de la Capa de Presentación
La interfaz gráfica (PForm) se comunica exclusivamente con la capa orquestadora SNS (Social Network System). Toda modificación que impacte a más de una estructura de datos es ejecutada y coordinada centralmente dentro de SNS, evitando inconsistencias entre el índice de nombres, el registro principal, el grafo de relaciones y los conjuntos disjuntos de comunidades.

Estructuras de Datos e Implementación Lógica
Para cumplir con la restricción de evitar colecciones genéricas nativas y sintaxis LINQ, todas las estructuras fueron construidas mediante nodos y arreglos de dimensión dinámica:
Tabla Hash (HashTable)
Propósito: Fuente de verdad primaria donde reside el perfil completo de los usuarios (User).
Implementación: Encadenamiento separado mediante una clase LinkedList<T> propia. Capacidad inicial de 16 posiciones.
Redimensionamiento: Factor de carga =0.75. Al superarse este umbral, se duplica la capacidad (2) y se redistribuyen las llaves.
Función Hash: Algoritmo djb2 modificado con operador XOR.
Árbol Trie (Trie)
Propósito: Autocompletado rápido de nombres de usuario para la barra de búsqueda en la interfaz gráfica.
Implementación: Nodos (NodoTrie) con arreglos estáticos de 128 referencias ASCII, bandera EsFinPalabra y un contador de subárbol (Conteo) que facilita la poda eficiente y el ordenamiento alfabético automático.
Grafo Ponderado no Dirigido (Graph)
Propósito: Modelado de las relaciones de amistad interpersonales y cálculo de afinidades.
Implementación: Lista de adyacencia basada en arreglos indexados por la clase auxiliar IndiceLlaves y LinkedList<Edge>.
Borrado Lógico: Arreglo auxiliar activo[] que deshabilita vértices sin necesidad de reindexar la estructura.
Conjuntos Disjuntos (UnionFind)
Propósito: Detección de comunidades conexas y validación previa de conectividad entre dos vértices antes de ejecutar el algoritmo de Dijkstra.
Optimización: Búsqueda con compresión de caminos iterativa (sin recursión) y unión por rango.
Montículo Mínimo (MinMound)
Propósito: Cola de prioridad binaria utilizada de manera exclusiva por el algoritmo de Dijkstra.
Implementación: Dos arreglos paralelos (vertices y costs) con operaciones HeapifyUp y HeapifyDown.
Lista Enlazada Simple (LinkedList<T>)
Propósito: Encadenamiento de colisiones en la Tabla Hash y listas de adyacencia en el Grafo.
Implementación: Punteros head y tail con operaciones AddLast, RemoveFirst, Find, Remove, ToArray y Clear.
Calculadora de Afinidad (AC)
Evalúa 4 criterios ponderados a 25 puntos cada uno (máximo 100%): misma música, misma carrera, mismo año y proporción de cursos compartidos.
Transforma la afinidad al costo de arista para Dijkstra mediante la fórmula: $$\text{Costo} = 101 - \text{PesoArista}(\text{Afinidad})$$ Donde a mayor afinidad, menor es el costo del camino.

Análisis de Complejidad (Big-O)
A continuación se resumen los costos temporales y espaciales de las estructuras y operaciones principales del sistema:
Estructura / Operación
Caso Promedio
Peor Caso
Complejidad Espacial
Observaciones
Tabla Hash (Insertar / Buscar)
$\mathcal{O}(L)$
$\mathcal{O}(n \cdot L)$
$\mathcal{O}(n + C)$
L = Longitud del username, C = Capacidad.
Trie (Autocompletar)
$\mathcal{O}(L + M \cdot L_{\max})$
$\mathcal{O}(\Sigma \cdot N)$
$\mathcal{O}(\Sigma \cdot N)$
M = Límite de sugerencias, =128 ASCII.
Union-Find (Buscar / Unir)
$\mathcal{O}(L + \alpha(n))$
$\mathcal{O}(L + \alpha(n))$
$\mathcal{O}(n)$
(n) es la inversa de Ackermann (4).
Grafo (BFS / DFS)
$\mathcal{O}(V + E)$
$\mathcal{O}(V + E)$
$\mathcal{O}(V + E)$
V = Vértices activos/inactivos, E = Aristas.
Grafo (Dijkstra)
$\mathcal{O}((V + E) \log V)$
$\mathcal{O}((V + E) \log V)$
$\mathcal{O}(V + E)$
Usa MinMound con borrado perezoso.
SNS (Registrar Usuario)
$\mathcal{O}(L)$
$\mathcal{O}(n \cdot L)$
—
Coordina el registro paralelo en Hash, Trie, Grafo y UF.
SNS (Reconstruir)
$\mathcal{O}(n \cdot L + E)$
$\mathcal{O}(n \cdot L + E \cdot \alpha(n))$
—
Se ejecuta tras eliminar usuario/relación al no poder deshacer uniones en UF.


Interfaz Gráfica (UI)
La interfaz gráfica se encuentra desarrollada sobre Windows Forms integrando componentes dinámicos y dibujado de gráficos:
Menú Lateral: Brinda accesos directos a las acciones del sistema (Buscar, Registrar, Crear Relaciones, Calcular Camino, Ver Comunidades, Cargar/Guardar CSV y Eliminar).
Buscador Inteligente: Integra una ventana flotante conectada al Trie que sugiere nombres en tiempo real a medida que se ingresa texto.
Panel de Perfil (PForm): Despliega el resumen del usuario, el conteo de visitas y las sugerencias de amistad generadas.
Renderizador del Grafo (GPanel): Dibuja los nodos distribuidos circularmente mediante GDI+.
Las conexiones (aristas) muestran líneas paralelas codificadas por colores según la afinidad compartida (Música: gris, Carrera: verde, Cursos: azul, Año: naranja).
El camino más fuerte (Dijkstra) se resalta visualmente con un trazado discontinuo.
Las comunidades (UnionFind) se diferencian coloreando los nodos con una paleta cromática pastel.

Persistencia de Datos (ChargeCSV)
La persistencia de datos utiliza archivos de texto plano CSV situados en el directorio Data/:
user.csv: Almacena la estructura username,name,career,year,music,courses,visits (los cursos se separan por ;).
relations.csv: Almacena las conexiones user1,user2,affinity.
Nota: Únicamente se almacenan las entidades base. Las estructuras derivadas (Trie y UnionFind) se reconstruyen dinámicamente en memoria durante el proceso de carga para evitar redundancia de datos.

Ejecución del Proyecto
Modos de Ejecución
El sistema dispone de un punto de entrada configurable en Program.cs:
Modo Gráfico (Predeterminado):

dotnet run

Inicializa la interfaz gráfica completa con Windows Forms (PForm).

Modo Pruebas por Consola (CLI):

dotnet run -- --trials

Abre el menú interactivo por consola en trial.cs con 15 opciones para probar las estructuras de datos individualmente o ejecutar la prueba automatizada en memoria sin modificar los archivos CSV.
