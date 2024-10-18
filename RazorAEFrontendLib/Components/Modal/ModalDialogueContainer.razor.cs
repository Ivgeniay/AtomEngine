using AtomEngineEditor.Services;
using Microsoft.AspNetCore.Components;

namespace AtomEngineEditor.Components
{
    public partial class ModalDialogueContainerComponent : ComponentBase
    {
        [Inject] protected IModalService ModalService { get; set; }

        protected override void OnInitialized()
        {
            ModalService.OnChange += StateHasChanged;
        }

        public void Dispose()
        {
            ModalService.OnChange -= StateHasChanged;
        }
    }
}
