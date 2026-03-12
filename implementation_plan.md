# Complete Module 1: Employee Management & Dashboards

This plan details the steps required to complete the remaining Module 1 tasks, specifically the Supplier Employee Management and the Warehouse/Delivery dashboards. 

## User Review Required
Please review the roles and dashboard routing below. If you have specific design requirements for Employee views, let me know.

## Proposed Changes

### Database & Authentication
#### [MODIFY] AccountController.cs
- Update the [Login](file:///c:/SCM_System/Controllers/AccountController.cs#359-373) method to support `Warehouse` and `Delivery` roles.
- Redirect `Warehouse` users to `Warehouse/Dashboard` and `Delivery` users to `Delivery/Dashboard`.

### Supplier Employee Management
#### [MODIFY] SupplierController.cs
- Add `Employees()` GET action to list all employees under the logged-in supplier.
- Add `AddEmployee()` GET and POST actions. The POST action will:
  - Create a new `User` with role `Warehouse` or `Delivery`, using a hashed generated/provided password.
  - Create a new [SupplierEmployee](file:///c:/SCM_System/Models/Entities/SupplierEmployee.cs#5-37) record linking the user to the supplier.
- Add `EditEmployee()` GET and POST actions.
- Add `DeleteEmployee()` POST action.

#### [NEW] Views/Supplier/Employees.cshtml
- A view to list all current employees belonging to the supplier.

#### [NEW] Views/Supplier/AddEmployee.cshtml
- A form to add a new employee (Name, Email, Phone, Password, Role).

#### [NEW] Views/Supplier/EditEmployee.cshtml
- A form to edit existing employee details.

### Employee Dashboards
#### [NEW] WarehouseController.cs
- Create the controller with `[Authorize(Roles = "Warehouse")]` or manual session checks. 
- Implement the [Dashboard](file:///c:/SCM_System/Controllers/SupplierController.cs#17-42) action.

#### [NEW] Views/Warehouse/Dashboard.cshtml
- Create an empty shell matching the "Warehouse Dashboard" ASCII wireframe provided in the requirements.

#### [NEW] DeliveryController.cs
- Create the controller with `[Authorize(Roles = "Delivery")]` or manual session checks.
- Implement the [Dashboard](file:///c:/SCM_System/Controllers/SupplierController.cs#17-42) action.

#### [NEW] Views/Delivery/Dashboard.cshtml
- Create an empty shell matching the "Delivery Dashboard" ASCII wireframe provided in the requirements.

## Verification Plan

### Automated Tests
- Build the solution using `dotnet build` to ensure there are no compilation errors.

### Manual Verification
1. Log in as a Supplier.
2. Navigate to "My Employees" to view the empty list.
3. Add a new Warehouse employee and a new Delivery employee.
4. Verify the employees appear in the list.
5. Log out.
6. Log in as the newly created Warehouse employee. Verify redirection to the Warehouse Dashboard shell.
7. Log out.
8. Log in as the newly created Delivery employee. Verify redirection to the Delivery Dashboard shell.
