using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using AudioVisualizer.ViewModels;

namespace AudioVisualizer
{
    public partial class MainWindow : Window
    {
        private double _textX = 400;

        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
            this.MouseLeftButtonDown += (s, e) => DragMove();
            
            // Start Rendering Loop for smoothest possible animation
            System.Windows.Media.CompositionTarget.Rendering += OnRendering;
        }

        private void OnRendering(object? sender, System.EventArgs e)
        {
            if (ScrollingText.ActualWidth == 0) return;

            // Pixels per frame (at 60fps, 1.0 = 60px/sec). 
            // 0.5 is slower and more readable.
            double speed = 0.5; 
            
            _textX -= speed;

            // If text has fully scrolled off the left side
            if (_textX < -ScrollingText.ActualWidth)
            {
                // Reset to just outside the right side
                _textX = this.ActualWidth; 
            }

            MarqueeTransform.X = _textX;
        }

        private void Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void CopyBtc_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem item && item.Header.ToString() is string header)
            {
                // Extract address part after "btc: "
                var parts = header.Split("btc: ");
                if (parts.Length > 1)
                {
                    Clipboard.SetText(parts[1].Trim());
                    MessageBox.Show("BTC Address copied to clipboard!", "Copied", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }
    }
}