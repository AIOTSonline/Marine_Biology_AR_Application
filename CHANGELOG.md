# Changelog

## 2026-05-30

### Android camera and permissions
- Added `android.permission.CAMERA` to the Android manifest.
- Added Android camera feature declarations while keeping AR hardware optional.
- Expanded runtime permission handling for camera and microphone on Android and iOS.
- Added permission checks before opening AR, Free Explore, Module Wise, Human Interaction, QR Scanner, and AR placement flows.
- Added clear user feedback when camera permission is denied.

### Installable APK build
- Disabled split application binary / APK expansion usage for command-line APK builds.
- Added `Assets/Editor/CommandLineAndroidBuild.cs` to build a single installable APK from batch mode.
- Built `Builds/MarineBiologyAR.apk` as a single APK with no OBB file.
- Used debug signing for the generated installable test APK when the release keystore password was unavailable.

### AR surface detection
- Improved AR placement raycasts to accept faster ARCore hit types:
  - plane polygon
  - plane bounds
  - estimated planes
  - feature points
- Added `ARSurfaceDetectionOptimizer` to request horizontal and vertical plane detection on active AR plane managers.
- Updated Free Explore water-plane placement to react earlier instead of waiting only for strict plane-polygon detection.
- Updated custom AR placement to place from the full hit pose, improving alignment when early ARCore hits are used.

### Navigation and runtime UI polish
- Added `AppRuntimeUI`, a shared runtime overlay with:
  - consistent top-left Back button on non-home scenes
  - Android hardware-back handling
  - camera permission/loading status messages
  - scanning guidance for AR surface detection
- Added back navigation coverage for QR Scanner, AR placement, Human Interaction, Free Explore, Module Wise, and custom-create scenes.
- Added safer AR session reset when navigating back from AR scenes.
- Added status messages while opening camera-heavy modules.

### QR scanner
- Updated QR scanner to request camera permission before starting.
- Prefer back camera for QR scanning.
- Added scan/loading user feedback.
- Added camera cleanup on scene exit to avoid leaving the webcam running.
- Added null checks around optional QR scanner UI references.

### Human Interaction
- Added camera/model loading status messages for the Human Interaction module.
- Added consistent Back button and Android hardware-back support through the runtime UI layer.
- Updated human detection status text to clearer, more professional labels.
- Added safer camera startup handling when permission is denied or no camera is available.

### Stability and safety
- Added null checks in AR activation, module selection, back navigation, and custom-create entry points.
- Prevented repeated taps from launching multiple scene-load routines while permissions are still open.
- Stopped MediaPipe camera startup from waiting forever when camera permission is denied.

### 2026-05-30 follow-up
- Fixed AR placement after fast scanning by preferring stable plane/estimated-plane hits over weak feature-point hits.
- Added legacy touch fallback for Free Explore placement so taps still work if the Input Action does not fire on a device.
- Added tap-miss guidance when the user taps somewhere ARCore cannot place yet.
- Reduced the runtime Back button to a compact control and hid it on scenes that already have an active Back button.
- Added command-line release signing support for the existing Android keystore.
- Fixed AR spawner tap placement so real finger/mouse screen positions are preferred over Input Action values.
- Made Module Wise / legacy AR spawner placement accept plane bounds, estimated planes, and feature points like Ocean Explore.
- Prevented custom AR placement from locking after a tap when the saved module data is missing or cannot be loaded.
- Added clearer placement error messages when an AR module prefab or saved module data is unavailable.
- Built release outputs with Android version name `0.2.5` and version code `12`.
- Added a direct AR_Spawn tap fallback so scanned-dot taps place through `ARRaycastManager` instead of the brittle sample AR interactor path.
- Disabled the sample AR interactor spawn trigger in AR_Spawn at runtime to avoid UI/raycast conflicts.
- Updated AAB command-line builds to enable Unity Play Asset Delivery splitting while keeping APK builds single-file/no-OBB.
- Built the next release with Android version name `0.2.6` and version code `13`.

### 2026-06-09 follow-up
- Fixed AR_Spawn model selection when legacy menu buttons request indexes outside the available prefab list.
- Mapped older AR_Spawn button indexes back onto the available Octopus, Nautilus, and Squid prefabs.
- Added placement feedback when AR_Spawn has no valid selected model.
- Re-enabled renderers on newly spawned AR_Spawn objects so selected models cannot spawn hidden.
- Built the next release with Android version name `0.2.7` and version code `14`.

### Known release note
- The generated APK is installable for device testing. A Play Store upload normally requires a signed release AAB or APK using the project keystore and valid store metadata.
