# Firebase Remote Config Setup Guide

## 🎯 Overview
Hệ thống dynamic server configuration sử dụng Firebase Remote Config để load địa chỉ server từ cloud thay vì hardcode trong Unity.

## 📦 Prerequisites

### 1. Firebase Unity SDK
Download Firebase Unity SDK từ: https://firebase.google.com/download/unity

**Packages cần import:**
- `FirebaseRemoteConfig.unitypackage`
- `FirebaseAnalytics.unitypackage` (dependency)

### 2. Firebase Project Setup

1. Tạo project tại: https://console.firebase.google.com/
2. Add Android/iOS app vào project
3. Download config files:
   - Android: `google-services.json` → `Assets/`
   - iOS: `GoogleService-Info.plist` → `Assets/`

## 🔧 Unity Setup

### Step 1: Add ServerConfigService to Scene

1. Tạo empty GameObject trong scene đầu tiên (hoặc scene main menu)
2. Đổi tên: `ServerConfigService`
3. Add component: `ServerConfigService.cs`
4. Configure trong Inspector:
   ```
   Default Server Host: localhost
   Default Server Port: 8080
   Use SSL: false
   Fetch Timeout Seconds: 10
   Enable Developer Mode: true (for testing)
   ```

5. **Important:** Đánh dấu DontDestroyOnLoad để service tồn tại qua scenes

### Step 2: Add UILoadingScreen to Canvas

1. Tạo Canvas → Panel `LoadingPanel`
2. Add component: `UILoadingScreen.cs`
3. Setup UI hierarchy:
   ```
   Canvas
   └─ LoadingPanel
      ├─ StatusText (TextMeshProUGUI)
      ├─ ProgressBar (Image with Image Type: Filled)
      └─ SpinnerImage (Image - rotating icon)
   ```

4. Link references trong Inspector

### Step 3: Update UIScreenController

Đã được update tự động trong code. Chỉ cần link `Loading Screen` reference trong Inspector.

## ☁️ Firebase Remote Config Setup

### 1. Tạo Parameters trong Firebase Console

Vào: **Firebase Console → Remote Config → Add parameter**

**Required parameters:**

| Parameter Key | Type | Default Value | Description |
|--------------|------|---------------|-------------|
| `server_host` | String | `localhost` | Server hostname/IP |
| `server_port` | Number | `8080` | Server port |
| `use_ssl` | Boolean | `false` | Use SSL/TLS |
| `maintenance_mode` | Boolean | `false` | Enable maintenance |
| `config_version` | String | `1.0.0` | Config version |
| `min_client_version` | String | `1.0.0` | Minimum client version |

### 2. Set Production Values

**For Production Server:**
```json
{
  "server_host": "your-server.com",
  "server_port": 8080,
  "use_ssl": false,
  "maintenance_mode": false,
  "config_version": "1.0.0",
  "min_client_version": "1.0.0"
}
```

**For Testing:**
```json
{
  "server_host": "192.168.1.100",
  "server_port": 8080,
  "use_ssl": false,
  "maintenance_mode": false,
  "config_version": "1.0.0",
  "min_client_version": "1.0.0"
}
```

### 3. Publish Changes

Click **"Publish changes"** trong Firebase Console để apply config mới.

## 🧪 Testing

### Test trong Unity Editor

1. Chạy game trong Editor
2. Xem Console logs:
   ```
   [ServerConfig] Firebase initialized successfully
   [ServerConfig] Remote config fetched and activated successfully
   [ServerConfig] Loaded config: your-server.com:8080
   [CLIENT] Using Firebase Remote Config: your-server.com:8080
   [CLIENT] Connected to your-server.com:8080 at HH:mm:ss
   ```

### Debug Menu (Editor Only)

Right-click `ServerConfigService` trong Inspector:
- **Test Fetch Config** - Test fetch từ Firebase
- **Print Current Config** - In ra config hiện tại

### Test với Different Configs

1. Change values trong Firebase Console
2. Click **Publish changes**
3. Restart Unity game
4. Verify new config loaded

## 🔄 Flow Diagram

```
App Start
    ↓
UIScreenController.Start()
    ↓
Show Loading Screen
    ↓
Initialize Firebase Remote Config
    ↓
Fetch server_host, server_port from Firebase
    ↓
[Success] Use Firebase config
[Failed]  Use fallback (localhost:8080)
    ↓
NetworkManager.ConnectAsync()
    ↓
Connect to server with dynamic config
    ↓
Hide Loading Screen
    ↓
Show Login/Main Menu
```

## 🛡️ Fallback Mechanism

Config load có nhiều levels của fallback:

1. **Firebase Remote Config** (Priority 1)
   - Fetch từ cloud
   - Cache locally

2. **Inspector Settings** (Priority 2)
   - Nếu set trong NetworkManager Inspector
   - Override Firebase config

3. **Default Values** (Priority 3)
   - Hardcoded trong ServerConfigService
   - localhost:8080

## 🚀 Production Deployment

### Build Settings

**Android:**
1. Build Settings → Android
2. Ensure `google-services.json` in Assets
3. Build

**iOS:**
1. Build Settings → iOS
2. Ensure `GoogleService-Info.plist` in Assets
3. Build Xcode project
4. Add GoogleService-Info.plist vào Xcode project

### Update Server Address

**Để thay đổi server address mà không rebuild app:**

1. Vào Firebase Console
2. Remote Config → Edit `server_host` / `server_port`
3. Publish changes
4. Users sẽ nhận config mới khi restart app

**Cache duration:**
- Development mode: Fetch mỗi lần
- Production: Cache 1 hour (configurable)

## 🔍 Troubleshooting

### Firebase không initialize

**Symptoms:**
```
[ServerConfig] Could not resolve Firebase dependencies
```

**Solutions:**
- Check `google-services.json` / `GoogleService-Info.plist` trong Assets
- Re-import Firebase SDK packages
- Ensure AndroidManifest.xml có Firebase configurations

### Config không fetch được

**Symptoms:**
```
[ServerConfig] Fetch failed: Network error
```

**Solutions:**
- Check internet connection
- Verify Firebase project active
- Check Remote Config có parameters chưa
- Enable Developer Mode trong ServerConfigService Inspector

### Server connection failed

**Symptoms:**
```
[CLIENT] Connection failed: No connection could be made
```

**Solutions:**
- Verify server đang chạy
- Check firewall không block port
- Verify `server_host` và `server_port` đúng
- Try ping server từ client machine

## 📊 Monitoring

### Firebase Analytics

Firebase tự động track:
- Config fetch success/failure rate
- Config activation rate
- User engagement

View tại: **Firebase Console → Analytics**

### Custom Logging

Thêm custom events:
```csharp
Firebase.Analytics.FirebaseAnalytics.LogEvent("config_loaded", new Parameter[] {
    new Parameter("server_host", configService.ServerHost),
    new Parameter("server_port", configService.ServerPort)
});
```

## 🎯 Advanced Features

### Conditions & Targeting

Trong Firebase Console, có thể set conditions:
- **Country/Region** - Different servers per region
- **App Version** - Different configs per version
- **User Properties** - Different configs per user type

Example: US users → US server, EU users → EU server

### A/B Testing

Use Firebase Remote Config A/B testing để test different server configurations.

## 📝 Best Practices

1. ✅ **Always set default values** trong code
2. ✅ **Enable caching** để reduce Firebase calls
3. ✅ **Handle fetch failures** gracefully
4. ✅ **Log config loads** for debugging
5. ✅ **Test with airplane mode** để verify fallback
6. ✅ **Version your configs** với `config_version`
7. ✅ **Use maintenance_mode** for server downtime
8. ✅ **Monitor fetch success rate** trong Firebase Analytics

## 🔐 Security

- Firebase Remote Config is **public** - không lưu sensitive data
- Server address là public info, OK to store
- Authentication tokens/passwords **KHÔNG** lưu trong Remote Config
- Use Firebase Security Rules cho Realtime DB/Firestore nếu cần private config

## 📚 References

- Firebase Remote Config Docs: https://firebase.google.com/docs/remote-config/unity
- Firebase Unity SDK: https://firebase.google.com/docs/unity/setup
- Remote Config Best Practices: https://firebase.google.com/docs/remote-config/best-practices

---

## ✅ Quick Checklist

- [ ] Firebase Unity SDK imported
- [ ] `google-services.json` / `GoogleService-Info.plist` added
- [ ] ServerConfigService GameObject in scene
- [ ] UILoadingScreen setup in Canvas
- [ ] Firebase Remote Config parameters created
- [ ] Config values set and published
- [ ] Tested in Editor
- [ ] Tested build on device
- [ ] Fallback mechanism verified

🎉 **Done!** Server address giờ được load từ Firebase và có thể update bất kỳ lúc nào!
