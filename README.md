# webloc-opener

A small Windows utility that opens macOS `.webloc` shortcut files in your default browser, and registers itself as the handler for the `.webloc` file type.

`.webloc` files are Apple property-list (plist) shortcuts created by Safari and Finder. They contain a single `URL` entry pointing at a web page. This tool understands both the XML and binary plist variants.

## Features

- Opens `.webloc` files in your default browser
- Supports both XML and binary plist formats
- Per-user file association (no admin rights required)
- Tiny, single-file executable
- No console window flash when double-clicking

## Usage

```
webloc-opener.exe <file.webloc>     Open the URL in the default browser
webloc-opener.exe --register        Register the .webloc handler (per-user)
webloc-opener.exe --unregister      Remove the handler
```

## Setup

1. Download the latest release from the [Releases](https://github.com/slohmaier/webloc-opener/releases) page:
   - `webloc-opener-<version>-win-x64.exe` for x64 PCs (Intel/AMD)
   - `webloc-opener-<version>-win-arm64.exe` for ARM64 (Surface Pro X etc.)
2. Rename it to `webloc-opener.exe` and put it somewhere stable, e.g. `%LOCALAPPDATA%\WeblocOpener\webloc-opener.exe`.
3. Run once with `--register` to register the file type:

   ```powershell
   .\webloc-opener.exe --register
   ```

4. Set it as the default app for `.webloc` (one-time, manual step):
   - Right-click any `.webloc` file → **Open with** → **Choose another app**
   - Pick **webloc-opener** (or **Choose another app** → browse to the exe)
   - Tick **Always use this app**

   Windows protects file associations via the `UserChoice` registry hash, so the default cannot be set programmatically without admin tools. This is a one-time manual confirmation.

## Building from source

Requires the .NET 8 SDK.

```powershell
cd WeblocOpener
dotnet publish -c Release -r win-x64 `
  -p:PublishSingleFile=true `
  -p:SelfContained=true `
  -p:IncludeNativeLibrariesForSelfExtract=true
```

The single-file exe ends up in `WeblocOpener\bin\Release\net8.0-windows\win-x64\publish\webloc-opener.exe`.

For ARM64 (Surface Pro X etc.), replace `win-x64` with `win-arm64`.

## Cutting a release

Releases are built and published by the [`release.yml`](.github/workflows/release.yml) GitHub Actions workflow on tag push:

```powershell
git tag v1.0.0
git push --tags
```

The workflow builds win-x64 and win-arm64 single-file self-contained exes, attaches them to a GitHub Release whose tag matches, and auto-generates release notes from the commits since the previous tag. You can also trigger a dry-run build (no release published) via the **Run workflow** button in the Actions tab.

## How it works

`.webloc` files are Apple property lists. The tool parses them using [plist-cil](https://github.com/claunia/plist-cil), reads the `URL` key from the root dictionary, and launches it via `ShellExecute` — which hands off to whatever browser the user has set as default.

Registration writes per-user keys under `HKCU\Software\Classes\.webloc` and `HKCU\Software\Classes\WeblocOpener.webloc`, plus an `OpenWithProgids` hint under `HKCU\Software\Microsoft\Windows\CurrentVersion\Explorer\FileExts\.webloc`.

## License

MIT — see [LICENSE](LICENSE).
