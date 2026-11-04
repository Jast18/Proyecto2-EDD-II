using System;

// AC = Affinity Calculator
// Fórmula de peso en aristas: (afinidad / 100) * (1 / distancia) * 100
//   afinidad  : 0..100 (resultado de Calcular)
//   distancia : >= 1. Para una relación directa es 1 (peso = afinidad); a más distancia, menos peso.
static class AC
{
    // Peso de una arista según la fórmula anterior, redondeado a entero (0..100).
    public static int PesoArista(int afinidad, int distancia = 1)
    {
        if (afinidad < 0) afinidad = 0;
        if (afinidad > 100) afinidad = 100;
        if (distancia < 1) distancia = 1;
        double peso = (afinidad / 100.0) * (1.0 / distancia) * 100.0;
        return (int)Math.Round(peso, MidpointRounding.AwayFromZero);
    }

    // Afinidad 0..100 sobre los 4 criterios de la imagen (25 puntos c/u):
    // misma música, misma carrera, mismo año y cursos en común (proporcional).
    public static (int puntaje, bool musica, bool carrera, bool cursos, bool anio) Calcular(User a, User b)
    {
        bool musica = !string.IsNullOrEmpty(a.Music) &&
                      string.Equals(a.Music, b.Music, StringComparison.OrdinalIgnoreCase);
        bool carrera = !string.IsNullOrEmpty(a.Career) &&
                       string.Equals(a.Career, b.Career, StringComparison.OrdinalIgnoreCase);
        bool anio = a.Year != 0 && a.Year == b.Year;

        int comunes = 0;
        foreach (string ca in a.Courses)
            foreach (string cb in b.Courses)
                if (string.Equals(ca, cb, StringComparison.OrdinalIgnoreCase)) { comunes++; break; }

        int maxCursos = Math.Max(a.Courses.Length, b.Courses.Length);
        int puntosCursos = maxCursos == 0 ? 0 : (25 * comunes) / maxCursos;

        int puntaje = (musica ? 25 : 0) + (carrera ? 25 : 0) + (anio ? 25 : 0) + puntosCursos;
        return (puntaje, musica, carrera, comunes > 0, anio);
    }
}