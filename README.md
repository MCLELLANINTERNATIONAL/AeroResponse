# AeroResponse – Aircraft Emergency Response Training Simulator

**Train. Respond. Save Lives.**

AeroResponse is a web-based Aircraft Emergency Response Training Simulator designed to provide realistic, scenario-based aviation emergency training through an interactive digital cockpit environment. The platform combines software engineering, real-time simulation, cloud deployment, interactive cockpit systems, databases, performance analytics, voice technologies, and AI-supported instruction to create an engaging and industry-relevant aviation training experience.

## Project Overview

Modern aviation relies heavily on simulation-based training to prepare pilots for emergency situations where rapid decision-making, situational awareness, procedural accuracy, and effective aircraft control are critical. Full-scale commercial flight simulators are highly sophisticated training systems that can cost millions of dollars and are used extensively by airlines, flight schools, aviation organisations, and professional training providers worldwide.

AeroResponse explores how modern web technologies can make scenario-based emergency training more accessible through a browser-based simulation environment.

Rather than attempting to replace certified full-flight simulators, AeroResponse demonstrates how software engineering can be used to create an accessible training platform where pilots and aviation learners can practise emergency decision-making, interact with aircraft systems, follow emergency procedures, and review their performance after completing a scenario.

### Trello Board

https://trello.com/b/B0G3uHpe/aeroresponse

### Web Browser URL

https://aeroresponse.onrender.com

### Demo Video

https://youtu.be/8YpR12w458w

*** Role Based Access in Webapp Render URL ***
* Pilot
    Username: kimkbrown100@outlook.com
    Password: Test@100
* Trainer
    Username: GHef@gmail.com
    Password: Greg1!
* Admin
    Username: Admin@gmail.com
    Password: Admin1!

### Purpose of AeroResponse is to develop an accessible, web-based emergency response simulator that allows users to:

* Select an aircraft type
* Select an emergency scenario
* Operate within an interactive digital cockpit
* Respond using interactive cockpit controls
* Monitor aircraft instruments and system conditions
* Follow emergency procedures and checklists
* Use supported voice commands to interact with the simulation
* Receive AI-supported instructor guidance and feedback
* Receive performance evaluation and scoring
* Review completed simulation reports
* Track historical training performance and progression
* Allow instructors to monitor pilot training performance
* Allow administrators to review platform-wide training analytics

### Project combines multiple areas of software engineering, including:

* Interactive web application development
* Real-time aircraft simulation
* Scenario-based simulation logic
* Cloud computing and production deployment
* Authentication and authorization
* Role and permission-based access control
* Relational and document database design
* Human-computer interaction
* Artificial intelligence
* Voice recognition and command processing
* Performance analytics and reporting
* Software architecture and design patterns
* Repository and service-layer architecture
* DevOps and CI/CD deployment pipelines

## Team

* Kim Kathleen Brown
* Jayce Odin Nephi Brown
* Nathaniel Cole Stokes

## Project Milestones

## Sprint Development

* ### Sprint 1 – Project Foundation and Core Simulation

Sprint 1 focus on establishing the foundation of AeroResponse and developing the initial user-facing and simulation components.

*** Key development included:***

- GitHub repository and Trello project setup
- Project architecture and application structure
- Blazor application setup
- Initial database configuration
- README documentation
- Emergency Scenario interface
- Ten pre-existing emergency scenarios
- Landing page development
- Navigation bar and footer
- Consistent application styling
- Interactive landing-page content
- Simulation landing page
- Initial cockpit instrument displays
- Interactive throttle controls
- Fuel control interaction
- Fire handle controls
- Landing gear controls
- Flap controls
- Cockpit warning-system development

* ### Sprint 2 – Management, Membership and Pilot Reporting

Sprint 2 expand AeroResponse from the initial simulation foundation into a broader training-management platform.

*** Key development included:***

- Emergency Scenario CRUD functionality
- Reusable Scenario Form component
- Scenario creation, viewing, editing and deletion
- Scenario search and filtering
- Configurable scenario difficulty levels
- Scenario trigger-condition management
- Pilot Results Dashboard
- Performance analytics
- Telemetry charts
- Reaction-time tracking
- Flight logs
- Procedure-accuracy scoring
- Performance reports
- Membership plans
- Mock payment processing
- Membership activation and expiry
- Payment confirmation
- Membership CRUD functionality
- Login-page development
- Role-based access groundwork
- Simulation selection-page development
- Aircraft Management CRUD
- Cessna cockpit simulation development
- Interactive aircraft controls and warning systems

* ### Sprint 3 – Authentication, Scenario Triggers and Simulation Integration

Sprint 3 focus on connecting the major AeroResponse systems and strengthening authentication, access control, emergency logic and platform reporting.

*** Key development included:***

- Administrator Reports Dashboard
- Platform-wide usage reporting
- Scenario popularity analytics
- Pass/fail reporting
- Emergency scenario difficulty triggers
- Configurable scenario activation conditions
- User login and logout
- Secure account authentication
- User registration
- Pilot profile management
- Role management
- Pilot, instructor and administrator permissions
- Membership CRUD development
- Membership-based permissions
- Mock subscription processing
- Simulator access authorization
- Aircraft and scenario selection integration
- Standardised aircraft controls and instruments
- Aircraft limitation configuration
- Simulation-to-instrument communication
- Cockpit response to pilot actions
- Emergency-condition instrument response


* ### Sprint 4 – AI, Voice, Instructor Reporting and Final Integration

Sprint 4 focus on advanced platform functionality, final simulation integration and the development of several AeroResponse stretch features.

*** Key development included:***

- Additional aircraft cockpit layouts
- Full cockpit instrument integration
- Final instrument development
- Emergency audio and visual alert development
- Emergency trigger integration
- Instructor Dashboard development
- Company-linked pilot performance reporting
- AI Instructor functionality
- Real-time pilot-action monitoring
- Gold-standard procedure comparison
- AI-generated corrective feedback
- AI-enhanced simulation results
- AI-generated improvement recommendations
- Voice-command control
- Browser-based hands-free cockpit interaction
- Personal user account pages
- User account editing and deletion
- Owner access and referral codes
- Company member-management functionality
- Permission-based application access
- Deploy Project to the cloud

## Current Implementation Highlights

AeroResponse project extends beyond the original minimum project scope and integrates multiple systems into a single aviation emergency training platform.

*** Current implementation includes:***

* Six seeded aircraft with aircraft-specific configuration
* Ten built-in emergency training scenarios
* Aircraft-specific cockpit layouts
* Configurable scenario trigger conditions
* Interactive cockpit instruments and controls
* Real-time aircraft and emergency state management
* Emergency procedure checklists
* Pilot action recording
* Reaction-time and procedure-based performance assessment
* Voice-command processing
* AI instructor feedback
* Pilot performance dashboards
* Flight logs, scoring, analytics, feedback, and achievements
* Instructor pilot-performance reporting
* Administrator platform-wide reporting
* Downloadable instructor and administrator reports
* Membership tiers and account access control
* ASP.NET Core Identity authentication
* Permission-based pilot, trainer, and administrator access
* SQLite relational data storage
* MongoDB account, membership, payment-method, and referral data services
* SignalR infrastructure for real-time communication
* Production-oriented deployment configuration

## Core Features

### User Management

* User registration and authentication
* ASP.NET Core Identity integration
* Pilot profiles and account information
* Training history
* Membership-linked access
* Pilot, trainer/instructor, and administrator permissions
* Permission-based page access
* Pilot performance dashboards and reporting
* Instructor reporting
* Administrator reporting
* User account and membership management

### Aircraft Management (CRUD)

* Create aircraft profiles
* Edit aircraft specifications
* Delete aircraft
* View aircraft information
* Configure aircraft characteristics
* Support multiple aircraft types
* Aircraft-specific cockpit layout assignment
* Aircraft engine, fuel-tank, brake, and landing-gear configuration
* Aircraft access based on account permissions and membership

### Supported Aircraft

*** Current seeded AeroResponse fleet includes:***

* Cessna 172
* Gulfstream G700
* ATR 72-600
* De Havilland Dash 8 Q400
* Boeing 747-8 Intercontinental
* Airbus A320-200

Aircrafts represent multiple aviation categories including general aviation, business aviation, regional turboprops, narrow-body commercial aircraft, and wide-body commercial aircraft.

### Emergency Scenario Management (CRUD)

* Create emergency scenarios
* Edit emergency procedures
* Delete scenarios
* View scenario details
* Assign difficulty levels
* Configure emergency trigger conditions
* Define expected emergency procedures
* Configure scenario assessment rules
* Support scenario-specific activation conditions
* Integrate scenarios with the simulation and scoring systems

#### Emergency Scenarios

*** AeroResponse currently includes ten built-in emergency scenarios:***

* Engine Fire
* Engine Failure
* Bird Strike
* Cabin Depressurization
* Hydraulic Failure
* Electrical Failure
* Fuel Leak
* Landing Gear Malfunction
* Smoke or Fire
* Wind Shear

Scenarios range from Intermediate to Expert difficulty and contain scenario-specific emergency conditions, expected procedures, trigger logic, and assessment requirements.

## Interactive Cockpit Simulation

AeroResponse simulation environment combines aircraft configuration, cockpit state, emergency scenario logic, interactive controls, and pilot actions.

*** Current simulation capabilities include:***

* Real-time cockpit instrument displays
* Aircraft-specific cockpit layouts
* Airspeed indication
* Altitude indication
* Vertical-speed indication
* Artificial horizon
* Heading indication
* Turn coordination
* Engine state monitoring
* Interactive throttle controls
* Fuel management systems
* Fuel quantity and fuel-leak simulation
* Electrical-system monitoring
* Hydraulic-system emergency behaviour
* Landing-gear state and status
* Fire detection and suppression controls
* Engine fire handling
* Warning and alert indicators
* Emergency control switches
* Emergency procedure checklists
* Visual emergency warnings
* Audio emergency alerts
* Scenario-specific emergency activation
* Configurable emergency trigger conditions
* Real-time simulation state updates

Simulator uses aircraft configuration data to determine characteristics such as engine count, fuel-tank configuration, landing gear, cockpit layout, cruise speed, and maximum operating altitude.

## Pilot Emergency Response

Pilots respond to simulated emergencies using interactive cockpit controls and, where supported, voice commands.

*** Depending on the selected aircraft and emergency scenario, pilot actions can include:***

* Operate throttle controls
* Reduce engine thrust
* Toggle fuel controls
* Isolate affected systems
* Shut down affected engines
* Activate fire suppression systems
* Pull emergency fire handles
* Discharge fire suppression systems
* Manage landing gear
* Respond to electrical or hydraulic failures
* Monitor fuel conditions
* Declare emergency situations
* Complete emergency checklist actions
* Stabilise aircraft conditions
* Respond to cockpit warnings
* Use supported voice commands
* Follow scenario-specific emergency procedures

Pilot actions are recorded during the simulation and can be evaluated against the expected procedure for the selected emergency scenario.

## Training Assessment

AeroResponse includes a performance assessment and reporting system designed to evaluate how the pilot responded during each emergency simulation.

*** Assessment and reporting capabilities include:***

* Reaction time tracking
* Procedure accuracy scoring
* Decision-making evaluation
* Checklist performance
* Overall performance scoring
* Pilot action tracking
* Scenario completion records
* AI-supported performance feedback
* Historical performance tracking
* Flight logs
* Scoring reports
* Performance analytics
* Feedback reports
* Pilot achievements and badges
* Performance trend analysis
* Scenario outcome analysis
* Pass/fail tracking

Completed simulation data is used throughout the reporting system so pilots can review individual attempts and monitor their progression across multiple training sessions.

*** Instructor reporting provides additional visibility across linked pilots, including:***

* Pilot performance trends
* Average scores
* Pass rates
* Reaction times
* Scenario outcomes
* Individual pilot performance
* Recent pilot training activity
* Training priorities
* Downloadable performance reports

*** Administrator reporting provides broader platform-level visibility including:***

* Platform training activity
* Active pilots
* Average performance
* Scenario usage
* Scenario popularity
* Pass and failure rates
* Recent simulation activity
* Platform-wide training trends
* Downloadable administrator reports

## Membership and Access Control

AeroResponse includes a membership and account-access system designed to support individual pilots, smaller aviation organisations, and larger commercial organisations.

*** Current membership plans include:***

* Private
* Small Commercial
* Large Commercial

*** Membership functionality includes:***

* User membership registration
* Membership plan selection
* Membership-based aircraft and simulator access
* Account and company information
* Mock payment processing
* Saved payment-method data
* Subscription and membership management
* Company-linked pilot and trainer accounts
* Role and permission-based authorization
* Pilot page permissions
* Trainer/instructor reporting permissions
* Administrator page permissions
* User dashboard and account management
* Company member limits
* Owner referral-code functionality

Membership model is designed to demonstrate how AeroResponse could evolve from an academic software project into a scalable aviation training platform serving individual pilots, flight schools, charter operators, and larger aviation organisations.

## Stretch Features

Several features originally identified as stretch objectives were developed as part of the current AeroResponse implementation.

*** Implement stretch functionality includes:***

* Voice command interaction
* Web Speech API integration
* Voice command parsing
* AI-powered flight instructor
* AI-generated performance feedback
* Scenario-aware instructor feedback
* Dynamic emergency activation
* Configurable emergency trigger logic
* Performance trend reporting
* Instructor analytics
* Administrator analytics
* Downloadable reporting

*** Additional stretch concepts remain available for future development, including:***

* Flight replay
* Expanded real-time telemetry visualisation
* Leaderboards and performance comparisons
* Multiplayer training
* More advanced AI instructor capabilities

# Technology Stack

### Front-End

* Blazor Web Application
* Razor Components
* Interactive Server Components
* C#
* HTML5
* CSS3
* JavaScript Interoperability
* SVG-Based Interactive Cockpit Displays
* Custom aviation instrument components
* Interactive cockpit controls
* CSS animations and responsive interface design
* Web Speech API integration

### Back-End

* ASP.NET Core
* .NET 10
* C#
* Custom Aircraft Simulation Engine
* Emergency Scenario Engine
* Scenario Trigger Evaluator
* Performance Scoring Engine
* Cockpit Command Service
* Voice Command Parser
* AI Instructor Service
* SignalR
* Repository pattern
* Service-layer architecture
* Entity Framework Core

## Authentication and Security

* ASP.NET Core Identity
* Identity-based user authentication
* Authentication state integration
* Role and permission-based access control
* Authorization policies
* Pilot page permissions
* Trainer/instructor report permissions
* Administrator page permissions
* Account permission services
* Pilot report access controls
* Aircraft access controls
* Identity cookie authentication
* Anti-forgery protection

## Cloud and Render

* Render production web deployment
* Docker deployment configuration
* GitHub
* Git source control
* Trello project management
* Production static asset configuration
* Environment-based application configuration
* Database migration support
* CI/CD-ready project architecture

## Payment and Membership

* Mock Payment Gateway
* Membership plan selection
* Private membership
* Small Commercial membership
* Large Commercial membership
* Subscription and membership management
* Membership Access Control
* Company account support
* Saved payment-method data
* Owner referral codes
* Company member limits
* ASP.NET Core Identity
* Permission-Based Authorization
* Entity Framework Core
* MongoDB-backed membership and account data

## Artificial Intelligence and Voice Technologies

* AI Instructor Service
* Scenario-aware AI instructor feedback
* Pilot action analysis
* Procedure-sequence evaluation
* AI-supported performance feedback
* Web Speech API
* Browser-based speech recognition
* Voice Command Parser
* Cockpit Command Service
* Voice-controlled simulator actions

AI instructor monitors pilot actions against the expected emergency procedure and provides training feedback based on the pilot's response during the simulation.

# Project Architecture

```text
Frontend
│
├── Blazor Web Application
├── Razor Components
├── Interactive Server Components
├── Aircraft-Specific Cockpit Layouts
├── SVG / CSS Cockpit Displays
├── Interactive Aircraft Controls
├── Voice Control Interface
└── Performance & Reporting Dashboards

Backend
│
├── ASP.NET Core
├── Aircraft Simulation Engine
├── Emergency Scenario Engine
├── Scenario Trigger Evaluator
├── Performance Scoring Engine
├── Cockpit Command Service
├── Voice Command Parser
├── AI Instructor Service
├── Membership & Access Services
├── Reporting Services
└── SignalR Hub

Database
│
├── SQLite
│   └── Entity Framework Core
│
└── MongoDB
    ├── User Account Data
    ├── Membership Timeline Data
    ├── Saved Payment Methods
    └── Referral Data

Security
│
├── ASP.NET Core Identity
├── Authentication
├── Authorization Policies
├── Account Permissions
└── Membership-Based Access

Deployment
│
├── Docker
├── Render
└── GitHub
```

# Repository Structure

```text
AeroResponse
│
├── Components
│   ├── Account
│   ├── Aircraft
│   ├── Cockpit
│   ├── FlightControl
│   ├── Instruments
│   ├── Layout
│   ├── Membership
│   ├── Pages
│   ├── Reports
│   ├── Scenarios
│   ├── Shared
│   └── SystemGauges
│
├── Data
│   ├── ApplicationDbContext.cs
│   ├── ApplicationUser.cs
│   ├── SeedData.cs
│   ├── app.db
│   └── Mongo
│       ├── Accounts
│       ├── Memberships
│       ├── Payments
│       └── Referrals
│
├── DTOs
│
├── Hubs
│   └── CockpitHub.cs
│
├── Migrations
│
├── Models
│   ├── Aircraft.cs
│   ├── CockpitLayout.cs
│   ├── EmergencyScenario.cs
│   ├── FlightLog.cs
│   ├── Membership.cs
│   ├── PerformanceResult.cs
│   ├── PilotAchievement.cs
│   ├── PilotAction.cs
│   ├── PilotProfile.cs
│   ├── ScenarioProcedureStep.cs
│   ├── ScenarioRun.cs
│   └── SimulationReport.cs
│
├── Repositories
│   ├── AircraftRepository.cs
│   ├── CockpitLayoutRepository.cs
│   ├── ScenarioRepository.cs
│   ├── MembershipRepository.cs
│   └── EfGenericRepository.cs
│
├── Services
│   ├── Authorization
│   ├── AdminDashboardService.cs
│   ├── AiInstructorService.cs
│   ├── AircraftService.cs
│   ├── CockpitLayoutService.cs
│   ├── InstructorDashboardService.cs
│   ├── MembershipService.cs
│   ├── PerformanceDashboardService.cs
│   ├── PerformanceService.cs
│   ├── ScenarioService.cs
│   ├── SimulationScenarioDataService.cs
│   └── SimulationService.cs
│
├── Simulation
│   ├── Controls
│   ├── Instruments
│   ├── Layouts
│   ├── Scenarios
│   ├── AircraftSimulationEngine.cs
│   ├── CockpitState.cs
│   ├── EmergencyEngine.cs
│   ├── PerformanceScoringEngine.cs
│   ├── ScenarioTriggerEvaluator.cs
│   └── SimulationEngine.cs
│
├── wwwroot
│   ├── audio
│   ├── images
│   ├── js
│   ├── sounds
│   ├── svg
│   ├── videos
│   ├── app.css
│   ├── app.js
│   ├── reports.css
│   └── voice-control.js
│
├── Dockerfile
├── Program.cs
├── appsettings.json
├── AeroResponse.csproj
└── AeroResponse.sln
```

# Future Enhancements

*** Potential future enhancements include:***

* More advanced AI-powered instructor assistance
* Expanded voice-controlled cockpit interaction
* Advanced aircraft telemetry
* Flight replay and post-flight reconstruction
* Multiplayer and instructor-led training sessions
* Real-time instructor intervention
* Machine learning performance analysis
* Predictive identification of pilot training weaknesses
* Advanced leaderboards and performance comparisons
* Virtual reality cockpit integration
* Additional commercial and general aviation aircraft
* Expanded aircraft-specific cockpit systems
* More sophisticated aerodynamic simulation
* Weather and environmental emergency conditions
* Air traffic control simulation
* Commercial subscription model expansion
* Training organisation management tools
* FAA/EASA-aligned training scenario expansion
* External aviation data and training-system integrations

AeroResponse demonstrates how modern web development, simulation engineering, cloud technologies, databases, artificial intelligence, voice interaction, and performance analytics can be combined to create a scalable foundation for future digital aviation emergency training.

# Favorite Quotes!

* Nathan's : "It's not the pursuit of happiness, it's the happiness in the pursuit." - Jimmy Carr

* Kim's : "It always seems impossible until it's done." - Nelson Mandela

* Nephi's : "Believe you can and you're halfway there." — Theodore Roosevelt