# API информационной системы «Страховая компания»

Проект ASP.NET Core Web API (.NET 8), разработанный для курсового проекта по МДК 05.02.
API предоставляет доступ к базе данных `user90_db` (MS SQL Server) и используется как
серверная часть для сайта, мобильного приложения или других информационных систем.

## Состав проекта

- `Controllers/AuthController.cs` — вход по логину и паролю, выдача JWT-токена;
- `Controllers/ClientsController.cs` — операции с клиентами;
- `Controllers/DogovorsController.cs` — операции с договорами страхования;
- `Controllers/CasesController.cs` — операции со страховыми случаями;
- `Models/` — DTO и модели запросов/ответов;
- `Services/JwtService.cs` — формирование JWT-токена;
- `Program.cs` — настройка Swagger, JWT-аутентификации и маршрутизации;
- `appsettings.json` — строка подключения к базе данных и параметры JWT.

## Требования

- .NET SDK 8.0;
- доступ к серверу MS SQL Server с базой `user90_db`;
- Visual Studio 2022 (необязательно) или командная строка.

## Сборка и запуск

Из папки с файлом `InsuranceApi.csproj` выполните:

```powershell
dotnet restore
dotnet build
dotnet run
```

По умолчанию проект использует профиль запуска `http`/`https` из папки `Properties`.
Адреса после запуска:

- Swagger UI: `https://localhost:7001/swagger` или `http://localhost:5000/swagger`;
- OpenAPI-спецификация: `/swagger/v1/swagger.json`;
- API: `/api/clients`, `/api/dogovors`, `/api/cases`, `/api/auth/login`.

Если порт занят, его можно задать явно:

```powershell
dotnet run --no-launch-profile --urls "http://localhost:5002"
```

## Аутентификация

Все методы, кроме `POST /api/auth/login`, требуют JWT-токен с ролью `Admin`.
Учебная учётная запись: логин `admin`, пароль `123`.

Пример получения токена (PowerShell):

```powershell
$body = @{ login = "admin"; password = "123" } | ConvertTo-Json
$response = Invoke-RestMethod -Method Post -Uri "http://localhost:5002/api/auth/login" `
    -ContentType "application/json" -Body $body
$token = $response.token
```

Пример защищённого запроса:

```powershell
Invoke-RestMethod -Method Get -Uri "http://localhost:5002/api/clients" `
    -Headers @{ Authorization = "Bearer $token" }
```

В Swagger UI токен вводится через кнопку **Authorize** в формате `Bearer <token>`
(в некоторых версиях Swagger достаточно вставить только сам токен).

## Основные конечные точки

| Метод | URL | Назначение |
|---|---|---|
| POST | `/api/auth/login` | Получение JWT-токена |
| GET | `/api/clients?search=` | Список клиентов и поиск |
| GET | `/api/clients/{id}` | Клиент по идентификатору |
| POST | `/api/clients` | Добавление клиента |
| PUT | `/api/clients/{id}` | Изменение клиента |
| DELETE | `/api/clients/{id}` | Удаление клиента |
| GET | `/api/dogovors?clientId=&dateFrom=&dateTo=` | Список договоров |
| GET | `/api/dogovors/{id}` | Договор по идентификатору |
| POST | `/api/dogovors` | Оформление договора |
| PUT | `/api/dogovors/{id}` | Изменение договора |
| DELETE | `/api/dogovors/{id}` | Удаление договора |
| GET | `/api/cases?contractId=` | Список страховых случаев |
| GET | `/api/cases/{id}` | Страховой случай по идентификатору |
| POST | `/api/cases` | Регистрация страхового случая |
| PUT | `/api/cases/{id}` | Изменение страхового случая |
| DELETE | `/api/cases/{id}` | Удаление страхового случая |

## Коды ответов

- `200 OK` — успешное чтение или изменение;
- `201 Created` — объект создан;
- `204 No Content` — объект удалён;
- `400 Bad Request` — некорректные входные данные;
- `401 Unauthorized` — токен отсутствует, просрочен или неверен;
- `403 Forbidden` — недостаточно прав;
- `404 Not Found` — запись не найдена;
- `409 Conflict` — нарушено бизнес-правило или ограничение базы данных;
- `500 Internal Server Error` — внутренняя ошибка сервера.

## Безопасность

- все SQL-запросы параметризованные (`SqlParameter` / `AddWithValue`);
- пароль и токен должны передаваться только по HTTPS;
- строка подключения и ключ JWT находятся в `appsettings.json`;
- для промышленной эксплуатации рекомендуется вынести секреты в переменные окружения,
  включить хеширование паролей и использовать доверенный сертификат сервера.


