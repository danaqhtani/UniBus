# UniBus – Smart University Bus Coordination & Tracking System

![ASP.NET Core](https://img.shields.io/badge/ASP.NET%20Core-.NET%208-blue)
![SQL Server](https://img.shields.io/badge/Database-SQL%20Server-red)
![Google Maps API](https://img.shields.io/badge/API-Google%20Maps-green)
![Status](https://img.shields.io/badge/Status-Active-success)

## 🌐 Live Demo

Published Website:  
http://unibus.somee.com/

---

# 📌 Overview

UniBus is a smart university transportation system designed to improve transportation efficiency through:

- Smart route optimization
- Real-time bus tracking
- ETA calculation
- Dynamic stop handling
- Live driver location updates
- Interactive Google Maps integration

The system minimizes unnecessary stops by dynamically optimizing routes based on active student pickup requests.

---

# ✨ Features

## 👨‍🎓 Student Features
- View assigned trips
- Track bus location live on map
- View estimated arrival time (ETA)
- Monitor trip status in real time
- Interactive route visualization

## 🚌 Driver Features
- Start and end trips
- Share live GPS location
- View optimized routes
- Navigate using Google Maps integration

## 🛠️ Admin Features
- Manage buses
- Manage students and drivers
- Create and assign routes
- Monitor active trips
- Control transportation operations

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
- HTML/CSS

## APIs & Services
- Google Maps API
- Google Directions API
- Geolocation API

---

# 🏗️ System Architecture

The system follows a layered architecture:

- Presentation Layer (Razor Views + JavaScript)
- Business Logic Layer (Services & Algorithms)
- Data Access Layer (Entity Framework Core)
- Database Layer (SQL Server)

### Main Components
- Route Optimization Service
- Trip Tracking Service
- Driver Tracking Module
- Student Tracking Module
- ETA Calculation Engine

---

# 🧠 Smart Routing Algorithm

UniBus uses a shortest-path based routing strategy combined with dynamic route optimization.

The algorithm:
- Optimizes pickup order
- Removes unnecessary stops
- Reduces travel distance
- Improves ETA accuracy

Google Directions API is used to generate:
- Real road paths
- Smooth route polylines
- Accurate travel duration

---

# 📍 Real-Time Tracking

The system supports live bus tracking similar to modern delivery applications.

Features include:
- Continuous GPS updates
- Smooth marker animation
- Dynamic ETA refresh
- Live route rendering
- Student-specific arrival estimation

---

# 🗄️ Database

Main entities include:
- Students
- Drivers
- Buses
- Trips
- TripStops
- OptimizedRoutes
- LiveLocations

---

# ⚙️ Installation

## Clone Repository

```bash
git clone https://github.com/your-username/UniBus.git
```

---

## Configure Database

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

## Run Migrations

```bash
Update-Database
```

---

## Run Project

```bash
dotnet run
```

---

# 🗺️ Google Maps Setup

Add your Google Maps API key inside:

```html
<script src="https://maps.googleapis.com/maps/api/js?key=YOUR_API_KEY"></script>
```

Required APIs:
- Maps JavaScript API
- Directions API
- Geolocation API

---

# 🚀 Deployment

UniBus is deployed using:
- Somee Hosting
- SQL Server Database Hosting

### Live Website
http://unibus.somee.com/

---

# 🔮 Future Improvements

- Push notifications
- AI-based traffic prediction
- Driver mobile application
- Attendance integration
- Multi-university support

---

# 🎯 Project Goals

- Improve transportation efficiency
- Reduce unnecessary travel
- Enhance student experience
- Provide real-time visibility
- Optimize university transportation management

---

# 👩‍💻 Author

### Dana Saeed Al-qahtani
Computer Science Student  
Imam Mohammad Ibn Saud Islamic University

---

# 📄 License

This project is for educational and graduation project purposes.
