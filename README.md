# AeroResponse – Aircraft Emergency Response Training Simulator

Train. Respond. Save Lives.

AeroResponse is a web-based Aircraft Emergency Response Training Simulator designed to provide realistic, scenario-based aviation emergency training through an interactive digital cockpit environment. The platform combines software engineering, simulation, cloud computing, interactive graphics, databases, and emerging AI technologies to create an engaging and industry-relevant training experience.

## Project Overview
Modern aviation relies heavily on simulation-based training to prepare pilots for emergency situations where rapid decision-making, situational awareness, and procedural accuracy are critical. Commercial pilot training simulators cost millions of dollars and are used extensively by airlines and aviation training organizations worldwide.

### Trello Board
https://trello.com/b/B0G3uHpe/aeroresponse

### Demo Video
Video Coming Soon

### Purpose of AeroResponse is to develop an accessible, web-based emergency response simulator that allows users to:
- Select an aircraft type
- Select an emergency scenario
- Respond using interactive cockpit controls
- Follow emergency procedures and checklists
- Receive performance evaluation and feedback
- Track historical training performance and progression

### Project combines multiple areas of software engineering, including:
- Interactive web application development
- Real-time simulation
- Cloud computing
- Authentication and authorization
- Database design
- Human-computer interaction
- Artificial intelligence
- Software architecture and design patterns
- DevOps and CI/CD deployment pipelines

## Team
- Kim Kathleen Brown
- Jayce Odin Nephi Brown 
- Nathaniel Cole Stokes

## Project Milestones
- Week 1: Landing Page, Simulation Page Layout (AirCrafts) | App Layout
- Week 2: Membership Tier Implementation and Payment Forms, Emergency Scenarios
- Week 3: Security Authentication and Full Simulation Finalization, Results Dashboard
- Week 4: Stretch Features Finalization, Production Deployment and Demo Recording

## Core Features

### User Management
- User registration and authentication
- Pilot profiles and training history
- Administrator and instructor roles
- Performance dashboards and reporting

### Aircraft Management (CRUD)
- Create aircraft profiles
- Edit aircraft specifications
- Delete aircraft
- View aircraft information
- Support multiple aircraft types

### Supported Aircraft
- Boeing 737
- Airbus A320
- Boeing 787
- Gulfstream

### Emergency Scenario Management (CRUD)
- Create emergency scenarios
- Edit emergency procedures
- Delete scenarios
- Assign difficulty levels
- Configure emergency triggers

#### Example Emergency Scenarios
- Engine fire
- Engine failure
- Cabin depressurization
- Hydraulic system failure
- Electrical failure
- Fuel leak
- Bird strike
- Landing gear malfunction

## Interactive Cockpit Simulation
- Real-time cockpit instrument displays
- Interactive throttle controls
- Engine control panels
- Fuel management systems
- Warning and alert indicators
- Emergency control switches
- Visual and audio emergency alerts

## Pilot Emergency Response
Pilots respond to simulated emergencies using interactive cockpit controls:
- Drag throttle levers
- Toggle fuel switches
- Activate fire suppression systems
- Pull emergency handles
- Declare emergency situations
- Complete emergency checklists

## Training Assessment
- Reaction time tracking
- Procedure accuracy scoring
- Emergency response evaluation
- Historical performance tracking
- Flight logs and analytics

## Membership and Access Control
- User membership registration
- Membership plan selection
- Mock payment processing
- Subscription management
- Membership-based simulator access
- Role-based authorization
- User dashboard and account management

## Stretch Features
- Voice command interaction
- AI-powered flight instructor
- AI-generated performance feedback
- Dynamic emergency scenarios
- Flight replay system
- Real-time telemetry graphs
- Leaderboards and performance comparisons

# Technology Stack

### Front-End
- Blazor Web Application
- Razor Components
- JavaScript Interoperability
- SignalR Client
- Professional Aviation Instrument Libraries
- SVG-Based Interactive Cockpit Displays
- Chart.js
- CSS3 Animations

### Back-End
- ASP.NET Core
- C#
- Custom Aircraft Simulation Engine
- Emergency Scenario Engine
- Performance Scoring Engine
- SignalR
- Entity Framework Core
- SQLite

## Authentication and Security
- ASP.NET Core Identity
- Role-Based Access Control

## Cloud and DevOps
- Microsoft Azure
- Azure App Services
- Azure DevOps CI/CD Pipelines
- GitHub

## Payment and Membership
- Mock Payment Gateway
- Subscription Management
- Membership Access Control
- ASP.NET Core Identity
- Role-Based Authorization
- Entity Framework Core
- Stripe Test Mode (Stretch Feature)

## Artificial Intelligence and Voice Technologies
- Azure AI Services
- OpenAI APIs
- Web Speech API

# Project Architecture

Frontend
│
├── Blazor Web Application
├── Razor Components
├── SVG Cockpit Displays
├── SignalR Client
└── Chart.js

Backend
│
├── ASP.NET Core
├── Aircraft Simulation Engine
├── Emergency Scenario Engine
├── Performance Scoring Engine
└── SignalR Hub

Database
│
├── SQLite
└── Entity Framework Core

Cloud
│
├── Azure App Services
├── Azure DevOps
└── GitHub

# Repository Structure

AeroResponse

├── Components
│   ├── Aircraft
│   ├── Cockpit
│   ├── Dashboard
│   ├── Membership
│   ├── Scenarios
│   └── Shared
│
├── Data
├── DTOs
├── Hubs
├── Models
├── Repositories
├── Services
├── Simulation
│
├── wwwroot
│   ├── audio
│   ├── images
│   ├── svg
│   └── videos
│
├── Program.cs
└── appsettings.json

# Future Enhancements

Potential future enhancements include:
- AI-powered instructor assistance
- Voice-controlled cockpit interaction
- Advanced aircraft telemetry
- Multiplayer training sessions
- Machine learning performance analysis
- Virtual reality cockpit integration

# Favorite Quotes!
- Nathan's : "It's not the pursuit of happiness, it's the happiness in the pursuit." - Jimmy Carr
- Commercial subscription model
- FAA/EASA training scenario expansion
