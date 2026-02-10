# Redis API – Arquitectura y Setup Local

API REST con arquitectura:
- Cache distribuida (Redis)
- Rate limiting en gateway (NGINX)
- Base de datos real (SQL Server)
- Todo reproducible en local con Docker

---

## Objetivo

Construir una API que:
- Sea **stateless**
- Use **Redis** como cache distribuida
- Use **SQL Server** como fuente de verdad
- Aplique **rate limiting antes de llegar a la API**
- Se pueda levantar completa en local

---

## Arquitectura

```
Cliente
  |
  v
API Gateway (NGINX)
  - Rate limiting
  |
  v
API (.NET)
  - Cache-aside
  |
  +--> Redis
  |
  +--> SQL Server
```

---

## Componentes

### API Gateway (NGINX)
- Reverse proxy
- Rate limiting por IP
- Devuelve **HTTP 429** cuando se excede el límite
- Expone la API en `http://localhost:8080`

### API (.NET)
- Stateless
- Usa Redis con patrón cache-aside
- Corre en `https://localhost:7016`

### Redis
- Cache distribuida
- TTL definido

### SQL Server
- Base de datos 
- Corre en Docker
- Fuente datos del sistema

---

## Creación del Proyecto (.NET)

Crear la API:

```bash
dotnet new webapi -n RedisAPI
cd RedisAPI
```


---

## Librerías Utilizadas

Entity Framework Core + SQL Server:

```bash
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Tools
```

Redis cache distribuida:

```bash
dotnet add package Microsoft.Extensions.Caching.StackExchangeRedis
```

---

## Base de Datos (SQL Server)

### Connection Strings (Development)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=RedisApiDb;User Id=sa;Password=Your_password123;TrustServerCertificate=True",
    "Redis": "localhost:6379"
  }
}
```

### Migraciones

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

### Datos de Prueba

```sql
INSERT INTO Products (Name, Price)
VALUES ('Keyboard', 100), ('Mouse', 50);
```

---

## Cache (Redis)

### Patrón: Cache-Aside

Flujo:
1. Request entra a la API
2. Se consulta Redis
3. Si existe → se responde
4. Si no existe → se consulta SQL
5. Se guarda en Redis con TTL
6. Se responde al cliente

Redis vive **fuera del proceso**, manteniendo la API stateless.

---

## Docker (Infraestructura Local)

### Servicios
- `sqlserver-local`
- `redis-local`
- `api-gateway`

### Levantar todo

```bash
docker compose up -d
```

### Ver contenedores

```bash
docker ps
docker ps -a
```

### Ver configuración efectiva

```bash
docker compose config
```

---

## Redis (Comandos Útiles)

Probar conexión:

```bash
docker exec -it redis-local redis-cli ping
```

Respuesta esperada:

```
PONG
```

---

## Rate Limiting (NGINX)

El rate limiting se aplica **en el gateway**

### Conceptos Clave
- `rate` → velocidad promedio permitida
- `burst` → requests inmediatas permitidas
- `nodelay` → rechaza en lugar de encolar
- `zone` → memoria compartida para conteo

### Configuración NGINX (`nginx.conf`)

### Reiniciar Gateway

```bash
docker compose restart gateway
```

## Pruebas con curl

Request simple:

```bash
curl -i http://localhost:8080/api/v1/products/1
```

Loop para forzar rate limit (Windows CMD):

```cmd
for /L %i in (1,1,6) do curl -i http://localhost:8080/api/v1/products/1
```

Resultado esperado:
- Requests iniciales → `200 OK`
- Requests excedidos → `429 Too Many Requests`

Confirmar paso por gateway:
```
Server: nginx
```

---

## Pruebas con Postman

- Crear una **Collection**
- Usar **Collection Runner**
- Iterations = número de requests
- Ver transición `200 → 429`
- Confirmar header `Server: nginx`

---

## Decisiones de Diseño

- El rate limiting vive en el gateway
- La API no conoce límites
- Redis y SQL no llaman a la API
- Cache reduce carga, no reemplaza la DB
- Todo el estado vive fuera del proceso

---

## Estado del Proyecto

- Infraestructura funcional en local
- Cache distribuida operativa
- Rate limiting aplicado correctamente
- API stateless
- Arquitectura consistente
