using System.Collections.Generic;

namespace Core.Screenshot
{
    internal static class ScreenSizesStorage
    {
        public static KeyValuePair<int, int>[] ScreenSizes => new KeyValuePair<int, int>[] {
                new KeyValuePair<int, int>( 1920, 1080 ), new KeyValuePair<int, int>( 1600, 900 ),
                new KeyValuePair<int, int>( 1366, 768 ), new KeyValuePair<int, int>( 1280, 1024 ),
                new KeyValuePair<int, int>( 768, 1024 ), new KeyValuePair<int, int>( 375, 667 ),
                new KeyValuePair<int, int>( 375, 720 ), new KeyValuePair<int, int>( 360, 640 ),
                new KeyValuePair<int, int> (320, 480 ) };
    }
}
