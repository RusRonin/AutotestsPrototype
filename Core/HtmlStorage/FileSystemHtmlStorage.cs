using System;
using System.IO;

namespace Core.HtmlStorage
{
    public class FileSystemHtmlStorage : IHtmlStorage
    {

        public string LoadHtml( int launchId, int resourceId )
        {
            string path = $"{ Directory.GetCurrentDirectory() }//PageSources//{ launchId }";

            try
            {
                return File.ReadAllText( $"{ path }//{ resourceId }.html" );
            }
            catch
            {
                return String.Empty;
            }
        }

        public void SaveHtml( int launchId, int resourceId, string html )
        {
            string path = $"{ Directory.GetCurrentDirectory() }//PageSources//{ launchId }";
            //ensure that directory for launch is created
            Directory.CreateDirectory( path );

            File.WriteAllText( $"{ path }//{ resourceId }.html", html );
        }
    }
}
