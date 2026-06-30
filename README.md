# Identity Platform (Personal IAM Project)

This is a personal Identity & Access Management (IAM) platform built to study and practice modern authentication, authorization, and enterprise identity architecture.

## 🚀 Tech Stack
- ASP.NET Core
- OpenIddict (OAuth2 / OpenID Connect)
- Entity Framework Core
- PostgreSQL
- Clean Architecture

## 🔐 Key Features

### Authentication & Authorization
- OAuth 2.0 / OpenID Connect (Authorization Code Flow + PKCE)
- JWT token validation and refresh tokens
- Cookie-based interactive login
- Policy-based authorization using permission claims

### Role-Based Access Control (RBAC)
- Users, Roles, and Permissions model
- Separation between Portal Roles and Target Roles
- Many-to-many relationships (UserRoles, RolePermissions)

### Core Modules
- User management (CRUD + activation/deactivation)
- Role management (CRUD)
- Permission management (CRUD)
- Aggregated user access (roles + permissions)

### Architecture
- Clean Architecture (Domain / Application / Infrastructure / API)
- Seeders for initial admin user and OpenIddict client
- PostgreSQL with EF Core migrations

## 🎯 Purpose
This project is focused on understanding how real-world IAM systems work under the hood, including authentication flows, authorization models, and scalable identity design.

---

> Built as a continuous learning project to strengthen backend, security, and identity management skills.