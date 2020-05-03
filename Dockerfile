# Build the NetDaemon with build container
#mcr.microsoft.com/dotnet/core/sdk:3.1.200
#ludeeus/container:dotnet-base
FROM mcr.microsoft.com/dotnet/core/sdk:3.1.200

# Copy the source to docker container
COPY ./src /usr/src

# COPY Docker/rootfs/etc /etc
COPY ./Docker/rootfs/etc/services.d/NetDaemon/run /rundaemon

# Set default values of NetDaemon env
ENV \
    DOTNET_NOLOGO=true \
    DOTNET_CLI_TELEMETRY_OPTOUT=true \
    HASS_RUN_PROJECT_FOLDER=/usr/src/Service \
    HASS_HOST=localhost \
    HASS_PORT=8123 \
    HASS_TOKEN=NOT_SET \
    HASS_DAEMONAPPFOLDER=/data


ENTRYPOINT ["bash", "/rundaemon"]
