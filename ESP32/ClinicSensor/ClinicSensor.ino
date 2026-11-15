
#include <Wire.h>
#include <MAX30105.h>
#include "spo2_algorithm.h"
#include <WiFi.h>
#include <WiFiClient.h>
#include <WiFiClientSecure.h>
#include <HTTPClient.h>
#include <ArduinoJson.h>
#include "secrets.h"

// ---------------- CMS CONFIGURATION ----------------

#ifndef CMS_BASE_URL
#define CMS_BASE_URL "http://10.68.154.149:5018"
#endif

#ifndef CMS_ROOT_CA_CERT
#define CMS_ROOT_CA_CERT ""
#endif

String cmsBaseUrl = CMS_BASE_URL;
String nextRequestUrl;
String resultUrl;
String progressUrl;

// ---------------- SENSOR CONFIGURATION ----------------

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

// Forward declaration.
bool sendProgress(
    const String &requestId,
    int windowNumber,
    int totalWindows,
    int heartRate,
    int oxygenSaturation,
    bool hrValid,
    bool spo2Valid,
    bool accepted,
    const String &message
);

// ---------------- SENSOR FUNCTIONS ----------------

bool readNextSample(uint32_t &red, uint32_t &ir) {
    unsigned long start = millis();

    while (particleSensor.available() == false) {
        particleSensor.check();

        if (millis() - start >= SAMPLE_TIMEOUT_MS) {
            return false;
        }

        delay(1);
    }

    red = particleSensor.getFIFORed();
    ir = particleSensor.getFIFOIR();

    particleSensor.nextSample();

    return true;
}

bool measureVitals(const String &requestId) {
    unsigned long measurementStartMs = millis();
    fingerDetected = false;
    beatAvg = 0;
    spo2 = 0;

    Serial.println("CMS request received. Waiting for finger...");

    unsigned long fingerDetectionStartMs = millis();
    bool foundFinger = false;
    Serial.println("Checking initial sensor signal...");
    while (millis() - fingerDetectionStartMs < FINGER_WAIT_MS) {
        uint32_t red, ir;

        if (readNextSample(red, ir)) {
            if (ir >= FINGER_THRESHOLD) {
                Serial.print("Finger detected at IR: ");
                Serial.println(ir);

                foundFinger = true;
                break;
            }
        }
    }

    unsigned long fingerDetectionElapsedMs =
    millis() - fingerDetectionStartMs;

    Serial.print("FINGER DETECTION TIME: ");
    Serial.print(fingerDetectionElapsedMs);
    Serial.println(" ms");

    if (!foundFinger) {
        Serial.println("Timed out: no finger detected.");
        return false;
    }

    fingerDetected = true;

    Serial.println("Finger detected. Stabilizing signal...");

    particleSensor.clearFIFO();

    unsigned long settleStart = millis();

    while (millis() - settleStart < 2000) {
        uint32_t discardRed, discardIr;

        if (!readNextSample(discardRed, discardIr)) {
            Serial.println("Warning: sensor signal not stabilizing.");
            break;
        }
    }
    unsigned long stabilizationElapsedMs =
    millis() - settleStart;

    Serial.print("STABILIZATION TIME: ");
    Serial.print(stabilizationElapsedMs);
    Serial.println(" ms");

    particleSensor.clearFIFO();

    Serial.println("Signal stabilization complete. Measuring...");

    long hrSum = 0;
    long spo2Sum = 0;
    int validCount = 0;

    for (int m = 0; m < TOTAL_MEASUREMENTS; m++) {

        unsigned long windowStartMs = millis();

        bool ok = true;

        int samplesToRead = (m == 0) ? SAMPLES : 25;

        if (m > 0) {
            for (int i = 0; i < SAMPLES - 25; i++) {
                irBuffer[i] = irBuffer[i + 25];
                redBuffer[i] = redBuffer[i + 25];
            }
        }

        int startIndex = (m == 0) ? 0 : SAMPLES - 25;

        // Measure actual sample collection duration.
        unsigned long collectionStartMs = millis();

        for (int i = 0; i < samplesToRead; i++) {

            if (!readNextSample(
                    redBuffer[startIndex + i],
                    irBuffer[startIndex + i])) {

                ok = false;
                break;
            }
        }

        unsigned long collectionElapsedMs =
            millis() - collectionStartMs;

        Serial.print("Samples collected: ");
        Serial.println(ok ? samplesToRead : 0);

        Serial.print("Collection duration: ");
        Serial.print(collectionElapsedMs);
        Serial.println(" ms");

        if (ok && collectionElapsedMs > 0) {
            float measuredHz =
                (float)samplesToRead * 1000.0 /
                collectionElapsedMs;

            Serial.print("Approx. acquisition rate: ");
            Serial.print(measuredHz, 2);
            Serial.println(" samples/sec");
        }

        if (!ok) {
            Serial.println("Sensor stopped delivering samples.");

            sendProgress(
                requestId,
                m + 1,
                TOTAL_MEASUREMENTS,
                -999,
                -999,
                false,
                false,
                false,
                "Sensor stopped delivering samples."
            );

            break;
        }

        heartRateFromSpo2 = -999;
        spo2FromAlgorithm = -999;
        validHeartRate = 0;
        validSpO2 = 0;

        // Measure Maxim algorithm processing duration precisely.
        unsigned long algorithmStartUs = micros();

        maxim_heart_rate_and_oxygen_saturation(
            irBuffer,
            SAMPLES,
            redBuffer,
            &spo2FromAlgorithm,
            &validSpO2,
            &heartRateFromSpo2,
            &validHeartRate
        );
        Serial.println("\n--- RAW ALGORITHM OUTPUT ---");

        Serial.print("Raw HR: ");
        Serial.println(heartRateFromSpo2);

        Serial.print("HR valid flag: ");
        Serial.println((int)validHeartRate);

        Serial.print("Raw SpO2: ");
        Serial.println(spo2FromAlgorithm);

        Serial.print("SpO2 valid flag: ");
        Serial.println((int)validSpO2);

        Serial.println("----------------------------");

        unsigned long algorithmElapsedUs =
            micros() - algorithmStartUs;

        Serial.print("Algorithm duration: ");
        Serial.print(algorithmElapsedUs);
        Serial.println(" microseconds");

        uint32_t irMin = UINT32_MAX;
        uint32_t irMax = 0;
        uint32_t redMin = UINT32_MAX;
        uint32_t redMax = 0;

        for (int i = 0; i < SAMPLES; i++) {
            if (irBuffer[i] < irMin) irMin = irBuffer[i];
            if (irBuffer[i] > irMax) irMax = irBuffer[i];

            if (redBuffer[i] < redMin) redMin = redBuffer[i];
            if (redBuffer[i] > redMax) redMax = redBuffer[i];
        }

        Serial.println("\n--- ROLLING WINDOW DIAGNOSTIC ---");

        Serial.print("Window: ");
        Serial.println(m + 1);

        Serial.print("IR range: ");
        Serial.print(irMin);
        Serial.print(" - ");
        Serial.println(irMax);

        Serial.print("RED range: ");
        Serial.print(redMin);
        Serial.print(" - ");
        Serial.println(redMax);

        Serial.println("---------------------------------");

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

        String progressMessage;

        if (good) {
            hrSum += heartRateFromSpo2;
            spo2Sum += spo2FromAlgorithm;
            validCount++;

            progressMessage = "Reading accepted.";
        } else {
            progressMessage =
                "Reading rejected: invalid or out-of-range values.";
                }
                // Send progress after EVERY measurement window.

                unsigned long progressStartMs = millis();

                bool progressSent = sendProgress(
                    requestId,
                    m + 1,
                    TOTAL_MEASUREMENTS,
                    heartRateFromSpo2,
                    spo2FromAlgorithm,
                    validHeartRate != 0,
                    validSpO2 != 0,
                    good,
                    progressMessage
                );

                unsigned long progressElapsedMs =
                    millis() - progressStartMs;

                Serial.print("Progress HTTP duration: ");
                Serial.print(progressElapsedMs);
                Serial.println(" ms");

                if (progressSent) {
                    Serial.print("Progress delivered: window ");
                    Serial.println(m + 1);
                } else {
                    Serial.print("Progress delivery failed: window ");
                    Serial.println(m + 1);
                }
        // Total time for this window.
        unsigned long windowElapsedMs =
            millis() - windowStartMs;

        Serial.print("WINDOW TOTAL DURATION: ");
        Serial.print(windowElapsedMs);
        Serial.println(" ms");

        Serial.println("---------------------------------");
        }

    if (validCount < MIN_VALID_READINGS) {
        Serial.println("Not enough valid readings.");

        fingerDetected = false;
        return false;
    }

    beatAvg = hrSum / validCount;
    spo2 = spo2Sum / validCount;

    Serial.print("Average HR: ");
    Serial.println(beatAvg);

    Serial.print("Average SpO2: ");
    Serial.println(spo2);

    Serial.print("TOTAL MEASUREMENT TIME: ");
    Serial.print(millis() - measurementStartMs);
    Serial.println(" ms");

    return true;
}
// ---------------- HTTPS SECURITY ----------------

bool prepareSecureClient(WiFiClientSecure &client) {
    if (strlen(CMS_ROOT_CA_CERT) == 0) {
        Serial.println(
            "ERROR: CMS root CA is missing. HTTPS connection refused."
        );

        return false;
    }

    client.setCACert(CMS_ROOT_CA_CERT);

    return true;
}

// ---------------- CMS PROGRESS SUBMISSION ----------------

template <typename TClient>
bool sendProgressWithClient(
    TClient &client,
    const String &requestId,
    int windowNumber,
    int totalWindows,
    int heartRate,
    int oxygenSaturation,
    bool hrValid,
    bool spo2Valid,
    bool accepted,
    const String &message
) {
    HTTPClient http;

    if (!http.begin(client, progressUrl)) {
        Serial.println("Could not initialize progress POST.");
        return false;
    }

    http.setTimeout(10000);

    http.addHeader("Content-Type", "application/json");
    http.addHeader("X-IoT-Key", IOT_API_KEY);

    JsonDocument doc;

    doc["requestId"] = requestId;
    doc["windowNumber"] = windowNumber;
    doc["totalWindows"] = totalWindows;
    doc["heartRate"] = heartRate;
    doc["spo2"] = oxygenSaturation;
    doc["validHeartRate"] = hrValid;
    doc["validSpO2"] = spo2Valid;
    doc["accepted"] = accepted;
    doc["message"] = message;

    String payload;
    serializeJson(doc, payload);

    Serial.println("Sending measurement progress...");
    Serial.println(payload);

    int statusCode = http.POST(payload);

    if (statusCode < 0) {
        Serial.print("Progress POST connection error: ");
        Serial.println(http.errorToString(statusCode));

        http.end();
        return false;
    }

    String response = http.getString();

    Serial.print("Progress HTTP status: ");
    Serial.println(statusCode);

    Serial.println(response);

    http.end();

    return statusCode >= 200 && statusCode < 300;
}

bool sendProgress(
    const String &requestId,
    int windowNumber,
    int totalWindows,
    int heartRate,
    int oxygenSaturation,
    bool hrValid,
    bool spo2Valid,
    bool accepted,
    const String &message
) {
    if (progressUrl.startsWith("https://")) {
        WiFiClientSecure client;

        if (!prepareSecureClient(client)) {
            return false;
        }

        return sendProgressWithClient(
            client,
            requestId,
            windowNumber,
            totalWindows,
            heartRate,
            oxygenSaturation,
            hrValid,
            spo2Valid,
            accepted,
            message
        );
    }

    if (progressUrl.startsWith("http://")) {
        WiFiClient client;

        return sendProgressWithClient(
            client,
            requestId,
            windowNumber,
            totalWindows,
            heartRate,
            oxygenSaturation,
            hrValid,
            spo2Valid,
            accepted,
            message
        );
    }

    Serial.println("Unsupported CMS URL scheme.");

    return false;
}

// ---------------- CMS REQUEST CLAIMING ----------------

template <typename TClient>
bool claimCMSRequestWithClient(
    TClient &client,
    String &requestId
) {
    HTTPClient http;

    if (!http.begin(client, nextRequestUrl)) {
        Serial.println("Could not initialize CMS request.");
        return false;
    }

    http.setTimeout(10000);

    http.addHeader("X-IoT-Key", IOT_API_KEY);
    http.addHeader("Accept", "application/json");

    Serial.println("Polling CMS for pending request...");

    int statusCode = http.GET();

    Serial.print("CMS HTTP Response Code: ");
    Serial.println(statusCode);

    Serial.print("WiFi Status: ");
    Serial.println(WiFi.status());

    Serial.print("ESP32 RSSI: ");
    Serial.print(WiFi.RSSI());
    Serial.println(" dBm");

    Serial.print("ESP32 IP: ");
    Serial.println(WiFi.localIP());

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

bool claimCMSRequest(String &requestId) {
    if (nextRequestUrl.startsWith("https://")) {
        WiFiClientSecure client;

        if (!prepareSecureClient(client)) {
            return false;
        }

        return claimCMSRequestWithClient(client, requestId);
    }

    if (nextRequestUrl.startsWith("http://")) {
        WiFiClient client;

        return claimCMSRequestWithClient(client, requestId);
    }

    Serial.println("Unsupported CMS URL scheme.");

    return false;
}

// ---------------- CMS RESULT SUBMISSION ----------------

template <typename TClient>
bool sendResultWithClient(
    TClient &client,
    const String &requestId,
    int heartRate,
    int oxygenSaturation,
    bool finger
) {
    HTTPClient http;

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

    Serial.println("Sending result to CMS...");
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

bool sendResult(
    const String &requestId,
    int heartRate,
    int oxygenSaturation,
    bool finger
) {
    const byte MAX_ATTEMPTS = 3;

    for (byte attempt = 1; attempt <= MAX_ATTEMPTS; attempt++) {

        Serial.print("Result submission attempt ");
        Serial.print(attempt);
        Serial.print("/");
        Serial.println(MAX_ATTEMPTS);

        if (WiFi.status() != WL_CONNECTED) {
            connectWiFi();
        }

        bool success = false;

        if (resultUrl.startsWith("https://")) {
            WiFiClientSecure client;

            if (!prepareSecureClient(client)) {
                return false;
            }

            success = sendResultWithClient(
                client,
                requestId,
                heartRate,
                oxygenSaturation,
                finger
            );
        }
        else if (resultUrl.startsWith("http://")) {
            WiFiClient client;

            success = sendResultWithClient(
                client,
                requestId,
                heartRate,
                oxygenSaturation,
                finger
            );
        }
        else {
            Serial.println("Unsupported CMS URL scheme.");
            return false;
        }

        if (success) {
            Serial.println("Final result successfully delivered.");
            return true;
        }

        if (attempt < MAX_ATTEMPTS) {
            Serial.println("Submission failed. Retrying in 2 seconds...");
            delay(2000);
        }
    }

    Serial.println("ERROR: Final result delivery failed after all attempts.");

    return false;
}

// ---------------- WIFI ----------------

void connectWiFi() {
    WiFi.mode(WIFI_STA);
    WiFi.setSleep(false);
    WiFi.setAutoReconnect(true);

    Serial.print("Connecting to clinic Wi-Fi");

    WiFi.begin(WIFI_SSID, WIFI_PASSWORD);

    unsigned long lastRetry = millis();

    while (WiFi.status() != WL_CONNECTED) {
        delay(500);
        Serial.print(".");

        if (millis() - lastRetry >= 15000) {
            Serial.println("\nRetrying Wi-Fi connection...");

            WiFi.disconnect(false, false);
            delay(250);
            WiFi.begin(WIFI_SSID, WIFI_PASSWORD);

            lastRetry = millis();
        }
    }

    Serial.println("\nWi-Fi connected.");

    Serial.print("ESP32 IP: ");
    Serial.println(WiFi.localIP());

    Serial.print("ESP32 RSSI: ");
    Serial.print(WiFi.RSSI());
    Serial.println(" dBm");
}

// ---------------- SETUP ----------------

void setup() {
    Serial.begin(115200);
    delay(1000);

    Serial.println("\n===== ESP32 Clinic Sensor =====");

    cmsBaseUrl.trim();

    while (cmsBaseUrl.endsWith("/")) {
        cmsBaseUrl.remove(cmsBaseUrl.length() - 1);
    }

    if (!cmsBaseUrl.startsWith("http://") &&
        !cmsBaseUrl.startsWith("https://")) {

        Serial.println("Invalid CMS_BASE_URL.");
        Serial.println("Use http:// for local development or https:// for production.");

        while (true) {
            delay(1000);
        }
    }

    nextRequestUrl =
        cmsBaseUrl + "/api/iot/vital-signs/device/next";

    resultUrl =
        cmsBaseUrl + "/api/iot/vital-signs/device/result";

    progressUrl =
        cmsBaseUrl + "/api/iot/vital-signs/device/progress";

    Serial.print("CMS URL: ");
    Serial.println(cmsBaseUrl);

    if (cmsBaseUrl.startsWith("http://")) {
        Serial.println(
            "WARNING: Using unencrypted HTTP. Development only."
        );
    } else if (strlen(CMS_ROOT_CA_CERT) == 0) {
        Serial.println(
            "WARNING: CMS_ROOT_CA_CERT is missing. HTTPS requests will be refused."
        );
    }

    Wire.begin(21, 22);

    while (!particleSensor.begin(Wire, I2C_SPEED_FAST)) {
    Serial.println("MAX30102 NOT FOUND!");
    Serial.println("Retrying sensor initialization in 5 seconds...");

    delay(5000);
    }

    // MAX30102 configuration based on SparkFun's official SpO2 example.
byte ledBrightness = 60;
byte sampleAverage = 4;
byte ledMode = 2;       // Red + IR
int sampleRate = 100;   // 25 effective samples/second after averaging
int pulseWidth = 411;
int adcRange = 4096;

particleSensor.setup(
    ledBrightness,
    sampleAverage,
    ledMode,
    sampleRate,
    pulseWidth,
    adcRange
);

particleSensor.setPulseAmplitudeRed(ledBrightness);
particleSensor.setPulseAmplitudeIR(ledBrightness);
particleSensor.setPulseAmplitudeGreen(0);

    Serial.println("MAX30102 ready.");

    connectWiFi();

    Serial.println("ESP32 is ready to poll CMS.");
}

// ---------------- LOOP ----------------

void loop() {
    if (WiFi.status() != WL_CONNECTED) {
    Serial.println("Wi-Fi disconnected. Reconnecting...");

    WiFi.reconnect();

    unsigned long reconnectStart = millis();

    while (WiFi.status() != WL_CONNECTED) {
        delay(500);

        if (millis() - reconnectStart >= 30000) {
            Serial.println("Reconnection taking too long. Retrying Wi-Fi...");

            WiFi.disconnect(false, false);
            delay(250);
            WiFi.begin(WIFI_SSID, WIFI_PASSWORD);

            reconnectStart = millis();
        }
    }

    Serial.println("Wi-Fi connection restored.");
    Serial.println(WiFi.localIP());

    return;
}

    if (millis() - lastPoll < POLL_INTERVAL_MS) {
        delay(10);
        return;
    }

    lastPoll = millis();

    String requestId;

    // Only measure when CMS has a pending request.
    if (!claimCMSRequest(requestId)) {
        return;
    }

    unsigned long requestStartMs = millis();
    bool measurementSuccess = measureVitals(requestId);

    if (measurementSuccess) {

    unsigned long resultStartMs = millis();

    bool sent = sendResult(
        requestId,
        beatAvg,
        spo2,
        true
    );

    unsigned long resultElapsedMs =
        millis() - resultStartMs;

    Serial.print("FINAL RESULT HTTP DURATION: ");
    Serial.print(resultElapsedMs);
    Serial.println(" ms");

        if (sent) {
            Serial.println("Measurement successfully delivered to CMS.");
        } else {
            Serial.println("Measurement delivery failed.");
        }
    } else {
        Serial.println("Measurement unsuccessful.");
        Serial.println("Sending failure result to CMS.");

        bool sent = sendResult(
            requestId,
            0,
            0,
            false
        );

        if (!sent) {
            Serial.println("Failure result delivery also failed.");
        }
    }
    Serial.print("TOTAL REQUEST DURATION: ");
    Serial.print(millis() - requestStartMs);
    Serial.println(" ms");

    Serial.println("=================================");

    Serial.println("Waiting for the next CMS request...");
}
