using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Quest_Data_Builder.Logger
{
    public enum LogLevel
    {
        Misc = 3,
        Info = 2,
        Warn = 1,
        Error = 0,
        Text = -1,
    }

    internal static class CustomLogger
    {
        public static LogLevel Level { get; set; } = LogLevel.Warn;

        public static bool LogToFile { get; set; } = false;

        public static ConcurrentBag<Exception> Errors { get; set; } = new();

        private static readonly ConcurrentQueue<string> _logQueue = new();
        private static readonly CancellationTokenSource _cts = new();
        private static readonly Task _logWriterTask = Task.Run(() => LogWriterLoop(_cts.Token));

        public static void WriteLine(LogLevel level, string str)
        {
            if (Level >= level)
            {
                Console.WriteLine(str);
                if (LogToFile)
                {
                    foreach (var line in str.Split('\n'))
                    {
                        _logQueue.Enqueue($"{DateTime.Now}: [{level}] {line}\n");
                    }
                }
            }
        }

        private static void LogWriterLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                while (_logQueue.TryDequeue(out var logLine))
                {
                    File.AppendAllText("log.txt", logLine);
                }
                Thread.Sleep(100);
            }
        }

        public static void ClearLogFile()
        {
            try
            {
                File.WriteAllText("log.txt", string.Empty);
            }
            catch (Exception ex)
            {
                LogToFile = false;
                Console.WriteLine($"Error clearing log file: {ex.Message}");
            }
        }

        public static void RegisterErrorException(Exception exception)
        {
            Errors.Add(exception);
        }

        public static void Shutdown()
        {
            _cts.Cancel();
            _logWriterTask.Wait();

            while (_logQueue.TryDequeue(out var logLine))
            {
                File.AppendAllText("log.txt", logLine);
            }
        }
    }
}
