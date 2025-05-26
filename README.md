# Система Анализа Текстов на Плагиат "AntiPlagiarism"

Проект представляет собой микросервисную систему для анализа студенческих отчетов на плагиат и их статистической обработки.

## Архитектура

Система состоит из следующих микросервисов:

1.  **API Gateway (`AntiPlagiarism.ApiGateway`)**:
    *   Единая точка входа для всех клиентских запросов.
    *   Маршрутизирует запросы к соответствующим внутренним сервисам.
    *   Реализован с использованием Ocelot.
    *   Порт: `http://localhost:5000`

2.  **File Storing Service (`AntiPlagiarism.FileStoringService`)**:
    *   Отвечает за загрузку, хранение и предоставление текстовых файлов (`.txt`).
    *   Файлы хранятся в локальной папке `uploads_fss` (создается автоматически).
    *   Порт: `http://localhost:5101`
    *   Swagger UI: `http://localhost:5101/swagger`

3.  **File Analysis Service (`AntiPlagiarism.FileAnalysisService`)**:
    *   Выполняет анализ текста: подсчет статистики (абзацы, слова, символы), вычисление SHA256 хеша.
    *   Определяет 100% плагиат путем сравнения хешей файлов.
    *   Генерирует "облако слов" с помощью внешнего API (quickchart.io).
    *   Хранит результаты анализа (in-memory).
    *   Взаимодействует с `FileStoringService` для получения файлов.
    *   Порт: `http://localhost:5102`
    *   Swagger UI: `http://localhost:5102/swagger`

## Стек технологий

*   C#
*   .NET 7
*   ASP.NET Core
*   Ocelot (API Gateway)
*   Swagger/OpenAPI (Документация API)
*   xUnit, Moq (Тестирование)

## Запуск проекта

### Требования

*   .NET 7 SDK

### Инструкции

1.  **Клонировать репозиторий** (если это был бы реальный репозиторий):
    ```bash
    git clone <repository-url>
    cd AntiPlagiarismSystem
    ```
2.  **Открыть решение** `AntiPlagiarismSystem.sln` в Visual Studio или использовать .NET CLI.
3.  **Настройка запускаемых проектов (для Visual Studio):**
    *   Правой кнопкой мыши по решению в "Обозревателе решений" -> "Настроить запускаемые проекты..."
    *   Выбрать "Несколько запускаемых проектов".
    *   Установить "Действие" -> "Запустить" для:
        *   `AntiPlagiarism.ApiGateway`
        *   `AntiPlagiarism.FileStoringService`
        *   `AntiPlagiarism.FileAnalysisService`
4.  **Запуск через .NET CLI (из корневой папки `AntiPlagiarismSystem`):**
    Откройте три терминала и запустите каждый сервис отдельно:
    ```bash
    # Терминал 1
    dotnet run --project AntiPlagiarism.ApiGateway/AntiPlagiarism.ApiGateway.csproj
    # Терминал 2
    dotnet run --project AntiPlagiarism.FileStoringService/AntiPlagiarism.FileStoringService.csproj
    # Терминал 3
    dotnet run --project AntiPlagiarism.FileAnalysisService/AntiPlagiarism.FileAnalysisService.csproj
    ```
5.  После запуска сервисы будут доступны по указанным выше портам.

## Тестирование API

Используйте Postman или Swagger UI каждого сервиса (доступ через API Gateway предпочтительнее для эмуляции клиентского взаимодействия).

**Основные сценарии через API Gateway:**

*   **Загрузка файла:** `POST http://localhost:5000/api/gateway/files` (form-data, поле "file")
*   **Анализ файла:** `POST http://localhost:5000/api/gateway/analysis/{fileId}`
*   **Получение результатов анализа:** `GET http://localhost:5000/api/gateway/analysis/{fileId}`
*   **Получение облака слов:** `GET http://localhost:5000/api/gateway/analysis/{fileId}/wordcloud`

## Запуск тестов

Из корневой папки `AntiPlagiarismSystem`:
```bash
dotnet test AntiPlagiarism.FileAnalysisService.Tests/AntiPlagiarism.FileAnalysisService.Tests.csproj
```