# 🏥 Capstone Clinic Management System

A digital clinic management system developed using **ASP.NET Core Razor Pages** and **Entity Framework Core** for managing student health records, consultations, and vital sign monitoring. The project is designed as the software foundation for future **IoT-based health monitoring** using an ESP32 and MAX30102 sensor.

---

# 📌 Project Overview

The Capstone Clinic Management System streamlines clinic operations by providing an organized platform for managing student information, medical records, consultations, and vital sign history.

The system follows a student-centered workflow:

**Student Lookup → Medical Record → Vital Signs → History**

The software phase is complete and prepared for IoT integration.

---

# 🛠 Technologies Used

- ASP.NET Core Razor Pages
- C#
- .NET 8
- Entity Framework Core 8
- SQL Server LocalDB
- Bootstrap 5
- Visual Studio 2022
- Git & GitHub

---

# 📂 Modules

## ✅ Student Module

- Register Student
- Student Lookup by Student Number
- Edit Student Information
- Medical Requirement Status Tracking
- Direct Navigation to Medical Record

---

## ✅ Medical Records Module

- Create Consultation
- Medical Record Dashboard
- Consultation Details
- Edit Consultation
- Delete Medical Record
- Visit Statistics
- Latest Health Status Summary

---

## ✅ Vital Sign Logs Module

- Record Vital Signs
- Vital Sign History
- View Individual Records
- Edit Vital Sign Records
- Delete Vital Sign Records
- Automatic Health Status Classification
  - 🟢 Normal
  - 🟡 Warning
  - 🔴 Critical

---

## ✅ Alerts Module

- Create Alerts
- View Alert History
- Update Alert Status
- Delete Alerts

---

## 🚧 Reports Module

Planned for a future milestone.

Possible reports include:

- Medical History Report
- Student Consultation Summary
- Daily Clinic Report
- Monthly Clinic Report
- Student Health Statistics

---

# 📊 Current Progress

| Module | Status |
|---------|--------|
| Planning & Design | ✅ Complete |
| Development Environment | ✅ Complete |
| Database Design | ✅ Complete |
| Student Module | ✅ Complete |
| Medical Records Module | ✅ Complete |
| Vital Signs Module | ✅ Complete |
| Alerts Module | ✅ Complete |
| UI & Workflow Polish | ✅ Complete |
| Reports Module | 🚧 Planned |
| IoT Integration | ⏳ Next Phase |
| Mobile Application | ⏳ Planned |

---

# 🚀 Current Features

- Student Registration
- Student Lookup
- Medical Requirement Tracking
- Medical Consultation Management
- Vital Sign Recording
- Vital Sign History
- Student Medical Dashboard
- Health Status Classification
- CRUD Operations
- Responsive Bootstrap UI
- Entity Framework Core Integration
- SQL Server Database
- Navigation Workflow Optimization

---

# 🔄 Current Workflow

```text
Student Lookup
        │
        ▼
Medical Record Dashboard
        │
        ▼
Record Vital Signs
        │
        ▼
Vital Sign History
        │
        ▼
Individual Record Details
```

---

# 📡 Upcoming Phase — IoT Integration

The next development milestone focuses on integrating an ESP32 microcontroller with a MAX30102 sensor.

Planned functionality:

- Read Heart Rate automatically
- Read SpO₂ automatically
- Transfer readings over Wi-Fi
- Auto-populate Vital Signs page
- Save sensor readings directly into the database
- Maintain existing health status evaluation

---

# 📱 Future Enhancements

## Mobile Application

- Flutter-based mobile app
- Student login
- Medical record viewing
- Notifications
- Appointment support

## Reporting

- Printable reports
- Daily and monthly statistics
- Student health analytics
- Export to PDF

---

# 📅 Project Roadmap

| Phase | Status |
|--------|--------|
| Phase 1 – Planning | ✅ Complete |
| Phase 2 – Development Setup | ✅ Complete |
| Phase 3 – Database Design | ✅ Complete |
| Phase 4 – Core System Development | ✅ Complete |
| Phase 5 – UI & Workflow Polish | ✅ Complete |
| Phase 6 – IoT Integration (ESP32 + MAX30102) | 🚧 Next |
| Phase 7 – Reports | ⏳ Planned |
| Phase 8 – Mobile Application | ⏳ Planned |
| Phase 9 – Testing & Documentation | ⏳ Planned |

---

# 👨‍💻 Developer

**John Paul Santos**

---

# 📌 Current Status

## ✅ Phase 1 Complete

The clinic management system is fully operational and includes:

- Student Management
- Medical Records
- Vital Sign Management
- Alert Management
- Consultation Workflow
- Medical Dashboard
- Health Status Classification

The project is now transitioning into **Phase 2: IoT Integration**, where live Heart Rate and SpO₂ readings from an ESP32 + MAX30102 sensor will be integrated into the Vital Signs module.

---

**Last Updated:** July 29, 2026
