# Lily Market Backend

В исходном сообщении была рассинхронизация: заголовок задания говорит про аукционы, а ссылка вела на `frogchess`. Реализация в этом репозитории сделана для аукционного варианта, потому что в исходниках уже лежит соответствующий фронтенд: `ServerProg_Ind/src/greenswamp/indiv/auction`.

## Архитектурное решение

Выбран гибридный вариант: команды идут через REST, а события через SignalR. Создание лота, регистрация, логин, редактирование, отмена и подача ставки лучше выражаются обычными HTTP-эндпоинтами: у них понятная семантика, они удобнее для тестирования и проще для повторного вызова с мобильного клиента после потери сети.

SignalR используется там, где нужен realtime: обновление карточки аукциона, подтверждение новой ставки, уведомление о перебитой ставке, предупреждение о скором завершении и финальное закрытие аукциона. Чистый SignalR здесь дал бы более сложную клиентскую интеграцию и хуже тестировался бы. Чистый REST с polling хуже подходит под мобильный сценарий живых торгов и создаёт лишний трафик и задержку.

## Что реализовано

- Регистрация и логин по университетской почте (`*.edu`) с JWT.
- Хранение данных в PostgreSQL через EF Core.
- Раздача статического фронтенда из `src/greenswamp`.
- Создание, просмотр, редактирование и отмена аукционов.
- Подача ставок с серверной валидацией.
- Немедленное закрытие по `buyNowPrice`.
- Фоновое завершение аукционов по серверному времени.
- SignalR-хаб для live-обновлений и пользовательских уведомлений.
- Защита от гонок на уровне одного аукциона через `SemaphoreSlim`.
- Тесты, запускаемые командой `dotnet test`.

## Структура

- `ServerProg_Ind/Program.cs` — состав приложения, DI, middleware, endpoints и hub.
- `ServerProg_Ind/Application` — прикладная логика, обработка ставок, фоновые задачи, маппинг DTO.
- `ServerProg_Ind/Domain` — сущности и правила аукциона.
- `ServerProg_Ind/Infrastructure` — EF Core, JWT, SignalR.
- `ServerProg_Ind.Tests` — unit-тесты обязательных сценариев.

## Основные эндпоинты

- `POST /api/auth/register`
- `POST /api/auth/login`
- `GET /api/auth/me`
- `GET /api/auctions`
- `GET /api/auctions/{id}`
- `POST /api/auctions`
- `PUT /api/auctions/{id}`
- `POST /api/auctions/{id}/cancel`
- `POST /api/auctions/{id}/bids`
- `POST /api/auctions/{id}/confirm-sale`
- `GET /api/notifications`
- `POST /api/notifications/{id}/read`
- `GET/WS /hubs/auctions`

## Что нужно сделать, чтобы запустить проект

Нужно:

- установить `.NET 8 SDK`
- поднять `PostgreSQL`
- создать базу данных `lily_market`
- при необходимости изменить строку подключения в [appsettings.json](/C:/Users/sassy/Documents/study/ServerProg/ServerProg_Ind/ServerProg_Ind/appsettings.json)

По умолчанию проект ожидает такой доступ к PostgreSQL:

```text
Host=localhost;Port=5432;Database=lily_market;Username=postgres;Password=postgres
```

### Вариант 1: через Docker

Если Docker установлен, самый быстрый способ такой:

```powershell
docker run --name lily-market-postgres `
  -e POSTGRES_PASSWORD=postgres `
  -e POSTGRES_USER=postgres `
  -e POSTGRES_DB=lily_market `
  -p 5432:5432 `
  -d postgres:16
```

После этого можно запускать приложение.

### Вариант 2: локально установленный PostgreSQL

Если PostgreSQL установлен как сервис:

1. Убедись, что сервер запущен на `localhost:5432`.
2. Создай БД:

```sql
CREATE DATABASE lily_market;
```

3. Если у тебя другие логин, пароль, порт или имя базы, поменяй `ConnectionStrings:DefaultConnection`.

Пример альтернативной строки подключения:

```text
Host=localhost;Port=5433;Database=lily_market;Username=myuser;Password=mypassword
```

Её можно задать:

- прямо в `ServerProg_Ind/appsettings.json`
- или через переменную окружения `ConnectionStrings__DefaultConnection`

## Команды запуска

Из корня решения:

```powershell
dotnet restore ServerProg_Ind.sln
dotnet run --project .\ServerProg_Ind\ServerProg_Ind.csproj
```

После запуска открой в браузере:

- `http://localhost:5000/indiv/auction/auctions.html`
- или тот же путь на адресе, который приложение напишет в консоль в строке `Now listening on:`

Корень `/` тоже работает и редиректит на страницу аукционов.

## Тесты

```powershell
dotnet test ServerProg_Ind.sln
```

Тестами покрыто:

- валидная и невалидная подача ставок;
- запрет ставки продавца на собственный лот;
- закрытие по `buy now`;
- выбор победителя при завершении;
- завершение без ставок;
- конкурентная подача двух ставок в один аукцион.

## Замечания по фронтенду

- `POST /api/auctions` принимает либо JSON, либо `multipart/form-data` с полем `data` и файлами `photos`, как ожидает шаблон `auctions-create.html`.
- Для переподключения мобильный клиент заново подключается к `/hubs/auctions`, вызывает `JoinAuction` и при необходимости `RequestSnapshot`.

## Использование ИИ

ИИ использовался как инструмент ускорения разработки и проверки структуры решения. Архитектурный выбор, модель данных, серверная логика и итоговая интеграция были адаптированы под это конкретное задание и кодовую базу.
