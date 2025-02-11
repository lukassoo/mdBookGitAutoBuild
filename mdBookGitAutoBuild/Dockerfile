FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine

COPY bin/Release/net9.0/publish/ app/
WORKDIR /app

RUN apk add --no-cache --update git openssh-keygen openssh-client
RUN /bin/sh -c 'wget https://github.com/rust-lang/mdBook/releases/download/v0.4.44/mdbook-v0.4.44-x86_64-unknown-linux-musl.tar.gz && \
    tar xzf mdbook-v0.4.44-x86_64-unknown-linux-musl.tar.gz && \
    chmod +x mdbook && \
    mv mdbook /usr/bin/'
EXPOSE 80
EXPOSE 8080
ENV DOTNET_EnableDiagnostics=0

VOLUME ["/root/.ssh"]

ENTRYPOINT ["dotnet", "mdBookGitAutoBuild.dll"]