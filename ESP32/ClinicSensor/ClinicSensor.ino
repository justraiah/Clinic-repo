#include <Wire.h>
#include <MAX30105.h>
#include <heartRate.h>
#include "spo2_algorithm.h"
#include <WiFi.h>
#include <WebServer.h>

const char* ssid = "DSP 2.4Ghz";
const char* password = "";

const byte TOTAL_MEASUREMENTS = 5;
const byte MIN_VALID_READINGS = 3;

WebServer server(80);
MAX30105 particleSensor;

const byte RATE_SIZE = 4;

byte rates[RATE_SIZE];
byte rateSpot = 0;
long lastBeat = 0;

// -------- Current Measurements --------
float beatsPerMinute = 0;
int beatAvg = 0;

int32_t spo2 = 0;

bool fingerDetected = false;

// -------- Measurement Buffers --------
const byte SPO2_SAMPLE_SIZE = 100;

uint32_t irBuffer[SPO2_SAMPLE_SIZE];
uint32_t redBuffer[SPO2_SAMPLE_SIZE];

// -------- Maxim Algorithm Results --------
int32_t heartRateFromSpo2 = 0;
int8_t validHeartRate = 0;

int32_t spo2FromAlgorithm = 0;
int8_t validSpO2 = 0;

bool measurementInProgress = false;
bool measurementComplete = false;

bool measureVitals();


String createJsonResponse()
{
    String json = "{";

    json += "\"heartRate\":" + String(beatAvg) + ",";
    json += "\"spo2\":" + String(spo2) + ",";
    json += "\"fingerDetected\":" + String(fingerDetected ? "true" : "false");

    json += "}";

    return json;
}

void handleRoot()
{
    Serial.println("Request received");

    if (!measureVitals())
    {
        server.send(
            400,
            "application/json",
            "{\"status\":\"error\",\"message\":\"No finger detected\"}"
        );

        return;
    }

    Serial.println("Measurement finished.");
    Serial.println(createJsonResponse());

    server.send(
        200,
        "application/json",
        createJsonResponse()
    );

    Serial.println("Response sent");
}

void setup()
{
  Serial.begin(115200);
  delay(1000);

  Serial.println();
  Serial.println("===== ESP32 Clinic IoT =====");
  Serial.print("Connecting to WiFi: ");
  Serial.println(ssid);

WiFi.mode(WIFI_OFF);
delay(1000);

WiFi.mode(WIFI_STA);
delay(1000);

WiFi.begin(ssid, password);

int retries = 0;

while (WiFi.status() != WL_CONNECTED && retries < 40)
{
    delay(500);
    Serial.print(".");
    retries++;
}

  Serial.println();

  if (WiFi.status() == WL_CONNECTED)
  {
    Serial.println("WiFi Connected!");
    Serial.print("ESP32 IP Address: ");
    Serial.println(WiFi.localIP());

    server.on("/", handleRoot);
    server.begin();

    Serial.println("Web server started!");
  }
  else
  {
    Serial.println("WiFi FAILED!");
    Serial.print("Status Code: ");
    Serial.println(WiFi.status());

    Serial.println();
    Serial.println("WiFi connection failed.");

    int n = WiFi.scanNetworks();

    Serial.println("WiFi FAILED!");
    Serial.print("Status Code: ");
    Serial.println(WiFi.status());

    Serial.println("WiFi connection failed.");

    delay(3000);

    ESP.restart();
  }

  Wire.begin(21, 22);

  Serial.println("Initializing sensor...");

  if (!particleSensor.begin(Wire, I2C_SPEED_FAST))
  {
    Serial.println("MAX30102 NOT FOUND!");
    while (1);
  }

  Serial.println("MAX30102 FOUND!");

  particleSensor.setup();
  particleSensor.setPulseAmplitudeRed(0x0A);
  particleSensor.setPulseAmplitudeGreen(0);
}

bool measureVitals()
{
    measurementInProgress = true;
    measurementComplete = false;

    unsigned long startTime = millis();

    while (particleSensor.getIR() < 50000)
    {
        particleSensor.check();

        if (millis() - startTime > 10000)
        {
            measurementInProgress = false;
            fingerDetected = false;
            return false;
        }

        delay(10);
    }

    fingerDetected = true;

    long hrSum = 0;
    long spo2Sum = 0;
    int validCount = 0;

    for (int measurement = 0; measurement < TOTAL_MEASUREMENTS; measurement++)
    {
        for (int i = 0; i < 100; i++)
        {
            while (!particleSensor.available())
{
    particleSensor.check();
    yield();
}

            redBuffer[i] = particleSensor.getRed();
            irBuffer[i] = particleSensor.getIR();

            particleSensor.nextSample();
        }

        maxim_heart_rate_and_oxygen_saturation(
            irBuffer,
            100,
            redBuffer,
            &spo2FromAlgorithm,
            &validSpO2,
            &heartRateFromSpo2,
            &validHeartRate
        );

        if (validHeartRate &&
    validSpO2 &&
    heartRateFromSpo2 >= 55 &&
    heartRateFromSpo2 <= 120 &&
    spo2FromAlgorithm >= 90 &&
    spo2FromAlgorithm <= 100)
{
    hrSum += heartRateFromSpo2;
    spo2Sum += spo2FromAlgorithm;
    validCount++;

    Serial.print("Accepted HR=");
    Serial.print(heartRateFromSpo2);

    Serial.print("  SpO2=");
    Serial.println(spo2FromAlgorithm);
}
else
{
    Serial.print("Rejected HR=");
    Serial.print(heartRateFromSpo2);

    Serial.print("  SpO2=");
    Serial.println(spo2FromAlgorithm);
}

// Give the sensor a moment before the next measurement
delay(250);
    }

    measurementInProgress = false;
    measurementComplete = true;

    const int MIN_VALID_READINGS = 3;

if (validCount < MIN_VALID_READINGS)
{
    Serial.println("---------------------");
    Serial.println("Measurement failed.");
    Serial.print("Only ");
    Serial.print(validCount);
    Serial.println(" valid reading(s).");
    Serial.println("---------------------");

    fingerDetected = false;
    return false;
}

    beatAvg = hrSum / validCount;
    spo2 = spo2Sum / validCount;

    Serial.println("---------------------");
    Serial.print("Average HR: ");
    Serial.println(beatAvg);

    Serial.print("Average SpO2: ");
    Serial.println(spo2);
    Serial.println("---------------------");

    return true;
}
void loop()
{
  if (WiFi.status() != WL_CONNECTED)
  {
    Serial.println("WiFi disconnected. Reconnecting...");

    WiFi.disconnect();
    WiFi.begin(ssid, password);

    delay(1000);
    return;
  }
server.handleClient();

  particleSensor.check();

  redBuffer[0] = particleSensor.getRed();
irBuffer[0] = particleSensor.getIR();

  long irValue = particleSensor.getIR();
  fingerDetected = (irValue > 50000);

  if (checkForBeat(irValue))
  {
    long delta = millis() - lastBeat;
    lastBeat = millis();

    beatsPerMinute = 60 / (delta / 1000.0);

    if (beatsPerMinute < 255 && beatsPerMinute > 20)
    {
      rates[rateSpot++] = (byte)beatsPerMinute;
      rateSpot %= RATE_SIZE;

      beatAvg = 0;
      for (byte x = 0; x < RATE_SIZE; x++)
        beatAvg += rates[x];
      beatAvg /= RATE_SIZE;
    }
  }

  // Serial.print("IR=");
  // Serial.print(irValue);

  // Serial.print(" BPM=");
  // Serial.print(beatsPerMinute);

  // Serial.print(" Avg BPM=");
  // Serial.print(beatAvg);

  // if (irValue < 50000)
  //   Serial.print(" No finger?");

  // Serial.println();

yield();
}
