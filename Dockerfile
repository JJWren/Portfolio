FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Portfolio.slnx global.json ./
COPY src/Portfolio.Web/Portfolio.Web.csproj src/Portfolio.Web/
COPY tests/Portfolio.Tests/Portfolio.Tests.csproj tests/Portfolio.Tests/
RUN dotnet restore

COPY . .
RUN dotnet publish src/Portfolio.Web/Portfolio.Web.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
# libgssapi: Npgsql probes it for Kerberos auth; curl: container healthcheck.
RUN apt-get update \
    && apt-get install -y --no-install-recommends libgssapi-krb5-2 curl \
    && rm -rf /var/lib/apt/lists/*
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
ENV Uploads__Path=/app/uploads
EXPOSE 8080
VOLUME /app/uploads

ENTRYPOINT ["dotnet", "Portfolio.Web.dll"]
