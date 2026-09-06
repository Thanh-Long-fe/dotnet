# ─────────────────────────── build ───────────────────────────
FROM node:20-alpine AS build
WORKDIR /app

# Cài dependency trước, tách khỏi source để tận dụng cache layer.
COPY package*.json ./
RUN npm ci

COPY . .

# API base nhúng lúc build (Vite thay import.meta.env khi build, không phải lúc chạy).
# Mặc định RỖNG = FE gọi same-origin (/api/...). Hợp với kế hoạch: nginx trên VPS
# đứng trước, proxy /api về backend, phục vụ FE ở cùng origin -> không cần CORS.
# Muốn FE gọi thẳng API ở origin khác thì:
#   docker build --build-arg VITE_API_BASE_URL=http://165.99.14.219:8080 ...
# (và nhớ thêm origin của FE vào Cors:AllowedOrigins ở backend).
ARG VITE_API_BASE_URL=""
ENV VITE_API_BASE_URL=$VITE_API_BASE_URL
RUN npm run build

# ─────────────────────────── serve ───────────────────────────
FROM nginx:alpine

# Cấu hình proxy + SPA fallback.
COPY nginx.conf /etc/nginx/conf.d/default.conf

# Static đã build.
COPY --from=build /app/dist /usr/share/nginx/html

EXPOSE 80
