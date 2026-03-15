# VR Video Library Player (Meta Quest)

## Overview
This project is a VR video player for Meta Quest with two usage modes:

- **Regular users (no controllers):** gaze-select and play videos from the local library.
- **Admin users (with controllers):** import videos from an inbox and delete videos from the library.

The app no longer depends on a hardcoded filename in `StreamingAssets`.

## Requirements
- Meta Quest headset + USB-C cable
- macOS
- Unity with Android build support
- Meta XR SDK / OpenXR setup
- Android platform tools (`adb`)

## Runtime Folders (on device)
The app uses `Application.persistentDataPath`:

- Library: `.../files/videos`
- Inbox: `.../files/inbox`

In practice on Quest this maps to:
`/sdcard/Android/data/<your.package.name>/files/`

## Usage
1. In Unity run **VR Video Player -> Setup Scene**.
2. Build and install the app.
3. Push videos to device:
   - Directly playable library:
     `adb push "/path/to/video.mp4" "/sdcard/Android/data/<package>/files/videos/video.mp4"`
   - Admin-import inbox:
     `adb push "/path/to/video.mp4" "/sdcard/Android/data/<package>/files/inbox/video.mp4"`
4. Launch app:
   - Gaze a video button to play.
   - Controller admin actions:
     - Right A: Play selected
     - Right B: Delete selected
     - Left X: Import from inbox
     - Left Y: Back to library

## Build Notes
- If no videos are available in the library, app stays in empty/error state and does not start playback.
- Very large videos should remain external (device storage), not bundled in the APK.

## Corporate Proxy / SSL
If Gradle resolution fails behind SSL-inspecting corporate proxy:

- `~/.gradle/init.gradle` is configured for this environment.
- Custom truststore is at:
  `~/.gradle/certs/netskope-cacerts.jks`
- Optional proxy settings in `~/.gradle/gradle.properties`:
  ```
  systemProp.http.proxyHost=your.proxy.host
  systemProp.http.proxyPort=8080
  systemProp.https.proxyHost=your.proxy.host
  systemProp.https.proxyPort=8080
  ```
- Restart daemons after changes:
  `gradle --stop`
