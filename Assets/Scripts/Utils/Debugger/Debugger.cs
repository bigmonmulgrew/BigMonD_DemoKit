using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UI;
using System.Diagnostics;
using static Utils.DebuggerConfig; // Allows properties to be called as if they belong to this object

using Debug = UnityEngine.Debug;


namespace Utils
{
    public class Debugger : MonoBehaviour
    {
        
        #region Statics
        public static Debugger Instance;
        //static DebuggerSettings settings;
        static readonly Dictionary<Type, int> logLevelCache = new();
        
        private static Dictionary<int, StreamWriter> logWriters = new();
        private const int COMBINED_LOG_KEY = -1; // Must not overlap with index of LogType
        #endregion

        #region Cached references
        Canvas canvas;
        #endregion

        #region Runtime Variables
        
        List<(Text text, float clearTime)> logEntries = new();
        private RectTransform scrollContent;
        #endregion

        /// <summary>
        /// Initializes the debugger at runtime after the scene has loaded.
        /// </summary>
        /// <remarks>This method is automatically invoked by Unity after the scene has loaded, as
        /// specified by the  <see cref="RuntimeInitializeOnLoadMethodAttribute"/> with the <see
        /// cref="RuntimeInitializeLoadType.AfterSceneLoad"/> parameter. It ensures that a singleton instance of the
        /// <c>Debugger</c> class is created and persists across scene loads.  The method creates a GameObject named
        /// "Utilities: Debugger" with a <c>Debugger</c> component attached,  and marks it as non-destroyable on scene
        /// loads. Additionally, it sets up a child Canvas GameObject configured  for screen space overlay rendering,
        /// with a resolution scaling mode, default of 1080p.</remarks>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void RuntimeInit()
        {
            if (Instance != null) return;

            // Create singleton debugger object
            GameObject debuggerGameObject = new GameObject("Utilities: Debugger");
            Instance = debuggerGameObject.AddComponent<Debugger>();
            DontDestroyOnLoad(debuggerGameObject);

            // Create a canvas and set it to be the child of the Debugger GameObject
            // Add canvas with screen space overlay, sort order 1000
            // Add a CanvasScaler component to the canvas and set the UI Scale Mode to Scale With Screen Size to 1080p
            // Add a GraphicRaycaster component to the canvas
            Instance.canvas = new GameObject("DebuggerCanvas").AddComponent<Canvas>();
            Instance.canvas.transform.SetParent(debuggerGameObject.transform);
            Instance.canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler canvasScaler = Instance.canvas.gameObject.AddComponent<CanvasScaler>();
            canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            canvasScaler.referenceResolution = ScreenResolution;
            canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            canvasScaler.matchWidthOrHeight = 0.5f;

            Instance.canvas.gameObject.AddComponent<GraphicRaycaster>();

            // Add a scrollable area to the canvas.


        }
        private void Awake()
        {
            EnforceSingleton();
        }
        /// <summary>
        /// Ensures that only one instance of the class exists in the scene.
        /// </summary>
        /// <remarks>If another instance of the class already exists, the current instance is destroyed. 
        /// Otherwise, the current instance is set as the singleton instance and marked to persist  across scene
        /// loads.</remarks>
        private void EnforceSingleton()
        {

            if (Instance != null && Instance != this)
            {
                Destroy(this.gameObject);
            }
            else
            {
                Instance = this;
                DontDestroyOnLoad(this.gameObject);
            }
        }
        private void Update()
        {
            ClearEntries();
        }
        private void ClearEntries()
        {
            if (logEntries.Count == 0) return;

            float currentTime = Time.time;

            foreach (var entry in logEntries)
            {
                if (entry.clearTime <= currentTime)
                {
                    // clear text first to avoid flicker
                    entry.text.text = "";
                    // Destroy the text object, then remove from list
                    Destroy(entry.text.gameObject);
                    logEntries.Remove(entry);
                    break;
                }
            }
        }
        public static void Log(object message, UnityEngine.Object  context = null, LogType logType = LogType.Log, int logLevel = 2)
        {
            if (Instance == null) return;

            if (!ShouldLog(logLevel)) return;

            Instance.SendUnityDebugMessage(message, context, logType);
            Instance.SendDebugInfoToFile(message, context, logType, logLevel);
            Instance.SendDebugInfoToScreen(message, context, logType);
            Instance.SendDebugInfoToRemote(message, context, logType, logLevel);
        }
        public static void LogWarning(object message, UnityEngine.Object context = null, int logLevel = 2)          => Log(message, context, LogType.Warning, logLevel);
        public static void LogError(object message, UnityEngine.Object context = null, int logLevel = 2)            => Log(message, context, LogType.Error, logLevel);
        public static void LogException(Exception exception, UnityEngine.Object context = null, int logLevel = 2)   => Log(exception, context, LogType.Exception, logLevel);
        public static void LogAssertion(object message, UnityEngine.Object context = null, int logLevel = 2)        => Log(message, context, LogType.Assert, logLevel);
        #region Overloads
        public static void Log(object message)                  => Log(message, null, LogType.Log, (int)LogLevel.Common);
        public static void LogWarning(object message)           => Log(message, null, LogType.Warning, (int)LogLevel.Common);
        public static void LogError(object message)             => Log(message, null, LogType.Error, (int)LogLevel.Common);
        public static void LogException(Exception exception)    => Log(exception, null, LogType.Exception, (int)LogLevel.Common);
        public static void LogAssertion(object message)         => Log(message, null, LogType.Assert, (int)LogLevel.Common);

        public static void Log(object message, UnityEngine.Object context)               => Log(message, context, LogType.Log, (int)LogLevel.Common);
        public static void LogWarning(object message, UnityEngine.Object context)        => Log(message, context, LogType.Warning, (int)LogLevel.Common);
        public static void LogError(object message, UnityEngine.Object context)          => Log(message, context, LogType.Error, (int)LogLevel.Common);
        public static void LogException(Exception exception, UnityEngine.Object context) => Log(exception, context, LogType.Exception, (int)LogLevel.Common);
        public static void LogAssertion(object message, UnityEngine.Object context)      => Log(message, context, LogType.Assert, (int)LogLevel.Common);

        public static void Log(object message, int logLevel) => Log(message, null, LogType.Log, logLevel);
        public static void LogWarning(object message, int logLevel) => Log(message, null, LogType.Warning, logLevel);
        public static void LogError(object message, int logLevel) => Log(message, null, LogType.Error, logLevel);
        public static void LogException(Exception exception, int logLevel) => Log(exception, null, LogType.Exception, logLevel);
        public static void LogAssertion(object message, int logLevel) => Log(message, null, LogType.Assert, logLevel);

        public static void Log<T>(T obj, int logLevel = (int)LogLevel.Common) => Log(obj?.ToString(), null, LogType.Log, logLevel);
        public static void LogWarning<T>(T obj, int logLevel = (int)LogLevel.Common) => Log(obj?.ToString(), null, LogType.Warning, logLevel);
        public static void LogError<T>(T obj, int logLevel = (int)LogLevel.Common) => Log(obj?.ToString(), null, LogType.Error, logLevel);
        public static void LogAssertion<T>(T obj, int logLevel = (int)LogLevel.Common) => Log(obj?.ToString(), null, LogType.Assert, logLevel);
        public static void LogException<T>(T obj, int logLevel = (int)LogLevel.Common) => Log(obj?.ToString(), null, LogType.Exception, logLevel);

        public static void LogException(string message, Exception ex, UnityEngine.Object context = null) => Log(message + "\n" + ex, context, LogType.Exception, (int)LogLevel.Common);
        #endregion
        private static bool ShouldLog(int messageLogLevel)
        {
            // Determine caller type (skip this method and Log())
            var frame = new System.Diagnostics.StackTrace().GetFrame(2);
            Type callerType = frame?.GetMethod()?.DeclaringType;

            if (callerType == null)
            {
                Debug.LogError("Unable to determine class of calling function.");
                return true; // fallback, allow log
            }

            // Look up cached value
            if (!logLevelCache.TryGetValue(callerType, out int effectiveLevel))
            {
                effectiveLevel = GetClassLogLevel(callerType);
                logLevelCache[callerType] = effectiveLevel;
            }

            return messageLogLevel <= effectiveLevel;
        }
        private static int GetClassLogLevel(Type type)
        {
            int level = EffectiveGlobalLogLevel;

            // Look for const/static fields named LOG_LEVEL
            var field = type.GetField("LOG_LEVEL",
                BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.Static | BindingFlags.FlattenHierarchy);

            if (field != null)
            {
                if (field.FieldType == typeof(int))
                    level = (int)field.GetRawConstantValue();
                else if (field.FieldType == typeof(LogLevel))
                    level = (int)(LogLevel)field.GetRawConstantValue();
                else
                    Debug.LogWarning("Field called LOG_LEVEL of incorrect type found, please use int or LogLevel, or if an unrelated variable rename to prevent reflection issues.");
            }

            return level;
        }
        /// <summary>
        /// Logs a message to the Unity console with the specified log type and optional context.
        /// </summary>
        /// <remarks>This method acts as a wrapper around Unity's <see cref="Debug.Log"/>, <see
        /// cref="Debug.LogWarning"/>,  <see cref="Debug.LogError"/>, <see cref="Debug.LogAssertion"/>, and <see
        /// cref="Debug.LogException"/> methods,  providing a unified interface for logging messages with different log
        /// types and optional context. <para> If <paramref name="logType"/> is <see cref="LogType.Exception"/>, the
        /// <paramref name="message"/> parameter  must be an <see cref="System.Exception"/> object. If it is not, an
        /// error message will be logged instead. </para></remarks>
        /// <param name="message">The message to log. This can be any object, and its string representation will be logged.</param>
        /// <param name="context">An optional Unity <see cref="Object"/> that provides context for the log message.  If specified, the message
        /// will be associated with this object in the Unity Editor.</param>
        /// <param name="logType">The type of log message to send. This determines how the message is categorized in the Unity console  (e.g.,
        /// <see cref="LogType.Log"/>, <see cref="LogType.Warning"/>, <see cref="LogType.Error"/>).</param>
        private void SendUnityDebugMessage(object message, UnityEngine.Object context = null, LogType logType = LogType.Log)
        {
            // Treating this as a wrapper for Unity's Debug.Log methods, adding context and log type handling.
            // Not using Ilogger directly to avoid needing to manage ILogger instance and maintain Debug.Log compatibility.

            switch (logType)
            {
                case LogType.Error:
                    if (context == null)
                        Debug.LogError(message);
                    else
                        Debug.LogError(message, context);
                    break;
                case LogType.Assert:
                    if (context == null)
                        Debug.LogAssertion(message);
                    else
                        Debug.LogAssertion(message, context);
                    break;
                case LogType.Warning:
                    if (context == null)
                        Debug.LogWarning(message);
                    else
                        Debug.LogWarning(message, context);
                    break;
                case LogType.Log:
                    // if context is null use Debug.Log, else use Debug.Log with context
                    if(context == null)
                        Debug.Log(message);
                    else
                        Debug.Log(message, context);
                    break;
                case LogType.Exception:
                    if (message is System.Exception ex)
                    {
                        if(context == null) { 
                            Debug.LogException(ex);
                        }
                        else
                            Debug.LogException(ex, context);
                    }
                    else
                    {
                        if (context == null)
                            Debug.LogError("LogException called with a non-exception message: message follows \n " + message);
                        else
                            Debug.LogError("LogException called with a non-exception message: message follows \n " + message, context);
                    }
                    break;
                default:
                    Debug.Log(message, context);
                    break;
            }

        }
        private void SendDebugInfoToFile(object message, UnityEngine.Object context = null, LogType logType = LogType.Log, int logLevel = 2)
        {
            if (!EnableFileLogging) return;

            EnsureLogFileInitialized();

            var logEntry = new LogEntry
            {
                timestamp = DateTime.UtcNow.ToString("o"),
                type = logType.ToString(),
                level = logLevel,
                context = context ? context.name : null,
                message = message?.ToString()
            };

            string json = JsonUtility.ToJson(logEntry);

            if (SingleCombinedLog)
            {
                logWriters[COMBINED_LOG_KEY]?.WriteLine(json);
            }
            else
            {
                if (logWriters.TryGetValue((int)logType, out var writer))
                {
                    writer.WriteLine(json);
                }
            }
        }
        private void SendDebugInfoToScreen(object message, UnityEngine.Object context = null, LogType logType = LogType.Log)
        {
            if (!EnableScreenLogging) return;
            if (canvas == null) return;

            SetupScreenLogger();

            GameObject textGO = new GameObject("LogEntry", typeof(RectTransform), typeof(Text));
            textGO.transform.SetParent(scrollContent, false);

            RectTransform rt = textGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            Text textComp = textGO.GetComponent<Text>();
            textComp.text = message?.ToString();
            textComp.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            textComp.fontSize = FontSize;
            textComp.horizontalOverflow = HorizontalWrapMode.Wrap;
            textComp.verticalOverflow = VerticalWrapMode.Overflow;

            switch (logType)
            {
                case LogType.Warning:   textComp.color = Color.yellow; break;
                case LogType.Error:     textComp.color = Color.red; break;
                case LogType.Exception: textComp.color = new Color(0.8f, 0.2f, 0, 1); break;
                case LogType.Assert:    textComp.color = Color.magenta; break;
                default: textComp.color = Color.white; break;
            }

            logEntries.Add((textComp, Time.time + ScreenShowTime));
        }
        private async void SendDebugInfoToRemote(object message, UnityEngine.Object context = null, LogType logType = LogType.Log, int logLevel = 2)
        {
            // If settings explicitly disable remote logging, bail out
            if (!EnableRemoteLogging) return;

            string endpoint = RemoteEndpoint;
            if (string.IsNullOrEmpty(endpoint)) return;

            var logEntry = new LogEntry
            {
                timestamp = DateTime.UtcNow.ToString("o"),
                type = logType.ToString(),
                level = logLevel,
                context = context ? context.name : null,
                message = message?.ToString(),
                stacktrace = new StackTrace(2, true).ToString()
            };

            string json = JsonUtility.ToJson(logEntry);

            try
            {
                using var request = new UnityEngine.Networking.UnityWebRequest(endpoint, "POST");
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");
                request.timeout = 10;       // Keeping short since debugs can potentially be triggered multiple times every frame. System Not feasible on very slow networks.

                var operation = request.SendWebRequest();
                while (!operation.isDone)
                    await System.Threading.Tasks.Task.Yield();

                if (request.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning("Remote log failed: " + request.error);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning("Remote log exception: " + ex.Message);
            }
        }
        private static void EnsureLogFileInitialized()
        {
            // Early exist if alreeady initialised
            if (logWriters.Count > 0) return;

            // Exit early if disabled file logging
            if (!EnableFileLogging) return;

            string basePath = LogFilePath;
            Directory.CreateDirectory(basePath);

            string fileName = LogFileName;

            if (SingleCombinedLog)
            {
                CreateStreamWriterForLogType(basePath, fileName);
            }
            else
            {
                foreach (LogType type in Enum.GetValues(typeof(LogType)))
                {
                    CreateStreamWriterForLogType(basePath, fileName, (int)type);
                }
            }
        }
        private static void CreateStreamWriterForLogType(string basePath, string fileName, int type = COMBINED_LOG_KEY)
        {
            string suffix1 = type switch
            {
                (int)LogType.Log => "_info",
                (int)LogType.Warning => "_warning",
                (int)LogType.Error => "_error",
                (int)LogType.Assert => "_assert",
                (int)LogType.Exception => "_exception",
                _ => ""
            };

            string suffix2 = "";
            switch (StorageStrategy)
            {
                case LogStorage.Monolithic:
                    break;
                case LogStorage.CurrentSessionOnly:
                    break;

                case LogStorage.Generational: suffix2 = "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"); break;

                case LogStorage.KeepPrevious:
                    string newlogFilePath = Path.Combine(basePath, $"{fileName}{suffix1}.jsonl");
                    string prevPath = Path.Combine(basePath, $"{fileName}{suffix1}" + "_previous.jsonl");

                    if (File.Exists(prevPath))
                        File.Delete(prevPath);
                    if (File.Exists(newlogFilePath))
                        File.Move(newlogFilePath, prevPath);

                    break;
            }

            bool append = (StorageStrategy) switch
            {
                LogStorage.CurrentSessionOnly => false,
                LogStorage.KeepPrevious => false,
                _ => true
            };

            string path = Path.Combine(basePath, $"{fileName}{suffix1}{suffix2}.jsonl");
            var writer = new StreamWriter(path, append: append, Encoding.UTF8) { AutoFlush = true };
            logWriters[type] = writer;
            
        }
        private void SetupScreenLogger()
        {
            // Skip if already setup
            if (scrollContent != null) return;

            // Create ScrollRect object
            GameObject scrollGO = new GameObject("LogScrollRect", typeof(RectTransform), typeof(CanvasRenderer));
            scrollGO.transform.SetParent(canvas.transform, false);
            RectTransform scrollRectTransform = scrollGO.GetComponent<RectTransform>();
            scrollRectTransform.anchorMin = new Vector2(0, 0);
            scrollRectTransform.anchorMax = new Vector2(1, 1);
            scrollRectTransform.offsetMin = new Vector2(10, 10);
            scrollRectTransform.offsetMax = new Vector2(-10, -10);

            ScrollRect scrollRect = scrollGO.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;

            // Add viewport
            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollGO.transform, false);
            RectTransform viewportRT = viewport.GetComponent<RectTransform>();
            viewportRT.anchorMin = Vector2.zero;
            viewportRT.anchorMax = Vector2.one;
            viewportRT.offsetMin = Vector2.zero;
            viewportRT.offsetMax = Vector2.zero;
            scrollRect.viewport = viewportRT;
            viewport.GetComponent<Image>().color = new Color(0, 0, 0, 0.25f); // semi-transparent background
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            // Add content container
            GameObject content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            scrollContent = content.GetComponent<RectTransform>();
            scrollContent.anchorMin = new Vector2(0, 1);
            scrollContent.anchorMax = new Vector2(1, 1);
            scrollContent.pivot = new Vector2(0.5f, 1);
            scrollContent.offsetMin = Vector2.zero;
            scrollContent.offsetMax = Vector2.zero;

            VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.spacing = 4f;

            ContentSizeFitter fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.content = scrollContent;
        }
        private void OnDestroy()
        {
            foreach (var writer in logWriters.Values)
            {
                writer.Dispose();
            }
            logWriters.Clear();
        }
    }
}


