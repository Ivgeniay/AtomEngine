using System.Collections.ObjectModel;
using AtomEngineEditor.Services.Modal; 

namespace AtomEngineEditor.Services
{
    public class ModalService : IModalService
    {
        private readonly List<ModalInstance> _modals = new List<ModalInstance>();
        public ObservableCollection<ModalInstance> Modals { get; } = new ObservableCollection<ModalInstance>();

        public event Action? OnChange;

        public ModalInstance Show(ModalOptions options)
        {
            var modal = new ModalInstance { Options = options }; 
            _modals.Add(modal);
            Modals.Add(modal);
            OnChange?.Invoke();
            return modal;
        }
        public void Close(Guid id)
        {
            var modal = _modals.FirstOrDefault(m => m.Id == id);
            if (modal != null)
            {
                _modals.Remove(modal);
                Modals.Remove(modal);
                OnChange?.Invoke();
            }
        }
        public void ToggleMinimize(Guid id)
        {
            var modal = _modals.FirstOrDefault(m => m.Id == id);
            if (modal != null)
            {
                modal.IsMinimized = !modal.IsMinimized;
                OnChange?.Invoke();
            }
        }
    }
}
