//Graph Panel

//Graficación del grafo en la interfaz

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace RedSocial.UI
{
    /// <summary>
    /// Dibuja el grafo con GDI+. No conoce las estructuras: solo recibe los usuarios y una función
    /// que devuelve los usernames vecinos de un username. Cada criterio compartido entre dos usuarios
    /// (música, carrera, cursos, año) se dibuja como una línea de color (multigrafo, como en el boceto);
    /// los criterios salen de AC.Calcular. Las relaciones son de amistad (no dirigidas).
    /// </summary>
    class GPanel : Panel
    {
        private const float R = 24f; // radio del nodo

        private User[] usuarios = new User[0];
        private Func<string, string[]> obtenerVecinos;
        private Dictionary<string, int> indicePorUser = new Dictionary<string, int>();
        private string[] camino;        // usernames del camino de Dijkstra a resaltar
        private int[] comunidadDe;      // comunidadDe[posición del usuario] = número de comunidad (o -1)
        private string seleccionado;

        private static readonly Color[] Paleta =
        {
            Color.FromArgb(201, 214, 245), Color.FromArgb(200, 232, 214), Color.FromArgb(250, 224, 190),
            Color.FromArgb(235, 210, 240), Color.FromArgb(252, 214, 214), Color.FromArgb(215, 235, 240),
            Color.FromArgb(240, 238, 190), Color.FromArgb(220, 220, 220)
        };

        /// <summary>Se dispara al hacer clic sobre un nodo.</summary>
        public event Action<User> NodoClic;

        public GPanel()
        {
            DoubleBuffered = true;
            ResizeRedraw = true;
            BackColor = Tema.Panel;
        }

        // ---------- API pública ----------
        public void Cargar(User[] lista, Func<string, string[]> vecinos)
        {
            usuarios = lista ?? new User[0];
            obtenerVecinos = vecinos;
            camino = null; comunidadDe = null;

            indicePorUser = new Dictionary<string, int>();
            for (int i = 0; i < usuarios.Length; i++) indicePorUser[usuarios[i].Username] = i;
            Invalidate();
        }

        public void ResaltarCamino(string[] usernames) { camino = usernames; Invalidate(); }
        public void Seleccionar(string username) { seleccionado = username; Invalidate(); }

        /// <summary>Pinta cada comunidad de un color. Pasar null para quitar los colores.</summary>
        public void ColorearComunidades(string[][] grupos)
        {
            if (grupos == null) { comunidadDe = null; Invalidate(); return; }
            comunidadDe = new int[usuarios.Length];
            for (int i = 0; i < comunidadDe.Length; i++) comunidadDe[i] = -1;
            for (int k = 0; k < grupos.Length; k++)
                for (int m = 0; m < grupos[k].Length; m++)
                {
                    int idx = Indice(grupos[k][m]);
                    if (idx >= 0) comunidadDe[idx] = k;
                }
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
                string[] vecinos = Vecinos(usuarios[i].Username);
                for (int k = 0; k < vecinos.Length; k++)
                {
                    int j = Indice(vecinos[k]);
                    if (j <= i) continue; // la relación es no dirigida: se dibuja una sola vez (i < j)
                    DibujarArista(g, pos[i], pos[j], AC.Calcular(usuarios[i], usuarios[j]));
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

        private void DibujarArista(Graphics g, PointF a, PointF b, (int puntaje, bool musica, bool carrera, bool cursos, bool anio) af)
        {
            Color[] colores = Colores(af.musica, af.carrera, af.cursos, af.anio);
            float dx = b.X - a.X, dy = b.Y - a.Y;
            float len = (float)Math.Sqrt(dx * dx + dy * dy);
            if (len < 1f) return;
            float ux = dx / len, uy = dy / len, nx = -uy, ny = ux;

            for (int k = 0; k < colores.Length; k++)
            {
                float off = (k - (colores.Length - 1) / 2f) * 5f; // líneas paralelas
                using (Pen pen = new Pen(colores[k], 3f))
                    g.DrawLine(pen, a.X + nx * off, a.Y + ny * off, b.X + nx * off, b.Y + ny * off);
            }
        }

        private Color[] Colores(bool musica, bool carrera, bool cursos, bool anio)
        {
            int c = 0;
            if (musica) c++;
            if (carrera) c++;
            if (cursos) c++;
            if (anio) c++;
            if (c == 0) return new Color[] { Color.FromArgb(200, 205, 215) };
            Color[] r = new Color[c];
            int i = 0;
            if (musica) r[i++] = Tema.Musica;
            if (carrera) r[i++] = Tema.Carrera;
            if (cursos) r[i++] = Tema.Cursos;
            if (anio) r[i++] = Tema.Anio;
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
                User u = usuarios[i];
                PointF p = pos[i];
                bool enCamino = EnCamino(u.Username);
                bool esSel = u.Username == seleccionado;
                using (SolidBrush relleno = new SolidBrush(ColorNodo(i)))
                    g.FillEllipse(relleno, p.X - R, p.Y - R, 2 * R, 2 * R);

                Color cb = esSel ? Tema.Primario : (enCamino ? Tema.Texto : Tema.TextoSuave);
                float grosor = esSel ? 3.5f : (enCamino ? 2.5f : 1.5f);
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
        private Color ColorNodo(int posicion)
        {
            if (comunidadDe != null && posicion < comunidadDe.Length && comunidadDe[posicion] >= 0)
                return Paleta[comunidadDe[posicion] % Paleta.Length];
            return Color.FromArgb(228, 232, 240);
        }

        private bool EnCamino(string username)
        {
            if (camino == null) return false;
            for (int i = 0; i < camino.Length; i++) if (camino[i] == username) return true;
            return false;
        }

        private int Indice(string username)
        {
            int i;
            return (username != null && indicePorUser.TryGetValue(username, out i)) ? i : -1;
        }

        private string[] Vecinos(string username)
        {
            string[] v = obtenerVecinos == null ? null : obtenerVecinos(username);
            return v ?? new string[0];
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
                    seleccionado = usuarios[i].Username;
                    Invalidate();
                    if (NodoClic != null) NodoClic(usuarios[i]);
                    return;
                }
            }
        }
    }
}