using UnityEngine;

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
public class UnityConsoleHandler : BaseLogHandler
{
    public override void Log(LogData data)
    {
        switch (data.logType)
        {
            case LogType.Log:
                if (data.context == null)
                    Debug.Log(data.message);
                else
                    Debug.Log(data.message, data.context);
                break;
            case LogType.Warning: 
                if (data.context == null)
                    Debug.LogWarning(data.message);
                else
                    Debug.LogWarning(data.message, data.context);
                break;
            case LogType.Error:
                if (data.context == null)
                    Debug.LogError(data.message);
                else
                    Debug.LogError(data.message, data.context); 
                break;
            case LogType.Assert:
                if (data.context == null)
                    Debug.LogAssertion(data.message);
                else
                    Debug.LogAssertion(data.message, data.context);
                break;
            case LogType.Exception:
                if (data.message is System.Exception ex)
                {
                    if (data.context == null)
                    {
                        Debug.LogException(ex);
                    }
                    else
                        Debug.LogException(ex, data.context);
                }
                else
                {
                    string ex_msg = "LogException called with a non-exception message: message follows \n " + data.message;
                    if (data.context == null)
                        Debug.LogError(ex_msg);
                    else
                        Debug.LogError(ex_msg, data.context);
                }
                break;
            default:
                string msg = "Invalid log type used, faling over to LogError. " + data.message;
                if (data.context == null)
                    Debug.LogError(msg);
                else
                    Debug.LogError(msg, data.context);
                break;
        }
    }
}
