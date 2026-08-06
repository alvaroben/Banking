using InternetBankingApp.Models;

namespace InternetBankingApp.Controls;

/// <summary>
/// Dibuja el gráfico de barras de transferencias por mes del dashboard. Se apoya solo en
/// Microsoft.Maui.Graphics (sin librerías externas): calcula la escala a partir del mayor total,
/// reparte el ancho disponible entre las barras y rotula cada una con su mes y su monto.
/// </summary>
public class GraficoBarrasDrawable : IDrawable
{
    private static readonly Color ColorBarra = Color.FromArgb("#10B981");
    private static readonly Color ColorBarraDestacada = Color.FromArgb("#047857");
    private static readonly Color ColorTexto = Color.FromArgb("#06243B");
    private static readonly Color ColorTextoSuave = Color.FromArgb("#6E6E6E");
    private static readonly Color ColorGuia = Color.FromArgb("#C8C8C8");

    public IReadOnlyList<TotalPorMes> Datos { get; set; } = [];

    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.FillColor = Colors.Transparent;
        canvas.FillRectangle(dirtyRect);

        if (Datos.Count == 0)
        {
            canvas.FontColor = ColorTextoSuave;
            canvas.FontSize = 13;
            canvas.DrawString(
                "Aún no hay transferencias para graficar.",
                dirtyRect,
                HorizontalAlignment.Center,
                VerticalAlignment.Center);
            return;
        }

        const float espacioEtiquetas = 34f;  // franja inferior para el mes
        const float espacioMontos = 18f;     // franja superior para el monto de cada barra
        const float separacion = 10f;

        var maximo = Datos.Max(d => d.Total);
        if (maximo <= 0)
        {
            maximo = 1;
        }

        var baseY = dirtyRect.Bottom - espacioEtiquetas;
        var alturaDisponible = baseY - dirtyRect.Top - espacioMontos;
        var anchoBarra = (dirtyRect.Width - separacion * (Datos.Count + 1)) / Datos.Count;

        // Línea base del gráfico.
        canvas.StrokeColor = ColorGuia;
        canvas.StrokeSize = 1;
        canvas.DrawLine(dirtyRect.Left, baseY, dirtyRect.Right, baseY);

        var mesMasAlto = Datos.OrderByDescending(d => d.Total).First();

        for (var indice = 0; indice < Datos.Count; indice++)
        {
            var dato = Datos[indice];
            var altura = (float)(dato.Total / maximo) * alturaDisponible;

            // Una barra con valor pero casi invisible confunde más que ayuda: piso de 4 px.
            if (altura < 4 && dato.Total > 0)
            {
                altura = 4;
            }

            var x = dirtyRect.Left + separacion + indice * (anchoBarra + separacion);
            var y = baseY - altura;

            canvas.FillColor = dato == mesMasAlto ? ColorBarraDestacada : ColorBarra;
            canvas.FillRoundedRectangle(x, y, anchoBarra, altura, 6);

            // Monto encima de la barra, en miles para que quepa.
            canvas.FontColor = ColorTexto;
            canvas.FontSize = 11;
            canvas.DrawString(
                FormatearMonto(dato.Total),
                x,
                y - espacioMontos,
                anchoBarra,
                espacioMontos,
                HorizontalAlignment.Center,
                VerticalAlignment.Bottom);

            // Mes y cantidad de operaciones debajo.
            canvas.FontColor = ColorTextoSuave;
            canvas.FontSize = 12;
            canvas.DrawString(
                dato.Etiqueta,
                x,
                baseY + 3,
                anchoBarra,
                16,
                HorizontalAlignment.Center,
                VerticalAlignment.Center);

            canvas.FontSize = 10;
            canvas.DrawString(
                $"{dato.Cantidad} op.",
                x,
                baseY + 18,
                anchoBarra,
                14,
                HorizontalAlignment.Center,
                VerticalAlignment.Center);
        }
    }

    private static string FormatearMonto(decimal monto) => monto >= 1000
        ? $"{monto / 1000m:0.#}k"
        : $"{monto:0}";
}
