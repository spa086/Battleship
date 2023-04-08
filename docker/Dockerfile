# Set the base image as the .NET runtime
FROM mcr.microsoft.com/dotnet/runtime:7.0.4-jammy

# Set the working directory
WORKDIR /app

# Copy the published application files from the build stage
COPY ./publish /app

# Expose the port the app runs on
EXPOSE 80

# Start the application
ENTRYPOINT ["dotnet", "BattleshipApi.dll"]