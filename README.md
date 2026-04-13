# 🏗️ **MANGO MICROSERVICES ARCHITECTURE OVERVIEW**

## 📋 **TECHNICAL REQUIREMENTS**

### **Prerequisites**
- **.NET 10.0** or later
- **Visual Studio 2022** or **VS Code** with C# extension
- **Docker Desktop** (for containerized services)
- **PostgreSQL** or **SQL Server** database
- **Redis** (for caching)
- **RabbitMQ** (for messaging)

### **Development Tools**
- **Postman** or similar API testing tool
- **Git** for version control
- **Azure Account** (optional, for Azure Service Bus)
- **Stripe Account** (for payment processing)

---

## **🔐 1. AuthAPI (Authentication Service)**
**Port: 7002**
- **User Management**: Registration, login, role-based authentication
- **JWT Token Generation**: Issues JWT tokens for authenticated users
- **Identity Framework Integration**: Uses ASP.NET Core Identity with custom ApplicationUser
- **Features**:
  - User registration and login
  - Role assignment (Admin, Customer)
  - JWT token generation and validation
  - Password management
  - User profile management with timestamps (CreatedAt/UpdatedAt)

## **🛍️ 2. ProductAPI (Product Catalog Service)**
**Port: 7000**
- **Product Management**: CRUD operations for products
- **Category & Brand Management**: Organize products by categories and brands
- **Image Handling**: Product image upload and management
- **Caching**: Redis/Memory caching for performance
- **Features**:
  - Add/Edit/Delete products
  - Category and brand management
  - Product search and filtering
  - Pagination support
  - Image upload to local storage
  - Stock quantity tracking
  - Product seeding with sample data

## **🎟️ 3. CouponAPI (Coupon Management Service)**
**Port: 7001**
- **Coupon Management**: Create, update, delete discount coupons
- **Discount Validation**: Validate coupon codes and calculate discounts
- **Features**:
  - Create percentage and fixed-amount coupons
  - Set expiration dates and usage limits
  - Validate coupon codes
  - Apply discounts to orders
  - Coupon code generation

## **🛒 4. ShoppingCartAPI (Cart Management Service)**
**Port: 7003**
- **Cart Operations**: Add, remove, update items in shopping cart
- **User Cart Persistence**: Maintain cart state across sessions
- **External Service Integration**: Fetch product and coupon data
- **Messaging**: RabbitMQ integration for cart events
- **Features**:
  - Add/remove products to/from cart
  - Update item quantities
  - Apply coupons to cart
  - Calculate cart totals with discounts
  - Persist cart across user sessions
  - Integration with Product and Coupon APIs

## **📦 5. OrderAPI (Order Management Service)**
**Port: 7004**
- **Order Processing**: Create and manage orders
- **Order Status Tracking**: Track order lifecycle (Pending, Approved, ReadyForPickup, etc.)
- **Payment Integration**: Handle payment confirmation
- **Messaging**: RabbitMQ for order events and email notifications
- **Features**:
  - Create orders from shopping cart
  - Order status management
  - Order history and tracking
  - Integration with Payment API
  - Email notifications via RabbitMQ
  - Shipping address management

## **💳 6. PaymentAPI (Payment Processing Service)**
**Port: 7005**
- **Stripe Integration**: Process payments using Stripe
- **Payment Confirmation**: Handle payment success/failure
- **Order Communication**: Update order status after payment
- **Features**:
  - Stripe payment processing
  - Payment intent creation
  - Payment confirmation handling
  - Integration with Order API
  - Payment status tracking
  - Secure payment data handling

## **🎁 7. RewardAPI (Loyalty Points Service)**
**Port: 7006**
- **Reward Points Management**: Track and award loyalty points
- **Payment Event Processing**: Listen for successful payments
- **Point Calculation**: Calculate rewards based on order amounts
- **Messaging**: RabbitMQ/Azure Service Bus integration
- **Features**:
  - Award points for completed orders
  - Track user reward history
  - Point calculation algorithms
  - Integration with payment events
  - User reward balance tracking

## **📧 8. EmailAPI (Notification Service)**
**Port: 7007**
- **Email Processing**: Send transactional emails
- **Template Management**: Email templates for different events
- **Queue Processing**: RabbitMQ message consumption
- **Features**:
  - Order confirmation emails
  - Cart abandonment notifications
  - User registration emails
  - Password reset notifications
  - Email template management
  - Queue-based email processing

## **🌐 9. GatewaySolution (API Gateway)**
**Port: 7777**
- **Request Routing**: Route requests to appropriate microservices
- **Authentication Gateway**: Centralized authentication validation
- **Load Balancing**: Distribute requests across service instances
- **Rate Limiting**: Control API usage
- **Features**:
  - Ocelot gateway configuration
  - JWT token validation
  - Request/response transformation
  - Service discovery
  - Cross-cutting concerns (logging, monitoring)

---

# 🔧 **SHARED INFRASTRUCTURE COMPONENTS**

## **📚 Mango.Common.Extensions**
- **Database Abstraction**: Multi-provider support (PostgreSQL/SQL Server)
- **Authentication**: Shared JWT configuration
- **Base Classes**: BaseEntity, BaseIdentityUser, BaseEntityDto
- **Timestamp Management**: Automatic CreatedAt/UpdatedAt tracking (UTC)
- **Database Initialization**: Three-tier initialization strategy

## **💾 Mango.Cache**
- **Caching Strategy**: Redis and Memory cache implementations
- **Cache Management**: Keyed cache services with configurable expiration

## **🐰 Mango.MessageRabbit**
- **RabbitMQ Integration**: Message publishing and consuming
- **Health Monitoring**: Connection health checks
- **Queue Management**: Configurable queue and exchange setup

## **☁️ Mango.MessageBus**
- **Azure Service Bus**: Alternative messaging implementation
- **Topic/Subscription**: Pub/sub messaging patterns

---

# 🔄 **SERVICE COMMUNICATION PATTERNS**

## **Synchronous Communication (HTTP)**
- **API Gateway** ↔ All Services
- **ShoppingCartAPI** ↔ **ProductAPI** (product details)
- **ShoppingCartAPI** ↔ **CouponAPI** (discount validation)
- **PaymentAPI** ↔ **OrderAPI** (order updates)

## **Asynchronous Communication (Messaging)**
- **OrderAPI** → **EmailAPI** (order notifications)
- **PaymentAPI** → **RewardAPI** (reward points)
- **ShoppingCartAPI** → **EmailAPI** (cart events)

---

# 🎯 **BUSINESS WORKFLOWS**

1. **User Registration/Login**: AuthAPI handles authentication
2. **Browse Products**: ProductAPI serves catalog with caching
3. **Apply Coupons**: CouponAPI validates and calculates discounts
4. **Manage Cart**: ShoppingCartAPI maintains cart state
5. **Place Order**: OrderAPI creates order from cart
6. **Process Payment**: PaymentAPI handles Stripe integration
7. **Award Points**: RewardAPI tracks loyalty rewards
8. **Send Notifications**: EmailAPI sends confirmation emails
9. **Gateway Routing**: All external requests go through API Gateway

---

# 🚀 **TECHNICAL ARCHITECTURE**

## **Database Strategy**
- **Multi-Provider Support**: PostgreSQL and SQL Server
- **Runtime Switching**: Configure provider via `DatabaseProvider` setting
- **Shared Base Classes**: BaseEntity for automatic timestamp tracking
- **Provider-Specific Contexts**: Separate contexts for each database provider

## **Authentication & Authorization**
- **JWT Tokens**: Shared JWT configuration across all services
- **Role-Based Access**: Admin and Customer roles
- **Gateway Authentication**: Centralized token validation

## **Caching Strategy**
- **Redis**: Distributed caching for production
- **Memory Cache**: Local caching for development
- **Configurable**: Switch between providers via configuration

## **Messaging Architecture**
- **RabbitMQ**: Primary messaging system
- **Azure Service Bus**: Alternative cloud messaging
- **Event-Driven**: Asynchronous communication between services

## **Logging & Monitoring**
- **log4net**: Structured logging across all services
- **Health Checks**: Service health monitoring
- **Centralized Configuration**: Shared logging configuration

---

# 📊 **SERVICE DEPENDENCIES**

## **Core Dependencies**
```
API Gateway
├── AuthAPI (Authentication)
├── ProductAPI (Catalog)
├── CouponAPI (Discounts)
├── ShoppingCartAPI (Cart Management)
├── OrderAPI (Order Processing)
├── PaymentAPI (Payment Processing)
├── RewardAPI (Loyalty Points)
└── EmailAPI (Notifications)
```

## **External Dependencies**
- **Stripe**: Payment processing
- **RabbitMQ**: Message broker
- **Redis**: Distributed caching
- **PostgreSQL/SQL Server**: Database providers

---

# 🔐 **SECURITY FEATURES**

- **JWT Authentication**: Secure token-based authentication
- **Role-Based Authorization**: Admin and Customer access levels
- **API Gateway Security**: Centralized security enforcement
- **HTTPS Enforcement**: Secure communication
- **CORS Configuration**: Cross-origin resource sharing control

---

# 🎨 **DESIGN PATTERNS IMPLEMENTED**

- **Microservices Architecture**: Domain-driven service separation
- **API Gateway Pattern**: Centralized routing and security
- **Repository Pattern**: Data access abstraction
- **CQRS**: Command Query Responsibility Segregation
- **Event Sourcing**: Event-driven communication
- **Circuit Breaker**: Fault tolerance (via HTTP clients)
- **Shared Kernel**: Common extensions and base classes

This architecture provides a complete e-commerce platform with microservices handling distinct business domains, shared infrastructure for cross-cutting concerns, and both synchronous and asynchronous communication patterns for optimal performance and scalability.