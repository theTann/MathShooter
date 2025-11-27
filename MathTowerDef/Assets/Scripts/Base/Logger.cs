// 플레이 상태가 아닐때에도 timeline같은 곳에서 logger를 불러서 익셉션이 나기도한다.
using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;

public enum LogTyoe {
    debug, info, warning, error,
}

struct LogMessage {
    public LogTyoe type;
    public string tag;
    public string timeStr;
    public object message;

    public string makeStr() {
        string str = $"[{timeStr}] ({type}){message}";
        if (string.IsNullOrEmpty(tag) != false) {
            str = $"[{tag}] {str}";
        }
        return str;
    }
}

public interface ICustomLogger {
    public void debug(object message);
    public void info(object message);
    public void warning(object message);
    public void error(object message);
}

public class Logger {
    private static ConcurrentQueue<LogMessage> _logQueue = new();
    private static string _tag;
    private static string _logFilePath;
    private static Thread _backgroundThread;
    private static readonly AutoResetEvent _flushSignal = new(false);
    private static readonly CancellationTokenSource _cts = new();
    private static ICustomLogger _custumLogger;

    public static void init(string tag, ICustomLogger customLogger) {
        _tag = tag;
        _logFilePath = $"{DateTime.Now:yyyy-MM-dd_HH.mm.ss.fff}.log";
        _backgroundThread = new Thread(flushLogQueue) {
            IsBackground = true,
            Name = "LoggerBackgroundThread"
        };
        _backgroundThread.Start();
        _custumLogger = customLogger;
    }

    public static void destroy() {
        _cts.Cancel();
        _flushSignal.Set(); // 종료 시 강제 플러시
        _backgroundThread.Join();
        _flushSignal.Dispose();
        _cts.Dispose();
        _logQueue = null;
    }

#if UNITY_EDITOR
    [UnityEngine.HideInCallstack]
#endif
    [Conditional("LOG_ENABLE")]
    public static void debug(object message) {
        _logQueue.Enqueue(new LogMessage() {
            type = LogTyoe.debug,
            timeStr = getTimeStr(),
            message = message,
            tag = _tag,
        });

        _flushSignal.Set();
        _custumLogger?.debug(message);
    }

#if UNITY_EDITOR
    [UnityEngine.HideInCallstack]
#endif
    [Conditional("LOG_ENABLE")]
    public static void info(object message) {
        _logQueue.Enqueue(new LogMessage() {
            type = LogTyoe.info,
            timeStr = getTimeStr(),
            message = message,
            tag = _tag,
        });

        _flushSignal.Set();
        _custumLogger?.debug(message);
    }

#if UNITY_EDITOR
    [UnityEngine.HideInCallstack]
#endif
    [Conditional("LOG_ENABLE")]
    public static void warn(object message) {
        _logQueue.Enqueue(new LogMessage() {
            type = LogTyoe.warning,
            timeStr = getTimeStr(),
            message = message,
            tag = _tag,
        });

        _flushSignal.Set();
        _custumLogger?.warning(message);
    }

#if UNITY_EDITOR
    [UnityEngine.HideInCallstack]
#endif
    public static void error(object message) {
        _logQueue.Enqueue(new LogMessage() {
            type = LogTyoe.error,
            timeStr = getTimeStr(),
            message = message,
            tag = _tag,
        });

        _flushSignal.Set();
        _custumLogger?.error(message);
    }

    public static void flushLogQueue() {
        using var writer = new StreamWriter(_logFilePath, append: true, Encoding.UTF8);
        while (!_cts.Token.IsCancellationRequested) {
            _flushSignal.WaitOne(1000); // 1초마다 또는 강제 신호로 체크
            while (_logQueue.TryDequeue(out var logLine)) {
                writer.WriteLine(logLine.makeStr());
            }
            writer.Flush();
        }

        // 종료 시 남은 로그도 처리
        while (_logQueue.TryDequeue(out var logLine)) {
            writer.WriteLine(logLine.makeStr());
        }

        writer.Flush();
    }

    public static string getTimeStr() {
        return $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}";
    }
}
