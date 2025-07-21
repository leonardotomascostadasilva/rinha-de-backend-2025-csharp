# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0-preview AS build
WORKDIR /app

# Copia os arquivos csproj e restaura as dependências
COPY rinha-de-backend-2025-csharp/rinha-de-backend-2025-csharp.csproj ./rinha-de-backend-2025-csharp/
RUN dotnet restore ./rinha-de-backend-2025-csharp/rinha-de-backend-2025-csharp.csproj

# Copia todo o código
COPY . .

# Build e publish em modo Release
RUN dotnet publish ./rinha-de-backend-2025-csharp/rinha-de-backend-2025-csharp.csproj -c Release -o /app/publish /p:UseAppHost=false

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/aspnet:9.0-preview AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# Define a porta que a aplicação vai expor
EXPOSE 80

# Comando para rodar a aplicação
ENTRYPOINT ["dotnet", "rinha-de-backend-2025-csharp.dll"]