using System.Drawing;
using System.Windows.Forms;

namespace RedSocial.UI
{
    /// <summary>Ventana simple que pide varios textos. Devuelve null si el usuario cancela.</summary>
    static class DialogoCampos
    {
        public static string[] Pedir(IWin32Window dueno, string titulo, string[] etiquetas)
        {
            using (Form f = new Form())
            {
                f.Text = titulo;
                f.FormBorderStyle = FormBorderStyle.FixedDialog;
                f.MaximizeBox = false;
                f.MinimizeBox = false;
                f.StartPosition = FormStartPosition.CenterParent;
                f.Font = Tema.Normal;
                f.BackColor = Tema.Fondo;
                int alto = 16 + etiquetas.Length * 58 + 60;
                f.ClientSize = new Size(380, alto);

                TextBox[] cajas = new TextBox[etiquetas.Length];
                for (int i = 0; i < etiquetas.Length; i++)
                {
                    Label l = new Label();
                    l.Text = etiquetas[i];
                    l.ForeColor = Tema.TextoSuave;
                    l.AutoSize = true;
                    l.Left = 16; l.Top = 14 + i * 58;
                    cajas[i] = new TextBox();
                    cajas[i].Left = 16; cajas[i].Top = 14 + i * 58 + 22; cajas[i].Width = 348;
                    f.Controls.Add(l);
                    f.Controls.Add(cajas[i]);
                }

                Button ok = Tema.Boton("Aceptar", true, 100);
                ok.Left = 380 - 16 - 100 - 108; ok.Top = alto - 48;
                ok.DialogResult = DialogResult.OK;
                Button cancelar = Tema.Boton("Cancelar", false, 100);
                cancelar.Left = 380 - 16 - 100; cancelar.Top = alto - 48;
                cancelar.DialogResult = DialogResult.Cancel;
                f.Controls.Add(ok);
                f.Controls.Add(cancelar);
                f.AcceptButton = ok;
                f.CancelButton = cancelar;

                if (f.ShowDialog(dueno) != DialogResult.OK) return null;

                string[] r = new string[cajas.Length];
                for (int i = 0; i < cajas.Length; i++) r[i] = cajas[i].Text.Trim();
                return r;
            }
        }
    }
}