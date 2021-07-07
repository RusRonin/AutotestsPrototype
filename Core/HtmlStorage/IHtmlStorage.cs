namespace Core.HtmlStorage
{
    public interface IHtmlStorage
    {
        public void SaveHtml( int launchId, int resourceId, string html );
        public string LoadHtml( int launchId, int resourceId );
    }
}
