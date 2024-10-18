namespace AtomEngineEditor.Services.Modal
{
    public class ModalInstance
    {
        public Guid Id { get; } = Guid.NewGuid();
        public ModalOptions Options { get; set; } = new ModalOptions();
        public bool IsVisible { get; set; } = true;
        public bool IsMinimized { get; set; } = false;
    }
}
