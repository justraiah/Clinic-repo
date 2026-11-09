// ESP32 + MAX30102 -> live CMS
// Station mode: poll the public CMS for a measurement request, then submit the result.
//
// Copy secrets.h.example to secrets.h and set:
// WIFI_SSID, WIFI_PASSWORD, CMS_BASE_URL (https), IOT_API_KEY

#include <Wire.h>
#include <MAX30105.h>
#include "spo2_algorithm.h"
#include <WiFi.h>
#include <HTTPClient.h>
#include <WiFiClientSecure.h>
#include "secrets.h"

const byte TOTAL_MEASUREMENTS = 8;
const int MIN_VALID_READINGS = 3;
const long FINGER_THRESHOLD = 50000;
const unsigned long FINGER_WAIT_MS = 10000;
const unsigned long SAMPLE_TIMEOUT_MS = 2000;
const unsigned long POLL_INTERVAL_MS = 2000;

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

String cmsBaseUrl;

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

  Serial.println("Request claimed. Waiting for finger...");

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

bool httpsRequest(
  const String &path,
  bool post,
  const String &body,
  int &statusCode,
  String &response) {
  WiFiClientSecure client;

  if (strlen(CMS_ROOT_CA) == 0) {
    client.setInsecure();
  } else {
    client.setCACert(CMS_ROOT_CA);
  }

  HTTPClient http;
  http.setTimeout(15000);

  if (!http.begin(client, cmsBaseUrl + path)) {
    statusCode = -1;
    return false;
  }

  http.addHeader("Accept", "application/json");
  http.addHeader("X-IoT-Key", IOT_API_KEY);

  if (post) {
    http.addHeader("Content-Type", "application/json");
    statusCode = http.POST(body);
  } else {
    statusCode = http.GET();
  }

  if (statusCode > 0) {
    response = http.getString();
  }

  http.end();
  return statusCode > 0;
}

bool extractRequestId(const String &payload, String &requestId) {
  const char *key = "\"requestId\":\"";
  int start = payload.indexOf(key);

  if (start < 0) {
    return false;
  }

  start += strlen(key);
  int end = payload.indexOf('"', start);

  if (end <= start) {
    return false;
  }

  requestId = payload.substring(start, end);
  return requestId.length() >= 32;
}

void submitResult(const String &requestId, int heartRate, int oxygen, bool finger) {
  String body = "{";
  body += "\"requestId\":\"" + requestId + "\",";
  body += "\"heartRate\":" + String(heartRate) + ",";
  body += "\"spo2\":" + String(oxygen) + ",";
  body += "\"fingerDetected\":";
  body += finger ? "true" : "false";
  body += "}";

  int statusCode = 0;
  String response;
  bool sent = httpsRequest(
    "/api/iot/vital-signs/device/result",
    true,
    body,
    statusCode,
    response);

  Serial.print("Result HTTP status: ");
  Serial.println(sent ? statusCode : -1);
}

void pollForWork() {
  int statusCode = 0;
  String response;

  bool reached = httpsRequest(
    "/api/iot/vital-signs/device/next",
    false,
    "",
    statusCode,
    response);

  if (!reached) {
    Serial.println("Could not reach the live CMS.");
    return;
  }

  if (statusCode == 204) {
    return;
  }

  if (statusCode == 401) {
    Serial.println("Live CMS rejected the device key.");
    return;
  }

  if (statusCode != 200) {
    Serial.print("Unexpected poll status: ");
    Serial.println(statusCode);
    return;
  }

  String requestId;
  if (!extractRequestId(response, requestId)) {
    Serial.println("CMS response did not include a request id.");
    return;
  }

  Serial.println("Measurement request claimed.");

  if (!measureVitals()) {
    submitResult(requestId, 0, 0, false);
    return;
  }

  submitResult(requestId, beatAvg, spo2, true);
}

void connectWiFi() {
  WiFi.mode(WIFI_STA);
  WiFi.setSleep(false);
  WiFi.begin(WIFI_SSID, WIFI_PASSWORD);

  Serial.print("Connecting to Wi-Fi");

  for (int attempt = 0; attempt < 40 && WiFi.status() != WL_CONNECTED; attempt++) {
    delay(500);
    Serial.print(".");
  }

  Serial.println();

  if (WiFi.status() != WL_CONNECTED) {
    Serial.println("Wi-Fi connection failed.");
    return;
  }

  Serial.print("Wi-Fi connected. ESP32 IP: ");
  Serial.println(WiFi.localIP());
}

void setup() {
  Serial.begin(115200);
  delay(1000);

  Serial.println("\n===== ESP32 Clinic Sensor: Live CMS polling =====");

  cmsBaseUrl = CMS_BASE_URL;
  while (cmsBaseUrl.endsWith("/")) {
    cmsBaseUrl.remove(cmsBaseUrl.length() - 1);
  }

  if (!cmsBaseUrl.startsWith("https://")) {
    Serial.println("CMS_BASE_URL must be the public https address of the live CMS.");
    while (true) delay(1000);
  }

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

  if (WiFi.status() != WL_CONNECTED) {
    Serial.println("Restarting to retry Wi-Fi...");
    delay(3000);
    ESP.restart();
  }

  if (strlen(CMS_ROOT_CA) == 0) {
    Serial.println("HTTPS is on. Server certificate verification is not configured.");
  }
}

void loop() {
  if (WiFi.status() != WL_CONNECTED) {
    Serial.println("Wi-Fi dropped. Reconnecting...");
    connectWiFi();
    delay(2000);
    return;
  }

  pollForWork();
  delay(POLL_INTERVAL_MS);
}
