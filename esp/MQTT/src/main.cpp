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
const int heizungPin = 32;

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
const char *topicHumid = "/feucht";
const char *topicTemper = "/temper";
const char *topicBattery = "/batterie";
bool heizungAn = false;

float batteryPercentage()
{
  int input = (analogRead(batteryPin));
  float percentage = map(input, 0, 4095, 0.0, 100.00);
  return percentage;
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
void subscribe()
{
  if (true)
  {
    mqttClient.subscribe(topicHeizung);
    mqttClient.subscribe("test");
  }
}
void heizung(char *topic, byte *payload, unsigned int length)
{

  String msg;
  for (byte i = 0; i < length; i++)
  {
    char tmp = char(payload[i]);
    msg += tmp;
  }
  Serial.print(msg);
  Serial.println(topic);
  if (strcmp(topic, topicHeizung) == 0)
  {
    if (msg == "1" || msg == "true")
    {
      heizungAn = true;
    }
    else if (msg == "0" || msg == "false")
    {
      heizungAn = false;
    }
    else
    {
      Serial.println("invalid input");
    }
  }
  digitalWrite(heizungPin, heizungAn);
}
void connectMQTT()
{
  for (size_t i = 0; i < 5; i++)
  {
    if (!mqttClient.connected())
    {
      Serial.println("Connecting mqtt... ");
      mqttClient.connect("ESP32");
      mqttClient.subscribe(topicHeizung);
      delay(1000);
    }
    else
    {
      return;
    }
  }
  Serial.println("Connecting failed");
}
void publish()
{
  String batt = String(batteryPercentage(), 2);
  String hum = String(humidity, 2);
  String temper = String(temperature, 2);
  String heiz = String(digitalRead(heizungPin));
  mqttClient.publish(topicBattery, batt.c_str());
  mqttClient.publish(topicHumid, hum.c_str());
  mqttClient.publish(topicTemper, temper.c_str());
  // mqttClient.publish(topicHeizung, heiz.c_str());
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
  pinMode(heizungPin, OUTPUT);
  mqttClient.setServer(broker, mqqtPort);
  connectMQTT();
  subscribe();
  mqttClient.setCallback(heizung);
}

void loop()
{
  showSCrn();
  String dataString = localTime() + csvSperator + temperature + csvSperator + humidity;
  Serial.println(dataString);
  Serial.println(batteryPercentage());

  if (!mqttClient.connected())
  {
    connectMQTT();
  }
  else
  {
    mqttClient.loop();
    publish();
  }
  delay(1 * 60 * 1000);
}