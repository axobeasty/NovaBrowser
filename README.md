# NovaBrowser

Собственный браузер на **WinUI 3** и **WebView2** (Chromium).

## Возможности (v1.0)

### Браузинг
- Вкладки с перетаскиванием, закреплением, дублированием, группами и контекстным меню
- Favicon, горячие клавиши (Ctrl+T/W/Shift+T, Ctrl+Tab, Ctrl+1–9, Ctrl+F/L/H/J/D и др.)
- Поиск на странице, масштаб, печать, DevTools (F12)
- Восстановление сессии и crash recovery
- Приватное окно и несколько окон (Ctrl+N)

### Данные
- Закладки (Ctrl+D), панель закладок, импорт из Chrome/Edge/HTML
- История (Ctrl+H), боковая панель
- Менеджер загрузок (Ctrl+J)

### Настройки и UI
- Темы (20+ цветов, пресеты), ru/en локализация
- Поисковик на стартовой странице из настроек
- Профили, userscripts, блокировка трекеров
- Менеджер паролей (Windows Credential Locker)
- Экспорт/импорт sync-данных, очистка данных
- Jump List, переход в «Приложения по умолчанию»

### Дистрибуция
- Автообновление через GitHub Releases
- Установщик и полный деинсталлятор

Подробная инструкция: **[RELEASE.md](RELEASE.md)**

## Требования

- Windows 10 1809+ / Windows 11
- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- [WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/)

## Запуск

```powershell
cd NovaBrowser
dotnet run
```

Приватное окно:

```powershell
dotnet run -- /private
```

## Сборка

```powershell
dotnet publish -c Release -p:Platform=x64 -r win-x64
powershell -ExecutionPolicy Bypass -File NovaBrowser.Installer/build-installer.ps1 -Platform x64
```

## Структура

```
NovaBrowser/
├── Controls/       # Вкладки, WebView2, боковая панель, закладки, поиск
├── ViewModels/     # MVVM
├── Services/       # История, закладки, загрузки, профили, sync, ad-block
├── Models/         # Настройки и данные
└── Assets/         # Стартовая страница, локализация
```

## Лицензия

MIT
