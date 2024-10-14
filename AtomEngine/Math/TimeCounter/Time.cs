namespace AtomEngine
{
    public class Time
    {
        public static double DeltaTime { get; private set; }
        public static double TimeScale { get; set; } = 1.0;
        public static double TimeSinceStart { get; private set; }
        private Time() { }


        public class TimeDisposer : IDisposable
        {
            public void Update(double deltaTime)
            {
                DeltaTime = deltaTime * TimeScale;
                TimeSinceStart += DeltaTime;
            }

            public void Dispose()
            {
                TimeSinceStart = 0;
                DeltaTime = 0;
                TimeScale = 1.0;
            }
        }
    }
}
