using System.Collections.Generic;
using System.Threading.Tasks;
using BaristaLabs.ChromeDevTools.Runtime;
using Core.Chrome;
using Microsoft.Extensions.Logging;

namespace Core.LogExtractor
{
    public class LogExtractor : ILogExtractor
    {
        private readonly IChromeFrameManager _frameManager;
        private readonly ILogger<LogExtractor> _logger;

        public LogExtractor( IChromeFrameManager frameManager, ILogger<LogExtractor> logger )
        {
            _frameManager = frameManager;
            _logger = logger;
        }

        public async Task<List<string>> Extract( ChromeSession session, int chromeRemoteDebuggerPort )
        {
            _logger.LogInformation( $"Starting log extraction from session on {chromeRemoteDebuggerPort} port" );

            List<string> logs = new List<string>();

            logs.AddRange( await ExtractLogs( session ) );
            logs.AddRange( await ExtractExceptions( session ) );

            _logger.LogInformation( $"Extracted page logs from session on {chromeRemoteDebuggerPort} port, " +
                $"starting iframe log extraction" );

            ChromeBrowserFrame[] iframes = _frameManager.GetFramesByType( chromeRemoteDebuggerPort, "iframe" ).ToArray();

            foreach ( var iframe in iframes )
            {
                using ( var iframeSession = new ChromeSession( iframe.WebSocketDebuggerUrl ) )
                {
                    logs.AddRange( await ExtractLogs( iframeSession ) );
                    logs.AddRange( await ExtractExceptions( iframeSession ) );
                }
            }

            _logger.LogInformation( $"Finished log extraction from session on {chromeRemoteDebuggerPort} port" );

            return logs;
        }

        private async Task<List<string>> ExtractLogs( ChromeSession session )
        {
            List<string> logs = new List<string>();

            BaristaLabs.ChromeDevTools.Runtime.Log.LogAdapter logAdapter = new BaristaLabs.ChromeDevTools.Runtime.Log.LogAdapter( session );
            logAdapter.SubscribeToEntryAddedEvent( log =>
            {
                logs.Add( $"{ log.Entry.Url ?? ""}: {log.Entry.Text ?? "no error description"}" );
            } );
            await logAdapter.Enable( new BaristaLabs.ChromeDevTools.Runtime.Log.EnableCommand() );

            return logs;
        }

        private async Task<List<string>> ExtractExceptions( ChromeSession session )
        {
            List<string> exceptions = new List<string>();
            
            BaristaLabs.ChromeDevTools.Runtime.Runtime.RuntimeAdapter runtimeAdapter = new BaristaLabs.ChromeDevTools.Runtime.Runtime.RuntimeAdapter( session );
            runtimeAdapter.SubscribeToExceptionThrownEvent( exception =>
            {
                exceptions.Add( $"{ exception.ExceptionDetails.Text ?? ""  } " +
                                $"{ exception.ExceptionDetails.Exception.Value ?? "" } " +
                                $"{ exception.ExceptionDetails.Exception.Description ?? "" }" );
            } );
            await runtimeAdapter.Enable( new BaristaLabs.ChromeDevTools.Runtime.Runtime.EnableCommand() );

            return exceptions;
        }
    }
}
