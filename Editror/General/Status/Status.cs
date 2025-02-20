

using System.Collections.Generic;

namespace Editor
{
    public static class Status
    {
        private static List<IStatusProvider> _statuses = new List<IStatusProvider>();

        public static void UnRegisterStatusProvider(IStatusProvider status)
        {
            if (!_statuses.Contains(status)) return;
            _statuses.Remove(status);
        }
        public static void RegisterStatusProvider(IStatusProvider status)
        {
            if (_statuses.Contains(status)) return;
            _statuses.Add(status);
        }

        public static void SetStatus(string status) =>
            _statuses.ForEach(e => e.SetStatus(status));
    }
}
