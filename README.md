# ZenBlog Server

A RESTful blog API built with **ASP.NET Core (.NET 10)** following **Clean Architecture** principles. Features CQRS via MediatR, ASP.NET Core Identity, Entity Framework Core with PostgreSQL, a generic repository pattern, audit interceptors, and a FluentValidation pipeline.

---

## Table of Contents

- [Architecture Overview](#architecture-overview)
- [Project Structure](#project-structure)
- [Tech Stack](#tech-stack)
- [Domain Entities](#domain-entities)
- [Features & Endpoints](#features--endpoints)
- [Authentication (JWT)](#authentication-jwt)
- [Key Design Decisions](#key-design-decisions)
- [Getting Started](#getting-started)
- [Configuration](#configuration)
- [Running Migrations](#running-migrations)

---

## Architecture Overview

ZenBlog follows **Clean Architecture** with four layers that depend strictly inward:

```
┌─────────────────────────────────────────────┐
│            Presentation                     │
│            ZenBlog.API                      │  ← Minimal API endpoints, middleware
├─────────────────────────────────────────────┤
│            Infrastructure                  │
│            ZenBlog.Persistence             │  ← EF Core, Repositories, Migrations
│            ZenBlog.Infrastructure          │  ← JWT auth, current-user service
├─────────────────────────────────────────────┤
│            Core                             │
│            ZenBlog.Application             │  ← CQRS, Handlers, Validators, DTOs, Identity contracts
│            ZenBlog.Domain                  │  ← Entities, no dependencies
└─────────────────────────────────────────────┘
```

- **Domain** hosts entities; `AppUser`/`AppRole` inherit ASP.NET Identity store types (the only Domain package dep).
- **Application** depends only on Domain — no EF Core, no HTTP, no `UserManager`. It defines *ports* (`IJwtTokenGenerator`, `ICurrentUserService`, `IUserAccountService`, `IUserQueryService`, `IRoleChecker`, …) that outer layers implement.
- **Persistence** implements the persistence contracts — EF Core and PostgreSQL live here only.
- **Infrastructure** implements the identity/auth/media contracts — JWT, current-user, user account/query/role adapters wrapping Identity, Cloudinary.
- **API** wires everything together — minimal endpoints, middleware, no business logic.

---

## Project Structure

```
ZenBlogServer/
├── .github/
│   └── workflows/
├── Core/
│   ├── ZenBlog.Application/
│   │   ├── Base/
│   │   │   ├── BaseDto.cs
│   │   │   └── BaseResult.cs
│   │   ├── Behaviors/
│   │   │   └── ValidationBehavior.cs
│   │   ├── Contracts/
│   │   │   ├── Identity/
│   │   │   │   ├── ICurrentUserService.cs
│   │   │   │   └── IJwtTokenGenerator.cs
│   │   │   └── Persistence/
│   │   │       ├── IRepository.cs
│   │   │       └── IUnitOfWork.cs
│   │   ├── DTOs/
│   │   │   ├── BlogDto.cs
│   │   │   ├── CategoryDto.cs
│   │   │   └── UserDto.cs
│   │   ├── Extensions/
│   │   │   └── ServiceRegistration.cs
│   │   ├── Models/
│   │   │   └── JwtSettings.cs
│   │   └── Features/
│   │       ├── Auth/
│   │       │   ├── Commands/
│   │       │   │   └── LoginCommand.cs
│   │       │   ├── Handlers/
│   │       │   │   └── LoginCommandHandler.cs
│   │       │   ├── Results/
│   │       │   │   └── LoginResult.cs
│   │       │   └── Validators/
│   │       │       └── LoginValidator.cs
│   │       ├── Blogs/
│   │       │   ├── Commands/
│   │       │   │   ├── CreateBlogCommand.cs
│   │       │   │   ├── RemoveBlogCommand.cs
│   │       │   │   └── UpdateBlogCommand.cs
│   │       │   ├── Handlers/
│   │       │   │   ├── CreateBlogCommandHandler.cs
│   │       │   │   ├── GetBlogByIdQueryHandler.cs
│   │       │   │   ├── GetBlogsByCategoryIdQueryHandler.cs
│   │       │   │   ├── GetBlogsQueryHandler.cs
│   │       │   │   ├── RemoveBlogCommandHandler.cs
│   │       │   │   └── UpdateBlogCommandHandler.cs
│   │       │   ├── Mapping/
│   │       │   │   └── BlogMappingProfile.cs
│   │       │   ├── Queries/
│   │       │   │   ├── GetBlogByIdQuery.cs
│   │       │   │   ├── GetBlogsByCategoryIdQuery.cs
│   │       │   │   └── GetBlogsQuery.cs
│   │       │   ├── Results/
│   │       │   │   ├── CreateBlogResult.cs
│   │       │   │   └── GetBlogsQueryResult.cs
│   │       │   └── Validators/
│   │       │       ├── CreateBlogValidator.cs
│   │       │       └── UpdateBlogValidator.cs
│   │       ├── Categories/
│   │       │   ├── Commands/
│   │       │   │   ├── CreateCategoryCommand.cs
│   │       │   │   ├── RemoveCategoryCommand.cs
│   │       │   │   └── UpdateCategoryCommand.cs
│   │       │   ├── Handlers/
│   │       │   │   ├── CreateCategoryCommandHandler.cs
│   │       │   │   ├── GetCategoryByIdQueryHandler.cs
│   │       │   │   ├── GetCategoryQueryHandler.cs
│   │       │   │   ├── RemoveCategoryCommandHandler.cs
│   │       │   │   └── UpdateCategoryCommandHandler.cs
│   │       │   ├── Mapping/
│   │       │   │   └── CategoryMappingProfile.cs
│   │       │   ├── Queries/
│   │       │   │   ├── GetCategoryByIdQuery.cs
│   │       │   │   └── GetCategoryQuery.cs
│   │       │   ├── Results/
│   │       │   │   └── GetCategoryQueryResult.cs
│   │       │   └── Validators/
│   │       │       ├── CreateCategoryValidator.cs
│   │       │       └── UpdateCategoryValidator.cs
│   │       ├── Comments/
│   │       │   ├── Commands/
│   │       │   │   ├── CreateCommentCommand.cs
│   │       │   │   ├── RemoveCommentCommand.cs
│   │       │   │   └── UpdateCommentCommand.cs
│   │       │   ├── Handlers/
│   │       │   │   ├── CreateCommentCommandHandler.cs
│   │       │   │   ├── DeleteCommentCommandHandler.cs
│   │       │   │   ├── GetCommentByIdQueryHandler.cs
│   │       │   │   ├── GetCommentsByBlogIdQueryHandler.cs
│   │       │   │   └── UpdateCommentCommandHandler.cs
│   │       │   ├── Mapping/
│   │       │   │   └── CommentMappingProfile.cs
│   │       │   ├── Queries/
│   │       │   │   ├── GetCommentByIdQuery.cs
│   │       │   │   └── GetCommentsByBlogIdQuery.cs
│   │       │   ├── Results/
│   │       │   │   ├── CommentResult.cs
│   │       │   │   └── CreateCommentResult.cs
│   │       │   └── Validators/
│   │       │       ├── CreateCommentCommandValidation.cs
│   │       │       └── UpdateCommentCommandValidator.cs
│   │       └── Users/
│   │           ├── Commands/
│   │           │   └── CreateUserCommand.cs
│   │           ├── Handlers/
│   │           │   ├── CreateUserCommandHandler.cs
│   │           │   └── GetAllUsersQueryHandler.cs
│   │           ├── Mappings/
│   │           │   └── UserMappingProfile.cs
│   │           ├── Queries/
│   │           │   └── GetAllUsersQuery.cs
│   │           ├── Results/
│   │           │   ├── CreateUserResult.cs
│   │           │   └── GetAllUsersQueryResult.cs
│   │           └── Validators/
│   │               └── CreateUserCommandValidator.cs
│   └── ZenBlog.Domain/
│       ├── Entities/
│       │   ├── Common/
│       │   │   └── BaseEntity.cs
│       │   ├── AppRole.cs
│       │   ├── AppUser.cs
│       │   ├── Blog.cs
│       │   ├── Category.cs
│       │   ├── Comment.cs
│       │   ├── ContactInfo.cs
│       │   ├── Message.cs
│       │   └── SocialMedia.cs
│       └── ZenBlog.Domain.csproj
├── Infrastructure/
│   ├── ZenBlog.Infrastructure/
│   │   ├── Extensions/
│   │   │   └── ServiceRegistration.cs
│   │   ├── Identity/
│   │   │   ├── CurrentUserService.cs
│   │   │   └── JwtTokenGenerator.cs
│   │   └── ZenBlog.Infrastructure.csproj
│   └── ZenBlog.Persistence/
│       ├── Concrete/
│       │   ├── GenericRepository.cs
│       │   └── UnitOfWork.cs
│       ├── Context/
│       │   └── AppDbContext.cs
│       ├── Extentions/
│       │   └── ServiceRegistration.cs
│       ├── Intercepters/
│       │   └── AuditDbContextInterceptor.cs
│       ├── Migrations/
│       │   └── ...
│       └── ZenBlog.Persistence.csproj
└── Presentation/
    └── ZenBlog.API/
        ├── CustomMiddlewares/
        │   └── CustomExceptionHandlingMiddleware.cs
        ├── Endpoints/
        │   ├── AuthEndpoints.cs
        │   ├── BlogEndpoints.cs
        │   ├── CategoryEndpoints.cs
        │   ├── CommentEndpoints.cs
        │   ├── UserEndpoints.cs
        │   └── Registrations/
        │       └── EndpointRegistration.cs
        ├── Program.cs
        ├── appsettings.json
        ├── appsettings.Development.json
        └── ZenBlog.API.csproj
```

---

## Tech Stack

| Concern | Library | Version |
|---|---|---|
| Framework | ASP.NET Core Minimal APIs | .NET 10 |
| ORM | Entity Framework Core | 10.0.8 |
| Database | PostgreSQL via Npgsql | 10.0.1 |
| Identity | ASP.NET Core Identity + EF Core | 10.0.8 |
| Authentication | JWT Bearer (`Microsoft.AspNetCore.Authentication.JwtBearer`) | 10.0.8 |
| Token handling | `System.IdentityModel.Tokens.Jwt` | 8.15.1 |
| Lazy Loading | EF Core Proxies | 10.0.8 |
| CQRS / Mediator | MediatR | — |
| Object Mapping | AutoMapper | — |
| Validation | FluentValidation | — |

---

## Domain Entities

### AppUser
Extends `IdentityUser<string>` with `FirstName`, `LastName`, and `ImageUrl`. Owns blogs and comments.

### AppRole
Extends `IdentityRole<string>` for role-based authorization.

### Blog
Core content entity. Belongs to a `Category` and an `AppUser`. Has many `Comment`s.

```csharp
public class Blog : BaseEntity
{
    public string Title { get; set; }
    public string Description { get; set; }
    public string? CoverImageUrl { get; set; }
    public Guid CategoryId { get; set; }
    public string UserId { get; set; }
    public virtual IList<Comment> Comments { get; set; }
}
```

### Category
Groups blogs. One category has many blogs.

### Comment
Self-referencing entity supporting threaded replies.

```csharp
public class Comment : BaseEntity
{
    public string Body { get; set; }
    public Guid BlogId { get; set; }
    public string UserId { get; set; }
    public Guid? ParentCommentId { get; set; }  // null = top-level, set = reply
    public virtual IList<Comment> Replies { get; set; }
}
```

### Other Entities
`ContactInfo`, `Message`, and `SocialMedia` support site management features.

### BaseEntity
All domain entities inherit from `BaseEntity` which provides `Id` (Guid), `CreatedAt`, and `UpdatedAt` — automatically populated by `AuditDbContextInterceptor` on every save.

---

## Features & Endpoints

### Auth

| Method | Route | Description | Auth required |
|---|---|---|---|
| POST | `/auth/login` | Authenticate with email + password, returns a JWT | No |

### Users

| Method | Route | Description | Auth required |
|---|---|---|---|
| POST | `/users/register` | Register a new user | No |
| GET | `/users` | Get all users | 🔒 Yes  |

### Blogs

| Method | Route | Description | Auth required |
|---|---|---|---|
| GET | `/blogs` | Get all blogs with category | No |
| GET | `/blogs/{id}` | Get blog by ID | No |
| GET | `/blogs/category/{categoryId}` | Get blogs filtered by category | No |
| POST | `/blogs` | Create a blog | 🔒 Yes |
| PUT | `/blogs/{id}` | Update a blog | 🔒 Yes |
| DELETE | `/blogs/{id}` | Remove a blog | 🔒 Yes |

### Categories

| Method | Route | Description | Auth required |
|---|---|---|---|
| GET | `/categories` | Get all categories with blogs | No |
| GET | `/categories/{id}` | Get category by ID | No |
| POST | `/categories` | Create a category | 🔒 Yes |
| PUT | `/categories/{id}` | Update a category | 🔒 Yes |
| DELETE | `/categories/{id}` | Remove a category | 🔒 Yes |

### Comments

| Method | Route | Description | Auth required |
|---|---|---|---|
| GET | `/comments/blog/{blogId}` | Get top-level comments for a blog | No |
| GET | `/comments/{id}` | Get comment with its replies | No |
| POST | `/comments` | Create a comment or reply | 🔒 Yes |
| PUT | `/comments/{id}` | Update comment body | 🔒 Yes |
| DELETE | `/comments/{id}` | Remove a comment | 🔒 Yes |

---

## Authentication (JWT)

ZenBlog uses **JWT Bearer authentication**, implemented while preserving the Clean Architecture dependency rule: Application only knows about *ports* (interfaces); Infrastructure provides the actual implementation.

### How the pieces fit together

```
Application (ports, no JWT library dependency)
  Contracts/Identity/IJwtTokenGenerator.cs   — "give me a token for this user"
  Contracts/Identity/ICurrentUserService.cs  — "who is calling right now"
  Models/JwtSettings.cs                      — plain POCO bound from configuration
  Features/Auth/                             — LoginCommand → LoginCommandHandler → LoginResult

Infrastructure (implements the ports)
  Identity/JwtTokenGenerator.cs   — builds the signed JWT (System.IdentityModel.Tokens.Jwt)
  Identity/CurrentUserService.cs — reads claims off HttpContext.User
  Extensions/ServiceRegistration.cs
      → binds JwtSettings
      → registers IJwtTokenGenerator / ICurrentUserService
      → configures the JwtBearer authentication scheme (validates incoming tokens)

Presentation
  Endpoints/AuthEndpoints.cs → POST /auth/login
  Program.cs → app.UseAuthentication() before app.UseAuthorization()
  .RequireAuthorization() on every mutating (POST/PUT/DELETE) endpoint
```

`JwtTokenGenerator` (creates tokens at login) and the `AddJwtBearer(...)` scheme configured in `ServiceRegistration` (validates tokens on every request) are two separate responsibilities that happen to share the same secret/issuer/audience.

### Login flow

```
POST /auth/login  { "email": "...", "password": "..." }
        │
        ▼
LoginCommand → ValidationBehavior (email/password not empty)
        │
        ▼
LoginCommandHandler
   ├─ userManager.FindByEmailAsync(email)
   ├─ userManager.CheckPasswordAsync(user, password)
   ├─ userManager.GetRolesAsync(user)
   └─ tokenGenerator.GenerateToken(user, roles) → (token, expiresAtUtc)
        │
        ▼
200 OK { "userId": "...", "email": "...", "token": "eyJhbGciOi...", "expiresAtUtc": "..." }
```

A wrong email and a wrong password both return the same `"Invalid email or password."` message — this avoids leaking which one was incorrect (user-enumeration protection).

### Trusting the token, not the request body

Handlers that create owned resources (`CreateBlogCommandHandler`, `CreateCommentCommandHandler`) inject `ICurrentUserService` and overwrite the entity's `UserId` with the authenticated caller's id, ignoring any `UserId` sent in the request body:

```csharp
var blog = mapper.Map<Blog>(request);
blog.UserId = currentUser.UserId!;   // never trust a client-supplied UserId
await repository.CreateAsync(blog);
```

### Setting the JWT secret (required before running)

The signing secret is never committed — it's stored in **.NET User Secrets**, the same way the database connection string is handled:

```bash
dotnet user-secrets set "JwtSettings:Secret" "<a random string, at least 32 characters>" --project Presentation/ZenBlog.API
```

Generate a proper random value rather than typing one by hand:

```bash
openssl rand -base64 32
```

`Issuer`, `Audience`, and `ExpiryMinutes` are non-secret and live in `appsettings.json` under `JwtSettings` (see [Configuration](#configuration)).

### Calling a protected endpoint

```
POST /auth/login              → copy "token" from the response
POST /blogs
  Header: Authorization: Bearer <token>
```
Requests to protected routes without a valid, non-expired token receive `401 Unauthorized`.

---

### Generic Repository with Include Support
`IRepository<TEntity>` exposes `GetQuery()` for flexible querying, plus two include-capable methods that keep EF Core out of the Application layer:

```csharp
// All matching a filter with navigation properties loaded
Task<List<TEntity>> GetAllWithIncludesAsync(
    Expression<Func<TEntity, bool>> filter,
    CancellationToken ct,
    params Expression<Func<TEntity, object>>[] includes);

// Single entity with navigation properties loaded
Task<TEntity?> GetSingleWithIncludesAsync(
    Expression<Func<TEntity, bool>> filter,
    CancellationToken ct,
    params Expression<Func<TEntity, object>>[] includes);
```

### Identity users via application ports
`AppUser` inherits from `IdentityUser<string>` — it cannot use `IRepository<AppUser>` due to the `where TEntity : BaseEntity` constraint. Handlers use Application ports (`IUserAccountService`, `IUserQueryService`, `IRoleChecker`) instead of `UserManager` directly; Infrastructure adapters wrap Identity.

### Flat DTOs to Prevent Circular References
Navigation properties in result DTOs use flat summary types (`CategoryDto`, `BlogDto`, `UserDto`) that never reference back to their parent — preventing infinite JSON serialization cycles:

```
Blog → GetBlogsQueryResult
         └── Category → CategoryDto  ✅ stops here, no Blogs list inside
```

### Audit Interceptor
`AuditDbContextInterceptor` automatically sets `CreatedAt` and `UpdatedAt` on every `SaveChanges` call — handlers never set these manually.

### CQRS with MediatR
Every operation is a `Command` (mutates state) or `Query` (reads state). Commands return minimal result records (`CreateBlogResult`, `CreateCommentResult`); queries return full result DTOs (`GetBlogsQueryResult`, `CommentResult`).

### Validation Pipeline
FluentValidation validators run as a MediatR `ValidationBehavior` before any handler executes:

```
mediator.Send(command)
    → ValidationBehavior  ← short-circuits with errors if invalid
        → CommandHandler  ← only runs if validation passes
```

### Global Exception Handling
`CustomExceptionHandlingMiddleware` catches all unhandled exceptions and returns a structured `BaseResult` error response — no handler needs a try/catch for unexpected errors.

### BaseResult Envelope
All responses use a consistent envelope shape:

```json
{
  "data": { "id": "...", "title": "..." },
  "errors": []
}
```

```json
{
  "data": null,
  "errors": [{ "propertyName": "Email", "errorMessage": "Email is already in use." }]
}
```

---

## Getting Started

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- PostgreSQL server (local or remote)

### Clone & Restore

```bash
git clone https://github.com/RoaaAlsham/ZenBlog.git
cd ZenBlog
dotnet restore
```

Update the connection string in `appsettings.json` (see [Configuration](#configuration)), then:

```bash
dotnet ef database update \
  --project Infrastructure/ZenBlog.Persistence \
  --startup-project Presentation/ZenBlog.API

dotnet run --project Presentation/ZenBlog.API
```

The API will be available at `https://localhost:7117`.

---

## Configuration

`Presentation/ZenBlog.API/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=ZenBlogDb;Username=postgres;Password=yourpassword"
  },
  "JwtSettings": {
    "Issuer": "ZenBlogAPI",
    "Audience": "ZenBlogClient",
    "ExpiryMinutes": 60
  },
  "AdminSeed": {
    "Enabled": false,
    "Username": "admin",
    "FirstName": "Site",
    "LastName": "Admin"
  }
}
```

`JwtSettings:Secret` is **not** set here — like the connection string, it's kept out of source control via User Secrets:

```bash
dotnet user-secrets set "JwtSettings:Secret" "<a random string, at least 32 characters>" --project Presentation/ZenBlog.API
```

Without this, the app throws `ArgumentNullException` at startup when building the signing key.

### Admin seed (bootstrap Admin account)

Registration never creates Admins. On startup the API can seed the `Admin` role and one bootstrap user from the `AdminSeed` section.

`appsettings.json` leaves seeding **disabled**. Development enables it with `Enabled` and profile fields (`Username`, `FirstName`, `LastName`) in `appsettings.Development.json`. **`AdminSeed:Email` and `AdminSeed:Password` are secrets** — never commit them; set via User Secrets locally and environment variables in production:

```bash
dotnet user-secrets set "AdminSeed:Enabled" "true" --project Presentation/ZenBlog.API
dotnet user-secrets set "AdminSeed:Email" "admin@example.com" --project Presentation/ZenBlog.API
dotnet user-secrets set "AdminSeed:Username" "admin" --project Presentation/ZenBlog.API
dotnet user-secrets set "AdminSeed:Password" "AdminPassword123!" --project Presentation/ZenBlog.API
```

In production use environment variables (`AdminSeed__Enabled`, `AdminSeed__Email`, `AdminSeed__Password`, …).

Behavior:

- Skipped when `AdminSeed:Enabled` is `false`, or when the environment is `Testing`.
- If enabled but `Email` / `Password` are missing, startup fails fast with a clear error.
- Idempotent: existing bootstrap users keep their password (restart does **not** reset it).
- Roles are baked into the JWT at login — after a manual role change, log out and log in again.

The password must satisfy Identity rules (8+ chars, uppercase, digit, special character), same as public registration.

---

## Running Migrations

```bash
# Add a new migration
dotnet ef migrations add <MigrationName> \
  --project Infrastructure/ZenBlog.Persistence \
  --startup-project Presentation/ZenBlog.API

# Apply all pending migrations
dotnet ef database update \
  --project Infrastructure/ZenBlog.Persistence \
  --startup-project Presentation/ZenBlog.API

# Revert last migration
dotnet ef migrations remove \
  --project Infrastructure/ZenBlog.Persistence \
  --startup-project Presentation/ZenBlog.API
```