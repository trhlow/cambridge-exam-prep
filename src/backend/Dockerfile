# API image, built by docker/docker-compose.yml (context: src/backend).
# global.json is left out on purpose: it pins the local SDK and the test runner, neither is needed to publish.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/ExamPrep.Domain/ExamPrep.Domain.csproj src/ExamPrep.Domain/
COPY src/ExamPrep.Infrastructure/ExamPrep.Infrastructure.csproj src/ExamPrep.Infrastructure/
COPY src/ExamPrep.Api/ExamPrep.Api.csproj src/ExamPrep.Api/
RUN dotnet restore src/ExamPrep.Api/ExamPrep.Api.csproj
COPY src/ src/
RUN dotnet publish src/ExamPrep.Api/ExamPrep.Api.csproj -c Release --no-restore -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
# Uploaded pictures/audio and the Data Protection key ring; mounted from the host so they survive a rebuild.
VOLUME /app/App_Data
EXPOSE 8080
ENTRYPOINT ["dotnet", "ExamPrep.Api.dll"]
