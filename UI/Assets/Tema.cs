using System.Drawing;
using System.Windows.Forms;

namespace RedSocial.UI
{
    /// <summary>Colores, fuentes y botón base de la interfaz (paleta tomada del boceto).</summary>
    static class Tema
    {
        public static readonly Color Fondo = Color.FromArgb(243, 245, 249);
        public static readonly Color Panel = Color.White;
        public static readonly Color Texto = Color.FromArgb(20, 26, 43);
        public static readonly Color TextoSuave = Color.FromArgb(104, 112, 130);
        public static readonly Color Primario = Color.FromArgb(31, 64, 160);
        public static readonly Color PrimarioClaro = Color.FromArgb(226, 233, 250);

        // Un color por criterio de afinidad (igual que la leyenda del boceto)
        public static readonly Color Musica = Color.FromArgb(170, 176, 192);   // gris
        public static readonly Color Carrera = Color.FromArgb(34, 170, 90);    // verde
        public static readonly Color Cursos = Color.FromArgb(33, 125, 235);    // azul
        public static readonly Color Anio = Color.FromArgb(247, 160, 38);      // naranja

        public static readonly Font Normal = new Font("Segoe UI", 9.5f);
        public static readonly Font Pequena = new Font("Segoe UI", 8.5f);
        public static readonly Font Titulo = new Font("Segoe UI Semibold", 14f);

        public static Button Boton(string texto, bool primario, int ancho)
        {
            Button b = new Button();
            b.Text = texto;
            b.Width = ancho;
            b.Height = 36;
            b.FlatStyle = FlatStyle.Flat;
            b.Cursor = Cursors.Hand;
            b.Margin = new Padding(0, 0, 0, 8);
            b.Font = Normal;
            if (primario)
            {
                b.BackColor = Primario;
                b.ForeColor = Color.White;
                b.FlatAppearance.BorderSize = 0;
            }
            else
            {
                b.BackColor = Panel;
                b.ForeColor = Texto;
                b.FlatAppearance.BorderColor = Color.FromArgb(200, 205, 215);
            }
            return b;
        }
    }
}