FROM mcr.microsoft.com/dotnet/runtime:7.0-alpine AS base
RUN apk add --no-cache --update git openssh-keygen openssh-client python3 py3-pip
RUN /bin/sh -c 'wget https://github.com/rust-lang/mdBook/releases/download/v0.4.30/mdbook-v0.4.30-x86_64-unknown-linux-musl.tar.gz && \
    tar xzf mdbook-v0.4.30-x86_64-unknown-linux-musl.tar.gz && \
    chmod +x mdbook && \
    mv mdbook /usr/bin/'
EXPOSE 80
EXPOSE 8080
VOLUME ["/root/.ssh"]
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["mdBookGitAutoBuild.csproj", "mdBookGitAutoBuild/"]
RUN dotnet restore "mdBookGitAutoBuild/mdBookGitAutoBuild.csproj"
COPY . mdBookGitAutoBuild
WORKDIR "/src/mdBookGitAutoBuild"
RUN dotnet build "mdBookGitAutoBuild.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "mdBookGitAutoBuild.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "mdBookGitAutoBuild.dll"]
