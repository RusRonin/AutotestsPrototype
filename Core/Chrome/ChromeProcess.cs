using System.Diagnostics;
using System.IO;

namespace Core.Chrome
{
    public class ChromeProcess
    {
        public int RemoteDebuggingPort { get; private set; } = -1;
        private Process process = null;

        public void StartChrome(int remoteDebuggingPort = 9222)
        {
            RemoteDebuggingPort = remoteDebuggingPort;
            string path = Path.GetRandomFileName();
            var directoryInfo = Directory.CreateDirectory( Path.Combine( Path.GetTempPath(), path ) );
            string launchArgs = $"--remote-debugging-port={RemoteDebuggingPort} --user-data-dir=\"{directoryInfo.FullName}\"";
            process = Process.Start( new ProcessStartInfo( @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe", launchArgs ) );
        }

        public void StopChrome()
        {
            if ( process != null )
            {
                process.Kill();                               
                process = null;
            }
        }
    }
}
