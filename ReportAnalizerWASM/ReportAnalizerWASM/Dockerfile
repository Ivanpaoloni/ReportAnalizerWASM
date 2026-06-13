# Etapa 1: Compilación
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Al estar en la misma carpeta, copiamos directamente el archivo
COPY ["ReportAnalizerWASM.Client.csproj", "./"]
RUN dotnet restore "./ReportAnalizerWASM.Client.csproj"

# Copiamos el resto de los archivos de la carpeta .client
COPY . .
RUN dotnet publish "ReportAnalizerWASM.Client.csproj" -c Release -o /app/publish

# Etapa 2: Servidor de producción (Nginx)
FROM nginx:alpine
WORKDIR /usr/share/nginx/html

# Copiar los archivos estáticos que generó el publish
COPY --from=build /app/publish/wwwroot .

# Copiar la configuración de Nginx que también estará en esta carpeta
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]