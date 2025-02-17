namespace AtomEngine
{
    public static class Time
    {
        public static double DeltaTime { get; set; }
        public static double TimeSinceStart { get; set; }
        public static int SecondsSinceStart { get; set; }
        public static double TimeScale { get; set; } = 1.0;
        public static float FIXED_TIME_STEP { get; } = 0.02f;
        public static double MAX_TIMESTEP { get; } = 0.1f;

        public static void Update(double deltaTime)
        {
            Time.DeltaTime = deltaTime;
            Time.TimeSinceStart += deltaTime;
            Time.SecondsSinceStart = (int)Time.TimeSinceStart;
        }

    }
}
