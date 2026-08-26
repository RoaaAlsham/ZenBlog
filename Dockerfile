# Build stage
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ZenBlogServer.slnx ./
COPY Presentation/ZenBlog.API/ZenBlog.API.csproj Presentation/ZenBlog.API/
COPY Core/ZenBlog.Application/ZenBlog.Application.csproj Core/ZenBlog.Application/
COPY Core/ZenBlog.Domain/ZenBlog.Domain.csproj Core/ZenBlog.Domain/
COPY Infrastructure/ZenBlog.Persistence/ZenBlog.Persistence.csproj Infrastructure/ZenBlog.Persistence/
COPY Infrastructure/ZenBlog.Infrastructure/ZenBlog.Infrastructure.csproj Infrastructure/ZenBlog.Infrastructure/

RUN dotnet restore Presentation/ZenBlog.API/ZenBlog.API.csproj

COPY . .
RUN dotnet publish Presentation/ZenBlog.API/ZenBlog.API.csproj -c Release -o /app/publish --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

ENV PORT=8080
ENV ASPNETCORE_URLS=http://+:8080

# appsettings*.json is baked into this image and cannot change at runtime, so the
# default reload-on-change file watchers earn nothing. Each one costs an inotify
# instance from a per-uid limit shared with everything else on the host, and when
# that limit is exhausted CreateBuilder throws before the host exists.
ENV DOTNET_hostBuilder__reloadConfigOnChange=false

EXPOSE 8080

COPY --from=build /app/publish .

# Bind to PORT (Render injects this; defaults to 8080 locally).
ENTRYPOINT ["sh", "-c", "ASPNETCORE_URLS=http://+:${PORT:-8080} exec dotnet ZenBlog.API.dll"]
