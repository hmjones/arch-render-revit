# ArchRender for Revit

AI architectural rendering directly inside Revit 2026, powered by [ArchRender](https://archrender.com).

## Prerequisites

- Revit 2026
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8) (build only)

## Build

```powershell
cd src/ArchRender.Revit
dotnet build -c Release
```

The output DLL lands in `src/ArchRender.Revit/bin/Release/net8.0-windows/`.

## Install

1. Build the project (above).
2. Copy `ArchRender.Revit.addin` and `ArchRender.Revit.dll` to:
   ```
   %APPDATA%\Autodesk\Revit\Addins\2026\
   ```
3. Restart Revit — an **ArchRender** tab appears in the ribbon.

## First-time setup

1. Log in to [archrender.com](https://archrender.com) → Settings → API Keys → **Generate Key**.
2. In Revit, click **ArchRender → Settings** and paste the key.

## Usage

1. Open a Revit project and activate a **3D view**.
2. Click **ArchRender → Render** to open the side panel.
3. Choose render type, season, time of day, environment, and aspect ratio.
4. Click **Generate Render**.
5. The result appears in the panel. Double-click to open full size, or click **Save Image**.

## Backend

The plugin calls the `render-from-plugin` Supabase Edge Function in the ArchRender project (`klttztjdhaqtlgmctipe`). API keys are validated server-side and credits are deducted per render.
