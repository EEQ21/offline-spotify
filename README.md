# Offline Spotify

A Spotify-inspired desktop music player designed for offline listening, featuring built-in Spotify playlist downloading support.

## Features

* Offline music playback
* Play, pause, skip, and previous track controls
* Shuffle playback
* Like and save your favorite songs
* Spotify playlist downloading support
* Upload and play your own MP3 and M4A audio files
* Create and manage custom albums
* Organize your personal music library
* Simple and lightweight interface

## Credits

Offline Spotify is an independent project and is **not affiliated with Spotify**.

### Playlist downloading

Playlist import runs an external downloader (shipped or placed next to the app as `spotify-dl.exe`). That tool is built on:

| Project | Role |
| -------- | ----- |
| [SpotDL](https://github.com/spotdl/spotify-downloader) | Matches Spotify tracks and orchestrates downloads |
| [yt-dlp](https://github.com/yt-dlp/yt-dlp) | Fetches audio from YouTube and other sources (used by SpotDL) |
| [FFmpeg](https://ffmpeg.org/) | Audio conversion when required by the downloader |

Thank you to the SpotDL, yt-dlp, and FFmpeg maintainers and contributors.

### App libraries

| Project | Role |
| -------- | ----- |
| [TagLibSharp](https://github.com/mono/taglib-sharp) | Reading MP3 metadata and cover art (.NET) |
| [Microsoft WebView2](https://developer.microsoft.com/microsoft-edge/webview2/) | Embedded player UI |
| [Tabler Icons](https://github.com/tabler/tabler-icons) | Icons in the interface |
| [jsmediatags](https://github.com/aadsm/jsmediatags) | Tag reading for files loaded in the browser UI |

## Setup

For playlist downloading to function correctly, ensure that `spotify-dl.exe` is placed in the same directory as the built application executable.

Example:

```text
build/
├── offline-spotify.exe
└── spotify-dl.exe
```

If `spotify-dl.exe` is missing or located elsewhere, playlist downloading will not work.
