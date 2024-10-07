using System;
using System.IO;

namespace realtimeLogic
{
    public class Logger
    {
        private static Logger _instance;
        private readonly string _filePath;
        private readonly string _fileName;
        public event EventHandler<LogEventArgs> LogEvent;

        private Logger()
        {
            // Create a file name organized by date, time, and the log file name
            //_fileName = "log_" + DateTime.Now.ToString("yyyy-MM-dd-HH-mm") + ".txt";
            // Create temp directory if it doesn't exist
            if (!Directory.Exists(Path.GetTempPath()))
            {
                Directory.CreateDirectory(Path.GetTempPath());
            }

            // Get temp folder name
            _filePath = Path.GetTempPath() + DateTime.Now.ToString("yyyy-MM-dd-HH-mm") + ".txt";

            // write the file name to the file
            File.WriteAllText(_filePath, Environment.NewLine);
            // write the current date to the file
            string time = DateTime.Now.ToString();
            File.AppendAllText(_filePath, time + " | Debugging started" + Environment.NewLine);
        }

        public static Logger GetInstance()
        {
            if (_instance == null)
            {
                _instance = new Logger();
            }
            return _instance;
        }

        public void Log(object sender, string message)
        {
            string time = DateTime.Now.ToString();
            string objectName = sender.GetType().Name;
            // write the message to the file
            File.AppendAllText(_filePath, time + " | " + objectName + " | " + message + Environment.NewLine);
            LogEvent?.Invoke(sender, new LogEventArgs(_filePath));
        }

        public string ReadLog()
        {
            // read the file and return the content
            return File.ReadAllText(_filePath);
        }

        public void ClearLog()
        {
            // clear the file
            File.WriteAllText(_filePath, string.Empty);
        }

        /// <summary>
        /// Open the log file
        /// </summary>
        public void OpenLog()
        {
            System.Diagnostics.Process.Start(_filePath);
        }

        public string GetFilePath()
        {
            return _filePath;
        }

        public void Dispose()
        {
            // delete the file
            File.Delete(_filePath);
        }
    }

    public class LogEventArgs : EventArgs
    {
        public string File { get; }

        public LogEventArgs(string file)
        {
            File = file;
        }
    }
}
