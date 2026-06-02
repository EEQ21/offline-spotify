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

Playlist downloading is powered by SpotDL:
https://github.com/spotdl/spotify-downloader

## Setup

For playlist downloading to function correctly, ensure that `spotify-dl.exe` is placed in the same directory as the built application executable.

Example:

```text
build/
├── offline-spotify.exe
└── spotify-dl.exe
```

If `spotify-dl.exe` is missing or located elsewhere, playlist downloading will not work.
