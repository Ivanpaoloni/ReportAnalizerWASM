# Etapa 1: Compilación
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copiamos el .csproj que está en la misma carpeta que este Dockerfile
COPY ["ReportAnalizerWASM.Client.csproj", "./"]
RUN dotnet restore "./ReportAnalizerWASM.Client.csproj"

# Copiamos el resto de los archivos del cliente
COPY . .
RUN dotnet publish "ReportAnalizerWASM.Client.csproj" -c Release -o /app/publish

# Etapa 2: Servidor de producción (Nginx)
FROM nginx:alpine
WORKDIR /usr/share/nginx/html

# Copiar los archivos estáticos compilados
COPY --from=build /app/publish/wwwroot .

# Copiar la configuración de Nginx (que también debe estar en esta carpeta)
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]