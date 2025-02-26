using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using System;
using System.Threading.Tasks;

namespace Editor
{
    public partial class LoadingWindow : Window
    {
        private TextBlock statusTextBlock;

        public LoadingWindow()
        {
            SystemDecorations = SystemDecorations.None;
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            statusTextBlock = this.FindControl<TextBlock>("StatusTextBlock");
        }

        
        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public void UpdateStatus(string status)
        {
            if (statusTextBlock != null)
            {
                statusTextBlock.Text = status;
            }
        }

        public async Task UpdateLoadingStatus(string status)
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                UpdateStatus(status);
            });
        }
    }

}
