# 🔥 Firebase Setup Guide

This guide will help you set up Firebase for the Cat Virtual Pet game.

## Prerequisites

- Unity 6.2 installed
- Google account
- Firebase project created

## Step 1: Create Firebase Project

1. Go to [Firebase Console](https://console.firebase.google.com/)
2. Click "Add Project"
3. Name it "CatVirtualPet" (or your choice)
4. Disable Google Analytics (optional, can enable later)
5. Click "Create Project"

## Step 2: Register Your App

### For WebGL (Primary Platform)

1. In Firebase Console, click the **Web icon** (</>)
2. App nickname: "Cat Pet WebGL"
3. **Check** "Also set up Firebase Hosting"
4. Click "Register app"
5. **Save the Firebase config** - you'll need this:

```javascript
const firebaseConfig = {
  apiKey: "YOUR_API_KEY",
  authDomain: "your-project.firebaseapp.com",
  databaseURL: "https://your-project.firebaseio.com",
  projectId: "your-project",
  storageBucket: "your-project.appspot.com",
  messagingSenderId: "123456789",
  appId: "1:123456789:web:abcdef"
};
```

6. Copy this config - you'll paste it into Unity later

### For Desktop/Mobile Testing (Optional)

You can add additional platforms (iOS, Android, Desktop) later for testing.

## Step 3: Enable Authentication

1. In Firebase Console, go to **Authentication**
2. Click "Get Started"
3. Click **Sign-in method** tab
4. Enable **Google** sign-in:
   - Click "Google"
   - Toggle "Enable"
   - Select support email
   - Click "Save"

## Step 4: Set Up Realtime Database

1. In Firebase Console, go to **Realtime Database**
2. Click "Create Database"
3. Choose location (us-central1 recommended for US)
4. Start in **Test Mode** (we'll add security rules later)
5. Click "Enable"

## Step 5: Database Security Rules

Replace the default rules with these (click "Rules" tab):

```json
{
  "rules": {
    "users": {
      "$uid": {
        ".read": "$uid === auth.uid",
        ".write": "$uid === auth.uid"
      }
    },
    "globalPools": {
      "adoptionCenter": {
        ".read": true,
        ".write": false
      },
      "releasedCats": {
        ".read": true,
        "$catId": {
          ".write": "auth != null"
        }
      },
      "runawayCats": {
        ".read": true,
        "$catId": {
          ".write": "auth != null"
        }
      }
    },
    "publicRooms": {
      "$uid": {
        ".read": true,
        ".write": "$uid === auth.uid"
      }
    }
  }
}
```

**What these rules do:**
- Users can only read/write their own data
- Global adoption pools are read-only to prevent cheating
- Users can add released/runaway cats to global pools
- Public rooms are readable by all (for friend visits) but only writable by owner

## Step 6: Download Firebase Unity SDK

1. Go to [Firebase Unity SDK Download](https://firebase.google.com/download/unity)
2. Download the latest SDK (v11.x or newer)
3. Extract the .zip file
4. You'll need these packages:
   - `FirebaseAuth.unitypackage`
   - `FirebaseDatabase.unitypackage`
   - `FirebaseStorage.unitypackage` (optional, for user photos)

## Step 7: Import Firebase SDK into Unity

1. Open your Unity project
2. Go to **Assets > Import Package > Custom Package**
3. Select `FirebaseAuth.unitypackage`
4. Click "Import" (import everything)
5. Repeat for `FirebaseDatabase.unitypackage`
6. Unity will download dependencies automatically

**Note:** This may take 5-10 minutes. Don't interrupt the process.

## Step 8: Configure Firebase in Unity

1. In your project folder, create: `Assets/StreamingAssets/google-services-desktop.json`
2. Paste the Firebase config from Step 2:

```json
{
  "project_info": {
    "project_id": "your-project"
  },
  "client": [
    {
      "api_key": [
        {
          "current_key": "YOUR_API_KEY"
        }
      ],
      "services": {
        "appinvite_service": {
          "other_platform_oauth_client": []
        }
      }
    }
  ],
  "configuration_version": "1"
}
```

**For WebGL:** Create `Assets/StreamingAssets/firebase-config.json`:

```json
{
  "apiKey": "YOUR_API_KEY",
  "authDomain": "your-project.firebaseapp.com",
  "databaseURL": "https://your-project.firebaseio.com",
  "projectId": "your-project",
  "storageBucket": "your-project.appspot.com",
  "messagingSenderId": "123456789",
  "appId": "1:123456789:web:abcdef"
}
```

## Step 9: Test Firebase Connection

1. Open `Assets/Scripts/Firebase/FirebaseManager.cs`
2. Find the `TEST_MODE` constant and set it to `false`
3. Run the game in Unity Editor
4. Check Console for: "✅ Firebase initialized successfully"
5. If errors appear, check:
   - Config files are in `StreamingAssets`
   - Firebase packages imported correctly
   - Internet connection active

## Step 10: WebGL Build Settings

For WebGL builds with Firebase:

1. Go to **Edit > Project Settings > Player**
2. Select **WebGL** platform
3. Under **Publishing Settings**:
   - Compression Format: **Brotli** (smaller files)
   - Data Caching: **Enabled**
4. Under **Resolution and Presentation**:
   - Default Canvas Width: **1920**
   - Default Canvas Height: **1080**
   - Run in Background: **Enabled** (for idle mechanics)

## Step 11: Firebase Hosting (PWA Deployment)

To deploy your WebGL build as a PWA:

1. Install Firebase CLI:
   ```bash
   npm install -g firebase-tools
   ```

2. Login:
   ```bash
   firebase login
   ```

3. In your Unity project root:
   ```bash
   firebase init hosting
   ```

4. Select your Firebase project
5. Set public directory: `Build/WebGL`
6. Configure as single-page app: **Yes**
7. Set up automatic builds: **No**

8. Build your Unity WebGL project to `Build/WebGL`

9. Deploy:
   ```bash
   firebase deploy --only hosting
   ```

10. Your game is now live at: `https://your-project.web.app`

## Testing Without Firebase (Mock Mode)

The game can run in TEST_MODE for local development:

1. In `FirebaseManager.cs`, set `TEST_MODE = true`
2. Mock data will be used instead of real Firebase
3. All gameplay works, but data doesn't persist between sessions
4. Good for testing game mechanics before Firebase is set up

## Troubleshooting

### "Firebase initialization failed"
- Check `google-services-desktop.json` exists in `StreamingAssets`
- Verify JSON syntax is valid
- Ensure internet connection

### "Authentication failed"
- Check Google sign-in is enabled in Firebase Console
- Verify `authDomain` in config is correct
- For WebGL: Check browser allows third-party cookies

### "Database permission denied"
- Check security rules in Firebase Console
- Ensure user is authenticated before database access
- Verify database URL is correct

### WebGL build is huge (>100MB)
- Use Brotli compression
- Enable "Strip Engine Code" in Player Settings
- Use Addressables for cat/furniture assets (lazy loading)

## Next Steps

Once Firebase is set up:

1. ✅ Test Google Sign-In
2. ✅ Adopt your first cat
3. ✅ Close and reopen - data should persist!
4. ✅ Test offline progression (close for 1 hour, reopen)
5. ✅ Deploy to Firebase Hosting
6. ✅ Test on mobile browser
7. ✅ Set up PWA "Add to Home Screen"

## Support

- [Firebase Unity Docs](https://firebase.google.com/docs/unity/setup)
- [Firebase WebGL Limitations](https://firebase.google.com/docs/unity/webgl-support)
- Check console logs for detailed error messages

---

**🎉 You're ready to go!** Run the game and start adopting cats!
