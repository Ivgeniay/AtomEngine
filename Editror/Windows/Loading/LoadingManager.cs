using System.Threading.Tasks;
using Avalonia.Threading;
using Avalonia.Controls;
using AtomEngine;
using EngineLib;
using System;

namespace Editor
{
    internal class LoadingManager : IService, IDisposable
    {
        private static LoadingManager _instance;
        private LoadingOverlay _overlay;
        private Canvas _canvas;
        private bool _initialized = false;

        public static LoadingManager Instance
        {
            get
            {
                return _instance;
            }
        }

        public LoadingManager()
        {
            _instance = this;
        }

        public void SetCanvas(Canvas canvas)
        {
            if (_initialized) return;

            _canvas = canvas;
            _overlay = new LoadingOverlay();

            _overlay.Width = canvas.Bounds.Width;
            _overlay.Height = canvas.Bounds.Height;
            _overlay.ZIndex = 9999;

            _canvas.Children.Add(_overlay);

            Canvas.SetLeft(_overlay, 0);
            Canvas.SetTop(_overlay, 0);
            _overlay.ZIndex = 9999;

            canvas.SizeChanged += (s, e) =>
            {
                if (_overlay != null)
                {
                    _overlay.Width = e.NewSize.Width;
                    _overlay.Height = e.NewSize.Height;
                }
            };

            _initialized = true;
        }

        public void ShowLoading(string message = "Загрузка...")
        {
            if (!_initialized)
            {
                DebLogger.Error("LoadingManager не инициализирован!");
                return;
            }

            Dispatcher.UIThread.Post(() =>
            {
                _overlay.Show(message);
            });
        }

        public void HideLoading()
        {
            if (!_initialized) return;

            Dispatcher.UIThread.Post(() =>
            {
                _overlay.Hide();
            });
        }

        public void ShowCancelBtn()
        {
            Dispatcher.UIThread.Post(() =>
            {
                _overlay.ShowCancelBtn();
            });
        }
        public void HideCancelBtn()
        {
            Dispatcher.UIThread.Post(() =>
            {
                _overlay.HideCancelBtn();
            });
        }

        public void UpdateProgress(double progress, string message = null)
        {
            if (!_initialized) return;

            Dispatcher.UIThread.Post(() =>
            {
                _overlay.UpdateProgress(progress, message);
            });
        }

        public void SetIndeterminate(string message = null)
        {
            if (!_initialized) return;

            Dispatcher.UIThread.Post(() =>
            {
                _overlay.SetIndeterminate(message);
            });
        }

        public void RegisterCancelHandler(EventHandler handler)
        {
            if (!_initialized) return;
            _overlay.OnCancelled += handler;
        }

        public void UnregisterCancelHandler(EventHandler handler)
        {
            if (!_initialized) return;
            _overlay.OnCancelled -= handler;
        }

        public async Task RunWithLoading(Func<IProgress<(double, string)>, Task> action, string initialMessage = "Загрузка...")
        {
            ShowLoading(initialMessage);

            try
            {
                var progress = new Progress<(double, string)>(report =>
                {
                    UpdateProgress(report.Item1, report.Item2);
                });

                await action(progress);
            }
            finally
            {
                HideLoading();
            }
        }

        public void Dispose()
        {
            if (_initialized && _canvas.Children.Contains(_overlay))
            {
                _canvas.Children.Remove(_overlay);
            }
            _overlay = null;
            _canvas = null;
            _initialized = false;
        }

        public Task InitializeAsync()
        {
            return Task.CompletedTask;
        }
    }
}
