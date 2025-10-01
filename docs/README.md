# Payslip Management System

A comprehensive payslip management system built with .NET Core 8, Blazor Server, and Entity Framework Core.

## 🚀 Features

### Core Functionality
- **Multi-role Authentication** (Admin, HR Manager, Department Manager, Employee)
- **Employee Management** with complete profiles and organizational hierarchy
- **Department Management** with manager assignments
- **Payslip Generation** with automated calculations
- **Responsive Dashboard** with key metrics and quick actions
- **Role-based Authorization** for secure access control

### Technical Features
- **Clean Architecture** with separation of concerns
- **Entity Framework Core** with Code First approach
- **Identity Framework** for authentication and authorization
- **LocalDB** for development database
- **Blazor Server** for interactive frontend
- **AutoMapper** for object mapping
- **FluentValidation** for input validation

## 🏗️ Project Structure

```
PayslipManagement/
├── src/
│   ├── PayslipManagement.API/          # Web API Controllers
│   ├── PayslipManagement.Blazor/       # Blazor Server App
│   ├── PayslipManagement.Core/         # Business Logic
│   ├── PayslipManagement.Data/         # Entity Framework & Data Access
│   ├── PayslipManagement.Models/       # Domain Models & DTOs
│   └── PayslipManagement.Shared/       # Common Utilities
├── tests/
│   └── PayslipManagement.Tests/        # Unit & Integration Tests
└── docs/
    └── README.md
```

## 📊 Database Schema

### Core Entities
- **Company** - Organization details
- **Department** - Departmental structure
- **Employee** - Employee profiles with comprehensive information
- **ApplicationUser** - Identity-based user accounts
- **Payslip** - Salary slip records
- **AllowanceType/DeductionType** - Configurable salary components
- **EmployeeAllowance/EmployeeDeduction** - Employee-specific components

## 🔧 Getting Started

### Prerequisites
- .NET 8.0 SDK
- SQL Server LocalDB
- Visual Studio 2022 or VS Code

### Installation

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd PayslipManagement
   ```

2. **Restore NuGet packages**
   ```bash
   dotnet restore
   ```

3. **Build the solution**
   ```bash
   dotnet build
   ```

4. **Run the application**
   ```bash
   cd src/PayslipManagement.Blazor
   dotnet run
   ```

5. **Access the application**
   - Navigate to `https://localhost:5001` or `http://localhost:5000`
   - The database will be created automatically with seed data

### Default Login Credentials

| Role | Email | Password |
|------|--------|----------|
| Admin | admin@democompany.com | Admin@123 |
| HR Manager | hr@democompany.com | Hr@123 |
| Employee | john.doe@democompany.com | Employee@123 |

## 🎯 Current Implementation Status

### ✅ Completed Features
- [x] Project setup and architecture
- [x] Entity Framework models and relationships
- [x] Identity Framework with role-based authentication
- [x] Basic Blazor UI with dashboard
- [x] Navigation menu with role-based visibility
- [x] Database seeding with sample data
- [x] Clean architecture implementation

### 🚧 In Progress
- [ ] Database design completion
- [ ] Employee management CRUD operations
- [ ] Payslip calculation engine

### 📋 Planned Features
- [ ] Advanced payslip generation
- [ ] PDF export functionality
- [ ] Email integration
- [ ] Reporting and analytics
- [ ] Leave management integration
- [ ] Attendance tracking
- [ ] Multi-currency support
- [ ] Tax configuration management
- [ ] Audit trails
- [ ] Backup/restore functionality

## 🔒 Security Features

- **ASP.NET Core Identity** for user management
- **Role-based authorization** with policies
- **Secure password policies** enforced
- **SQL injection protection** via Entity Framework
- **XSS protection** through Blazor's automatic encoding

## 🎨 UI/UX Features

- **Responsive design** for mobile, tablet, and desktop
- **Modern Bootstrap-based UI**
- **Role-specific dashboards** and navigation
- **Real-time updates** via Blazor Server
- **Interactive components** and forms

## 📈 Performance Considerations

- **Entity Framework optimizations** with proper indexing
- **Efficient database queries** with Include statements
- **Blazor Server** for reduced client-side load
- **Proper disposal patterns** for resources

## 🧪 Testing

The solution includes a test project structure ready for:
- Unit tests for business logic
- Integration tests for data access
- End-to-end testing capabilities

## 🤝 Contributing

This project follows clean coding principles and architectural patterns:
- **SOLID principles**
- **Dependency injection**
- **Repository pattern** (ready for implementation)
- **Service layer pattern**
- **DTO pattern** for data transfer

## 📞 Support

For questions or support, please refer to the documentation or create an issue in the project repository.

---

**Built with ❤️ using .NET Core, Blazor, and Entity Framework**