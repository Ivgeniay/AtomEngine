using OpenglLib;

namespace WindowsBuild
{
    static class Program
    {
        private static void Main(string[] args)
        {
            var options = new AppOptions() { Width = 800, Height = 600, Debug = false };
            using App app = new App(options);

            app.Run();
        }
    }
}
