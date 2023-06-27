using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Ink;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace MQTT_Broker_Heizung
{
    class ViewModel
    {
        public List<ISeries> MeineSerie { get; private set; } // Eigenschaft an die gebunden wird

        public ViewModel()
        {
            MeineSerie = new List<ISeries>(); // Hier im Demo nur eine Serie
            LineSeries<ObservablePoint> sinusSerie = FülleValuesSinus(9);
            LineSeries<ObservablePoint> sinusSerie2 = FülleValuesSinus(13);
            MeineSerie.Add(sinusSerie);
            MeineSerie.Add(sinusSerie2);
        }

        private LineSeries<ObservablePoint> FülleValuesSinus(int anzahl)
        {
            Random rnd = new Random();
            double offset = rnd.NextDouble() + 0.5;
            double amplitude = 10.0 * offset;
            const double frequency = 50.0;
            const double periodendauer = 1.0 / frequency;
            double schrittweite = periodendauer / (anzahl - 1);

            var sinusWerte = new ObservableCollection<ObservablePoint>();

            double t = 0;
            for (int i = 0; i < anzahl; i++)
            {
                double wert = amplitude * Math.Sin(2 * Math.PI * frequency * t);
                sinusWerte.Add(new ObservablePoint(t * 1000, wert));
                t += schrittweite;
            }

            var lSerie = new LineSeries<ObservablePoint>();
            lSerie.Name = "Sinus";
            lSerie.Fill = null;
            //  lSerie.Stroke = new SolidColorPaint(SKColors.Blue, 2);
            lSerie.GeometryFill = null;
            //   lSerie.GeometryStroke = new SolidColorPaint(SKColors.Red, 1);
            lSerie.GeometrySize = 25;
            lSerie.Values = sinusWerte;

            lSerie.DataLabelsSize = 20;
            lSerie.DataLabelsPaint = new SolidColorPaint(SKColors.Blue);
            lSerie.DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.Top;
            lSerie.DataLabelsFormatter = (point) => point.PrimaryValue.ToString("N");

            return lSerie;
        }



    }
}
