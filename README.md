# NovaBrowser

Собственный браузер на **WinUI 3** и **WebView2** (Chromium).

## Возможности (v0.1)

- Вкладки с перетаскиванием и закрытием
- Адресная строка с автоподстановкой `https://` и поиском через Bing
- Навигация: назад, вперёд, обновить, домой
- Стартовая страница NovaBrowser (`nova://start`)
- Открытие ссылок «в новом окне» в новой вкладке
- Mica-эффект и современный интерфейс Windows 11

## Требования

- Windows 10 1809+ / Windows 11
- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- [WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/) (обычно уже установлен в Windows 11)

## Запуск

```powershell
cd NovaBrowser
dotnet run
```

## Сборка

```powershell
dotnet publish -c Release -p:Platform=x64 -r win-x64
```

Готовая сборка:

```
bin\Release\net9.0-windows10.0.26100.0\win-x64\publish\NovaBrowser.exe
```

> **Важно:** копируйте всю папку `publish`, а не только `.exe`.  
> Сборка **self-contained** — Windows App SDK runtime включён в папку, отдельная установка не нужна.

## Структура проекта

```
NovaBrowser/
├── Controls/          # BrowserTabView — WebView2 на вкладку
├── ViewModels/        # MVVM: вкладки и команды навигации
├── Models/            # Настройки браузера
├── Services/          # Нормализация URL
├── Assets/start.html  # Стартовая страница
└── MainPage.xaml      # Панель вкладок и адресная строка
```

## Дорожная карта

- [ ] Закладки и история
- [ ] Менеджер загрузок
- [ ] Расширения / userscripts
- [ ] Профили и синхронизация
- [ ] Боковая панель (закладки, история)
- [ ] Настройки (поисковик, домашняя страница, тема)
- [ ] Приватный режим
- [ ] Встроенный менеджер паролей

## Лицензия

MIT
