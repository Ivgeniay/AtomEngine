using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Input;
using System.IO;
using Avalonia;
using System;
using Key = Avalonia.Input.Key;


namespace Editor
{
    public class ExplorerFileOperations
    {
        private readonly ExplorerController _controller;
        private readonly Canvas _overlayCanvas;

        private string _clipboardPath;
        private bool _isCut;
        private bool _isDirectory;

        public ExplorerFileOperations(ExplorerController controller, Canvas overlayCanvas)
        {
            _controller = controller;
            _overlayCanvas = overlayCanvas;
        }

        public void CreateNewFolder()
        {
            string basePath = _controller.CurrentPath;
            string newFolderName = "New Folder";
            string folderPath = Path.Combine(basePath, newFolderName);

            int counter = 1;
            while (Directory.Exists(folderPath))
            {
                folderPath = Path.Combine(basePath, $"{newFolderName} ({counter})");
                counter++;
            }

            Directory.CreateDirectory(folderPath);
            _controller.RefreshView();
        }

        public void CreateNewFile()
        {
            string basePath = _controller.CurrentPath;
            string newFileName = "New File.txt";
            string filePath = Path.Combine(basePath, newFileName);

            int counter = 1;
            while (File.Exists(filePath))
            {
                filePath = Path.Combine(basePath, $"New File ({counter}).txt");
                counter++;
            }

            File.Create(filePath).Close();
            _controller.RefreshView();
        }

        public void CopyFile(string fileName)
        {
            _clipboardPath = Path.Combine(_controller.CurrentPath, fileName);
            _isCut = false;
            _isDirectory = false;
        }

        public void CopyDirectory(string path)
        {
            _clipboardPath = path;
            _isCut = false;
            _isDirectory = true;
        }

        public void CutFile(string fileName)
        {
            _clipboardPath = Path.Combine(_controller.CurrentPath, fileName);
            _isCut = true;
            _isDirectory = false;
        }

        public void CutDirectory(string path)
        {
            _clipboardPath = path;
            _isCut = true;
            _isDirectory = true;
        }

        public void Paste()
        {
            if (string.IsNullOrEmpty(_clipboardPath) || (!File.Exists(_clipboardPath) && !Directory.Exists(_clipboardPath)))
                return;

            try
            {
                string sourceName = Path.GetFileName(_clipboardPath);
                string destPath = Path.Combine(_controller.CurrentPath, sourceName);

                if (File.Exists(destPath) || Directory.Exists(destPath))
                {
                    int counter = 1;
                    if (_isDirectory)
                    {
                        while (Directory.Exists(destPath))
                        {
                            destPath = Path.Combine(_controller.CurrentPath, $"{sourceName} ({counter})");
                            counter++;
                        }
                    }
                    else
                    {
                        string name = Path.GetFileNameWithoutExtension(sourceName);
                        string ext = Path.GetExtension(sourceName);
                        while (File.Exists(destPath))
                        {
                            destPath = Path.Combine(_controller.CurrentPath, $"{name} ({counter}){ext}");
                            counter++;
                        }
                    }
                }

                if (_isDirectory)
                {
                    if (_isCut)
                        Directory.Move(_clipboardPath, destPath);
                    else
                        DirectoryCopy(_clipboardPath, destPath, true);
                }
                else
                {
                    if (_isCut)
                        File.Move(_clipboardPath, destPath);
                    else
                        File.Copy(_clipboardPath, destPath);
                }

                if (_isCut)
                {
                    _clipboardPath = null;
                    _isDirectory = false;
                }

                _controller.RefreshView();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error during paste operation: {ex.Message}");
            }
        }

        public void DeleteFile(string fileName)
        {
            string path = Path.Combine(_controller.CurrentPath, fileName);
            if (File.Exists(path))
            {
                File.Delete(path);
                _controller.RefreshView();
            }
        }

        public void DeleteDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
                _controller.RefreshView();
            }
        }

        public void RenameFile(string fileName, ListBoxItem fileListItem)
        {
            if (fileListItem == null) return;

            var position = fileListItem.TranslatePoint(new Point(0, 0), _controller);
            if (position == null) return;

            var textBox = new TextBox
            {
                Text = fileName,
                Classes = { "renameTextBox" },
                Width = fileListItem.Bounds.Width,
                Height = fileListItem.Bounds.Height
            };
            textBox.IsHitTestVisible = true;

            Canvas.SetLeft(textBox, position.Value.X);
            Canvas.SetTop(textBox, position.Value.Y);

            _overlayCanvas.Children.Add(textBox);

            void OnPointerPressed(object s, PointerPressedEventArgs e)
            {
                var point = e.GetPosition(textBox);

                if (point.X < 0 || point.X > textBox.Width ||
                    point.Y < 0 || point.Y > textBox.Height)
                {
                    _overlayCanvas.Children.Remove(textBox);
                    _controller.PointerPressed -= OnPointerPressed;
                }
            }

            _controller.PointerPressed += OnPointerPressed;

            textBox.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    _controller.PointerPressed -= OnPointerPressed;

                    string oldPath = Path.Combine(_controller.CurrentPath, fileName);
                    string newPath = Path.Combine(_controller.CurrentPath, textBox.Text);

                    if (File.Exists(oldPath) && !File.Exists(newPath))
                    {
                        try
                        {
                            File.Move(oldPath, newPath);
                            _controller.RefreshView();
                        }
                        catch (Exception ex)
                        {
                            Status.SetStatus($"Ошибка при переименовании файла: {ex.Message}");
                        }
                    }
                    _overlayCanvas.Children.Remove(textBox);
                }
                else if (e.Key == Key.Escape)
                {
                    _controller.PointerPressed -= OnPointerPressed;
                    _overlayCanvas.Children.Remove(textBox);
                }
            };

            textBox.LostFocus += (s, e) =>
            {
                _controller.PointerPressed -= OnPointerPressed;
                _overlayCanvas.Children.Remove(textBox);
            };

            textBox.Focus();
            textBox.SelectAll();
        }

        public void RenameDirectory(TreeViewItem treeItem)
        {
            if (treeItem == null) return;

            string originalPath = treeItem.Tag as string;
            if (originalPath == null) return;

            string originalName = Path.GetFileName(originalPath);

            var position = treeItem.TranslatePoint(new Point(0, 0), _controller);
            if (position == null) return;

            var textBox = new TextBox
            {
                Text = originalName,
                Classes = { "renameTextBox" },
                Width = treeItem.Bounds.Width - 20,
                Height = 20
            };
            textBox.IsHitTestVisible = true;

            Canvas.SetLeft(textBox, position.Value.X + 20);
            Canvas.SetTop(textBox, position.Value.Y);

            _overlayCanvas.Children.Add(textBox);

            void OnPointerPressed(object s, PointerPressedEventArgs e)
            {
                var point = e.GetPosition(textBox);
                if (point.X < 0 || point.X > textBox.Width ||
                    point.Y < 0 || point.Y > textBox.Height)
                {
                    _overlayCanvas.Children.Remove(textBox);
                    _controller.PointerPressed -= OnPointerPressed;
                }
            }

            _controller.PointerPressed += OnPointerPressed;

            textBox.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    _controller.PointerPressed -= OnPointerPressed;
                    string parentPath = Path.GetDirectoryName(originalPath);
                    string newPath = Path.Combine(parentPath, textBox.Text);

                    if (Directory.Exists(originalPath) && !Directory.Exists(newPath))
                    {
                        try
                        {
                            Directory.Move(originalPath, newPath);
                            _controller.RefreshView();
                        }
                        catch (Exception ex)
                        {
                            Status.SetStatus($"Ошибка при переименовании директории: {ex.Message}");
                        }
                    }
                    _overlayCanvas.Children.Remove(textBox);
                }
                else if (e.Key == Key.Escape)
                {
                    _controller.PointerPressed -= OnPointerPressed;
                    _overlayCanvas.Children.Remove(textBox);
                }
            };

            textBox.Focus();
            textBox.SelectAll();
        }
        public void OpenFile(string fileName)
        {
            string filePath = Path.Combine(_controller.CurrentPath, fileName);
            if (File.Exists(filePath))
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                });
            }
        }

        public async void HandleFileMoveOperation(string sourcePath, string destinationPath)
        {
            try
            {
                bool overwrite = false;

                if (File.Exists(destinationPath))
                {
                    overwrite = await ShowFileExistsDialog(Path.GetFileName(destinationPath));

                    if (!overwrite)
                        return;
                }

                if (overwrite && File.Exists(destinationPath))
                    File.Delete(destinationPath);

                File.Move(sourcePath, destinationPath);

                _controller.RefreshView();
                Status.SetStatus($"Файл {Path.GetFileName(sourcePath)} перемещен успешно");
            }
            catch (Exception ex)
            {
                Status.SetStatus($"Ошибка при перемещении файла: {ex.Message}");
            }
        }

        private async Task<bool> ShowFileExistsDialog(string fileName)
        {
            // Создаем диалоговое окно
            var window = new Window
            {
                Title = "Файл существует",
                Width = 400,
                Height = 150,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };

            var panel = new StackPanel
            {
                Margin = new Thickness(20),
                Spacing = 20
            };

            panel.Children.Add(new TextBlock
            {
                Text = $"Файл '{fileName}' уже существует. Перезаписать?",
                TextWrapping = TextWrapping.Wrap
            });

            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Spacing = 10
            };

            bool result = false;

            var yesButton = new Button { Content = "Да", Width = 80 };
            yesButton.Click += (s, e) =>
            {
                result = true;
                window.Close();
            };

            var noButton = new Button { Content = "Нет", Width = 80 };
            noButton.Click += (s, e) =>
            {
                result = false;
                window.Close();
            };

            buttonPanel.Children.Add(yesButton);
            buttonPanel.Children.Add(noButton);

            panel.Children.Add(buttonPanel);
            window.Content = panel;

            await window.ShowDialog(GetWindowFromVisual(_controller));

            return result;
        }


        private Window GetWindowFromVisual(Control control)
        {
            while (control != null)
            {
                if (control is Window window)
                    return window;

                control = control.Parent as Control;
            }

            return null;
        }

        private static void DirectoryCopy(string sourcePath, string destPath, bool copySubDirs)
        {
            DirectoryInfo dir = new DirectoryInfo(sourcePath);
            DirectoryInfo[] dirs = dir.GetDirectories();

            if (!Directory.Exists(destPath))
                Directory.CreateDirectory(destPath);

            FileInfo[] files = dir.GetFiles();
            foreach (FileInfo file in files)
            {
                string tempPath = Path.Combine(destPath, file.Name);
                file.CopyTo(tempPath, false);
            }

            if (copySubDirs)
            {
                foreach (DirectoryInfo subdir in dirs)
                {
                    string tempPath = Path.Combine(destPath, subdir.Name);
                    DirectoryCopy(subdir.FullName, tempPath, copySubDirs);
                }
            }
        }
    }
}