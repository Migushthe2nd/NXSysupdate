FROM node:16.20.0-alpine AS build-env

WORKDIR /

# Install .NET 3.1 for yui
RUN apk add --update --no-cache bash wget libintl libffi-dev openssl1.1-compat-dev dotnet7-sdk dotnet6-sdk
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=true
RUN wget https://dot.net/v1/dotnet-install.sh -O dotnet-install.sh
RUN chmod +x dotnet-install.sh
RUN ./dotnet-install.sh --channel 3.1 --install-dir /usr/share/dotnet3
RUN ln -s /usr/share/dotnet3/dotnet /usr/bin/dotnet3
RUN ln -s /usr/lib/libssl.so.47.0.6 /usr/lib/libssl.so.1.0.0

COPY ./tools ./tools

# build app
WORKDIR /usr/src/app

# Environment variables for production
ENV NODE_ENV=production

COPY package*.json ./
COPY yarn*.lock ./
RUN yarn global add typescript@5.0.4
RUN yarn install --network-timeout 1000000

COPY . .

RUN yarn run build

# Prune the dev dependencies
RUN yarn install --production --network-timeout 1000000

CMD yarn run start