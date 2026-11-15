# ESP32 Clinic Vital Signs Monitoring Firmware

## Overview

This firmware powers the clinic's IoT-based vital signs monitoring device using an ESP32 microcontroller and a MAX30102 pulse oximeter sensor.

It measures:
- Heart Rate (BPM)
- Peripheral Oxygen Saturation (SpO₂)

The firmware communicates with the Clinic Management System (CMS) through HTTP/HTTPS API endpoints.

## Hardware Requirements

- ESP32 development board
- MAX30102 pulse oximeter sensor
- USB cable for programming and power

## Hardware Connections

| MAX30102 | ESP32 |
|---|---|
| VIN | 3.3V |
| GND | GND |
| SDA | GPIO 21 |
| SCL | GPIO 22 |

## Software Requirements

- Arduino IDE
- ESP32 board support package
- MAX30105 / SparkFun MAX3010x sensor library
- ArduinoJson library
- Maxim heart rate and SpO₂ algorithm (`spo2_algorithm.h`)

The ESP32 networking libraries are provided by the ESP32 Arduino core.

## Firmware Features

- Eight measurement windows per request
- Initial finger detection
- Two-second signal stabilization
- Rolling 100-sample measurement buffer
- 25 new samples per subsequent measurement window
- Individual measurement progress submission
- Validation of heart rate and SpO₂ readings
- Averaging of accepted measurements
- Wi-Fi reconnection handling
- HTTPS certificate verification
- Automatic retries for final result submission

## Configuration

The firmware requires a local `secrets.h` file.

Create it using `secrets.h.example` as a reference.

Configure the following values:

- `WIFI_SSID` – Wi-Fi network name
- `WIFI_PASSWORD` – Wi-Fi password
- `IOT_API_KEY` – CMS device authentication key
- `CMS_BASE_URL` – CMS hosting address
- `CMS_ROOT_CA_CERT` – Trusted root CA certificate

**Important:** Never commit `secrets.h` or expose private credentials in the repository.

For production deployment, use the live CMS HTTPS address and a valid trusted CA certificate. The local development URL must not be used in production.

## CMS API Endpoints

The firmware communicates with these endpoints:

| Method | Endpoint | Purpose |
|---|---|---|
| GET | `/api/iot/vital-signs/device/next` | Retrieve pending measurement requests |
| POST | `/api/iot/vital-signs/device/progress` | Submit individual measurement progress |
| POST | `/api/iot/vital-signs/device/result` | Submit the final measurement result |

## Uploading the Firmware

1. Install the required Arduino libraries.
2. Configure the local `secrets.h` file.
3. Connect the ESP32 through USB.
4. Select the appropriate ESP32 board and COM port.
5. Compile and upload `ClinicSensor.ino`.
6. Open Serial Monitor at 115200 baud to monitor device activity.

## Repository Structure

```text
ClinicSensor/
├── ClinicSensor.ino
├── secrets.h.example
└── README.md
```

The `secrets.h` file is intentionally excluded from version control.

## Important Notice

This project is intended for an IoT-based clinic monitoring system and development deployment. Its measurements should not be treated as a substitute for clinically validated medical equipment or professional medical assessment.
