using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using CommunityToolkit.Mvvm.ComponentModel;
using System.Net;
using System.Windows;
using HiveMQtt.Client.Options;
using HiveMQtt.Client;

namespace MQTT_Broker_Heizung
{
    internal partial class ViewModel : ObservableObject
    {
        public List<ISeries> MeineSerie { get; private set; } // Eigenschaft an die gebunden wird
        public List<ISeries> MeineSerie2 { get; private set; } // Eigenschaft an die gebunden wird

        public ViewModel()
        {
            MeineSerie = new List<ISeries>(); // Hier im Demo nur eine Serie
            MeineSerie2 = new List<ISeries>(); // Hier im Demo nur eine Serie
            LineSeries<ObservablePoint> sinusSerie = FülleValuesSinus(9);
            LineSeries<ObservablePoint> sinusSerie2 = FülleValuesSinus(13);
            sinusSerie2.Stroke = new SolidColorPaint(SKColors.Red, 2);
            MeineSerie.Add(sinusSerie);
            MeineSerie2.Add(sinusSerie2);
            Subscribe();
        }
        public async void Subscribe()
        {
            var hostName = "raspberryFrank";
            var ipAddress = await Dns.GetHostAddressesAsync(hostName);
            if (ipAddress.Length == 0)
            {
                MessageBox.Show("Host not found.");
                return;
            }
            var options = new HiveMQClientOptions();
            options.Host = ipAddress[0].ToString();
            options.Port = 1883;

            var client = new HiveMQClient(options);
            var connectResult = await client.ConnectAsync().ConfigureAwait(false);

            var topicTemper = await client.SubscribeAsync("/temper").ConfigureAwait(false);
            var topicHumid = await client.SubscribeAsync("/feucht").ConfigureAwait(false);
            var topicBatter = await client.SubscribeAsync("/batterie").ConfigureAwait(false);

            client.OnMessageReceived += (sender, e) =>
            {
                string topic = e.PublishMessage.Topic;
                string msg = e.PublishMessage.PayloadAsString;

            };
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
