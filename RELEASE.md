# Инструкция по релизу NovaBrowser

Пошаговое руководство: от локальной сборки до публикации обновления на GitHub.

Репозиторий: [github.com/axobeasty/NovaBrowser](https://github.com/axobeasty/NovaBrowser)

---

## Что понадобится

- Windows 10/11
- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- [Git](https://git-scm.com/)
- [GitHub CLI](https://cli.github.com/) (`gh`) — для создания репозитория и релиза из командной строки
- Аккаунт GitHub

---

## Шаг 1. Компилируем exe

Открой PowerShell в папке проекта:

```powershell
cd d:\cursorai\ASTUDIO\NovaBrowser
```

Собери **Release**-версию для нужной архитектуры.

### x64 (рекомендуется для большинства ПК)

```powershell
dotnet publish -c Release -p:Platform=x64 -r win-x64
```

### x86

```powershell
dotnet publish -c Release -p:Platform=x86 -r win-x86
```

### ARM64

```powershell
dotnet publish -c Release -p:Platform=ARM64 -r win-arm64
```

### Где лежит готовый exe

После сборки x64:

```
bin\Release\net9.0-windows10.0.26100.0\win-x64\publish\NovaBrowser.exe
```

> **Важно:** для запуска нужна **вся папка `publish`**, а не только `.exe`.  
> Внутри — все DLL, `Assets/` и runtime WinUI. Сборка **self-contained**, отдельно ставить .NET не нужно.

### Проверка перед релизом

```powershell
& ".\bin\Release\net9.0-windows10.0.26100.0\win-x64\publish\NovaBrowser.exe"
```

Убедись, что браузер открывается, вкладки работают, навигация и WebView2 в порядке.

---

## Шаг 1b. Сборка WinUI 3 установщика (опционально)

Кастомный мастер установки в стиле NovaBrowser (`NovaBrowser.Setup.exe`) лежит в проекте `NovaBrowser.Installer/`.

### Сборка x64

```powershell
cd d:\cursorai\ASTUDIO\NovaBrowser
powershell -ExecutionPolicy Bypass -File "NovaBrowser.Installer\build-installer.ps1" -Platform x64
```

Скрипт:

1. Публикует браузер и `NovaBrowser.Uninstall.exe` во временную папку
2. Упаковывает их в `NovaBrowser.Installer\Assets\SetupBundle.zip` (встраивается в exe)
3. Публикует один файл `dist\installer-x64\NovaBrowser.Setup.exe`
4. Создаёт архив `dist\NovaBrowser.Setup-win-x64.zip` (только Setup.exe)

### Запуск установщика

```powershell
& ".\dist\installer-x64\NovaBrowser.Setup.exe"
```

Для распространения нужен **только** `NovaBrowser.Setup.exe` — отдельная папка `payload\` не требуется.

### Что делает установщик

- Мастер: приветствие → параметры → **прогресс-бар** → готово
- Распаковывает встроенный архив в `%LocalAppData%\NovaBrowser` (или выбранную папку)
- Копирует вместе с браузером `NovaBrowser.Uninstall.exe` и его зависимости
- Создаёт ярлыки на рабочем столе и в меню «Пуск»
- Регистрирует удаление в `Параметры → Приложения` (запускает `NovaBrowser.Uninstall.exe`)
- Удаление из папки установки: `NovaBrowser.Uninstall.exe --path "C:\путь\к\NovaBrowser"`

### Другие архитектуры

```powershell
powershell -ExecutionPolicy Bypass -File "NovaBrowser.Installer\build-installer.ps1" -Platform x86
powershell -ExecutionPolicy Bypass -File "NovaBrowser.Installer\build-installer.ps1" -Platform ARM64
```

---

## Шаг 2. Заливаем проект на GitHub

### Первый раз (репозиторий ещё не создан)

```powershell
cd d:\cursorai\ASTUDIO\NovaBrowser

# Авторизация (один раз)
gh auth login

# Инициализация git (если ещё не сделано)
git init
git branch -M main

# Коммит
git add .
git -c user.name="axobeasty" -c user.email="axobeasty@users.noreply.github.com" commit -m "Initial commit"

# Создание репозитория и push
gh repo create NovaBrowser --public --source=. --remote=origin --push
```

### Обычное обновление кода (репозиторий уже есть)

```powershell
cd d:\cursorai\ASTUDIO\NovaBrowser

git add .
git commit -m "Описание изменений"
git push origin main
```

### Что не попадает в git

Папки `bin/`, `obj/`, `publish/` и данные WebView2 уже перечислены в `.gitignore` — их пушить не нужно.

---

## Шаг 3. Присваиваем номер версии

Версия задаётся в файле `NovaBrowser.csproj`:

```xml
<Version>0.2.0</Version>
<AssemblyVersion>0.2.0.0</AssemblyVersion>
<FileVersion>0.2.0.0</FileVersion>
```

### Правила нумерации

| Поле | Пример | Назначение |
|------|--------|------------|
| `<Version>` | `0.2.0` | Версия приложения и проверка обновлений |
| `<AssemblyVersion>` | `0.2.0.0` | Четыре числа через точку |
| `<FileVersion>` | `0.2.0.0` | Версия файла exe в свойствах Windows |

Используй формат **SemVer**: `MAJOR.MINOR.PATCH` (например `0.1.0`, `0.2.0`, `1.0.0`).

### Пример для релиза 0.2.0

1. Открой `NovaBrowser.csproj`.
2. Замени `0.1.0` на `0.2.0` во всех трёх полях.
3. Закоммить изменение:

```powershell
git add NovaBrowser.csproj
git commit -m "Bump version to 0.2.0"
git push origin main
```

> Браузер сравнивает эту версию с тегом GitHub Release (`v0.2.0`).  
> Тег и `<Version>` должны совпадать по номеру.

---

## Шаг 4. Делаем релиз на GitHub

Есть два способа: **автоматический** (через GitHub Actions) и **ручной**.

---

### Способ A — автоматический (рекомендуется)

При push тега `v*.*.*` workflow `.github/workflows/release.yml` сам:

1. Соберёт проект для x64, x86 и arm64
2. Создаст zip-архивы
3. Опубликует GitHub Release

```powershell
cd d:\cursorai\ASTUDIO\NovaBrowser

# Тег должен совпадать с версией из csproj
git tag v0.2.0
git push origin v0.2.0
```

Через 5–10 минут на странице репозитория появится Release с файлами:

- `NovaBrowser-win-x64.zip`
- `NovaBrowser-win-x86.zip`
- `NovaBrowser-win-arm64.zip`
- `NovaBrowser.Setup-win-x64.zip` (один файл `NovaBrowser.Setup.exe`)
- `NovaBrowser.Setup-win-x86.zip`
- `NovaBrowser.Setup-win-arm64.zip`

Статус сборки: вкладка **Actions** в репозитории на GitHub.

---

### Способ B — ручной релиз

Если нужно выложить только локальную сборку без CI:

**1. Собери exe** (шаг 1).

**2. Упакуй папку `publish` в zip:**

```powershell
cd "bin\Release\net9.0-windows10.0.26100.0\win-x64\publish"
Compress-Archive -Path * -DestinationPath "..\..\..\..\..\NovaBrowser-win-x64.zip" -Force
cd d:\cursorai\ASTUDIO\NovaBrowser
```

**3. Создай Release через GitHub CLI:**

```powershell
gh release create v0.2.0 `
  --title "NovaBrowser 0.2.0" `
  --notes "Описание изменений в этом релизе." `
  NovaBrowser-win-x64.zip
```

Или через сайт: **Releases → Draft a new release → Choose tag `v0.2.0` → Upload zip → Publish release**.

---

## Как пользователи получат обновление

После публикации Release:

- при запуске браузер **тихо проверит** GitHub (через ~3 секунды);
- если версия в Release новее — покажет диалог «Доступно обновление»;
- вручную: кнопка **↓** справа на панели навигации.

Для автообновления нужен zip с именем **`NovaBrowser-win-x64.zip`** (или x86/arm64 под архитектуру).

---

## Чеклист перед каждым релизом

- [ ] Локально собран и проверен `NovaBrowser.exe`
- [ ] В `NovaBrowser.csproj` обновлены `<Version>`, `<AssemblyVersion>`, `<FileVersion>`
- [ ] Изменения закоммичены и запушены в `main`
- [ ] Создан и запушен тег `vX.Y.Z` (или вручную загружен zip)
- [ ] На GitHub появился Release с нужными zip-файлами
- [ ] Старая версия браузера видит обновление и может его установить

---

## Быстрая шпаргалка (повторный релиз)

```powershell
cd d:\cursorai\ASTUDIO\NovaBrowser

# 1. Сборка
dotnet publish -c Release -p:Platform=x64 -r win-x64

# 2. Версия в NovaBrowser.csproj → 0.2.0

# 3. GitHub
git add .
git commit -m "Release v0.2.0"
git push origin main
git tag v0.2.0
git push origin v0.2.0

# 4. Ждём GitHub Actions → Release готов
```

---

## Частые проблемы

| Проблема | Решение |
|----------|---------|
| exe не запускается | Копируй всю папку `publish`, проверь WebView2 Runtime |
| «Нет файла NovaBrowser-win-x64.zip» при обновлении | В Release должен быть zip с **точным** именем |
| Обновление не предлагается | Тег Release (`v0.2.0`) должен быть **новее** `<Version>` в установленной сборке |
| GitHub Actions упал | Смотри логи во вкладке Actions, проверь .NET 9 и WinUI templates |
| `gh: not authenticated` | Выполни `gh auth login` |
