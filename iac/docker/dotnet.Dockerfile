FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY src/dotnet ./src/dotnet
COPY Car.Agentic.sln ./
ARG PROJECT
RUN dotnet restore "src/dotnet/${PROJECT}/${PROJECT}.csproj"
RUN dotnet publish "src/dotnet/${PROJECT}/${PROJECT}.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ARG DLL
ENV DLL=${DLL}
ENTRYPOINT ["sh", "-c", "dotnet $DLL"]
