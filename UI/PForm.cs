//Principal Form
//Menu, buscador, perfil y panelde camino

using System;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using RedSocial.Models;
using RedSocial.Servicios;

namespace RedSocial.UI
{
    /// <summary>
    /// Ventana principal (ver boceto): menú lateral, buscador con autocompletado (Trie),
    /// perfil (Tabla Hash), grafo (Grafo + Union-Find) y panel de camino más fuerte (Dijkstra).
    /// La UI SOLO habla con RedSocialService; nunca toca las estructuras directamente.
    /// </summary>
    public class PForm : Form
    {
        private readonly RedSocialService servicio = new RedSocialService();

        private TextBox txtBuscar, txtOrigen, txtDestino;
        private ListBox lstSugerencias;
        private Label lblUsuario, lblNombre, lblDetalle, lblResultado, lblAmistad, lblEstado;
        private GPanel grafo;
        private Usuario usuarioActual;
        private bool actualizandoTexto; // evita que TextChanged se dispare al poner texto por código

        public PForm()
        {
            Text = "CampusLink - Red Social Universitaria";
            ClientSize = new Size(1360, 860);
            MinimumSize = new Size(1100, 700);
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Tema.Fondo;
            Font = Tema.Normal;

            Panel menu = ConstruirMenuLateral();
            Control contenido = ConstruirContenido();

            var raiz = new TableLayoutPanel();
            raiz.Dock = DockStyle.Fill; raiz.ColumnCount = 2; raiz.RowCount = 1;
            raiz.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230));
            raiz.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            raiz.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            raiz.Controls.Add(menu, 0, 0);
            raiz.Controls.Add(contenido, 1, 0);
            Controls.Add(raiz);

            // Lista flotante de sugerencias del Trie (se coloca sobre todo lo demás)
            lstSugerencias = new ListBox();
            lstSugerencias.Visible = false;
            lstSugerencias.Font = Tema.Normal;
            lstSugerencias.IntegralHeight = false;
            lstSugerencias.Click += (s, e) => ElegirSugerencia();
            lstSugerencias.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { ElegirSugerencia(); e.SuppressKeyPress = true; }
                if (e.KeyCode == Keys.Escape) { lstSugerencias.Visible = false; txtBuscar.Focus(); }
            };
            Controls.Add(lstSugerencias);
            lstSugerencias.BringToFront();

            RefrescarGrafo();
        }

        // =====================  CONSTRUCCIÓN DE LA INTERFAZ  =====================

        private Panel ConstruirMenuLateral()
        {
            var flujo = new FlowLayoutPanel();
            flujo.Dock = DockStyle.Fill;
            flujo.FlowDirection = FlowDirection.TopDown;
            flujo.WrapContents = false;
            flujo.BackColor = Tema.Panel;
            flujo.Padding = new Padding(10, 18, 10, 10);

            var logo = new Label();
            logo.Text = "CampusLink"; logo.Font = Tema.Titulo; logo.ForeColor = Tema.Primario;
            logo.AutoSize = true; logo.Margin = new Padding(8, 0, 0, 16);
            flujo.Controls.Add(logo);

            AgregarOpcion(flujo, "Buscar usuario", () => txtBuscar.Focus());
            AgregarOpcion(flujo, "Registrar usuario", AccionRegistrar);
            AgregarOpcion(flujo, "Agregar relación", AccionRelacion);
            AgregarOpcion(flujo, "Camino más fuerte", () => txtOrigen.Focus());
            AgregarOpcion(flujo, "Comunidades", AccionComunidades);
            AgregarOpcion(flujo, "Recorridos BFS / DFS", AccionRecorrido);
            AgregarOpcion(flujo, "Ver estructuras", AccionVerEstructuras);
            AgregarOpcion(flujo, "Cargar datos (CSV / TXT)", AccionCargar);
            AgregarOpcion(flujo, "Eliminar usuario", AccionEliminar);

            var cont = new Panel(); // contenedor para poder usar el borde derecho
            cont.Dock = DockStyle.Fill; cont.BackColor = Tema.Panel;
            cont.Controls.Add(flujo);
            return cont;
        }

        private void AgregarOpcion(FlowLayoutPanel destino, string texto, Action accion)
        {
            var b = new Button();
            b.Text = texto; b.Width = 205; b.Height = 42;
            b.FlatStyle = FlatStyle.Flat; b.FlatAppearance.BorderSize = 0;
            b.BackColor = Tema.Panel; b.ForeColor = Tema.Texto;
            b.TextAlign = ContentAlignment.MiddleLeft;
            b.Padding = new Padding(10, 0, 0, 0);
            b.Cursor = Cursors.Hand;
            b.Margin = new Padding(0, 0, 0, 4);
            b.FlatAppearance.MouseOverBackColor = Tema.PrimarioClaro;
            b.Click += (s, e) => accion();
            destino.Controls.Add(b);
        }

        private Control ConstruirContenido()
        {
            var contenido = new TableLayoutPanel();
            contenido.Dock = DockStyle.Fill; contenido.ColumnCount = 1; contenido.RowCount = 3;
            contenido.Padding = new Padding(20, 14, 20, 6);
            contenido.RowStyles.Add(new RowStyle(SizeType.Absolute, 86));
            contenido.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            contenido.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));

            // --- Fila 0: buscador ---
            var busqueda = new Panel(); busqueda.Dock = DockStyle.Fill;
            var lbl = new Label();
            lbl.Text = "BUSCAR POR NOMBRE DE USUARIO"; lbl.Font = Tema.Pequena; lbl.ForeColor = Tema.TextoSuave;
            lbl.Left = 0; lbl.Top = 0; lbl.AutoSize = true;
            txtBuscar = new TextBox();
            txtBuscar.Left = 0; txtBuscar.Top = 22; txtBuscar.Height = 34;
            txtBuscar.Font = new Font("Segoe UI", 13f);
            txtBuscar.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtBuscar.Width = 600;
            txtBuscar.PlaceholderText = "Escribe un username… (Enter para abrir el perfil)";
            txtBuscar.TextChanged += AlEscribirBusqueda;
            txtBuscar.KeyDown += AlTeclearBusqueda;
            busqueda.Controls.Add(lbl); busqueda.Controls.Add(txtBuscar);
            busqueda.Resize += (s, e) => txtBuscar.Width = Math.Min(busqueda.Width, 700);
            contenido.Controls.Add(busqueda, 0, 0);

            // --- Fila 1: tres columnas ---
            var principal = new TableLayoutPanel();
            principal.Dock = DockStyle.Fill; principal.ColumnCount = 3; principal.RowCount = 1;
            principal.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 300));
            principal.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            principal.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 330));
            principal.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            principal.Controls.Add(ConstruirPerfil(), 0, 0);
            principal.Controls.Add(ConstruirGrafo(), 1, 0);
            principal.Controls.Add(ConstruirColumnaDerecha(), 2, 0);
            contenido.Controls.Add(principal, 0, 1);

            // --- Fila 2: barra de estado ---
            lblEstado = new Label();
            lblEstado.Dock = DockStyle.Fill; lblEstado.ForeColor = Tema.TextoSuave;
            lblEstado.TextAlign = ContentAlignment.MiddleLeft;
            lblEstado.Text = "Listo. Carga un archivo CSV o registra usuarios para comenzar.";
            contenido.Controls.Add(lblEstado, 0, 2);
            return contenido;
        }

        private FlowLayoutPanel Tarjeta(Padding margen)
        {
            var p = new FlowLayoutPanel();
            p.Dock = DockStyle.Fill; p.FlowDirection = FlowDirection.TopDown; p.WrapContents = false;
            p.BackColor = Tema.Panel; p.BorderStyle = BorderStyle.FixedSingle;
            p.Padding = new Padding(14); p.Margin = margen;
            return p;
        }

        private Label Etiqueta(string texto, Font fuente, Color color, int ancho)
        {
            var l = new Label();
            l.Text = texto; l.Font = fuente; l.ForeColor = color;
            l.AutoSize = true; l.MaximumSize = new Size(ancho, 0);
            l.Margin = new Padding(0, 0, 0, 8);
            return l;
        }

        private Control ConstruirPerfil()
        {
            FlowLayoutPanel t = Tarjeta(new Padding(0, 0, 10, 0));
            t.Controls.Add(Etiqueta("PERFIL · TABLA HASH", Tema.Pequena, Tema.TextoSuave, 260));
            lblUsuario = Etiqueta("Selecciona un usuario", Tema.Titulo, Tema.Texto, 260);
            lblNombre = Etiqueta("", Tema.Normal, Tema.TextoSuave, 260);
            lblDetalle = Etiqueta("Busca un username o haz clic en un nodo del grafo.", Tema.Normal, Tema.Texto, 260);
            t.Controls.Add(lblUsuario); t.Controls.Add(lblNombre); t.Controls.Add(lblDetalle);

            Button bo = Tema.Boton("Usar como origen", false, 260);
            Button bd = Tema.Boton("Usar como destino", false, 260);
            Button br = Tema.Boton("Agregar relación…", true, 260);
            bo.Click += (s, e) => { if (usuarioActual != null) txtOrigen.Text = usuarioActual.Username; };
            bd.Click += (s, e) => { if (usuarioActual != null) txtDestino.Text = usuarioActual.Username; };
            br.Click += (s, e) => AccionRelacion();
            t.Controls.Add(bo); t.Controls.Add(bd); t.Controls.Add(br);
            return t;
        }

        private Control ConstruirGrafo()
        {
            var marco = new Panel();
            marco.Dock = DockStyle.Fill; marco.BackColor = Tema.Panel; marco.BorderStyle = BorderStyle.FixedSingle;
            marco.Padding = new Padding(6); marco.Margin = new Padding(0, 0, 10, 0);
            grafo = new GPanel();
            grafo.Dock = DockStyle.Fill;
            grafo.NodoClic += u => { lstSugerencias.Visible = false; MostrarPerfil(u.Username); };
            grafo.MouseDown += (s, e) => lstSugerencias.Visible = false;
            marco.Controls.Add(grafo);
            return marco;
        }

        private Control ConstruirColumnaDerecha()
        {
            var col = new TableLayoutPanel();
            col.Dock = DockStyle.Fill; col.ColumnCount = 1; col.RowCount = 2;
            col.RowStyles.Add(new RowStyle(SizeType.Percent, 58));
            col.RowStyles.Add(new RowStyle(SizeType.Percent, 42));

            // Camino más fuerte (Dijkstra)
            FlowLayoutPanel c1 = Tarjeta(new Padding(0, 0, 0, 10));
            c1.Controls.Add(Etiqueta("CAMINO MÁS FUERTE · DIJKSTRA", Tema.Pequena, Tema.TextoSuave, 290));
            c1.Controls.Add(Etiqueta("Origen", Tema.Normal, Tema.Texto, 290));
            txtOrigen = new TextBox(); txtOrigen.Width = 290; txtOrigen.Margin = new Padding(0, 0, 0, 8);
            c1.Controls.Add(txtOrigen);
            c1.Controls.Add(Etiqueta("Destino", Tema.Normal, Tema.Texto, 290));
            txtDestino = new TextBox(); txtDestino.Width = 290; txtDestino.Margin = new Padding(0, 0, 0, 10);
            c1.Controls.Add(txtDestino);
            Button calcular = Tema.Boton("Calcular camino", true, 290);
            calcular.Click += (s, e) => CalcularCamino();
            c1.Controls.Add(calcular);
            lblResultado = Etiqueta("", Tema.Normal, Tema.Texto, 290);
            c1.Controls.Add(lblResultado);
            col.Controls.Add(c1, 0, 0);

            // Sugerencias de amistad
            FlowLayoutPanel c2 = Tarjeta(new Padding(0));
            c2.Controls.Add(Etiqueta("SUGERENCIAS DE AMISTAD", Tema.Pequena, Tema.TextoSuave, 290));
            lblAmistad = Etiqueta("Abre un perfil para ver sugerencias.", Tema.Normal, Tema.Texto, 290);
            c2.Controls.Add(lblAmistad);
            col.Controls.Add(c2, 0, 1);
            return col;
        }

        // =====================  BÚSQUEDA / AUTOCOMPLETADO (Trie → Hash)  =====================

        private void AlEscribirBusqueda(object sender, EventArgs e)
        {
            if (actualizandoTexto) return;
            string prefijo = txtBuscar.Text.Trim();
            if (prefijo.Length == 0) { lstSugerencias.Visible = false; return; }

            string[] sugeridos = null;
            try { sugeridos = servicio.Sugerir(prefijo); }
            catch (Exception) { sugeridos = null; } // silencioso: no queremos un mensaje por cada tecla

            if (sugeridos == null || sugeridos.Length == 0) { lstSugerencias.Visible = false; return; }

            lstSugerencias.Items.Clear();
            for (int i = 0; i < sugeridos.Length; i++) lstSugerencias.Items.Add(sugeridos[i]);

            Point p = PointToClient(txtBuscar.Parent.PointToScreen(new Point(txtBuscar.Left, txtBuscar.Bottom)));
            lstSugerencias.Location = p;
            lstSugerencias.Width = txtBuscar.Width;
            lstSugerencias.Height = Math.Min(sugeridos.Length, 7) * lstSugerencias.ItemHeight + 6;
            lstSugerencias.Visible = true;
            lstSugerencias.BringToFront();
        }

        private void AlTeclearBusqueda(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                lstSugerencias.Visible = false;
                MostrarPerfil(txtBuscar.Text.Trim());
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Down && lstSugerencias.Visible && lstSugerencias.Items.Count > 0)
            {
                lstSugerencias.Focus(); lstSugerencias.SelectedIndex = 0;
                e.SuppressKeyPress = true;
            }
            else if (e.KeyCode == Keys.Escape) lstSugerencias.Visible = false;
        }

        private void ElegirSugerencia()
        {
            if (lstSugerencias.SelectedItem == null) return;
            string username = lstSugerencias.SelectedItem.ToString();
            lstSugerencias.Visible = false;
            actualizandoTexto = true; txtBuscar.Text = username; actualizandoTexto = false;
            MostrarPerfil(username);
        }

        private void MostrarPerfil(string username)
        {
            if (username.Length == 0) return;
            Usuario u = null;
            if (!Ejecutar(() => u = servicio.BuscarPerfil(username))) return; // incrementa visitas
            if (u == null) { lblEstado.Text = "No se encontró el usuario '" + username + "'."; return; }

            usuarioActual = u;
            lblUsuario.Text = "@" + u.Username;
            lblNombre.Text = u.Nombre;
            lblDetalle.Text = "Música: " + u.Musica + "\nCarrera: " + u.Carrera + "\nCursos: " + u.Cursos +
                              "\nAño: " + u.Anio + "\nVisitas al perfil: " + u.Visitas;
            grafo.Seleccionar(u.Id);
            lblEstado.Text = "Perfil de @" + u.Username + " localizado (Trie → Tabla Hash).";

            string texto = "Sin sugerencias por ahora.";
            try
            {
                Usuario[] sug = servicio.SugerenciasAmistad(u.Username);
                if (sug != null && sug.Length > 0)
                {
                    var sb = new StringBuilder();
                    for (int i = 0; i < sug.Length; i++)
                        sb.Append("• @").Append(sug[i].Username).Append("  (").Append(sug[i].Visitas).Append(" visitas)\n");
                    texto = sb.ToString();
                }
            }
            catch (Exception) { }
            lblAmistad.Text = texto;
        }

        // =====================  ACCIONES DEL MENÚ  =====================

        private void AccionRegistrar()
        {
            string[] v = DialogoCampos.Pedir(this, "Registrar usuario", new string[]
                { "Username (único)", "Nombre completo", "Música favorita", "Carrera", "Cursos", "Año (número)" });
            if (v == null) return;
            int anio = 0;
            if (v[0].Length == 0 || !int.TryParse(v[5], out anio)) { Aviso("El username es obligatorio y el año debe ser un número."); return; }

            if (Ejecutar(() => servicio.RegistrarUsuario(new Usuario(v[0], v[1], v[2], v[3], v[4], anio))))
            {
                RefrescarGrafo();
                lblEstado.Text = "Usuario @" + v[0] + " registrado (Hash, Trie, Grafo y Union-Find).";
            }
        }

        private void AccionRelacion()
        {
            string[] v = DialogoCampos.Pedir(this, "Agregar relación", new string[]
                { "Usuario A", "Usuario B", "Tipo: A = amistad (ambos sentidos), S = seguir (un sentido)" });
            if (v == null) return;
            if (v[0].Length == 0 || v[1].Length == 0) { Aviso("Escribe ambos usernames."); return; }
            bool dirigida = v[2].ToUpper() == "S";

            if (Ejecutar(() => servicio.AgregarRelacion(v[0], v[1], dirigida)))
            {
                RefrescarGrafo();
                lblEstado.Text = "Relación agregada entre @" + v[0] + " y @" + v[1] + ".";
            }
        }

        private void CalcularCamino()
        {
            string a = txtOrigen.Text.Trim(), b = txtDestino.Text.Trim();
            if (a.Length == 0 || b.Length == 0) { Aviso("Indica origen y destino."); return; }

            ResultadoCamino r = null;
            if (!Ejecutar(() => r = servicio.CaminoMasFuerte(a, b))) return;

            if (r == null || r.Camino == null || r.Camino.Length == 0)
            {
                grafo.ResaltarCamino(null);
                lblResultado.Text = (r != null && !string.IsNullOrEmpty(r.Mensaje))
                    ? r.Mensaje : "No existe un camino entre ambos usuarios.";
                return;
            }
            grafo.ResaltarCamino(r.Camino);
            lblResultado.Text = "Union-Find: misma comunidad.\n" + string.Join(" → ", NombresDe(r.Camino)) +
                                "\nAfinidad promedio: " + r.AfinidadPromedio + "%\nCosto total: " + r.CostoTotal;
            lblEstado.Text = "Camino calculado con Dijkstra.";
        }

        private void AccionComunidades()
        {
            int[][] grupos = null;
            if (!Ejecutar(() => grupos = servicio.ObtenerComunidades())) return;
            grafo.ColorearComunidades(grupos);

            var sb = new StringBuilder();
            if (grupos != null)
                for (int i = 0; i < grupos.Length; i++)
                    sb.Append("Comunidad ").Append(i + 1).Append(": ").Append(string.Join(", ", NombresDe(grupos[i]))).Append("\r\n");
            MostrarTexto("Comunidades (Union-Find)", sb.ToString());
        }

        private void AccionRecorrido()
        {
            string[] v = DialogoCampos.Pedir(this, "Recorrido del grafo", new string[]
                { "Usuario inicial", "Recorrido (BFS o DFS)" });
            if (v == null) return;
            bool bfs = v[1].ToUpper() == "BFS";
            if (!bfs && v[1].ToUpper() != "DFS") { Aviso("Escribe BFS o DFS."); return; }

            int[] orden = null;
            if (!Ejecutar(() => orden = bfs ? servicio.RecorridoBFS(v[0]) : servicio.RecorridoDFS(v[0]))) return;
            if (orden == null || orden.Length == 0) { Aviso("No se encontró el usuario o no tiene conexiones."); return; }
            MostrarTexto("Recorrido " + v[1].ToUpper() + " desde @" + v[0], string.Join(" → ", NombresDe(orden)));
        }

        private void AccionVerEstructuras()
        {
            var sb = new StringBuilder();
            bool ok = Ejecutar(() =>
            {
                Usuario[] us = servicio.ObtenerUsuarios();
                sb.Append("== TABLA HASH (perfiles) ==\r\n");
                for (int i = 0; i < us.Length; i++)
                    sb.Append("[").Append(us[i].Id).Append("] ").Append(us[i].Username).Append(" | ").Append(us[i].Nombre)
                      .Append(" | ").Append(us[i].Musica).Append(" | ").Append(us[i].Carrera).Append(" | ")
                      .Append(us[i].Cursos).Append(" | año ").Append(us[i].Anio).Append(" | visitas ").Append(us[i].Visitas).Append("\r\n");

                sb.Append("\r\n== TRIE (usernames) ==\r\n");
                string[] palabras = servicio.ListarUsernamesTrie();
                sb.Append(string.Join(", ", palabras)).Append("\r\n");

                sb.Append("\r\n== UNION-FIND (comunidades) ==\r\n");
                int[][] grupos = servicio.ObtenerComunidades();
                for (int i = 0; i < grupos.Length; i++)
                    sb.Append("Comunidad ").Append(i + 1).Append(": ").Append(string.Join(", ", NombresDe(grupos[i]))).Append("\r\n");

                sb.Append("\r\n== GRAFO (lista de adyacencia) ==\r\n");
                for (int i = 0; i < us.Length; i++)
                {
                    sb.Append(us[i].Username).Append(" -> ");
                    Arista[] vs = servicio.ObtenerRelaciones(us[i].Id);
                    for (int k = 0; k < vs.Length; k++)
                        sb.Append(NombresDe(new int[] { vs[k].Destino })[0]).Append("(").Append(vs[k].Afinidad).Append("%)  ");
                    sb.Append("\r\n");
                }
            });
            if (ok) MostrarTexto("Contenido de las estructuras", sb.ToString());
        }

        private void AccionCargar()
        {
            string carpeta = Path.Combine(AppContext.BaseDirectory, "Datos");
            string rutaUsuarios, rutaRelaciones = null;

            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "1/2 · Selecciona el archivo de USUARIOS";
                dlg.Filter = "CSV / TXT|*.csv;*.txt";
                if (Directory.Exists(carpeta)) dlg.InitialDirectory = carpeta;
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                rutaUsuarios = dlg.FileName;
            }
            using (var dlg = new OpenFileDialog())
            {
                dlg.Title = "2/2 · Selecciona el archivo de RELACIONES (cancela para omitir)";
                dlg.Filter = "CSV / TXT|*.csv;*.txt";
                dlg.InitialDirectory = Path.GetDirectoryName(rutaUsuarios);
                if (dlg.ShowDialog(this) == DialogResult.OK) rutaRelaciones = dlg.FileName;
            }

            if (Ejecutar(() => servicio.CargarDesdeCsv(rutaUsuarios, rutaRelaciones)))
            {
                RefrescarGrafo();
                lblEstado.Text = "Datos cargados desde " + Path.GetFileName(rutaUsuarios) + ".";
            }
        }

        private void AccionEliminar()
        {
            string[] v = DialogoCampos.Pedir(this, "Eliminar usuario", new string[] { "Username a eliminar" });
            if (v == null || v[0].Length == 0) return;
            if (MessageBox.Show(this, "¿Eliminar a @" + v[0] + " y todas sus relaciones?", "Confirmar",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            bool eliminado = false;
            if (Ejecutar(() => eliminado = servicio.EliminarUsuario(v[0])))
            {
                lblEstado.Text = eliminado ? "Usuario @" + v[0] + " eliminado." : "No se encontró @" + v[0] + ".";
                if (usuarioActual != null && usuarioActual.Username == v[0]) usuarioActual = null;
                RefrescarGrafo();
            }
        }

        // =====================  UTILIDADES  =====================

        /// <summary>Redibuja el grafo con los datos actuales. Es silencioso si la lógica aún no está implementada.</summary>
        private void RefrescarGrafo()
        {
            try
            {
                Usuario[] us = servicio.ObtenerUsuarios();
                grafo.Cargar(us, id => servicio.ObtenerRelaciones(id));
                lblEstado.Text = us.Length + " usuarios en la red.";
            }
            catch (Exception) { grafo.Cargar(new Usuario[0], null); }
        }

        /// <summary>Ejecuta una acción del servicio mostrando avisos amigables si algo falla.</summary>
        private bool Ejecutar(Action accion)
        {
            try { accion(); return true; }
            catch (NotImplementedException)
            {
                MessageBox.Show(this, "Esta operación todavía no está implementada en las estructuras.",
                    "Pendiente", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            return false;
        }

        private void Aviso(string mensaje)
        {
            MessageBox.Show(this, mensaje, "Aviso", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private string[] NombresDe(int[] ids)
        {
            string[] r = new string[ids.Length];
            for (int i = 0; i < ids.Length; i++)
            {
                Usuario u = null;
                try { u = servicio.ObtenerPorId(ids[i]); } catch (Exception) { }
                r[i] = u != null ? u.Username : "#" + ids[i];
            }
            return r;
        }

        /// <summary>Ventana simple de solo lectura para mostrar listados largos.</summary>
        private void MostrarTexto(string titulo, string contenido)
        {
            using (var f = new Form())
            {
                f.Text = titulo; f.Size = new Size(820, 560); f.StartPosition = FormStartPosition.CenterParent;
                var t = new TextBox();
                t.Multiline = true; t.ReadOnly = true; t.ScrollBars = ScrollBars.Both; t.WordWrap = false;
                t.Dock = DockStyle.Fill; t.Font = new Font("Consolas", 10f); t.Text = contenido;
                t.BackColor = Color.White; t.Select(0, 0);
                f.Controls.Add(t);
                f.ShowDialog(this);
            }
        }
    }
}