//Graph Panel

//Posible graficación del grafo en interfaz

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using RedSocial.Models;

namespace RedSocial.UI
{
    /// <summary>
    /// Dibuja el grafo con GDI+. No conoce las estructuras: solo recibe usuarios y una función
    /// que devuelve los vecinos de un Id. Cada criterio compartido se dibuja como una línea de color
    /// (multigrafo, como en el boceto). Las relaciones dirigidas llevan flecha.
    /// </summary>
    public class GPanel : Panel
    {
        private const float R = 24f; // radio del nodo

        private Usuario[] usuarios = new Usuario[0];
        private Func<int, Arista[]> obtenerVecinos;
        private int[] indicePorId = new int[0];
        private int[] camino;        // Ids del camino de Dijkstra a resaltar
        private int[] comunidadDe;   // comunidadDe[id] = número de comunidad (o -1)
        private int seleccionado = -1;

        private static readonly Color[] Paleta =
        {
            Color.FromArgb(201, 214, 245), Color.FromArgb(200, 232, 214), Color.FromArgb(250, 224, 190),
            Color.FromArgb(235, 210, 240), Color.FromArgb(252, 214, 214), Color.FromArgb(215, 235, 240),
            Color.FromArgb(240, 238, 190), Color.FromArgb(220, 220, 220)
        };

        /// <summary>Se dispara al hacer clic sobre un nodo.</summary>
        public event Action<Usuario> NodoClic;

        public GPanel()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            BackColor = Tema.Panel;
        }

        // ---------- API pública ----------
        public void Cargar(Usuario[] lista, Func<int, Arista[]> vecinos)
        {
            usuarios = lista ?? new Usuario[0];
            obtenerVecinos = vecinos;
            camino = null; comunidadDe = null;

            int max = -1;
            for (int i = 0; i < usuarios.Length; i++) if (usuarios[i].Id > max) max = usuarios[i].Id;
            indicePorId = new int[max + 1];
            for (int i = 0; i < indicePorId.Length; i++) indicePorId[i] = -1;
            for (int i = 0; i < usuarios.Length; i++) indicePorId[usuarios[i].Id] = i;
            Invalidate();
        }

        public void ResaltarCamino(int[] ids) { camino = ids; Invalidate(); }
        public void Seleccionar(int id) { seleccionado = id; Invalidate(); }

        /// <summary>Pinta cada comunidad de un color. Pasar null para quitar los colores.</summary>
        public void ColorearComunidades(int[][] grupos)
        {
            if (grupos == null) { comunidadDe = null; Invalidate(); return; }
            comunidadDe = new int[indicePorId.Length];
            for (int i = 0; i < comunidadDe.Length; i++) comunidadDe[i] = -1;
            for (int k = 0; k < grupos.Length; k++)
                for (int m = 0; m < grupos[k].Length; m++)
                    if (grupos[k][m] >= 0 && grupos[k][m] < comunidadDe.Length) comunidadDe[grupos[k][m]] = k;
            Invalidate();
        }

        // ---------- Dibujo ----------
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            DibujarLeyenda(g);

            if (usuarios.Length == 0)
            {
                TextRenderer.DrawText(g, "Sin datos. Carga un CSV o registra usuarios.", Tema.Normal,
                    ClientRectangle, Tema.TextoSuave, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
                return;
            }

            PointF[] pos = Posiciones();
            for (int i = 0; i < usuarios.Length; i++)
            {
                Arista[] vecinos = Vecinos(usuarios[i].Id);
                for (int k = 0; k < vecinos.Length; k++)
                {
                    int j = Indice(vecinos[k].Destino);
                    if (j < 0) continue;
                    bool inverso = ExisteArista(vecinos[k].Destino, usuarios[i].Id);
                    if (inverso && usuarios[i].Id > vecinos[k].Destino) continue; // evita dibujar dos veces la no dirigida
                    DibujarArista(g, pos[i], pos[j], vecinos[k], !inverso);
                }
            }
            DibujarCamino(g, pos);
            DibujarNodos(g, pos);
        }

        private PointF[] Posiciones()
        {
            int n = usuarios.Length;
            PointF[] pos = new PointF[n];
            float cx = Width / 2f, cy = (Height - 30) / 2f;
            float radio = Math.Max(10f, Math.Min(Width, Height - 30) / 2f - R - 20f);
            if (n == 1) { pos[0] = new PointF(cx, cy); return pos; }
            for (int i = 0; i < n; i++)
            {
                double ang = -Math.PI / 2 + 2 * Math.PI * i / n;
                pos[i] = new PointF(cx + (float)(radio * Math.Cos(ang)), cy + (float)(radio * Math.Sin(ang)));
            }
            return pos;
        }

        private void DibujarArista(Graphics g, PointF a, PointF b, Arista ar, bool dirigida)
        {
            Color[] colores = Colores(ar);
            float dx = b.X - a.X, dy = b.Y - a.Y;
            float len = (float)Math.Sqrt(dx * dx + dy * dy);
            if (len < 1f) return;
            float ux = dx / len, uy = dy / len, nx = -uy, ny = ux;
            PointF fin = dirigida ? new PointF(b.X - ux * R, b.Y - uy * R) : b;

            for (int k = 0; k < colores.Length; k++)
            {
                float off = (k - (colores.Length - 1) / 2f) * 5f; // líneas paralelas
                using (Pen pen = new Pen(colores[k], 3f))
                {
                    if (dirigida) pen.CustomEndCap = new AdjustableArrowCap(4, 4);
                    g.DrawLine(pen, a.X + nx * off, a.Y + ny * off, fin.X + nx * off, fin.Y + ny * off);
                }
            }
        }

        private Color[] Colores(Arista ar)
        {
            int c = 0;
            if (ar.MismaMusica) c++;
            if (ar.MismaCarrera) c++;
            if (ar.MismosCursos) c++;
            if (ar.MismoAnio) c++;
            if (c == 0) return new Color[] { Color.FromArgb(200, 205, 215) };
            Color[] r = new Color[c];
            int i = 0;
            if (ar.MismaMusica) r[i++] = Tema.Musica;
            if (ar.MismaCarrera) r[i++] = Tema.Carrera;
            if (ar.MismosCursos) r[i++] = Tema.Cursos;
            if (ar.MismoAnio) r[i++] = Tema.Anio;
            return r;
        }

        private void DibujarCamino(Graphics g, PointF[] pos)
        {
            if (camino == null || camino.Length < 2) return;
            using (Pen halo = new Pen(Color.FromArgb(70, 20, 26, 43), 12f))
            using (Pen linea = new Pen(Tema.Texto, 2f))
            {
                halo.StartCap = LineCap.Round; halo.EndCap = LineCap.Round;
                linea.DashStyle = DashStyle.Dash;
                for (int k = 0; k < camino.Length - 1; k++)
                {
                    int i = Indice(camino[k]), j = Indice(camino[k + 1]);
                    if (i < 0 || j < 0) continue;
                    g.DrawLine(halo, pos[i], pos[j]);
                    g.DrawLine(linea, pos[i], pos[j]);
                }
            }
        }

        private void DibujarNodos(Graphics g, PointF[] pos)
        {
            for (int i = 0; i < usuarios.Length; i++)
            {
                Usuario u = usuarios[i];
                PointF p = pos[i];
                bool enCamino = EnCamino(u.Id);
                using (SolidBrush relleno = new SolidBrush(ColorNodo(u.Id)))
                    g.FillEllipse(relleno, p.X - R, p.Y - R, 2 * R, 2 * R);

                Color cb = u.Id == seleccionado ? Tema.Primario : (enCamino ? Tema.Texto : Tema.TextoSuave);
                float grosor = u.Id == seleccionado ? 3.5f : (enCamino ? 2.5f : 1.5f);
                using (Pen borde = new Pen(cb, grosor))
                    g.DrawEllipse(borde, p.X - R, p.Y - R, 2 * R, 2 * R);

                string t = u.Username.Length > 8 ? u.Username.Substring(0, 7) + "…" : u.Username;
                Rectangle caja = new Rectangle((int)(p.X - R - 6), (int)(p.Y - 10), (int)(2 * R + 12), 20);
                TextRenderer.DrawText(g, t, Tema.Pequena, caja, Tema.Texto,
                    TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
            }
        }

        private void DibujarLeyenda(Graphics g)
        {
            string[] textos = { "Misma música", "Misma carrera", "Mismos cursos", "Mismo año" };
            Color[] colores = { Tema.Musica, Tema.Carrera, Tema.Cursos, Tema.Anio };
            int x = 12, y = Height - 24;
            for (int i = 0; i < textos.Length; i++)
            {
                using (SolidBrush b = new SolidBrush(colores[i]))
                    g.FillRectangle(b, x, y + 6, 18, 5);
                TextRenderer.DrawText(g, textos[i], Tema.Pequena, new Point(x + 22, y + 1), Tema.TextoSuave);
                x += 22 + TextRenderer.MeasureText(textos[i], Tema.Pequena).Width + 14;
            }
        }

        // ---------- Utilidades ----------
        private Color ColorNodo(int id)
        {
            if (comunidadDe != null && id < comunidadDe.Length && comunidadDe[id] >= 0)
                return Paleta[comunidadDe[id] % Paleta.Length];
            return Color.FromArgb(228, 232, 240);
        }

        private bool EnCamino(int id)
        {
            if (camino == null) return false;
            for (int i = 0; i < camino.Length; i++) if (camino[i] == id) return true;
            return false;
        }

        private int Indice(int id) { return (id >= 0 && id < indicePorId.Length) ? indicePorId[id] : -1; }

        private Arista[] Vecinos(int id)
        {
            Arista[] v = obtenerVecinos == null ? null : obtenerVecinos(id);
            return v ?? new Arista[0];
        }

        private bool ExisteArista(int origen, int destino)
        {
            Arista[] v = Vecinos(origen);
            for (int i = 0; i < v.Length; i++) if (v[i].Destino == destino) return true;
            return false;
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            if (usuarios.Length == 0) return;
            PointF[] pos = Posiciones();
            for (int i = 0; i < usuarios.Length; i++)
            {
                float dx = e.X - pos[i].X, dy = e.Y - pos[i].Y;
                if (dx * dx + dy * dy <= R * R)
                {
                    seleccionado = usuarios[i].Id;
                    Invalidate();
                    if (NodoClic != null) NodoClic(usuarios[i]);
                    return;
                }
            }
        }
    }
}