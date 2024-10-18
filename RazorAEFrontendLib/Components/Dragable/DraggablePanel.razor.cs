using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace AtomEngineEditor.Components
{
    public partial class DraggablePanelComponentModel : ComponentBase
    {
        [Parameter] public string Title { get; set; } = string.Empty;
        [Parameter] public RenderFragment ChildContent { get; set; }
        [Parameter] public int Left { get; set; }
        [Parameter] public int Top { get; set; }
        [Parameter] public int Width { get; set; } = 200;
        [Parameter] public int Height { get; set; } = 200;
        [Inject] protected IJSRuntime? JSRuntime { get; set; }

        protected ElementReference panelRef;
        protected bool isDragging;
        protected int startX;
        protected int startY;

        protected override void OnInitialized()
        {
            JSRuntime?.InvokeVoidAsync("addGlobalMouseEventListeners", DotNetObjectReference.Create(this));
        }

        [JSInvokable]
        public void OnMouseMove(int clientX, int clientY)
        {
            if (isDragging)
            {
                Left += clientX - startX;
                Top += clientY - startY;
                startX = clientX;
                startY = clientY;
                StateHasChanged();
            }
        }

        [JSInvokable]
        public void OnMouseUp()
        {
            isDragging = false;
        }

        protected void StartDragging(MouseEventArgs e)
        {
            isDragging = true;
            startX = (int)e.ClientX;
            startY = (int)e.ClientY;
        }

        public void Dispose()
        {
            JSRuntime?.InvokeVoidAsync("removeGlobalMouseEventListeners");
        }
    }
}
