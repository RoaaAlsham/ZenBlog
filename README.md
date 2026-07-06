# ZenBlog Server

A RESTful blog API built with **ASP.NET Core (.NET 10)** following **Clean Architecture** principles. Features CQRS via MediatR, ASP.NET Core Identity, Entity Framework Core with PostgreSQL, a generic repository pattern, audit interceptors, and a FluentValidation pipeline.

---
note: move to the `auth_2` design instead of extending this branch, this branch is now functionally correct but has some design issues that `auth_2` fixes. See [Authentication (JWT)](#authentication-jwt) for details.
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
│            ZenBlog.Infrastructure          │  ← (reserved for external services)
├─────────────────────────────────────────────┤
│            Core                             │
│            ZenBlog.Application             │  ← CQRS, Handlers, Validators, DTOs
│            ZenBlog.Domain                  │  ← Entities, no dependencies
└─────────────────────────────────────────────┘
```

- **Domain** has zero dependencies — pure C# entities.
- **Application** depends only on Domain — no EF Core, no HTTP.
- **Persistence** implements Application contracts — EF Core and PostgreSQL live here only.
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
│   │   │   └── Persistence/
│   │   │       ├── IRepository.cs
│   │   │       └── IUnitOfWork.cs
│   │   ├── DTOs/
│   │   │   ├── BlogDto.cs
│   │   │   ├── CategoryDto.cs
│   │   │   └── UserDto.cs
│   │   ├── Extensions/
│   │   │   └── ServiceRegistration.cs
│   │   └── Features/
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
    public string? BlogImageUrl { get; set; }
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

### Users

| Method | Route | Description | Auth required |
|---|---|---|---|
| POST | `/users/register` | Register a new user | No |
| POST | `/users/login` | Log in, receive a JWT | No |
| GET | `/users` | Get all users | **Yes** |

### Blogs

| Method | Route | Description | Auth required |
|---|---|---|---|
| GET | `/blogs` | Get all blogs with category | Yes |
| GET | `/blogs/{id}` | Get blog by ID | Yes |
| GET | `/blogs/category/{categoryId}` | Get blogs filtered by category | Yes |
| POST | `/blogs` | Create a blog | Yes |
| PUT | `/blogs/{id}` | Update a blog | Yes |
| DELETE | `/blogs/{id}` | Remove a blog | Yes |

### Categories

| Method | Route | Description | Auth required |
|---|---|---|---|
| GET | `/categories` | Get all categories with blogs | Yes |
| GET | `/categories/{id}` | Get category by ID | Yes |
| POST | `/categories` | Create a category | Yes |
| PUT | `/categories/{id}` | Update a category | Yes |
| DELETE | `/categories/{id}` | Remove a category | Yes |

### Comments

| Method | Route | Description | Auth required |
|---|---|---|---|
| GET | `/comments/blog/{blogId}` | Get top-level comments for a blog | Yes |
| GET | `/comments/{id}` | Get comment with its replies | Yes |
| POST | `/comments` | Create a comment or reply | Yes |
| PUT | `/comments/{id}` | Update comment body | Yes |
| DELETE | `/comments/{id}` | Remove a comment | Yes |

> **Note:** every route above requires a bearer token by default (`RequireAuthorization()` is applied to the whole `/api` group in `Program.cs`) except the two explicitly marked "No". See [Authentication (JWT)](#authentication-jwt) for why this is soon to change for the read-only (`GET`) routes.

---

## Authentication (JWT)

This branch (`auth_1`) adds JWT-based authentication on top of ASP.NET Core Identity. This section documents how it's wired up, how to configure it locally, and an important note on where this design should go next.

### How it works

1. `POST /users/register` creates a user via `UserManager<AppUser>`.
2. `POST /users/login` (`GetLoginQueryHandler`) verifies the email/password via `UserManager.CheckPasswordAsync`, then asks `IJwtService` to mint a token.
3. `JwtService` (`Infrastructure/ZenBlog.Persistence/Concrete/JwtService.cs`) builds a `JwtSecurityToken` signed with HMAC-SHA256, using a `SymmetricSecurityKey` derived from a configured secret. It adds identity claims (`NameIdentifier`, `Name`, `Email`, `GivenName`) plus one `ClaimTypes.Role` claim per role the user is in (via `UserManager.GetRolesAsync`), so `[Authorize(Roles = "...")]` can be used downstream.
4. On every subsequent request, the ASP.NET Core JWT Bearer middleware (registered via `AddAuthentication().AddJwtBearer(...)` in `Persistence`'s `ServiceRegistration`) validates the token's signature, issuer, audience, and expiry, and populates `HttpContext.User` with its claims.
5. `Program.cs` applies `RequireAuthorization()` to the whole `/api` route group, with `/users/register` and `/users/login` explicitly opted out via `.AllowAnonymous()` — see the table in [Features & Endpoints](#features--endpoints) for exactly which routes that currently covers.

### Configuring the signing key: appsettings vs. User Secrets

The token's `Issuer`, `Audience`, and `SecretKey` are bound from a `JwtTokenOptions` section in configuration (`configuration.GetSection("JwtTokenOptions")`). ASP.NET Core merges configuration from several sources, layered in this order (each later source overrides the same key from an earlier one): `appsettings.json` → `appsettings.{Environment}.json` → **User Secrets** (Development only) → environment variables → command-line args.

This matters because `appsettings*.json` files are committed to git — anything in them is public the moment it's pushed, permanently (even if later removed, it stays in git history). The `SecretKey` must never be a real value in a committed file. Two different mechanisms handle "real value, not in git," for two different situations:

| | `appsettings.json` | User Secrets |
|---|---|---|
| Stored where | In the repo, tracked by git | A JSON file outside the repo, in your OS user profile |
| Committed to GitHub? | Always | Never |
| Use for | Non-secret config, or placeholders | Real secrets, local development only |
| Available in production? | Yes | No — only loads when `ASPNETCORE_ENVIRONMENT=Development` |
| Set via | Editing the file directly | `dotnet user-secrets set "Key" "value"` |

**Local development — use User Secrets:**

```bash
cd Presentation/ZenBlog.API

# Only needed if the project doesn't already have a UserSecretsId in its .csproj
# (this one already does, so this step is a no-op here):
dotnet user-secrets init

dotnet user-secrets set "JwtTokenOptions:SecretKey" "a-long-random-string-at-least-32-bytes"
dotnet user-secrets set "JwtTokenOptions:Issuer" "ZenBlog.API"
dotnet user-secrets set "JwtTokenOptions:Audience" "ZenBlog.Client"

# Confirm what's stored, and where the file lives:
dotnet user-secrets list
```

The `:` addresses the nested `SecretKey` property inside the `JwtTokenOptions` section, matching `configuration.GetSection("JwtTokenOptions")` in code. `appsettings.json`'s own `JwtTokenOptions.SecretKey` is intentionally left as `""` — a non-functional placeholder — precisely so a real key can only come from User Secrets (locally) or the next mechanism (elsewhere).

**Staging/production — use environment variables**, since User Secrets never loads outside Development:

```bash
export JwtTokenOptions__SecretKey="the-real-production-secret"
```

(`__`, double underscore, instead of `:`, since most shells don't allow `:` in variable names.) This is exactly why `appsettings.json`'s placeholder must stay empty rather than holding a real key — a real deployment is expected to override it this way, via whatever secret-injection mechanism the host provides (Docker `-e`, Kubernetes `Secret` env vars, Azure App Service application settings, etc.).

### ⚠️ Recommended next step: move to the `auth_2` design instead of extending this branch

While this branch (`auth_1`) is now functionally correct after the fixes above (token validation actually runs, roles are included, the login endpoint doesn't leak which emails are registered), a separate implementation — referred to here as **`auth_2`** — was built independently on top of `main` and is the version recommended to move forward with. Reasons:

1. **Fixes a real authorization bug that this branch does not.** `CreateBlogCommandHandler` and `CreateCommentCommandHandler` on this branch still trust a client-supplied `UserId` in the request body. `auth_2` takes the id from the validated token (via a new `ICurrentUserService`) instead, so an authenticated caller can no longer create a post or comment and attribute it to a different user's id.
2. **Cleaner architecture.** `main` already has an empty `ZenBlog.Infrastructure` project reserved for cross-cutting concerns like auth. This branch instead added all the JWT/Identity wiring into `ZenBlog.Persistence`, mixing database-persistence concerns with authentication concerns. `auth_2` puts `IJwtTokenGenerator`, `ICurrentUserService`, and the authentication scheme registration in `ZenBlog.Infrastructure`, where they belong.
3. **Fails fast instead of silently.** `auth_2` doesn't commit a `Secret` value anywhere, including as a placeholder, and throws immediately at startup if `JwtSettings` isn't configured — instead of this branch's approach of a silent empty-string placeholder that only fails when a token is actually requested.
4. **More standard, more future-proof claims.** `auth_2` includes `jti` (a unique token id, useful later for revocation/blacklisting) and standard `JwtRegisteredClaimNames` (`sub`, `email`) alongside the `ClaimTypes` this branch uses alone.
5. **Login input is validated.** `auth_2` adds a `LoginValidator` (FluentValidation); this branch's login accepts any shape of request.
6. **A more deliberate endpoint-protection model.** `auth_2` applies `.RequireAuthorization()` per mutating endpoint (create/update/delete) rather than to the whole `/api` group, leaving `GET` (read) routes public by default — arguably the correct behavior for a public blog, where reading posts shouldn't require an account. This branch currently requires a login even to read a blog post, which likely isn't the intended product behavior.




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

### Identity Users via UserManager
`AppUser` inherits from `IdentityUser<string>` — it cannot use `IRepository<AppUser>` due to the `where TEntity : BaseEntity` constraint. All user operations go through `UserManager<AppUser>`, which is Identity's own repository abstraction.

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
  }
}
```

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