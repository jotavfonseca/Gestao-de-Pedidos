# Stage 1: build
FROM node:18-alpine AS build
WORKDIR /app

# Evita reinstalar dependências se package.json não mudou
COPY package*.json ./
RUN npm ci --silent

# Copia todo o código-fonte e gera o build
COPY . .
RUN npm run build

# Stage 2: serve com nginx
FROM nginx:stable-alpine AS runtime
# opcional: instalar curl (para healthcheck se precisar)
RUN apk add --no-cache curl

# Copia build do Vite (geralmente em /dist) para nginx
COPY --from=build /app/dist /usr/share/nginx/html

# Configuração customizada do nginx
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
