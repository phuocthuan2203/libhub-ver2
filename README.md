# LibHub - Library Management System

A microservices-based library management system built with ASP.NET Core 8.0.

## Project Structure

- `src/Services/UserService` - User authentication and identity management
- `src/Services/CatalogService` - Book catalog and inventory management
- `src/Services/LoanService` - Book borrowing and returns
- `src/Gateway` - API Gateway (Ocelot)
- `frontend` - HTML/CSS/JavaScript user interface
- `docs` - Project documentation
- `tests` - Unit and integration tests

## Tech Stack

- **Backend**: ASP.NET Core 8.0 Web API
- **Database**: MySQL 8.0
- **ORM**: Entity Framework Core 8.0
- **API Gateway**: Ocelot
- **Authentication**: JWT (JSON Web Tokens)
- **Frontend**: Vanilla JavaScript (ES6+)

## Development Environment

- Ubuntu 25.10
- .NET 8 SDK
- MySQL 8.0
- Visual Studio Code with C# Dev Kit

## Getting Started

(Instructions will be added as development progresses)

## Architecture

This project follows:
- Microservices Architecture
- Clean Architecture (per microservice)
- Database per Service pattern
- Saga pattern for distributed transactions

## License

Academic project - All rights reserved
