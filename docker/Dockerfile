# Set the base image as the .NET runtime
FROM mcr.microsoft.com/dotnet/runtime:7.0.4-jammy

# Set the working directory
WORKDIR /app

# Copy the published application files from the build stage
COPY ./publish /app

# Expose the port the app runs on
EXPOSE 80

# Make the BattleshipApi binary executable
RUN chmod +x /app/BattleshipApi

# Start the application
ENTRYPOINT ["/app/BattleshipApi"]