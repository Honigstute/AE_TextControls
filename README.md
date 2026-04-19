## Installation

This plugin is a ZXP extension and can be installed using a ZXP installer.

---

### Step 1 — Download a ZXP Installer

Download and install a ZXP installer.

Recommended:
[aescripts + aeplugins ZXP Installer](https://aescripts.com/learn/post/zxp-installer)

---

### Step 2 — Install the Plugin

- Open the ZXP Installer
- Drag & drop the `TextControls-v1.0.0.zxp` file into the installer
- Follow the installation process

---

### Step 3 — Launch After Effects

Open After Effects and find the panel under:

```text
Window -> Extensions -> Text Controls
```

---

## Transcription Setup

Text Controls supports two transcription paths:

- `OpenAI Whisper API`
- `Local Whisper`

---

### OpenAI Whisper API

This is the easiest setup.

You need:

- an OpenAI API key

Recommended setup:

- `Engine`: `OpenAI Whisper API`
- `OpenAI Render`: `Slow Render (compatibility)`

With that setup, you do not need:

- Python
- local Whisper
- ffmpeg

OpenAI speech-to-text docs:
[Speech to text](https://platform.openai.com/docs/guides/speech-to-text)

OpenAI transcription API reference:
[Audio transcriptions API](https://platform.openai.com/docs/api-reference/audio/createTranscription)

OpenAI model reference:
[whisper-1](https://developers.openai.com/api/docs/models/whisper-1)

Note:

- `Fast Render (ffmpeg needed)` requires `ffmpeg`
- file uploads are limited by OpenAI
- OpenAI currently supports common upload formats such as `mp3`, `mp4`, `mpeg`, `mpga`, `m4a`, `wav`, and `webm` according to the current speech-to-text guide

---

### Local Whisper

This is the offline/local setup.

You need:

- Python 3
- Whisper
- ffmpeg

Official links:

- [Python downloads](https://www.python.org/downloads/)
- [OpenAI Whisper on GitHub](https://github.com/openai/whisper)
- [FFmpeg downloads](https://www.ffmpeg.org/download.html)

Basic install idea:

- install Python 3
- install Whisper with `pip install openai-whisper`
- install ffmpeg
- make sure `whisper` and `ffmpeg` are available on your system `PATH`
- run `Check Setup` inside the panel

---

## Windows / macOS Notes

If you install the panel with a ZXP installer, the extension install itself is simple on both systems.

If you install manually, Adobe CEP documents these extension folders:

- Windows per-user: `C:\Users\<USERNAME>\AppData\Roaming\Adobe\CEP\extensions`
- macOS per-user: `~/Library/Application Support/Adobe/CEP/extensions`

Adobe CEP extension folder reference:
[Adobe CEP HTML Extension Cookbook](https://github.com/Adobe-CEP/CEP-Resources/blob/master/CEP_12.x/Documentation/CEP%2012%20HTML%20Extension%20Cookbook.md)

Current Text Controls behavior from the codebase:

- on Windows, Local Whisper setup checks rely on commands being available on `PATH`
- on macOS, the plugin checks `PATH` and also looks in common locations such as `/opt/homebrew/bin/whisper`, `/usr/local/bin/whisper`, `/opt/homebrew/bin/ffmpeg`, and `/usr/local/bin/ffmpeg`

Practical recommendation:

- on Windows, make sure `python`, `whisper`, and `ffmpeg` are on `PATH`
- on macOS, Homebrew or a standard Python install usually works well as long as the binaries are discoverable

---

## Notes

- ZXP plugins require a separate installer because Adobe no longer supports direct double-click installation.
- Make sure After Effects is closed during installation.
- If the panel does not appear, restart After Effects.
- For the easiest public-facing setup, use `OpenAI Whisper API` with `Slow Render (compatibility)`.
- For fully offline transcription, use `Local Whisper`.
- When transcribing a selected comp, the plugin creates a temporary audio export automatically.
- When transcribing a media file, the plugin can use the selected file directly.
