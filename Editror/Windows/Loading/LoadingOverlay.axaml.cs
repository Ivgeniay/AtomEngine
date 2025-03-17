using Avalonia.Markup.Xaml;
using Avalonia.Controls;
using System;

namespace Editor;

public partial class LoadingOverlay : UserControl
{
    private TextBlock _statusText;
    private TextBlock _dialogHeader;
    private ProgressBar _progressBar;
    private Border _dialogBorder;
    private Button _cancelButton;
    private bool _isActive = false;

    public event EventHandler OnCancelled;

    public LoadingOverlay()
    {
        InitializeComponent();

        _statusText = this.FindControl<TextBlock>("StatusText");
        _dialogHeader = this.FindControl<TextBlock>("DialogueHeader");
        _progressBar = this.FindControl<ProgressBar>("ProgressBar");
        _dialogBorder = this.FindControl<Border>("DialogBorder");
        _cancelButton = this.FindControl<Button>("CancelButton");

        _dialogHeader.Text = "Operation executing...";

        IsVisible = false;
        HideCancelBtn();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public void Show(string message = "Loading...")
    {
        _statusText.Text = message;
        _progressBar.Value = 0;
        _progressBar.IsIndeterminate = true;
        IsVisible = true;
        _isActive = true;

        CenterDialog();
    }

    public void Hide()
    {
        IsVisible = false;
        _isActive = false;
    }

    public void ShowCancelBtn()
    {
        _cancelButton.IsVisible = true;
    }
    public void HideCancelBtn()
    {
        _cancelButton.IsVisible = false;
    }

    public void UpdateProgress(double progress, string message = null)
    {
        if (!_isActive) return;

        _progressBar.IsIndeterminate = false;
        _progressBar.Value = progress;

        if (message != null)
        {
            _statusText.Text = message;
        }
    }

    public void SetIndeterminate(string message = null)
    {
        if (!_isActive) return;

        _progressBar.IsIndeterminate = true;

        if (message != null)
        {
            _statusText.Text = message;
        }
    }

    private void CancelButton_Click(object sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        OnCancelled?.Invoke(this, EventArgs.Empty);
    }

    private void CenterDialog()
    {
        if (Parent is Canvas canvas)
        {
            double centerX = canvas.Bounds.Width / 2;
            double centerY = canvas.Bounds.Height / 2;

            if (_dialogBorder != null)
            {
                Canvas.SetLeft(_dialogBorder, centerX - (_dialogBorder.Width / 2));
                Canvas.SetTop(_dialogBorder, centerY - (_dialogBorder.Height / 2));
            }
        }
    }

    protected override void OnSizeChanged(SizeChangedEventArgs e)
    {
        base.OnSizeChanged(e);
        if (_isActive)
        {
            CenterDialog();
        }
    }
}