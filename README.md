# Shuttle Service Management System

## Overview
This application is a shuttle service management system built using the MVC pattern with .NET and Entity Framework.

The project was originally developed by Team Revan and later handed off to Kody and Aiden for feature completion, improvements, and preparation for release.

The system is currently used by the school to manage shuttle operations, allowing both students and administrators to interact with the service efficiently.

---

## Purpose
The goal of this application is to streamline shuttle service usage by:
- Allowing students to request rides
- Helping administrators organize routes
- Tracking student check-ins
- Providing reporting and data export tools

---

## Student Features

Students are able to interact with the shuttle system in the following ways:

### Create Shuttle Requests
Students can submit requests for shuttle service, allowing administrators to plan routes based on demand.

### Check In to Shuttle
Students can check in when entering the shuttle using:
- QR codes
- NFC tags
- Manual navigation to the check-in page

This helps track who is on the shuttle in real time.

---

## Admin Features

Administrators have access to a dashboard that allows them to manage all aspects of the shuttle system.

### Management Sections
Admins can manage:
- Check-ins
- Requests
- Messages
- Mail
- Routes
- Shuttle vehicles
- Drivers (planned for future implementation)

### Reports
Admins can generate reports that:
- Display data in table format
- Export data to Excel files for external use

---

## System Status

The following features are currently present but planned for further expansion:
- Driver management
- Shuttle vehicle management

---

## Technology Stack

- ASP.NET MVC
- Entity Framework
- C#
- SQL Database
- Email API (for notifications and messaging)

---

## Unit Testing

Unit testing was implemented during the final stages of development to ensure the reliability and stability of core system functionality.

Tests were created to validate CRUD (Create, Read, Update, Delete) operations for key features, including:

- Registration
- Shuttle Requests
- Check-ins

These tests verify that data is correctly created, modified, and removed within the system, helping to prevent regressions and ensure consistent behavior.

In addition, unit tests were developed for authorization logic related to accessing the admin dashboard. This ensures that only properly authenticated and authorized users are able to access administrative features.

The inclusion of unit tests improves overall code quality, supports future development, and provides a foundation for safely extending system functionality.

VIEW THE ERRORCODE.md file for info on the codes present in the site, these are used for the unit tests.

---

## Additional Documentation

For more details, refer to:
- User Guide (how to use the system)
- Developer Documentation (system architecture and maintenance)