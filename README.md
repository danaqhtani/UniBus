# 🚌 UniBus  
### Smart University Bus Coordination & Live Tracking System

<p align="center">
  <img src="https://img.shields.io/badge/ASP.NET%20Core-.NET%208-blue?style=for-the-badge" />
  <img src="https://img.shields.io/badge/SQL%20Server-Database-red?style=for-the-badge" />
  <img src="https://img.shields.io/badge/Google%20Maps-API-green?style=for-the-badge" />
  <img src="https://img.shields.io/badge/Status-Live-success?style=for-the-badge" />
</p>

---

## 🌐 Live Demo

🔗 **Website:**  
http://unibus.somee.com/

---

# 🚀 Project Highlights

- Real-time university bus tracking system
- Smart route optimization algorithm
- Dynamic ETA calculation engine
- Google Maps & Directions API integration
- Smooth live GPS synchronization
- Interactive map visualization
- ASP.NET Core MVC layered architecture
- SQL Server database integration
- Real-world deployment on live hosting

---

# 📖 About The Project

UniBus is a smart transportation management system designed to improve university bus operations through intelligent routing and real-time tracking.

The system dynamically optimizes routes based on active student pickup requests, reducing unnecessary stops and improving transportation efficiency.

UniBus provides:
- Real-time bus tracking
- Smart route optimization
- ETA calculation
- Interactive map visualization
- Live driver location updates
- Dynamic trip management

---

# 🖼️ System Preview

## 🔐 Login Page

<p align="center">
  <img src="images/login.png" width="850"/>
</p>

---

## 📍 Live Tracking System

<p align="center">
  <img src="images/live-tracking.png" width="850"/>
</p>

---

## 🚌 Driver Tracking Interface

<p align="center">
  <img src="images/driver-tracking.png" width="850"/>
</p>

---

## 🛠️ Admin Dashboard

<p align="center">
  <img src="images/admin-dashboard.png" width="850"/>
</p>

---

# 🎥 Demo Preview

<p align="center">
  <img src="images/unibus-demo.gif" width="900"/>
</p>

---

# 🏗️ System Architecture

<p align="center">
  <img src="images/system-architecture.png" width="950"/>
</p>

---

## 🧠 Architecture Overview

The project follows a layered architecture structure:

```text
Presentation Layer
│
├── Razor Views
├── JavaScript Frontend
│
Business Logic Layer
│
├── Route Optimization Service
├── Trip Tracking Service
├── ETA Engine
│
Data Access Layer
│
├── Entity Framework Core
│
Database Layer
│
└── SQL Server
```

---

# ✨ Features

## 👨‍🎓 Student Module
- View assigned trips
- Track buses live on Google Maps
- View estimated arrival time (ETA)
- Receive real-time trip updates
- Monitor route progress interactively

---

## 🚌 Driver Module
- Start and end trips
- Share live GPS location
- Access optimized route paths
- Real-time navigation support

---

## 🛠️ Admin Module
- Manage buses and routes
- Assign drivers and students
- Monitor active trips
- Control transportation operations
- Manage trip scheduling

---

# 🧠 Smart Route Optimization

UniBus uses shortest-path based optimization techniques combined with Google Directions API to generate efficient transportation routes.

### Optimization Goals
✔ Reduce unnecessary stops  
✔ Minimize travel distance  
✔ Improve ETA accuracy  
✔ Optimize pickup sequencing  

---

# 📍 Real-Time Tracking System

The live tracking system provides:
- Continuous GPS synchronization
- Smooth live bus movement
- Dynamic ETA updates
- Interactive route rendering
- Student-specific arrival estimation

Inspired by modern live delivery tracking systems.

---

# 🧰 Technologies Used

## Backend
- ASP.NET Core MVC
- C#
- Entity Framework Core
- SQL Server

## Frontend
- Razor Views
- JavaScript
- HTML5 / CSS3

## APIs & Services
- Google Maps API
- Google Directions API
- Geolocation API

---

# 🗄️ Database Entities

Main database tables include:
- Students
- Drivers
- Buses
- Trips
- TripStops
- OptimizedRoutes
- LiveLocations

---

# ⚙️ Installation & Setup

## 1️⃣ Clone Repository

```bash
git clone https://github.com/danaqhtani/UniBus.git
```

---

## 2️⃣ Configure Database

Update your connection string inside:

```json
appsettings.json
```

Example:

```json
"ConnectionStrings": {
  "DefaultConnection": "Server=.;Database=UniBusDB;Trusted_Connection=True;TrustServerCertificate=True"
}
```

---

## 3️⃣ Run Database Migration

```bash
Update-Database
```

---

## 4️⃣ Run Application

```bash
dotnet run
```

---

# 🗺️ Google Maps Configuration

Add your API key inside:

```html
<script src="https://maps.googleapis.com/maps/api/js?key=YOUR_API_KEY"></script>
```

### Required APIs
- Maps JavaScript API
- Directions API
- Geolocation API

---

# 🚀 Deployment

UniBus is deployed using:
- Somee Hosting
- SQL Server Hosting

### 🔗 Live Deployment
http://unibus.somee.com/

---

# 🔮 Future Improvements

- Push notifications
- AI-based traffic prediction
- Mobile application support
- Attendance system integration
- Multi-university scalability

---

# 🎯 Project Goals

- Improve university transportation efficiency
- Enhance student transportation experience
- Reduce unnecessary route delays
- Provide accurate real-time tracking
- Build a scalable smart transport solution

---

# 👩‍💻 Author

### Dana Saeed Al-qahtani  
Computer Science Student  
Imam Mohammad Ibn Saud Islamic University

---

# 📄 License

This project was developed for educational and graduation project purposes.
