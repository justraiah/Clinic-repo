// ESP32 + MAX30102 -> CMS
// Request-driven mode: measure ONLY when the CMS requests GET /
//
// Required secrets.h:
// WIFI_SSID, WIFI_PASSWORD

#include <Wire.h>
#include <MAX30105.h>
#include "spo2_algorithm.h"
#include <WiFi.h>
#include <WebServer.h>
#include "secrets.h"

const byte TOTAL_MEASUREMENTS = 8;
const int MIN_VALID_READINGS = 3;
const long FINGER_THRESHOLD = 50000;
const unsigned long FINGER_WAIT_MS = 10000;
const unsigned long SAMPLE_TIMEOUT_MS = 2000;

const byte SAMPLES = 100;
uint32_t irBuffer[SAMPLES];
uint32_t redBuffer[SAMPLES];

WebServer server(80);
MAX30105 particleSensor;

int beatAvg = 0;
int32_t spo2 = 0;
bool fingerDetected = false;

int32_t heartRateFromSpo2 = 0;
int8_t validHeartRate = 0;
int32_t spo2FromAlgorithm = 0;
int8_t validSpO2 = 0;

bool readNextSample(uint32_t &red, uint32_t &ir) {
  unsigned long start = millis();

  while (!particleSensor.available()) {
    particleSensor.check();

    if (millis() - start >= SAMPLE_TIMEOUT_MS) {
      return false;
    }

    delay(1);
  }

  red = particleSensor.getRed();
  ir = particleSensor.getIR();
  particleSensor.nextSample();
  return true;
}

String createJsonResponse() {
  String json = "{";
  json += "\"success\":true,";
  json += "\"heartRate\":" + String(beatAvg) + ",";
  json += "\"spo2\":" + String(spo2) + ",";
  json += "\"fingerDetected\":true";
  json += "}";

  return json;
}

bool measureVitals() {
  fingerDetected = false;
  beatAvg = 0;
  spo2 = 0;

  Serial.println("Request received. Waiting for finger...");

  // Wait for a fresh sensor sample showing a finger.
  unsigned long start = millis();
  bool foundFinger = false;

  while (millis() - start < FINGER_WAIT_MS) {
    uint32_t red, ir;

    if (readNextSample(red, ir)) {
      if (ir >= FINGER_THRESHOLD) {
        foundFinger = true;
        break;
      }
    }

    delay(1);
  }

  if (!foundFinger) {
    Serial.println("Timed out: no finger detected.");
    return false;
  }

  fingerDetected = true;
  Serial.println("Finger detected. Measuring...");

  particleSensor.clearFIFO();

  long hrSum = 0;
  long spo2Sum = 0;
  int validCount = 0;

  for (int m = 0; m < TOTAL_MEASUREMENTS; m++) {
    bool ok = true;

    for (int i = 0; i < SAMPLES; i++) {
      if (!readNextSample(redBuffer[i], irBuffer[i])) {
        ok = false;
        break;
      }
    }

    if (!ok) {
      Serial.println("Sensor stopped delivering samples.");
      break;
    }

    maxim_heart_rate_and_oxygen_saturation(
      irBuffer, SAMPLES, redBuffer,
      &spo2FromAlgorithm, &validSpO2,
      &heartRateFromSpo2, &validHeartRate
    );

    bool good = validHeartRate && validSpO2 &&
                heartRateFromSpo2 >= 55 &&
                heartRateFromSpo2 <= 120 &&
                spo2FromAlgorithm >= 90 &&
                spo2FromAlgorithm <= 100;

    Serial.print(good ? "Accepted" : "Rejected");
    Serial.print(" HR=");
    Serial.print(heartRateFromSpo2);
    Serial.print(" SpO2=");
    Serial.println(spo2FromAlgorithm);

    if (good) {
      hrSum += heartRateFromSpo2;
      spo2Sum += spo2FromAlgorithm;
      validCount++;
    }
  }

  if (validCount < MIN_VALID_READINGS) {
    Serial.println("Not enough stable readings.");
    fingerDetected = false;
    return false;
  }

  beatAvg = hrSum / validCount;
  spo2 = spo2Sum / validCount;

  Serial.print("Average HR: ");
  Serial.println(beatAvg);
  Serial.print("Average SpO2: ");
  Serial.println(spo2);

  return true;
}

void handleRoot() {
  // Prevent browser or proxy caching of an old measurement.
  server.sendHeader("Cache-Control", "no-store");

  if (!measureVitals()) {
    server.send(408, "application/json",
      "{\"success\":false,\"message\":\"No finger detected or insufficient stable readings\",\"fingerDetected\":false}");
    return;
  }

  server.send(200, "application/json", createJsonResponse());
  Serial.println("Measurement returned to requester.");
}


const char* AP_SSID = "CMS-Clinic-Sensor";
const char* AP_PASSWORD = "ClinicDemo26";

void connectWiFi() {
  WiFi.mode(WIFI_AP);
  WiFi.setSleep(false);

  bool started = WiFi.softAP(AP_SSID, AP_PASSWORD);

  if (!started) {
    Serial.println("ESP32 access point failed to start.");
    return;
  }

  Serial.println("ESP32 access point started.");
  Serial.print("Network name: ");
  Serial.println(AP_SSID);
  Serial.print("ESP32 IP: ");
  Serial.println(WiFi.softAPIP());
}


void setup() {
  Serial.begin(115200);
  delay(1000);

  Serial.println("\n===== ESP32 Clinic Sensor: Request-Driven Mode =====");

  Wire.begin(21, 22);

  if (!particleSensor.begin(Wire, I2C_SPEED_FAST)) {
    Serial.println("MAX30102 NOT FOUND!");
    while (true) delay(1000);
  }

  particleSensor.setup();
  particleSensor.setPulseAmplitudeRed(0x0A);
  particleSensor.setPulseAmplitudeGreen(0);

  Serial.println("MAX30102 ready.");

connectWiFi();

if (WiFi.getMode() != WIFI_AP) {
  Serial.println("Access point failed. Restarting...");
  delay(3000);
  ESP.restart();
}

Serial.print("ESP32 IP: ");
Serial.println(WiFi.softAPIP());  

  server.on("/health", HTTP_GET, []() {
  server.sendHeader("Cache-Control", "no-store");
  server.send(200, "application/json",
              "{\"status\":\"online\",\"sensor\":\"MAX30102\"}");
});

server.on("/", HTTP_GET, handleRoot);
server.begin();

  Serial.println("Web server ready. Waiting for a CMS request.");
}

void loop() {
  server.handleClient();
  delay(2);
}
