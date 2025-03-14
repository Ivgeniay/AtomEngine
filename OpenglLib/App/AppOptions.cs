namespace OpenglLib
{
    public class AppOptions
    {
        public int Width { get; set; } = 800;
        public int Height { get; set; } = 600;
        public int AspectRatio => Width / Height;
        public string Title { get; set; } = "Engine";
        public bool Debug { get; set; } =
#if DEBUG
            true;
#elif !DEBUG
            false;
#endif

        public Platform Platform { get; set; } = Platform.Exe;
        public Tuple<float, float, float, float> BackgroundColor { get; set; } = Tuple.Create<float, float, float, float> ( 0.1f, 0.1f, 0.1f, 0.1f );
    }
}
