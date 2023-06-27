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
DHT dht(dhtpin, DHT22);
TFT_eSPI tft = TFT_eSPI();

float temperature;
float humidity;

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

void setup() {
  // put your setup code here, to run once:
}

void loop() {
  // put your main code here, to run repeatedly:
}