namespace EngineLib
{
    public static class Time
    {
        public static double DeltaTime { get; set; }
        public static double TimeSinceStart { get; set; }
        public static int SecondsSinceStart { get; set; }
        public static double TimeScale { get; set; } = 1.0;
        public static double FixedDeltaTime { get; set; } = 0.02;

        public static void Update(double deltaTime)
        {
            Time.DeltaTime = deltaTime;
            Time.TimeSinceStart += deltaTime;
            Time.SecondsSinceStart = (int)Time.TimeSinceStart;
        }

    }
}
