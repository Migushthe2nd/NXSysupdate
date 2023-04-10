FROM node:16-alpine AS build-env

# Install yui
WORKDIR /tools
# RUN apk add --update --no-cache aspnetcore7-runtime
# RUN wget yui

# build app
WORKDIR /usr/src/app

# Environment variables for production
ENV NODE_ENV=production

COPY package*.json ./
COPY yarn*.lock ./
RUN yarn global add typescript@4.2.3
RUN yarn install --network-timeout 1000000

COPY . .

RUN yarn run build

# Prune the dev dependencies
RUN yarn install --production --network-timeout 1000000

CMD yarn run start