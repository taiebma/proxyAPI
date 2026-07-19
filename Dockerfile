FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build

WORKDIR /source

ARG TARGETPLATFORM
ARG TARGETARCH
ARG BUILDPLATFORM

# copy csproj and restore as distinct layers
COPY . .
RUN if [ "$TARGETPLATFORM" = "linux/amd64" ]; then \
    RID=linux-musl-x64 ; \
    elif [ "$TARGETPLATFORM" = "linux/arm64" ]; then \
    RID=linux-musl-arm64 ; \
    elif [ "$TARGETPLATFORM" = "linux/arm/v7" ]; then \
    RID=linux-musl-arm ; \
    fi \
    && dotnet restore -r $RID

# copy and publish app and libraries
RUN if [ "$TARGETPLATFORM" = "linux/amd64" ]; then \
    RID=linux-musl-x64 ; \
    elif [ "$TARGETPLATFORM" = "linux/arm64" ]; then \
    RID=linux-musl-arm64 ; \
    elif [ "$TARGETPLATFORM" = "linux/arm/v7" ]; then \
    RID=linux-musl-arm ; \
    fi \
    && dotnet publish ProxyAPI.sln -f net9.0 -c Release -o /app -r $RID --self-contained false

# final stage/image
FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS runtime
WORKDIR /app
COPY --from=build /app .
RUN mkdir certs param

EXPOSE 8080/tcp
EXPOSE 8081/tcp
ENTRYPOINT ["dotnet", "/app/ProxyAPI.Presentation.dll"]
