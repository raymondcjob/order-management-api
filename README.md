# Order Management API

Backend system simulating a real-world e-commerce order processing workflow, including inventory validation, order lifecycle management, and transactional business rules.

## Overview

This project demonstrates the design and implementation of a structured backend system using RESTful APIs and JSON-based communication. It models real-world business processes such as order creation, inventory validation, and controlled state transitions.

---

## Key Highlights

- Designed a multi-entity relational system (Orders, Products, OrderItems)
- Implemented business rules for checkout and inventory validation
- Enforced order lifecycle using controlled state transitions
- Applied service layer architecture with dependency injection
- Built RESTful APIs with DTO-based JSON request/response models

---

## Tech Stack

### Backend

- C#
- ASP.NET Core Web API (.NET 10)
- Entity Framework Core
- PostgreSQL

### Tools

- Swagger (API testing)
- Git

---

## Features

### Product Management

- Create products
- View products
- Track inventory levels

### Order Management

- Create orders
- Add items to orders
- View order details with totals

### Checkout System

- Validate inventory before checkout
- Prevent checkout if stock is insufficient
- Deduct stock upon successful checkout
- Automatically update order status

### Order Lifecycle

Supported transitions:

Pending → Paid → Shipped → Completed  
Pending → Cancelled  
Pending → Paid → Cancelled  

Invalid transitions are rejected.

---

## API Endpoints

### Products

- GET /api/products  
- GET /api/products/{id}  
- POST /api/products  

### Orders

- POST /api/orders  
- GET /api/orders/{id}  
- POST /api/orders/{id}/items  
- POST /api/orders/{id}/checkout  
- PUT /api/orders/{id}/status  

---

## Technical Skills

This project demonstrates:

- Writing, modifying, and testing software code
- Developing RESTful APIs using JSON-based communication
- Implementing business logic and system workflows
- Designing systems with controlled data flow and state transitions
- Evaluating system behavior through validation and rule enforcement

---

## Testing & Validation

- Input validation ensures data integrity
- Business rules enforce system reliability (e.g., inventory checks, state transitions)
- Invalid operations return appropriate error responses

---

## Architecture

Layered design for maintainability:

Controllers → Services → Data (EF Core) → Database

### Key Design Decisions

- DTOs separate API contracts from database models
- Service layer encapsulates business logic
- Enum-based status system ensures valid state transitions

---

## Business Rules

- Orders must be in `Pending` state to add items
- Orders must contain items before checkout
- Checkout fails if inventory is insufficient
- Stock updates only after successful checkout
- Invalid status transitions are blocked

---

## How to Run

### 1. Clone

git clone https://github.com/raymondcjob/order-management-api  
cd OrderManagementApi  

### 2. Configure database

Update `appsettings.json`

### 3. Apply migrations

dotnet ef database update  

### 4. Run

dotnet run  

### 5. Swagger

https://localhost:5070/swagger  

---

## Future Improvements

- Authentication (JWT)
- Global error handling middleware
- Pagination for large datasets
- Logging and monitoring

---

## Summary

This project demonstrates backend engineering skills including:

- REST API design
- JSON-based communication
- Business rule implementation
- System workflow modeling
- Data validation and reliability handling