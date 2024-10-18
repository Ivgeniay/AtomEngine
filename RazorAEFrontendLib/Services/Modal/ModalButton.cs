namespace AtomEngineEditor.Services.Modal
{
    public class ModalButton
    {
        public string Text { get; set; } = string.Empty;
        public Action? OnClick { get; set; } = null;
    }
}
