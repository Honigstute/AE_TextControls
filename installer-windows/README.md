## Text Controls Windows Dependencies Installer

This folder contains the Windows 10/11 x64 dependency manager for Text Controls
local transcription.

### What it installs

The dependency manager is portable and does not install itself. It manages only:

```text
%LOCALAPPDATA%\Text Controls Local\
```

Installed contents:

- `bin\tc-whisper-helper.exe`
- `bin\whisper-cli.exe`
- `bin\ffmpeg.exe`
- `models\ggml-base.bin`
- `install-manifest.json`
- `THIRD_PARTY_NOTICES\`

It does not install or remove the ZXP/CEP panel.

### Scope

- Windows 10/11 x64
- no admin rights required
- no Python
- no `openai-whisper`
- the Windows local engine payload is downloaded during Install/Repair and checksum-verified
- FFmpeg is downloaded during Install/Repair and checksum-verified
- the Whisper model is downloaded during Install/Repair and checksum-verified
- unsigned-first release; SmartScreen warnings are expected until signing and reputation are handled

### Build

The intended build path is GitHub Actions on `windows-latest`:

```text
.github/workflows/windows-dependencies.yml
```

That workflow builds `whisper-cli.exe`, publishes the helper, packages both into
`TextControlsLocalEngine-win-x64.zip`, and uploads the small public bootstrapper
`TextControlsDependencies-win-x64.exe`.

To keep the public EXE below small upload limits, the bootstrapper does not
embed the local engine payload. Publish `TextControlsLocalEngine-win-x64.zip`
as a GitHub release asset at the URL configured by the workflow before asking
users to run Install/Repair.

Local Windows build:

```powershell
cd installer-windows
dotnet test TextControlsDependencies.sln -c Release
dotnet publish src\tc-whisper-helper\tc-whisper-helper.csproj -c Release -r win-x64 -o payload\bin
# Copy whisper-cli.exe into payload\bin before publishing the app.
dotnet publish src\TextControlsDependencies.App\TextControlsDependencies.App.csproj -c Release -r win-x64 -o dist
```

### Runtime contract

The helper CLI contract is:

```text
tc-whisper-helper.exe check --json
tc-whisper-helper.exe version --json
tc-whisper-helper.exe transcribe --input <file> --model <model-path> --ffmpeg <ffmpeg-path> --output-format srt|json --output <file>
```

The CEP panel reads `%LOCALAPPDATA%\Text Controls Local\install-manifest.json`
and uses the helper/model/FFmpeg paths from that manifest on Windows.
