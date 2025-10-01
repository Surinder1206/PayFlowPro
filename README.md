# PayFlow Pro 🚀

**PayFlow Pro** is a comprehensive employee management and payroll system built with .NET 8 and Blazor Server. It provides a complete solution for managing employees, payroll processing, leave management, and audit tracking.

## 🌟 Features

### Core Functionality
- **Employee Management** - Complete employee lifecycle management
- **Payroll Processing** - Automated payroll calculation and generation
- **Leave Management** - Request, approve, and track employee leave
- **Department Management** - Organize employees by departments
- **User Management** - Role-based access control (Admin, HR, Manager, Employee)

### Advanced Features
- **Audit System** - Comprehensive audit logging and tracking
- **Reports & Analytics** - Detailed reporting and dashboard analytics
- **Self-Service Portal** - Employee self-service capabilities
- **Document Management** - Handle employee documents and payslips
- **Calendar Integration** - Leave calendar and schedule management

## 🛠️ Technical Stack

- **.NET 8** - Latest .NET framework
- **Blazor Server** - Interactive web UI framework
- **Entity Framework Core** - Object-relational mapping
- **SQL Server** - Database engine
- **Bootstrap 5** - Responsive UI framework
- **Identity Framework** - Authentication and authorization

## 🚀 Getting Started

### Prerequisites
- .NET 8 SDK
- SQL Server (LocalDB, Express, or Full)
- Visual Studio 2022 or VS Code

### Installation

1. **Clone the repository**
   ```bash
   git clone https://github.com/yourusername/PayFlowPro.git
   cd PayFlowPro
   ```

2. **Update Connection String**
   Update the connection string in `src/PayFlowPro.Web/appsettings.json`

3. **Run Database Migrations**
   ```bash
   dotnet ef database update --project src/PayFlowPro.Data --startup-project src/PayFlowPro.Web
   ```

4. **Build and Run**
   ```bash
   dotnet run --project src/PayFlowPro.Web
   ```

5. **Access the Application**
   Navigate to `https://localhost:5126` in your browser

## 📁 Project Structure

```
PayFlowPro/
├── src/
│   ├── PayFlowPro.Web/           # Blazor Server application
│   ├── PayFlowPro.Core/          # Business logic and services
│   ├── PayFlowPro.Data/          # Data access layer
│   └── PayFlowPro.Models/        # Domain models and DTOs
├── tests/
│   └── PayFlowPro.Tests/         # Unit and integration tests
├── docs/                         # Documentation
└── audit setup files            # Audit system setup scripts
```

## 👥 Default Users

The system comes with pre-configured users for testing:

| Role | Email | Password |
|------|--------|----------|
| Admin | admin@payflowpro.com | Admin@123 |
| HR | hr@payflowpro.com | Hr@123 |
| Manager | manager@payflowpro.com | Manager@123 |
| Employee | employee@payflowpro.com | Employee@123 |

## 🔧 Configuration

### Leave Types
The system includes pre-configured leave types:
- Annual Leave (20 days)
- Sick Leave (12 days)
- Emergency Leave (5 days)
- Maternity Leave (90 days)
- Paternity Leave (14 days)
- Compensatory Leave (earned)

### Audit System
To set up the audit system, run:
```bash
setup_audit_tables.bat
```

## 📊 Key Components

### Leave Management
- **Leave Requests** - Submit and track leave applications
- **Leave Approval** - Multi-level approval workflow
- **Leave Calendar** - Visual calendar view of team leave
- **Leave Balances** - Track available leave days

### Payroll Management
- **Payslip Generation** - Automated payslip creation
- **Salary Components** - Configure allowances and deductions
- **Payroll Reports** - Comprehensive payroll reporting
- **Tax Calculations** - Built-in tax computation

### Audit & Compliance
- **Audit Logs** - Track all system activities
- **Security Monitoring** - Monitor user access and actions
- **Data Integrity** - Ensure data consistency and accuracy
- **Compliance Reports** - Generate compliance documentation

## 🧪 Testing

Run the test suite:
```bash
dotnet test
```

For audit system testing, refer to:
- `AUDIT_TESTING_GUIDE.md`
- `HOW_TO_TEST_AUDIT.md`

## 📝 Documentation

- **API Documentation** - Available in `/docs` folder
- **User Manual** - Complete user guide
- **Developer Guide** - Technical implementation details
- **Audit Guide** - Audit system documentation

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests for new functionality
5. Submit a pull request

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🆘 Support

For support and questions:
- Create an issue on GitHub
- Contact the development team
- Check the documentation in `/docs`

## 🔄 Version History

### v2.0.0 (PayFlow Pro)
- Complete rebrand from PayslipManagement to PayFlow Pro
- Enhanced UI/UX with modern design
- Improved leave management system
- Advanced audit capabilities
- Performance optimizations

### v1.0.0 (PayslipManagement)
- Initial release
- Basic payroll functionality
- Employee management
- Simple leave system

---

**PayFlow Pro** - Streamlining payroll and employee management for modern businesses. 🌟
