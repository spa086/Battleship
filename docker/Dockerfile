# Set the base image as the .NET runtime for Ubuntu 22.04
FROM mcr.microsoft.com/dotnet/runtime:7.0-focal

# Set the working directory
WORKDIR /app

# Copy the published application files from the build stage
COPY ./publish /app

# Expose the port the app runs on
EXPOSE 80

# Start the application
ENTRYPOINT ["dotnet", "BattleShipLibrary.dll"]