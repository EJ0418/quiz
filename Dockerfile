FROM mcr.microsoft.com/dotnet/sdk:8.0-alpine AS build
WORKDIR /src
COPY quiz/*.csproj ./quiz/
RUN dotnet restore "quiz/quiz.csproj"
COPY . .
RUN dotnet publish "quiz/quiz.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:8.0-alpine AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "quiz.dll"]