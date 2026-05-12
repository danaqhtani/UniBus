# UniBus
### Smart University Bus Coordination and Live Tracking System

<p align="center">
  <img src="https://img.shields.io/badge/ASP.NET%20Core-.NET%208-blue?style=for-the-badge" />
  <img src="https://img.shields.io/badge/SQL%20Server-Database-red?style=for-the-badge" />
  <img src="https://img.shields.io/badge/Google%20Maps-API-green?style=for-the-badge" />
  <img src="https://img.shields.io/badge/Status-Live-success?style=for-the-badge" />
</p>

---

## Live Demo

Website:  
http://unibus.somee.com/

---

# Overview

UniBus is a smart transportation management system developed to improve university bus operations through intelligent routing, real-time GPS tracking, and dynamic trip management.

The system minimizes unnecessary stops by dynamically optimizing routes based on active student pickup requests while providing live ETA updates and interactive tracking visualization.

---

# Key Features

- Real-time bus tracking
- Smart route optimization
- Dynamic ETA calculation
- Google Maps integration
- Live GPS synchronization
- Student and driver portals
- Interactive route visualization
- ASP.NET Core MVC layered architecture
- SQL Server database integration
- Live deployment

---

# System Screenshots

## Student Home

<p align="center">
  <img src="images/student-home.png" width="320"/>
</p>

---

## Available Trips

<p align="center">
  <img src="images/available-trips.png" width="320"/>
</p>

---

## My Bookings

<p align="center">
  <img src="images/my-bookings.png" width="320"/>
</p>

---

## Live Student Tracking

<p align="center">
  <img src="images/student-live-tracking.png" width="320"/>
</p>

---

## Driver Live Tracking

<p align="center">
  <img src="images/driver-live-tracking.png" width="320"/>
</p>

---

## Driver Dashboard

<p align="center">
  <img src="images/driver-dashboard.png" width="320"/>
</p>

---

## Student Profile

<p align="center">
  <img src="images/student-profile.png" width="320"/>
</p>

---

# System Architecture

<p align="center">
  <img src="images/system-architecture.png" width="900"/>
</p>

---

# Architecture Overview

The system follows a layered architecture structure:

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

# Project Structure

```text
UniBus/
├── Controllers/
├── Models/
├── Services/
├── Views/
├── Data/
├── wwwroot/
├── appsettings.json
└── Program.cs
```

---

# Smart Route Optimization

UniBus uses shortest-path based optimization logic combined with Google Directions API to generate efficient transportation routes.

The routing engine dynamically reorders stops based on active pickup requests to:
- Reduce unnecessary stops
- Minimize travel distance
- Improve ETA accuracy
- Optimize pickup sequencing

Google Directions API is used to generate:
- Real road paths
- Accurate route durations
- Smooth route polylines
- Realistic route rendering

---

# Real-Time Tracking

The tracking system provides:
- Continuous GPS synchronization
- Smooth live bus movement
- Dynamic ETA updates
- Interactive route rendering
- Student-specific arrival estimation

The live tracking experience is inspired by modern ride-tracking systems.

---

# Technologies Used

## Backend
- ASP.NET Core MVC
- C#
- Entity Framework Core
- SQL Server

## Frontend
- Razor Views
- JavaScript
- HTML5 / CSS3

## APIs and Services
- Google Maps API
- Google Directions API
- Geolocation API

---

# Database Design

Main database entities include:
- Students
- Drivers
- Buses
- Trips
- TripStops
- OptimizedRoutes
- LiveLocations

---

# Installation and Setup

## Clone Repository

```bash
git clone https://github.com/danaqhtani/UniBus.git
```

---

## Configure Database

Update the connection string inside:

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

## Run Database Migration

```bash
Update-Database
```

---

## Run Application

```bash
dotnet run
```

---

# Google Maps Configuration

Add your API key inside:

```html
<script src="https://maps.googleapis.com/maps/api/js?key=YOUR_API_KEY"></script>
```

Required APIs:
- Maps JavaScript API
- Directions API
- Geolocation API

---

# Deployment

UniBus is deployed using:
- Somee Hosting
- SQL Server Hosting

Live Deployment:  
http://unibus.somee.com/

---

# Future Improvements

- Push notification support
- AI-based traffic prediction
- Mobile application integration
- Attendance system integration
- Multi-university scalability

---

# Project Goals

- Improve transportation efficiency
- Enhance student transportation experience
- Reduce unnecessary route delays
- Provide accurate real-time tracking
- Build a scalable smart transportation solution

---

# Author

Dana Saeed Al-qahtani  
Computer Science Student  
Imam Mohammad Ibn Saud Islamic University

---

# License

This project was developed for educational and graduation project purposes.
