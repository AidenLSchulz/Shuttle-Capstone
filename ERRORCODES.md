# Error & Status Codes

This document outlines the error and success codes used throughout the Shuttle Service Management System.

Codes follow this format:

- **E / S** → Error or Success  
- **Number** → Unique identifier  
- **Suffix** → Feature identifier (e.g., CI = Check-In, RG = Registration)

All codes are stored in:
`TempData["Code"]`

---

## Check-In Codes (CI)

| Code   | Message |
|--------|--------|
| E001CI | There have been too many submissions under your internet. Please wait before trying again. |
| E002CI | Please fill out all required fields. |
| S001CI | Check-in successful! |
| E003CI | Please fill out all required fields. |
| E004CI | Check-in not found. |
| S002CI | Check-in updated successfully. |

---

## Registration Codes (RG)

| Code   | Message |
|--------|--------|
| E001RG | There have been too many submissions under your internet. Please wait before trying again. |
| E002RG | At least one ride must be added to submit a registration. |
| E003RG | Every request day must contain at least one ride. |
| E004RG | Each ride must have either a route selected or a drop-off time. |
| E005RG | Each request day (Monday–Thursday) can only be selected once. |
| E006RG | At least one request day must be included. |
| E007RG | Each request day (Monday–Thursday) can only be selected once. |
| E008RG | Every request day must contain at least one ride. |
| E009RG | Each ride must have either a route selected or a drop-off time. |
| S001RG | Registration updated successfully. |
| E010RG | A custom message is required for special requests. |
| E011RG | The request date cannot be in the past. |
| S002RG | Special request updated successfully. |
| E012RG | Registration not found. |
| S003RG | Registration unarchived successfully. |
| E013RG | Registration not found. |
| E014RG | You do not have permission to archive registrations. |
| S004RG | Registration archived successfully. |

---