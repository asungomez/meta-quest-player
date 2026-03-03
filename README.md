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

## Notes
- All controls are triggered by gaze dwell time (default 1.5s).
- UI is in world space for VR comfort.
- Eye Tracking permission prompts appear on first launch.
