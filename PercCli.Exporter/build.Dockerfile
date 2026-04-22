FROM mcr.microsoft.com/dotnet/sdk:10.0-alpine

RUN apk update && apk add \
    bash \
    clang \
    build-base \
    zlib-dev \
    icu-dev \
    linux-headers