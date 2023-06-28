#include <Adafruit_Sensor.h>
#include <Arduino.h>
#include <DHT.h>
#include <ESPmDNS.h>
#include <PubSubClient.h>
#include <SPI.h>
#include <SPIFFS.h>
#include <WebServer.h>
#include <WiFi.h>
#include <esp32-hal-timer.h>
#include <TFT_eSPI.h>

const char *ssid = "Mesh Igel";        //"ASUSJC";
const char *password = "rhQ6WbSbE8Rv"; //"123456789";

const int dhtpin = 25;
const int batteryPin = 39;

DHT dht(dhtpin, DHT22);
TFT_eSPI tft = TFT_eSPI();

float temperature;
float humidity;

const char *ntpServer = "pool.ntp.org";
const long gmtOffset_sec = 3600;
const int daylightOffset_sec = 3600;
char csvSperator = ';';

WiFiClient espClient;
PubSubClient mqttClient(espClient);
const int mqqtPort = 1883;
const char *broker = "192.168.68.109";

const char *topicHeizung = "/heizung";

float batteryPercantage()
{
  int input = (analogRead(batteryPin));
  float percantage = map(input, 1000, 4095, 0.0, 100.00);
  return percantage;
}

String localTime()
{
  char buffer[80];
  struct tm timeinfo;
  if (!getLocalTime(&timeinfo))
  {
    return ("Failed to obtain time");
  }
  strftime(buffer, 80, "%Y-%m-%d %H:%M", &timeinfo);
  return buffer; //(&timeinfo, "%A, %B %d %Y %H:%M:%S");
}

void showSCrn()
{
  temperature = dht.readTemperature();
  humidity = dht.readHumidity();
  tft.setCursor(0, 0);
  tft.setTextSize(3);
  tft.fillScreen(TFT_BLACK);
  tft.setTextColor(TFT_GREEN, TFT_BLACK);
  tft.println("Temp_Humidity");
  // tft.drawLine(0, 35, 250, 35, TFT_BLUE);
  tft.println();
  tft.print(temperature);
  tft.print(F("C"));
  tft.print(("_"));
  tft.print(humidity);
  tft.print(F("%"));
  tft.println();
  tft.println();
  tft.setTextSize(2);
  tft.println(localTime());
}

void connectMQTT()
{
  while (!mqttClient.connected())
  {
    Serial.println("Connecting mqtt... ");
    mqttClient.connect("ESP32");
    mqttClient.subscribe(topicHeizung);
    delay(500);
  }
}
void setup()
{
  Serial.begin(9600);
  while (!Serial)
  {
    ;
  }
  int counter = 0;
  dht.begin();
  tft.begin();
  tft.setRotation(1);

  WiFi.begin(ssid, password);
  while (WiFi.status() != WL_CONNECTED && counter < 10)
  {
    delay(1000);
    Serial.println("Connecting to WiFi...");
    counter++;
  }
  Serial.println("Connected");
  Serial.println(WiFi.localIP());

  configTime(gmtOffset_sec, daylightOffset_sec, ntpServer); // Set the time server
  Serial.print("Time set: ");
  Serial.println(localTime());

  pinMode(batteryPin, INPUT);
  //////////////////TODO//////////////////////////////////////
  mqttClient.setServer(broker, mqqtPort);
  connectMQTT();
  mqttClient.publish("testlol", "hiersteht was");
}

void loop()
{
  showSCrn();
  String dataString = localTime() + csvSperator + temperature + csvSperator + humidity;
  Serial.println(dataString);
  Serial.println(batteryPercantage());
  delay(1000);
}