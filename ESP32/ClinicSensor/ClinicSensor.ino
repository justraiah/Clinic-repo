#include <Wire.h>
#include <MAX30105.h>
#include "spo2_algorithm.h"
#include <WiFi.h>
#include <WiFiClientSecure.h>
#include <HTTPClient.h>
#include <ArduinoJson.h>
#include "secrets.h"

// Live CMS. Override only by defining CMS_BASE_URL in secrets.h.
// The device refuses a non-HTTPS address, including a laptop LAN IP.
#ifndef CMS_BASE_URL
#define CMS_BASE_URL "https://cdm-clinic.runasp.net"
#endif

#ifndef CMS_ROOT_CA
#define CMS_ROOT_CA ""
#endif

const byte TOTAL_MEASUREMENTS = 8;
const int MIN_VALID_READINGS = 3;
const long FINGER_THRESHOLD = 50000;
const unsigned long FINGER_WAIT_MS = 10000;
const unsigned long SAMPLE_TIMEOUT_MS = 2000;

const byte SAMPLES = 100;
uint32_t irBuffer[SAMPLES];
uint32_t redBuffer[SAMPLES];

MAX30105 particleSensor;

int beatAvg = 0;
int32_t spo2 = 0;
bool fingerDetected = false;

int32_t heartRateFromSpo2 = 0;
int8_t validHeartRate = 0;
int32_t spo2FromAlgorithm = 0;
int8_t validSpO2 = 0;

unsigned long lastPoll = 0;
const unsigned long POLL_INTERVAL_MS = 1500;

String cmsBaseUrl;
String nextRequestUrl;
String resultUrl;

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

bool measureVitals() {
  fingerDetected = false;
  beatAvg = 0;
  spo2 = 0;

  Serial.println("CMS request received. Waiting for finger...");

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
      irBuffer,
      SAMPLES,
      redBuffer,
      &spo2FromAlgorithm,
      &validSpO2,
      &heartRateFromSpo2,
      &validHeartRate
    );

    bool good =
      validHeartRate &&
      validSpO2 &&
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

void prepareSecureClient(WiFiClientSecure &client) {
  if (strlen(CMS_ROOT_CA) == 0) {
    client.setInsecure();
  } else {
    client.setCACert(CMS_ROOT_CA);
  }
}

bool claimCMSRequest(String &requestId) {
  WiFiClientSecure client;
  HTTPClient http;

  prepareSecureClient(client);

  if (!http.begin(client, nextRequestUrl)) {
    Serial.println("Could not initialize CMS request.");
    return false;
  }

  http.setTimeout(10000);
  http.addHeader("X-IoT-Key", IOT_API_KEY);
  http.addHeader("Accept", "application/json");

  Serial.println("Polling live CMS for a pending request...");

  int statusCode = http.GET();

  Serial.print("CMS HTTP Response Code: ");
  Serial.println(statusCode);

  if (statusCode < 0) {
    Serial.print("CMS connection error: ");
    Serial.println(http.errorToString(statusCode));
    http.end();
    return false;
  }

  if (statusCode == 204) {
    Serial.println("No pending CMS request.");
    http.end();
    return false;
  }

  if (statusCode != 200) {
    Serial.print("CMS polling HTTP status: ");
    Serial.println(statusCode);
    Serial.println(http.getString());
    http.end();
    return false;
  }

  String response = http.getString();
  http.end();

  JsonDocument doc;
  DeserializationError error = deserializeJson(doc, response);

  if (error) {
    Serial.print("JSON parsing failed: ");
    Serial.println(error.c_str());
    return false;
  }

  requestId = doc["requestId"].as<String>();

  if (requestId.length() == 0) {
    Serial.println("CMS response has no RequestId.");
    return false;
  }

  Serial.print("Claimed CMS RequestId: ");
  Serial.println(requestId);

  return true;
}

bool sendResult(
  String requestId,
  int heartRate,
  int oxygenSaturation,
  bool finger
) {
  WiFiClientSecure client;
  HTTPClient http;

  prepareSecureClient(client);

  if (!http.begin(client, resultUrl)) {
    Serial.println("Could not initialize result POST.");
    return false;
  }

  http.setTimeout(15000);
  http.addHeader("Content-Type", "application/json");
  http.addHeader("X-IoT-Key", IOT_API_KEY);

  JsonDocument doc;
  doc["requestId"] = requestId;
  doc["heartRate"] = heartRate;
  doc["spo2"] = oxygenSaturation;
  doc["fingerDetected"] = finger;

  String payload;
  serializeJson(doc, payload);

  Serial.println("Sending result to live CMS...");
  Serial.println(payload);

  int statusCode = http.POST(payload);

  if (statusCode < 0) {
    Serial.print("CMS POST connection error: ");
    Serial.println(http.errorToString(statusCode));
    http.end();
    return false;
  }

  String response = http.getString();

  Serial.print("CMS result HTTP status: ");
  Serial.println(statusCode);
  Serial.println(response);

  http.end();

  return statusCode >= 200 && statusCode < 300;
}

void connectWiFi() {
  WiFi.mode(WIFI_STA);
  WiFi.setSleep(false);

  Serial.print("Connecting to clinic Wi-Fi");

  WiFi.begin(WIFI_SSID, WIFI_PASSWORD);

  unsigned long start = millis();

  while (WiFi.status() != WL_CONNECTED) {
    delay(500);
    Serial.print(".");

    if (millis() - start >= 30000) {
      Serial.println("\nWi-Fi connection timeout. Restarting...");
      delay(2000);
      ESP.restart();
    }
  }

  Serial.println("\nWi-Fi connected.");
  Serial.print("ESP32 IP: ");
  Serial.println(WiFi.localIP());
}

void setup() {
  Serial.begin(115200);
  delay(1000);

  Serial.println("\n===== ESP32 Clinic Sensor =====");

  cmsBaseUrl = CMS_BASE_URL;
  while (cmsBaseUrl.endsWith("/")) {
    cmsBaseUrl.remove(cmsBaseUrl.length() - 1);
  }

  if (!cmsBaseUrl.startsWith("https://")) {
    Serial.println("CMS_BASE_URL must be the public https address of the live CMS.");
    while (true) {
      delay(1000);
    }
  }

  nextRequestUrl = cmsBaseUrl + "/api/iot/vital-signs/device/next";
  resultUrl = cmsBaseUrl + "/api/iot/vital-signs/device/result";

  Serial.print("Live CMS: ");
  Serial.println(cmsBaseUrl);

  if (strlen(CMS_ROOT_CA) == 0) {
    Serial.println("HTTPS is on. Server certificate verification is not configured.");
  }

  Wire.begin(21, 22);

  if (!particleSensor.begin(Wire, I2C_SPEED_FAST)) {
    Serial.println("MAX30102 NOT FOUND!");

    while (true) {
      delay(1000);
    }
  }

  particleSensor.setup();
  particleSensor.setPulseAmplitudeRed(0x0A);
  particleSensor.setPulseAmplitudeGreen(0);

  Serial.println("MAX30102 ready.");

  connectWiFi();

  Serial.println("ESP32 is ready to poll the live CMS.");
}

void loop() {
  if (WiFi.status() != WL_CONNECTED) {
    Serial.println("Wi-Fi disconnected. Reconnecting...");
    WiFi.reconnect();
    delay(1000);
    return;
  }

  if (millis() - lastPoll < POLL_INTERVAL_MS) {
    delay(10);
    return;
  }

  lastPoll = millis();

  String requestId;

  if (!claimCMSRequest(requestId)) {
    return;
  }

  bool measurementSuccess = measureVitals();

  if (measurementSuccess) {
    bool sent = sendResult(
      requestId,
      beatAvg,
      spo2,
      true
    );

    if (sent) {
      Serial.println("Measurement successfully delivered to CMS.");
    } else {
      Serial.println("Measurement delivery failed.");
    }
  } else {
    Serial.println("Measurement unsuccessful.");
    Serial.println("Sending failure result to CMS.");

    sendResult(
      requestId,
      0,
      0,
      false
    );
  }

  Serial.println("Waiting for the next CMS request...");
}
