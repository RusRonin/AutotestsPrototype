using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.Logging;

namespace Core.Chrome
{
    public class ChromeProcess
    {
        public int RemoteDebuggingPort { get; private set; } = -1;
        private Process process = null;
        private readonly ILogger<ChromeProcess> _logger;

        public ChromeProcess( ILogger<ChromeProcess> logger )
        {
            _logger = logger;
        }

        public void StartChrome( int remoteDebuggingPort = 9222 )
        {
            RemoteDebuggingPort = remoteDebuggingPort;
            string path = Path.GetRandomFileName();
            var directoryInfo = Directory.CreateDirectory( Path.Combine( Path.GetTempPath(), path ) );
            string launchArgs = $"--remote-debugging-port={RemoteDebuggingPort} --user-data-dir=\"{directoryInfo.FullName}\"";
            process = Process.Start( new ProcessStartInfo( @"C:\Program Files (x86)\Google\Chrome\Application\chrome.exe", launchArgs ) );
            _logger.LogInformation( $"Started Chrome on port {RemoteDebuggingPort}" );
        }

        public void StopChrome()
        {
            if ( process != null )
            {
                process.Kill();
                _logger.LogInformation( $"Stopped Chrome on port {RemoteDebuggingPort}" );
                process = null;
            }
        }
    }
}
