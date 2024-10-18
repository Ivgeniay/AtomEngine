using Microsoft.AspNetCore.Components;
using AtomEngineEditor.Services.Modal;
using AtomEngineEditor.Services;
using Microsoft.AspNetCore.Components.Web;

namespace AtomEngineEditor.Components
{
    public class ModalComponentBase : ComponentBase
    {
        [Inject] protected IModalService ModalService { get; set; }
        [Inject] protected IConsoleService Console { get; set; }
        [Parameter] public ModalOptions Options { get; set; }
        [Parameter] public Guid Id { get; set; }

        protected bool _isVisible = false;
        protected bool _isMinimized = false;

        protected override async Task OnInitializedAsync()
        {
            await Task.Delay(50);
            _isVisible = true;
            StateHasChanged();
        }

        protected string GetAnimationClass()
        {
            return Options.AnimationType switch
            {
                ModalAnimationType.FadeInOut => "fade-in-out",
                ModalAnimationType.PopInOut => "pop-in-out",
                ModalAnimationType.PopIn => "pop-in",
                _ => ""
            };
        }

        protected async Task CloseModal()
        {
            Console.Log($"{Id} is closed");
            _isVisible = false;
            StateHasChanged();
            await Task.Delay(300); // Задержка для анимации закрытия
            ModalService.Close(Id);
        }

        protected async Task HandleOverlayClick(MouseEventArgs e)
        { 
            Console.Log($"{Id} overlay clicked {e.MetaKey}");
            if (e.Type == "EventTarget")  // Это приближение, может потребоваться дополнительная проверка
            {
                await CloseModal();
            }
        }
        protected void Test()
        {
            Console.Log($"{Id} TEST");
        }

        protected void ToggleMinimize()
        {
            Console.Log($"{Id} is minimized");
            ModalService.ToggleMinimize(Id);
            _isMinimized = !_isMinimized;
            StateHasChanged();
        }

        protected async void OnButtonClick(ModalButton button)
        {
            Console.Log($"Button {button.Text} clicked");
            button.OnClick?.Invoke();
            await CloseModal();
        }
    }
}
