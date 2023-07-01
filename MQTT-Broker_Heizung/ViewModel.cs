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
using System.IO;
using static CommunityToolkit.Mvvm.ComponentModel.__Internals.__TaskExtensions.TaskAwaitableWithoutEndValidation;

namespace MQTT_Broker_Heizung
{
    [ObservableObject]
    internal partial class ViewModel
    {
        public ICommand HeizungCommand { get; private set; }
        public ICommand CSVCommand { get; private set; }
        public List<ISeries> HumidSerie { get; private set; }
        public ObservableCollection<DateTimePoint> HumidValues { get; private set; }
        public List<ISeries> TemperSerie { get; private set; }
        public ObservableCollection<DateTimePoint> TemperValues { get; private set; }

        static MqttClient client;

        [ObservableProperty]
        bool _heizungStatus;
        [ObservableProperty]
        int _batterie;

        public Axis[] XAxes { get; set; } =
    {
            new Axis
            {
                Labeler = value => new DateTime((long) value).ToString("HH:mm:ss"),//fehler abfangen
                LabelsRotation = 15,
                UnitWidth = TimeSpan.FromMinutes(1).Ticks,
            MinStep = TimeSpan.FromMinutes(1).Ticks

        }

        };

        public ViewModel()
        {
            HumidSerie = new List<ISeries>();
            HumidValues = new ObservableCollection<DateTimePoint>();
            LineSeries<DateTimePoint> humidSeries = new LineSeries<DateTimePoint> { Values = HumidValues };

            TemperSerie = new List<ISeries>();
            TemperValues = new ObservableCollection<DateTimePoint>();
            LineSeries<DateTimePoint> temperSerie = new LineSeries<DateTimePoint> { Values = TemperValues };

            humidSeries.Name = "Luftfeuchtigkeit";
            temperSerie.Stroke = new SolidColorPaint(SKColors.LightBlue, 2);
            humidSeries.GeometrySize = 0.5;
            humidSeries.GeometryStroke = new SolidColorPaint(SKColors.OrangeRed, 5);
            humidSeries.DataLabelsFormatter = (point) => point.PrimaryValue.ToString("N");
            humidSeries.Fill = null;


            temperSerie.Name = "Temperatur";
            temperSerie.Stroke = new SolidColorPaint(SKColors.Red, 2);
            temperSerie.GeometrySize = 0.5;
            temperSerie.GeometryStroke = new SolidColorPaint(SKColors.AliceBlue, 5);
            temperSerie.DataLabelsFormatter = (point) => point.PrimaryValue.ToString("N");
            temperSerie.Fill = null;


            HumidSerie.Add(humidSeries);
            TemperSerie.Add(temperSerie);
            Subscribe();

            HeizungCommand = new RelayCommand(HeizungAnAus);
            CSVCommand = new RelayCommand(ExportDataToCsv);
        }
        private void ExportDataToCsv()
        {
            string msg = "Daten wurden als CSV exportiert.\n";
            string fileDirectory = @"C:\temp\";

            if (ExportToCsv(fileDirectory, "humid.csv", HumidValues, "Luftfeuchtigkeit")) HumidValues.Clear();
            else msg += "\nLuftfeuchtigkeit konnte nicht exportiert werden";

            if (ExportToCsv(fileDirectory, "temper.csv", TemperValues, "Temperatur")) TemperValues.Clear();
            else msg += "\nTemperatur konnte nicht exportiert werden";

            MessageBox.Show(msg);
        }
        private bool ExportToCsv(string fileDirectory, string filename, IEnumerable<DateTimePoint> data, string dataHeadline)
        {
            var file = Path.Combine(fileDirectory, filename);
            bool fileExist = File.Exists(file);
            try
            {
                using (StreamWriter sw = new StreamWriter(file, true))
                {
                    if (!fileExist)
                    {
                        sw.WriteLine($"Datum;{dataHeadline}");
                    }
                    foreach (var point in data)
                    {
                        string line = $"{point.DateTime.ToString("yyyy-MM-dd HH:mm:ss")};{point.Value.ToString()}";
                        sw.WriteLine(line);
                    }
                }
                return true;
            }
            catch (Exception)
            {
                return false;

            }
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






    }
}
