namespace EngineLib
{
    public static class Application
    {
        private static Queue<double> _fpsHistory = new Queue<double>();
        private const int FPS_SAMPLE_SIZE = 60;

        public static double FPS { get; set; }
        public static double FPS_raw { get; set; }

        public static void Update(double deltaTime)
        {
            _fpsHistory.Enqueue(1 / deltaTime);
            if (_fpsHistory.Count > FPS_SAMPLE_SIZE)
                _fpsHistory.Dequeue();
            double averageFps = _fpsHistory.Average();
            Application.FPS = averageFps;
            Application.FPS_raw = 1 / deltaTime;
        }
    }
}
