using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using System;

namespace Editor
{
    internal class DraggableWindowFactory
    {
        Action<Border> OnWindowClosed;

        private Canvas _parentCanvas;
        private int _zIndexCounter = 1;
        private Action<Border> _onWindowCreated;

        public DraggableWindowFactory(Canvas parentCanvas, Action<Border> onWindowCreated = null)
        {
            _parentCanvas = parentCanvas;
            _onWindowCreated = onWindowCreated;
        }

        /// <summary>
        /// Создает перетаскиваемое окно на указанной позиции
        /// </summary>
        /// <param name="title">Заголовок окна</param>
        /// <param name="content">Содержимое окна (опционально)</param>
        /// <param name="left">Позиция по X</param>
        /// <param name="top">Позиция по Y</param>
        /// <param name="width">Ширина окна (по умолчанию 200)</param>
        /// <param name="height">Высота окна (по умолчанию 150)</param>
        /// <returns>Созданное окно (Border)</returns>
        public DraggableWindow CreateWindow(string title, Control content = null, double left = 10, double top = 10, double width = 200, double height = 150)
        {
            // Создаем основной Border окна
            var window = new DraggableWindow
            {
                Classes = { "window" },
                Width = width,
                Height = height
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Star });

            var titleBar = CreateTitleBar(title, window);

            var contentBorder = new Border
            {
                Classes = { "contentArea" }
            };

            if (content != null)
            {
                contentBorder.Child = content;
            }
            else
            {
                contentBorder.Child = new TextBlock
                {
                    Text = $"Содержимое {title}",
                    Classes = { "contentText" }
                };
            }

            Grid.SetRow(titleBar, 0);
            Grid.SetRow(contentBorder, 1);

            grid.Children.Add(titleBar);
            grid.Children.Add(contentBorder);

            window.Child = grid;

            Canvas.SetLeft(window, left);
            Canvas.SetTop(window, top);
            SetZIndex(window, _zIndexCounter++);

            AttachDragHandlers(window, titleBar);
            AttachResizeHandlers(window);

            _parentCanvas.Children.Add(window);
            _onWindowCreated?.Invoke(window);

            return window;
        }



        private Border CreateTitleBar(string title, Border parentWindow)
        {
            var titleBar = new Border
            {
                Classes = { "titleBar" }
            };

            var titleGrid = new Grid();

            var titleText = new TextBlock
            {
                Text = title,
                Classes = { "titleText" }
            };

            var textButton = new Button
            {
                Content = "x",
                Classes = { "buttonText" }
            };

            var closeButton = new Button
            {
                Content = textButton,
                Classes = { "closeButton" }
            };

            closeButton.Click += (s, e) => CloseWindow(parentWindow);

            titleGrid.Children.Add(titleText);
            titleGrid.Children.Add(closeButton);
            titleBar.Child = titleGrid;

            return titleBar;
        }

        private void CloseWindow(Border window)
        {
            OnWindowClosed?.Invoke(window);
            _parentCanvas.Children.Remove(window);
        }

        private void AttachDragHandlers(Border window, Border titleBar)
        {
            Point startPoint = new Point();
            bool isDragging = false;
            Vector totalOffset = new Vector();

            titleBar.PointerPressed += (sender, e) =>
            {
                if (e.GetCurrentPoint(null).Properties.IsLeftButtonPressed)
                {
                    startPoint = e.GetPosition(null);
                    isDragging = true;
                    totalOffset = new Vector(Canvas.GetLeft(window), Canvas.GetTop(window));
                    titleBar.Cursor = new Cursor(StandardCursorType.DragMove);
                    e.Pointer.Capture(titleBar);
                    BringToFront(window);
                }
            };

            titleBar.PointerReleased += (sender, e) =>
            {
                isDragging = false;
                titleBar.Cursor = new Cursor(StandardCursorType.Arrow);
                e.Pointer.Capture(null);
            };

            titleBar.PointerMoved += (sender, e) =>
            {
                if (!isDragging) return;

                var currentPoint = e.GetPosition(null);
                var delta = currentPoint - startPoint;
                var newOffset = totalOffset + delta;

                var parentBounds = _parentCanvas.Bounds;
                if (newOffset.X < 0) newOffset = newOffset.WithX(0);
                if (newOffset.Y < 0) newOffset = newOffset.WithY(0);
                if (newOffset.X + window.Width > parentBounds.Width)
                    newOffset = newOffset.WithX(parentBounds.Width - window.Width);
                if (newOffset.Y + window.Height > parentBounds.Height)
                    newOffset = newOffset.WithY(parentBounds.Height - window.Height);

                Canvas.SetLeft(window, newOffset.X);
                Canvas.SetTop(window, newOffset.Y);
            };
        }

        private void AttachResizeHandlers(Border window)
        {
            Point startPoint = new Point();
            bool isResizing = false;
            ResizeDirection resizeDirection = ResizeDirection.None;
            Size originalSize = new Size();
            Vector totalOffset = new Vector();

            window.PointerPressed += (sender, e) =>
            {
                if (e.GetCurrentPoint(null).Properties.IsLeftButtonPressed)
                {
                    var positionInWindow = e.GetPosition(window);
                    resizeDirection = GetResizeDirection(window, positionInWindow);
                    BringToFront(window);

                    if (resizeDirection != ResizeDirection.None)
                    {
                        isResizing = true;
                        startPoint = e.GetPosition(null);
                        originalSize = new Size(window.Width, window.Height);
                        totalOffset = new Vector(Canvas.GetLeft(window), Canvas.GetTop(window));
                        e.Pointer.Capture(window);
                    }
                }
            };

            window.PointerReleased += (sender, e) =>
            {
                isResizing = false;
                resizeDirection = ResizeDirection.None;
                e.Pointer.Capture(null);
            };

            window.PointerMoved += (sender, e) =>
            {
                var mousePos = e.GetPosition(window);
                var direction = GetResizeDirection(window, mousePos);

                if (!isResizing)
                {
                    UpdateCursor(window, direction);
                }

                if (isResizing)
                {
                    var currentPoint = e.GetPosition(null);
                    var delta = currentPoint - startPoint;

                    double newWidth = originalSize.Width;
                    double newHeight = originalSize.Height;
                    double newLeft = totalOffset.X;
                    double newTop = totalOffset.Y;

                    switch (resizeDirection)
                    {
                        case ResizeDirection.Right:
                            newWidth = Math.Max(100, originalSize.Width + delta.X);
                            break;
                        case ResizeDirection.Bottom:
                            newHeight = Math.Max(100, originalSize.Height + delta.Y);
                            break;
                        case ResizeDirection.BottomRight:
                            newWidth = Math.Max(100, originalSize.Width + delta.X);
                            newHeight = Math.Max(100, originalSize.Height + delta.Y);
                            break;
                        case ResizeDirection.Left:
                            newWidth = Math.Max(100, originalSize.Width - delta.X);
                            newLeft = totalOffset.X + (originalSize.Width - newWidth);
                            break;
                        case ResizeDirection.Top:
                            newHeight = Math.Max(100, originalSize.Height - delta.Y);
                            newTop = totalOffset.Y + (originalSize.Height - newHeight);
                            break;
                        case ResizeDirection.TopLeft:
                            newWidth = Math.Max(100, originalSize.Width - delta.X);
                            newHeight = Math.Max(100, originalSize.Height - delta.Y);
                            newLeft = totalOffset.X + (originalSize.Width - newWidth);
                            newTop = totalOffset.Y + (originalSize.Height - newHeight);
                            break;
                        case ResizeDirection.TopRight:
                            newWidth = Math.Max(100, originalSize.Width + delta.X);
                            newHeight = Math.Max(100, originalSize.Height - delta.Y);
                            newTop = totalOffset.Y + (originalSize.Height - newHeight);
                            break;
                        case ResizeDirection.BottomLeft:
                            newWidth = Math.Max(100, originalSize.Width - delta.X);
                            newHeight = Math.Max(100, originalSize.Height + delta.Y);
                            newLeft = totalOffset.X + (originalSize.Width - newWidth);
                            break;
                    }

                    var parentBounds = _parentCanvas.Bounds;
                    if (newLeft < 0)
                    {
                        newWidth += newLeft;
                        newLeft = 0;
                    }
                    if (newTop < 0)
                    {
                        newHeight += newTop;
                        newTop = 0;
                    }
                    if (newLeft + newWidth > parentBounds.Width)
                    {
                        newWidth = parentBounds.Width - newLeft;
                    }
                    if (newTop + newHeight > parentBounds.Height)
                    {
                        newHeight = parentBounds.Height - newTop;
                    }

                    window.Width = newWidth;
                    window.Height = newHeight;
                    Canvas.SetLeft(window, newLeft);
                    Canvas.SetTop(window, newTop);
                }
            };
        }

        private ResizeDirection GetResizeDirection(Border window, Point position)
        {
            double borderSize = 8;
            bool isLeft = position.X <= borderSize;
            bool isRight = position.X >= window.Width - borderSize;
            bool isTop = position.Y <= borderSize;
            bool isBottom = position.Y >= window.Height - borderSize;

            if (isTop && isLeft) return ResizeDirection.TopLeft;
            if (isTop && isRight) return ResizeDirection.TopRight;
            if (isBottom && isLeft) return ResizeDirection.BottomLeft;
            if (isBottom && isRight) return ResizeDirection.BottomRight;
            if (isLeft) return ResizeDirection.Left;
            if (isRight) return ResizeDirection.Right;
            if (isTop) return ResizeDirection.Top;
            if (isBottom) return ResizeDirection.Bottom;

            return ResizeDirection.None;
        }

        private void UpdateCursor(Border window, ResizeDirection direction)
        {
            switch (direction)
            {
                case ResizeDirection.TopLeft:
                case ResizeDirection.BottomRight:
                    window.Cursor = new Cursor(StandardCursorType.TopLeftCorner);
                    break;
                case ResizeDirection.TopRight:
                case ResizeDirection.BottomLeft:
                    window.Cursor = new Cursor(StandardCursorType.TopRightCorner);
                    break;
                case ResizeDirection.Left:
                case ResizeDirection.Right:
                    window.Cursor = new Cursor(StandardCursorType.SizeWestEast);
                    break;
                case ResizeDirection.Top:
                case ResizeDirection.Bottom:
                    window.Cursor = new Cursor(StandardCursorType.SizeNorthSouth);
                    break;
                default:
                    window.Cursor = new Cursor(StandardCursorType.Arrow);
                    break;
            }
        }

        private void BringToFront(Border window)
        {
            int maxZ = 0;
            foreach (var child in _parentCanvas.Children)
            {
                if (child is Border border)
                {
                    maxZ = Math.Max(maxZ, GetZIndex(border));
                }
            }

            if (GetZIndex(window) == maxZ)
                return;

            SetZIndex(window, maxZ + 1);
            _zIndexCounter = maxZ + 2;
        }

        private int GetZIndex(Border window) => window.ZIndex;
        private void SetZIndex(Border window, int index) => window.ZIndex = index;

        private enum ResizeDirection
        {
            None,
            TopLeft,
            Top,
            TopRight,
            Right,
            BottomRight,
            Bottom,
            BottomLeft,
            Left
        }
    }
}