namespace Lib.Images
{
    public static class ImageProviderFactory
    {
        public static IImageProvider Get(string type)
        {
            switch (type)
            {
                case "Default":
                    return new DefaultImageSource();
                case "Bing":
                    return new BingImageSearchProvider();
                default:
                    return new DefaultImageSource();
            }
        }
    }
}