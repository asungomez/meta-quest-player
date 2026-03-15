# Gaze-Controlled VR Video Player for Meta Quest 3

## Description
This project is a VR video player for the Meta Quest 3 that uses **eye tracking only** for controls.
Supported actions:
- Play / Pause / Restart video
- Switch between multiple audio language tracks embedded in the video file

The interface is gaze-based with dwell-time activation (no hand controllers).

## Requirements
- **Hardware:** Meta Quest 3, USB-C cable
- **OS:** macOS (tested on Sonoma)
- **Unity:** Unity 2022 LTS+ with Android Build Support
- **Meta XR SDK:** Includes Interaction SDK and Eye Tracking module
- **Android SDK & Platform Tools**
- Video file: `.mp4` with multiple audio tracks

## Setup
1. Install Unity Hub and Unity 2022 LTS with Android modules.
2. Install Android SDK Platform Tools and add to PATH.
3. Enable Meta Quest 3 Developer Mode via Meta mobile app.
4. Connect headset to Mac and confirm `adb devices` shows it.
5. Create new Unity 3D project and set build target to Android.
6. Import Meta XR SDK and Interaction SDK via Unity Package Manager.
7. Enable Eye Tracking in Project Settings > XR Plug-in Management.

## Build & Run
1. Place your video file in `Assets/StreamingAssets/` (e.g. `360.mp4`).
2. Implement `VideoPlayer` component with gaze control scripts:
   - **Quick setup:** In Unity, go to menu **VR Video Player → Setup Scene**. This creates:
     - A "Play" screen you gaze at to start the video
     - A 360° video sphere that plays `360.mp4` from StreamingAssets
   - Uses head-gaze ray with 1.5s dwell time (configurable on `GazePlayButton`).
3. Add **OVRCameraRig** to your scene for Quest (Meta → Tools → Building Blocks → Camera Rig) if not present.
4. Build APK and deploy to Quest 3 using `adb install`.
5. Run inside headset and control via gaze.

## Corporate Proxy / SSL (Build Only)

If builds fail behind a corporate proxy due to SSL inspection:

- A Gradle init script at `~/.gradle/init.gradle` disables SSL verification for all Gradle builds on this machine.
- **To undo:** Delete `~/.gradle/init.gradle`.
- If you also need proxy host/port, add to `~/.gradle/gradle.properties`:
  ```
  systemProp.http.proxyHost=your.proxy.host
  systemProp.http.proxyPort=8080
  systemProp.https.proxyHost=your.proxy.host
  systemProp.https.proxyPort=8080
  ```
- Restart any running Gradle daemons (`gradle --stop`) or restart Unity before rebuilding.

## Corporate Proxy / SSL
If builds fail behind a corporate proxy that performs SSL inspection, an init script disables SSL verification for Gradle at `~/.gradle/init.gradle`. If you need proxy host/port, add to `~/.gradle/gradle.properties`:
```
systemProp.http.proxyHost=your.proxy.com
systemProp.http.proxyPort=8080
systemProp.https.proxyHost=your.proxy.com
systemProp.https.proxyPort=8080
```
**Security:** Remove `~/.gradle/init.gradle` when no longer behind the proxy.

## Corporate Proxy / SSL

If builds fail behind a corporate proxy (e.g. `could not resolve plugin artifact`), SSL verification has been disabled for Gradle via `~/.gradle/init.gradle`. This affects all Gradle builds on your machine. To remove: delete `~/.gradle/init.gradle`.

If you still can't reach Maven/Google repos, add proxy settings to `~/.gradle/gradle.properties`:
```
systemProp.http.proxyHost=your.proxy.host
systemProp.http.proxyPort=8080
systemProp.https.proxyHost=your.proxy.host
systemProp.https.proxyPort=8080
```

## Notes
- All controls are triggered by gaze dwell time (default 1.5s).
- UI is in world space for VR comfort.
- Eye Tracking permission prompts appear on first launch.

## Corporate Proxy / SSL

If you're behind a corporate proxy that performs SSL inspection, a Gradle init script has been added at `~/.gradle/init.gradle` to disable SSL certificate verification for builds. This applies to all Gradle builds on your machine.

- **To remove:** Delete `~/.gradle/init.gradle` when no longer needed.
- **If you also need proxy settings:** Add to `~/.gradle/gradle.properties`:
  ```
  systemProp.http.proxyHost=your.proxy.host
  systemProp.http.proxyPort=8080
  systemProp.https.proxyHost=your.proxy.host
  systemProp.https.proxyPort=8080
  ```
- **If builds still fail:** Run `gradle --stop` (or close Unity), then retry so a fresh Gradle daemon loads the init script.

## Building behind a corporate proxy

If Gradle fails to download dependencies (e.g. "Plugin was not found"), an init script has been added to disable SSL verification for Gradle builds.

- **Location:** `~/.gradle/init.gradle` (runs for all Gradle builds on your machine)
- **To remove:** Delete `~/.gradle/init.gradle` when no longer behind the proxy
- **Restart Gradle daemon:** Run `gradle --stop` (or restart Unity) so the next build picks up the change

If you also need to configure proxy host/port, add to `~/.gradle/gradle.properties`:
```properties
systemProp.http.proxyHost=your.proxy.host
systemProp.http.proxyPort=8080
systemProp.https.proxyHost=your.proxy.host
systemProp.https.proxyPort=8080
```

## Corporate Proxy / SSL

If you're behind a corporate proxy that performs SSL inspection, Gradle may fail to download the Android Gradle Plugin. An init script at `~/.gradle/init.gradle` disables SSL certificate verification for Gradle builds on this machine.

- **To remove:** Delete `~/.gradle/init.gradle` when you no longer need it.
- **If builds still fail:** Add proxy settings to `~/.gradle/gradle.properties`:
  ```
  systemProp.http.proxyHost=your.proxy.host
  systemProp.http.proxyPort=8080
  systemProp.https.proxyHost=your.proxy.host
  systemProp.https.proxyPort=8080
  ```

## Corporate Proxy / SSL
If builds fail behind a corporate proxy (e.g. `Plugin was not found` or `Failed to download`), an init script at `~/.gradle/init.gradle` disables SSL certificate verification for Gradle. This affects all Gradle builds on your machine. If you also need to set proxy host/port, add to `~/.gradle/gradle.properties`:

```
systemProp.http.proxyHost=your.proxy.host
systemProp.http.proxyPort=8080
systemProp.https.proxyHost=your.proxy.host
systemProp.https.proxyPort=8080
```

## Corporate Proxy / SSL

If builds fail behind a corporate proxy (e.g. "Plugin was not found" or "could not resolve plugin artifact"):

1. **SSL verification** is disabled via `~/.gradle/init.gradle`—this applies to all Gradle builds on your machine.
2. If you still need to configure proxy host/port, create or edit `~/.gradle/gradle.properties` and add:
   ```
   systemProp.http.proxyHost=your.proxy.host
   systemProp.http.proxyPort=8080
   systemProp.https.proxyHost=your.proxy.host
   systemProp.https.proxyPort=8080
   ```
3. Restart any running Gradle daemons: run `gradle --stop` in a terminal (if Gradle is installed), or restart Unity.
4. To revert the SSL workaround, delete `~/.gradle/init.gradle`.

## Corporate Proxy / SSL

If behind a corporate proxy that performs SSL inspection, Gradle may fail with "plugin not found" or certificate errors. An init script at `~/.gradle/init.gradle` disables SSL verification for Gradle builds (created automatically). This affects all Gradle builds on your machine.

**If builds still fail**, add proxy settings to `~/.gradle/gradle.properties`:
```properties
systemProp.http.proxyHost=your.proxy.host
systemProp.http.proxyPort=8080
systemProp.https.proxyHost=your.proxy.host
systemProp.https.proxyPort=8080
```

**To remove the SSL workaround** later: delete `~/.gradle/init.gradle`.

## Corporate Proxy / SSL

If the Android build fails behind a corporate proxy (e.g. `Plugin ... was not found`), SSL verification has been disabled for Gradle via `~/.gradle/init.gradle`. This runs for all Gradle builds on your machine.

**To remove when no longer needed:** Delete `~/.gradle/init.gradle`.

**If you still need proxy host/port:** Add to `~/.gradle/gradle.properties`:

```
systemProp.http.proxyHost=your.proxy.host
systemProp.http.proxyPort=8080
systemProp.https.proxyHost=your.proxy.host
systemProp.https.proxyPort=8080
```

## Corporate Proxy / SSL

If builds fail behind a corporate proxy (e.g. "Plugin was not found" or SSL handshake errors), an init script disables SSL verification for Gradle. It lives at `~/.gradle/init.gradle`.

**After changes:** Stop the Gradle daemon so the next build picks up the script:
```bash
gradle --stop
```
If `gradle` isn't in PATH, restart Unity and try a build (the daemon will restart).

**To remove:** Delete `~/.gradle/init.gradle` when you no longer need it.

**Optional – proxy settings:** If you also need to configure the proxy host/port, add to `~/.gradle/gradle.properties`:
```
systemProp.http.proxyHost=your.proxy.host
systemProp.http.proxyPort=8080
systemProp.https.proxyHost=your.proxy.host
systemProp.https.proxyPort=8080
```
