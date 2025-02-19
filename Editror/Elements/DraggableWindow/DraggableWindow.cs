using Avalonia;

namespace Editor
{
    internal class DraggableWindow : AvaloniaObject
    {
        public static readonly StyledProperty<string> TitleProperty =
            AvaloniaProperty.Register<DraggableWindow, string>(nameof(Title));

        public static readonly StyledProperty<double> WidthProperty =
            AvaloniaProperty.Register<DraggableWindow, double>(nameof(Width));

        public static readonly StyledProperty<double> HeightProperty =
            AvaloniaProperty.Register<DraggableWindow, double>(nameof(Height));

        public static readonly StyledProperty<double> LeftProperty =
            AvaloniaProperty.Register<DraggableWindow, double>(nameof(Left));

        public static readonly StyledProperty<double> TopProperty =
            AvaloniaProperty.Register<DraggableWindow, double>(nameof(Top));

        public static readonly StyledProperty<string> ContentProperty =
            AvaloniaProperty.Register<DraggableWindow, string>(nameof(Content));

        public string Title
        {
            get => GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public double Width
        {
            get => GetValue(WidthProperty);
            set => SetValue(WidthProperty, value);
        }

        public double Height
        {
            get => GetValue(HeightProperty);
            set => SetValue(HeightProperty, value);
        }

        public double Left
        {
            get => GetValue(LeftProperty);
            set => SetValue(LeftProperty, value);
        }

        public double Top
        {
            get => GetValue(TopProperty);
            set => SetValue(TopProperty, value);
        }

        public string Content
        {
            get => GetValue(ContentProperty);
            set => SetValue(ContentProperty, value);
        }
    }
}
