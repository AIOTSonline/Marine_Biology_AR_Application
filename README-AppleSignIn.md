# Apple Sign-In Integration Update

This branch (`feature/apple-signin-v4`) contains the fully implemented backend logic for native Apple Sign-In and fixes to the Google Sign-In iOS SDK.

## What is included:
1. **Google SDK Fixes**: Patched `GoogleSignIn.mm`, `GoogleSignIn.h`, and `GoogleSignInAppController.mm` to work perfectly with SDK v5.0+ and remove deprecated `uiDelegate` code.
2. **Apple Sign-In Logic**: Updated `AuthManager.cs` to fully integrate Apple Sign-In with Firebase Auth.
3. **Build Bump**: Incremented the iOS Build Number to `4` in Project Settings.

## Next Steps for the UI Developer:
1. Open the project in Unity.
2. Go to **Window > Package Manager** and add package from Git URL: `https://github.com/lupidan/apple-signin-unity.git`
3. Open `Assets/Scenes/AuthScene.unity`.
4. Duplicate the "Sign in with Google" button and rename it `AppleSignInButton`.
5. Change its Text to "Sign in with Apple" and its color to solid Black (#000000) with White text (#FFFFFF) to pass Apple Review guidelines.
6. In the Button's `On Click ()` event, point it to `AuthManager.AppleSignInUser`.
7. Build to iOS, Archive in Xcode, and upload!
