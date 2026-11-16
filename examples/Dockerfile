# Builds every example into one image: /app/<ProjectName>/<ProjectName>.dll
# Build context is the repository root (see compose.yaml) so examples reference the library projects.
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
# Behind a TLS-inspecting proxy? Drop its root CA (PEM, *.crt) into examples/certs/ (git-ignored).
RUN cp examples/certs/*.crt /usr/local/share/ca-certificates/ 2>/dev/null; update-ca-certificates
RUN for csproj in examples/*/*.csproj; do \
      name=$(basename "$csproj" .csproj); \
      dotnet publish "$csproj" -c Release -o "/app/$name" --nologo || exit 1; \
    done

FROM mcr.microsoft.com/dotnet/runtime:10.0
WORKDIR /app
COPY --from=build /app .
