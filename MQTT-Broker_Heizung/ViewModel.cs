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
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;
using System.Text;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using System.Globalization;

namespace MQTT_Broker_Heizung
{
    internal partial class ViewModel : ObservableObject
    {
        public ICommand HeizungCommand { get; private set; }
        public List<ISeries> HumidSerie { get; private set; }
        public ObservableCollection<DateTimePoint> HumidValues { get; private set; }
        public List<ISeries> TemperSerie { get; private set; }
        public ObservableCollection<DateTimePoint> TemperValues { get; private set; }

        static MqttClient client;

        [ObservableProperty]
        bool _heizungStatus;
        [ObservableProperty]
        int _batterie;

        public ViewModel()
        {
            HumidSerie = new List<ISeries>();
            HumidValues = new ObservableCollection<DateTimePoint>();
            LineSeries<DateTimePoint> humidSeries = new LineSeries<DateTimePoint> { Values = HumidValues };

            TemperSerie = new List<ISeries>();
            TemperValues = new ObservableCollection<DateTimePoint>();
            LineSeries<DateTimePoint> temperSerie = new LineSeries<DateTimePoint> { Values = TemperValues };

            humidSeries.Name = "Luftfeuchtigkeit";
            humidSeries.GeometrySize = 1;
            humidSeries.GeometryFill = humidSeries.Stroke;
            humidSeries.DataLabelsFormatter = (point) => point.PrimaryValue.ToString("N");
            humidSeries.Fill = null;


            temperSerie.Name = "Temperatur";
            temperSerie.Stroke = new SolidColorPaint(SKColors.Red, 2);
            temperSerie.GeometrySize = 0.5;
            temperSerie.DataLabelsFormatter = (point) => point.PrimaryValue.ToString("N");
            temperSerie.Fill = null;


            HumidSerie.Add(humidSeries);
            TemperSerie.Add(temperSerie);
            Subscribe();
            HeizungCommand = new RelayCommand(HeizungAnAus);
        }

        private void HeizungAnAus()
        {
            if (HeizungStatus == true)
            {
                Publish("/heizung", "0");
                HeizungStatus = false;

            }
            else
            {
                Publish("/heizung", "1");
                HeizungStatus = true;
            }
            OnPropertyChanged(nameof(HeizungStatusText));
            OnPropertyChanged(nameof(HeizungStatusForeground));
        }
        public string HeizungStatusText => HeizungStatus ? "An" : "Aus";
        public string HeizungStatusForeground => HeizungStatus ? "YellowGreen" : "OrangeRed";
        public async void Subscribe()
        {
            var hostName = "raspberryFrank";
            var ipAddress = await Dns.GetHostAddressesAsync(hostName);
            if (ipAddress.Length == 0)
            {
                MessageBox.Show("Host not found.");
                return;
            }
            client = new MqttClient(ipAddress[0]);
            client.MqttMsgPublishReceived += clientMsgRecieved;
            string clientId = Guid.NewGuid().ToString();
            client.Connect(clientId);
            client.Subscribe(new string[] { "#" }, new byte[] { MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
        }

        void clientMsgRecieved(object sender, MqttMsgPublishEventArgs e)
        {
            string str = System.Text.Encoding.ASCII.GetString(e.Message);
            switch (e.Topic)
            {
                case "/batterie":
                    double batterie;
                    double.TryParse(str, NumberStyles.Number, CultureInfo.InvariantCulture, out batterie);
                    Batterie = Convert.ToInt16(Math.Round(batterie, 0));
                    break;

                case "/feucht":
                    double humidity;
                    double.TryParse(str, NumberStyles.Number, CultureInfo.InvariantCulture, out humidity);
                    DateTimePoint humidPoint = new DateTimePoint(DateTime.Now, humidity);
                    HumidValues.Add(humidPoint);
                    break;

                case "/temper":
                    double temper;
                    double.TryParse(str, NumberStyles.Number, CultureInfo.InvariantCulture, out temper);
                    DateTimePoint temperPoint = new DateTimePoint(DateTime.Now, temper);
                    TemperValues.Add(temperPoint);
                    break;

                default:
                    break;
            }
        }
        void Publish(string topic, string payload)
        {
            client.Publish(topic, Encoding.UTF8.GetBytes(payload), MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE, false);
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
