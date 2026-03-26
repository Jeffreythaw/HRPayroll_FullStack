FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY HRPayroll.Core/HRPayroll.Core.csproj HRPayroll.Core/
COPY HRPayroll.Infrastructure/HRPayroll.Infrastructure.csproj HRPayroll.Infrastructure/
COPY HRPayroll.API/HRPayroll.API.csproj HRPayroll.API/
COPY HRPayroll.sln ./
RUN dotnet restore HRPayroll.API/HRPayroll.API.csproj

COPY . .
RUN dotnet publish HRPayroll.API/HRPayroll.API.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
ENV ASPNETCORE_URLS=http://0.0.0.0:10000
EXPOSE 10000
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "HRPayroll.API.dll"]
