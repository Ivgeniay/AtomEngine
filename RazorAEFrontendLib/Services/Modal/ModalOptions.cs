using AtomEngineEditor.Components;

namespace AtomEngineEditor.Services.Modal
{
    public class ModalOptions
    {
        public string Title { get; set; } = string.Empty;
        public string Content { get; set; } = string.Empty;
        public List<ModalButton> Buttons { get; set; } = new List<ModalButton>();
        public ModalAnimationType AnimationType { get; set; } = ModalAnimationType.FadeInOut;
        public bool ShowCloseButton { get; set; } = true;
        public bool ShowMinimizeButton { get; set; } = true; 
    }
}
