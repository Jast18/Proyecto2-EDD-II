using System.Runtime.InteropServices;
using RedSocial.UI;

// Punto de entrada.
//   dotnet run                -> abre la interfaz gráfica (PForm)
//   dotnet run -- --trials    -> abre el menú de pruebas por consola (Trials/trial.cs)
static class Program
{
    [DllImport("kernel32.dll")] private static extern bool AttachConsole(int pid);
    [DllImport("kernel32.dll")] private static extern bool AllocConsole();

    [STAThread]
    static void Main(string[] args)
    {
        if (Array.IndexOf(args, "--trials") >= 0)
        {
            if (!AttachConsole(-1)) AllocConsole();   // muestra la consola aunque la app sea WinExe
            Trials.Run();
            return;
        }

        ApplicationConfiguration.Initialize();
        Application.Run(new PForm());
    }
}