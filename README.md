# Procera Assessment

A web-based application developed using ASP.NET Core MVC and Entity Framework Core to manage people, their accounts, and financial transactions.

## Technologies

- C#
- ASP.NET Core MVC (.NET 10)
- Entity Framework Core
- Microsoft SQL Server
- Bootstrap 5
- BCrypt password hashing
- Cookie-based authentication

## Features

### Authentication
- Custom user authentication using a SQL Server Users table.
- Passwords hashed using BCrypt.
- Protected application functionality.
- Login and logout.

### Person Management
- Create, view, edit, and delete persons.
- Search by ID number, surname, or account number.
- Pagination with a maximum of 10 persons per page.
- Person deletion is restricted when associated accounts are open.

### Account Management
- Create, view, and edit accounts.
- Unique account numbers.
- Open and closed account statuses.
- Close accounts only when their outstanding balance is zero.
- Reopen closed accounts.
- Outstanding balances are updated automatically through transactions.

### Transaction Management
- Create and edit debit and credit transactions.
- Transaction dates cannot be in the future.
- Capture dates are automatically maintained by the application.
- Closed accounts cannot receive transactions.
- Transaction changes automatically adjust the associated account balance.

## Prerequisites

Before running the application, ensure you have:

- .NET 10 SDK
- Microsoft SQL Server, SQL Server Express, or Azure SQL Database
- SQL Server Management Studio or another SQL client
- Entity Framework Core CLI (version 10.0.14)

Install the EF Core CLI if it is not already installed:

```bash
dotnet tool install --global dotnet-ef --version 10.0.14
```

If it is already installed, update it if necessary:

```bash
dotnet tool update --global dotnet-ef --version 10.0.14
```

## Database Setup

### 1. Create a database

Create an empty SQL Server database named `ProceraAssessmentDb`, or use another database name of your choice.

### 2. Create the database schema

Execute the following script against the empty database:

```text
Database/01_CreateDatabaseSchema.sql
```

This creates the required tables, relationships, constraints, account statuses, and initial indexes.

**Important:** Run this script before applying Entity Framework Core migrations. The initial migration is a baseline migration because the original assessment tables are created through this SQL script.

### 3. Configure the connection string

The application uses .NET User Secrets to store sensitive configuration locally.

From the project directory, configure the database connection string:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "YOUR_CONNECTION_STRING"
```

For SQL Server Authentication, replace the placeholder with your actual SQL Server connection string.

Do not commit database credentials or connection strings containing passwords to source control.

### 4. Apply Entity Framework Core migrations

Run:

```bash
dotnet ef database update
```

This applies the remaining migrations, including the additional indexes and Users table.

### 5. Insert sample data

To populate the database with the assessment sample records, execute:

```text
Database/02_SeedAssessmentData.sql
```

The script inserts the supplied sample Persons, Accounts, and Transactions.

## Initial Login Configuration

The application provisions an initial assessment user automatically when running in the Development environment.

Configure the username and password using User Secrets:

```bash
dotnet user-secrets set "AssessmentAdmin:Username" "admin"
dotnet user-secrets set "AssessmentAdmin:Password" "YOUR_CHOSEN_PASSWORD"
```

The initial user is created only if the configured username does not already exist.

Passwords are hashed using BCrypt before being stored in the database.

## Running the Application

From the project directory, execute:

```bash
dotnet restore
dotnet run
```

Open the local URL displayed in the terminal.

Log in using the assessment credentials configured through User Secrets.

## Project Structure

```text
ProceraAssessment/
├── Controllers/
├── Data/
├── Database/
│   ├── 01_CreateDatabaseSchema.sql
│   └── 02_SeedAssessmentData.sql
├── Migrations/
├── Models/
├── ViewModels/
├── Views/
│   ├── Accounts/
│   ├── Auth/
│   ├── Home/
│   ├── Persons/
│   └── Transactions/
├── wwwroot/
├── Program.cs
└── ProceraAssessment.csproj
```

## Notes

- Database credentials and initial admin credentials are stored locally using User Secrets.
- The application must be run in Development mode for automatic initial user provisioning.
- Sample data is optional and can be inserted using the supplied SQL seed script.