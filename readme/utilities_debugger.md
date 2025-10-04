# 🧩 Unity Debugger Framework

A **modular, extensible debugging system for Unity** that enhances the built-in `Debug.Log` pipeline with configurable outputs, file logging, remote streaming, and on-screen display — all while maintaining **near-zero performance overhead** for standard log calls.

---

## ✨ Features

- **Drop-in replacement for Unity Debug.Log**
  - Matches Unity’s native behaviour for `Log`, `Warning`, `Error`, `Assert`, and `Exception`.
  - Negligible overhead in simple console logging mode.
  - Debug.Log() becomes Debugger.Log() etc
- **Multiple output targets (handlers):**
  - 🖥️ **Unity Console** – Standard output (fastest path).  
  - 📄 **File Logger** – JSONL log files with configurable storage modes.  
  - 🌐 **Remote Logger** – HTTP/JSON streaming for external dashboards or servers.  **Defaults disabled**. Web server sample provided (see below).
  - 🧾 **Screen Logger** – Lightweight in-game overlay for on-device debugging.  
- **OOP design** – clean separation using interfaces and base classes.  
- **Extensible** – easily add custom log handlers (e.g., analytics, telemetry).  
- **Configurable via ScriptableObject** – `DebuggerSettings` and `DebuggerConfig` provide full editor and runtime control.  
- **First run pregenerated settings** - `Resources\BuiltDebuggerSettings` and `Settings\EditorDebuggerSettings` are created automatically with preconfigured defaults.
- **Thread-safe initialization** using Unity’s `[RuntimeInitializeOnLoadMethod]`.  
- **Optional update, destroy, and UI initialization hooks** for lifecycle management.

---

## 🧠 Architecture Overview

```
Debugger (entry point)
│
├── DebuggerConfig
│   └── Loads ScriptableObject settings and provides safe defaults.
│
├── BaseLogHandler
│   ├── ConvertToLogEntry() – builds full log objects when needed.
│   └── Common logic for file formatting and stacktrace trimming.
│
├── Interfaces
│   ├── ILogHandler         – basic logging interface.
│   ├── IUpdatableHandler   – optional Update loop.
│   ├── IDestroyHandler     – clean resource shutdown.
│   └── ICanvasHandler      – initialization for UI-based handlers.
│
└── Handlers
    ├── UnityConsoleHandler – native Unity logging (fast path).
    ├── FileLogHandler      – writes JSONL logs, supports multiple strategies.
    ├── RemoteLogHandler    – streams logs to an HTTP endpoint.
    └── ScreenLogHandler    – renders live logs in a UI canvas.
```

---

## 🏗️ Design Principles

| Principle | Implementation |
|------------|----------------|
| **Abstraction** | Handlers are accessed through a unified `ILogHandler` interface. |
| **Encapsulation** | Each handler manages its own state and resources internally. |
| **Inheritance** | Shared conversion and helper logic in `BaseLogHandler`. |
| **Polymorphism** | `Debugger` invokes handlers generically without knowing their types. |
| **SOLID** | Clean separation of responsibilities and open/closed extensibility. |

---

## ⚙️ Setup

1. **Import the package** into your Unity project.  
2. (Optional) Create a **`DebuggerSettings` ScriptableObject**:  
   - Right-click → *Create → Debugger → Settings*.  
   - Configure log levels, output options, and storage strategy. 
   - Open `DebuggerSettingsInitializer` and change the *SETTINGS_FILE_NAME* for editor settings and *RESOURCES_FILE_NAME* for build settings.
3. IMPORTANT: For build settings place the asset under `Assets/Resources` so it’s available in builds.  

---

## 🧩 Basic Usage

Replace Unity’s `Debug.Log` calls with:

```csharp
using Utils; // Namespace containing Debugger

Debugger.Log("Hello World!");
Debugger.LogWarning("Potential issue detected");
Debugger.LogError("Something went wrong", this);
Debugger.LogAssertion("Unexpected value", this);
Debugger.LogException(new InvalidOperationException("Invalid state"));
```

> ⚡ For basic use, this behaves exactly like `Debug.Log` — no stack traces, no file IO — just console output.

---

## 🪄 Extended Usage

Enable additional handlers in your settings:

- **File Logging:**  
  Writes structured JSON lines (`.jsonl`) with timestamp, context, log level, and stacktrace.

- **Remote Logging:**  
  POSTs JSON payloads to a server endpoint (Flask, FastAPI, etc.).  
  Ideal for dashboards or live debug viewers.
  Web server demo app provided [here](https://github.com/bigmonmulgrew/UnityRemoteLogServer)

- **Screen Logging:**  
  Displays logs in-game. Useful for VR/AR and mobile builds where a console isn’t visible.

All handlers automatically register with `Debugger` and are updated each frame when needed.

---

## 🧱 Example Log Entry (JSONL)

```json
{
  "timestamp": "2025-10-03T12:45:10.239Z",
  "type": "Error",
  "level": 2,
  "context": "PlayerController",
  "message": "NullReferenceException: object reference not set",
  "stacktrace": "ObjectController:Update() (at Assets/Scripts/ObjectController.cs:45)"
}
```

---

## 🛠️ Extending the System

Create your own handler by implementing the interface:

```csharp
public class CustomNetworkHandler : BaseLogHandler
{
    public override void Log(LogData data)
    {
        var entry = ConvertToLogEntry(data, includeStackTrace: true);
        // Custom network upload logic here
    }
}
```

Add it to the handler list in `Debugger.cs` and it will automatically receive logs.

---

## 📚 Logging Lifecycle Interfaces

| Interface | Purpose |
|------------|----------|
| `ILogHandler` | Basic interface for log processing. Part of the log handler base class. |
| `IUpdatableHandler` | For handlers needing `OnUpdate()` each frame. |
| `IDestroyHandler` | Cleanup logic (close files, stop threads). |
| `ICanvasHandler` | For handlers that create UI (e.g., `ScreenLogHandler`). |

These are all automatically called from the `Debugger` loop — no extra setup required.

---

## 🧩 Performance Considerations

- Standard logs (UnityConsoleHandler) match the performance of `Debug.Log`.  
- File and Remote logs only perform heavy conversions (`LogEntry`, stack trace) when enabled.  
- All handlers are modular — disable unused ones to keep performance optimal.

---

## 🧠 Future Enhancements

- ✅ Batch sending for remote logging.  
- ✅ Filtering and log categories.  
- 🕓 Variable watch window  
- 🕓 Conditional integration with text mesh pro. Legacy was used to avoid dependencies.

---

## 🧾 License

License:
Free for personal, educational, and academic use with attribution.
Commercial use requires written permission from the author.
See LICENSE file for more details
