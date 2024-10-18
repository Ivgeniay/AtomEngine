using AtomEngineEditor.Services.Modal;
using System.Collections.ObjectModel;

namespace AtomEngineEditor.Services
{
    public interface IModalService
    {
        public event Action? OnChange;
        public ObservableCollection<ModalInstance> Modals { get; } 
        public ModalInstance Show(ModalOptions options);
        public void Close(Guid id);
        public void ToggleMinimize(Guid id);
    }
}
