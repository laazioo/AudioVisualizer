using AudioVisualizer.Services;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AudioVisualizer.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly AudioCaptureService _audioService;
        private readonly SpectrumService _spectrumService;
        private readonly MediaInfoService _mediaService;

        private const int BarCount = 60;
        private double[] _currentBars = new double[BarCount]; // For smoothing
        
        // Auto-Gain Control
        private double _maxPeak = 0.1; // Start low
        private const double GainAttack = 0.01; // Rise fast (reduce gain quickly on loud sounds)
        private const double GainDecay = 0.005; // Recover slow (increase gain slowly on quiet sounds)

        // Bass Pulse for visual effects (0.0 to 1.0)
        private double _bassAmplitude;
        public double BassAmplitude
        {
            get => _bassAmplitude;
            set 
            {
                _bassAmplitude = value; 
                OnPropertyChanged();
                OnPropertyChanged(nameof(BassBlur)); // Notify derived property
            }
        }

        public double BassBlur => _bassAmplitude * 40; // Scale for BlurRadius (0-40)

        private string _currentTrackName = "Waiting for audio...";
        public string CurrentTrackName
        {
            get => _currentTrackName;
            set { _currentTrackName = value; OnPropertyChanged(); }
        }

        // Using System.Windows.Media for Brush/Color
        public enum TextStyle
        {
            Modern,
            Digital
        }

        public enum TextColorOption
        {
            White,
            Red,
            Blue,
            Green,
            Yellow,
            Magenta,
            Cyan
        }

        private TextStyle _selectedTextStyle = TextStyle.Modern;
        public TextStyle SelectedTextStyle
        {
            get => _selectedTextStyle;
            set
            {
                if (_selectedTextStyle != value)
                {
                    _selectedTextStyle = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsModernStyle));
                    OnPropertyChanged(nameof(IsDigitalStyle));
                    ApplyStyle(_selectedTextStyle);
                }
            }
        }

        private TextColorOption _selectedTextColor = TextColorOption.White;
        public TextColorOption SelectedTextColor
        {
            get => _selectedTextColor;
            set
            {
                if (_selectedTextColor != value)
                {
                    _selectedTextColor = value;
                    OnPropertyChanged();
                    // Update boolean helpers for menu checkmarks
                    OnPropertyChanged(nameof(IsWhiteColor));
                    OnPropertyChanged(nameof(IsRedColor));
                    OnPropertyChanged(nameof(IsBlueColor));
                    OnPropertyChanged(nameof(IsGreenColor));
                    OnPropertyChanged(nameof(IsYellowColor));
                    OnPropertyChanged(nameof(IsMagentaColor));
                    OnPropertyChanged(nameof(IsCyanColor));
                    
                    ApplyColor(_selectedTextColor);
                }
            }
        }

        // Style Properties
        private System.Windows.Media.FontFamily _currentFontFamily = new System.Windows.Media.FontFamily("Segoe UI Semibold");
        public System.Windows.Media.FontFamily CurrentFontFamily
        {
            get => _currentFontFamily;
            set { _currentFontFamily = value; OnPropertyChanged(); }
        }

        private double _currentBlur = 2;
        public double CurrentBlur
        {
            get => _currentBlur;
            set { _currentBlur = value; OnPropertyChanged(); }
        }

        private double _currentShadowDepth = 1;
        public double CurrentShadowDepth
        {
            get => _currentShadowDepth;
            set { _currentShadowDepth = value; OnPropertyChanged(); }
        }

        // Color Properties
        private System.Windows.Media.Brush _currentBrush = System.Windows.Media.Brushes.White;
        public System.Windows.Media.Brush CurrentBrush
        {
            get => _currentBrush;
            set { _currentBrush = value; OnPropertyChanged(); }
        }

        private System.Windows.Media.Color _currentColor = System.Windows.Media.Colors.White; // For Text
        public System.Windows.Media.Color CurrentColor
        {
            get => _currentColor;
            set { _currentColor = value; OnPropertyChanged(); }
        }

        private System.Windows.Media.Color _currentShadowColor = System.Windows.Media.Colors.Black; // For Effect
        public System.Windows.Media.Color CurrentShadowColor
        {
            get => _currentShadowColor;
            set { _currentShadowColor = value; OnPropertyChanged(); }
        }

        private System.Windows.Media.TextFormattingMode _currentTextFormattingMode = System.Windows.Media.TextFormattingMode.Display;
        public System.Windows.Media.TextFormattingMode CurrentTextFormattingMode
        {
            get => _currentTextFormattingMode;
            set { _currentTextFormattingMode = value; OnPropertyChanged(); }
        }


        // Style Menu Helpers
        public bool IsModernStyle
        {
            get => _selectedTextStyle == TextStyle.Modern;
            set { if (value) SelectedTextStyle = TextStyle.Modern; }
        }

        public bool IsDigitalStyle
        {
            get => _selectedTextStyle == TextStyle.Digital;
            set { if (value) SelectedTextStyle = TextStyle.Digital; }
        }

        // Color Menu Helpers
        public bool IsWhiteColor { get => _selectedTextColor == TextColorOption.White; set { if(value) SelectedTextColor = TextColorOption.White; } }
        public bool IsRedColor { get => _selectedTextColor == TextColorOption.Red; set { if(value) SelectedTextColor = TextColorOption.Red; } }
        public bool IsBlueColor { get => _selectedTextColor == TextColorOption.Blue; set { if(value) SelectedTextColor = TextColorOption.Blue; } }
        public bool IsGreenColor { get => _selectedTextColor == TextColorOption.Green; set { if(value) SelectedTextColor = TextColorOption.Green; } }
        public bool IsYellowColor { get => _selectedTextColor == TextColorOption.Yellow; set { if(value) SelectedTextColor = TextColorOption.Yellow; } }
        public bool IsMagentaColor { get => _selectedTextColor == TextColorOption.Magenta; set { if(value) SelectedTextColor = TextColorOption.Magenta; } }
        public bool IsCyanColor { get => _selectedTextColor == TextColorOption.Cyan; set { if(value) SelectedTextColor = TextColorOption.Cyan; } }

        private void ApplyStyle(TextStyle style)
        {
            if (style == TextStyle.Modern)
            {
                CurrentFontFamily = new System.Windows.Media.FontFamily("Segoe UI Semibold");
                CurrentBlur = 2;
                CurrentShadowDepth = 1;
                CurrentShadowColor = System.Windows.Media.Colors.Black; // Restore sharp contrast
                
                // Auto-set Color to White if it feels "wrong" (optional, but safer for "Modern" feel)
                SelectedTextColor = TextColorOption.White;
            }
            else // Digital
            {
                CurrentFontFamily = new System.Windows.Media.FontFamily("Consolas");
                CurrentBlur = 10;
                CurrentShadowDepth = 0;
                
                // Auto-set Color to Green for the "Digital" experience
                SelectedTextColor = TextColorOption.Green;
                
                // Note: CurrentShadowColor effectively updates via ApplyColor(Green) called by SelectedTextColor setter
            }
            
            // Force Display everywhere for sharpness
            CurrentTextFormattingMode = System.Windows.Media.TextFormattingMode.Display; 
        }

        private void ApplyColor(TextColorOption color)
        {
            System.Windows.Media.Color c = System.Windows.Media.Colors.White;
            switch (color)
            {
                case TextColorOption.White: c = System.Windows.Media.Colors.White; break;
                case TextColorOption.Red: c = System.Windows.Media.Colors.Red; break;
                case TextColorOption.Blue: c = System.Windows.Media.Color.FromRgb(0, 100, 255); break; // A nice bright blue
                case TextColorOption.Green: c = System.Windows.Media.Colors.Lime; break;
                case TextColorOption.Yellow: c = System.Windows.Media.Colors.Yellow; break;
                case TextColorOption.Magenta: c = System.Windows.Media.Colors.Magenta; break;
                case TextColorOption.Cyan: c = System.Windows.Media.Colors.Cyan; break;
            }

            CurrentColor = c;
            CurrentBrush = new System.Windows.Media.SolidColorBrush(c);

            // If we are in Digital mode, update the glow color too.
            // If Modern, keep it Black.
            if (SelectedTextStyle == TextStyle.Digital)
            {
                CurrentShadowColor = c;
            }
            else
            {
                 CurrentShadowColor = System.Windows.Media.Colors.Black;
            }
        }

        private bool _isScrollingTextVisible = true;
        public bool IsScrollingTextVisible
        {
            get => _isScrollingTextVisible;
            set { _isScrollingTextVisible = value; OnPropertyChanged(); }
        }

        // Visualizer Style
        public enum VisualizerStyle
        {
            Gradient, // Default Cyan-Magenta
            Classic   // Winamp Green-Yellow-Red
        }

        private VisualizerStyle _selectedVisualizerStyle = VisualizerStyle.Gradient;
        public VisualizerStyle SelectedVisualizerStyle
        {
            get => _selectedVisualizerStyle;
            set
            {
                if (_selectedVisualizerStyle != value)
                {
                    _selectedVisualizerStyle = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(IsGradientStyle));
                    OnPropertyChanged(nameof(IsClassicStyle));
                    UpdateBarBrush();
                }
            }
        }

        public bool IsGradientStyle { get => _selectedVisualizerStyle == VisualizerStyle.Gradient; set { if(value) SelectedVisualizerStyle = VisualizerStyle.Gradient; } }
        public bool IsClassicStyle { get => _selectedVisualizerStyle == VisualizerStyle.Classic; set { if(value) SelectedVisualizerStyle = VisualizerStyle.Classic; } }

        private bool _isPeakHoldEnabled = true; // Default: true for now
        public bool IsPeakHoldEnabled
        {
            get => _isPeakHoldEnabled;
            set { _isPeakHoldEnabled = value; OnPropertyChanged(); }
        }

        private System.Windows.Media.Brush _barBrush;
        public System.Windows.Media.Brush BarBrush
        {
            get => _barBrush;
            set { _barBrush = value; OnPropertyChanged(); }
        }

        private void UpdateBarBrush()
        {
            if (_selectedVisualizerStyle == VisualizerStyle.Classic)
            {
                // Classic Winamp: Green (Bottom) -> Yellow -> Red (Top)
                // LinearGradientBrush needs StartPoint 0,1 (Bottom) to 0,0 (Top)
                var brush = new System.Windows.Media.LinearGradientBrush();
                brush.StartPoint = new System.Windows.Point(0, 1);
                brush.EndPoint = new System.Windows.Point(0, 0);
                brush.GradientStops.Add(new System.Windows.Media.GradientStop(System.Windows.Media.Colors.Lime, 0.0));
                brush.GradientStops.Add(new System.Windows.Media.GradientStop(System.Windows.Media.Colors.Yellow, 0.6)); // Yellow starts higher
                brush.GradientStops.Add(new System.Windows.Media.GradientStop(System.Windows.Media.Colors.Red, 0.9));
                BarBrush = brush;
            }
            else
            {
                // Default: Cyan -> Magenta
                var brush = new System.Windows.Media.LinearGradientBrush();
                brush.StartPoint = new System.Windows.Point(0, 1);
                brush.EndPoint = new System.Windows.Point(0, 0);
                brush.GradientStops.Add(new System.Windows.Media.GradientStop(System.Windows.Media.Color.FromRgb(0, 255, 255), 0.0));
                brush.GradientStops.Add(new System.Windows.Media.GradientStop(System.Windows.Media.Color.FromRgb(255, 0, 255), 1.0));
                BarBrush = brush;
            }
        }


        // Optimization: Use ObservableCollection of objects and update properties
        // instead of replacing the entire collection every frame.
        public class BarData : INotifyPropertyChanged
        {
            private double _value;
            public double Value
            {
                get => _value;
                set { if (Math.Abs(_value - value) > 0.001) { _value = value; OnPropertyChanged(); } }
            }

            // Peak Hold Logic
            private double _peakValue;
            public double PeakValue
            {
                get => _peakValue;
                set 
                { 
                    if (Math.Abs(_peakValue - value) > 0.001) 
                    { 
                        _peakValue = value; 
                        OnPropertyChanged();
                        OnPropertyChanged(nameof(PeakY)); // Notify derived property
                    } 
                }
            }

            // Calculated Y position for the peak bar (0 is top, 120 is bottom?)
            // Wait, in Canvas/Grid logic:
            // If we use specific Height=120 container:
            // Value 1.0 -> We want Peak at Top (Y=0 relative to container top? Or Bottom-Up?)
            // If we align Peak to Bottom, then TranslateY = -Peak * 120.
            // Let's assume Pixel Height is 120.
            public double PeakY => -_peakValue * 120.0;

            public int PeakHoldTime { get; set; }

            public event PropertyChangedEventHandler? PropertyChanged;
            private void OnPropertyChanged([CallerMemberName] string? name = null)
                => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public System.Collections.ObjectModel.ObservableCollection<BarData> SpectrumData { get; } 
            = new System.Collections.ObjectModel.ObservableCollection<BarData>();

        private readonly System.Windows.Threading.DispatcherTimer _renderTimer;
        private double[] _latestTargetBars = new double[BarCount];
        private readonly object _lock = new object();

        public MainViewModel()
        {
            _audioService = new AudioCaptureService();
            _spectrumService = new SpectrumService();
            _mediaService = new MediaInfoService();

            UpdateBarBrush(); // Init brush

            // Initialize bars once
            for (int i = 0; i < BarCount; i++)
            {
                SpectrumData.Add(new BarData { Value = 0.01, PeakValue = 0.01 }); 
            }

            _audioService.DataAvailable += OnAudioDataAvailable;
            _mediaService.TrackChanged += OnTrackChanged;
            
            // ... (Rest of constructor)

            // Render Loop: 60 FPS
            _renderTimer = new System.Windows.Threading.DispatcherTimer();
            _renderTimer.Interval = TimeSpan.FromMilliseconds(16); 
            _renderTimer.Tick += OnRenderTick;
            _renderTimer.Start();

            _mediaService.InitializeAsync();
            _audioService.Start();
        }

        private void OnRenderTick(object? sender, EventArgs e)
        {
            double[] targetBars;
            lock (_lock)
            {
                targetBars = (double[])_latestTargetBars.Clone();
            }

            // Apply Gain & Smoothing
            // Target Height = Raw / Peak
            
            for (int i = 0; i < BarCount; i++)
            {
                // Normalize using AGC
                double normalized = targetBars[i] / _maxPeak;
                
                // Silence Gate
                if (_maxPeak < 0.0001) normalized = 0;

                if (normalized > _currentBars[i])
                {
                    // Attack: Fast but not instant (smooth rise)
                    _currentBars[i] = _currentBars[i] + (normalized - _currentBars[i]) * 0.5; 
                }
                else
                {
                    // Decay: Exponential (Gravity looks better)
                    _currentBars[i] *= 0.85; 
                }

                // Clamp
                if (_currentBars[i] > 1.0) _currentBars[i] = 1.0;
                if (_currentBars[i] < 0.01) _currentBars[i] = 0.01; // Keep non-zero scale to avoid flicker
                
                // OPTIMIZATION: Update properties in place
                if (i < SpectrumData.Count)
                {
                    var bar = SpectrumData[i];
                    bar.Value = _currentBars[i];

                    // Peak Logic
                    if (bar.Value >= bar.PeakValue)
                    {
                        bar.PeakValue = bar.Value;
                        bar.PeakHoldTime = 30; // Hold for 30 frames (~0.5 sec)
                    }
                    else
                    {
                        if (bar.PeakHoldTime > 0)
                        {
                            bar.PeakHoldTime--;
                        }
                        else
                        {
                            // Drop gravity
                            bar.PeakValue -= 0.015; // Fall speed
                            if (bar.PeakValue < 0.01) bar.PeakValue = 0.01;
                        }
                    }
                }
            }

            // 3. Bass Pulse Logic
            // Calculate average of the first 8 bars (Deep Bass range)
            double bassSum = 0;
            for(int i=0; i<8; i++) bassSum += _currentBars[i];
            BassAmplitude = bassSum / 8.0;
        }

        private void OnTrackChanged(object? sender, MediaInfoEventArgs e)
        {
            var text = string.IsNullOrEmpty(e.Artist) ? e.Title : $"{e.Artist} - {e.Title}";
            CurrentTrackName = string.IsNullOrEmpty(text) ? "No media playing" : text;
        }

        private void OnAudioDataAvailable(object? sender, NAudio.Wave.WaveInEventArgs e)
        {
            if (e.BytesRecorded == 0) return;
            
            // FftLength is now 4096 -> Spectrum length = 2048
            // Resolution ~10.7 Hz per bin (at 44.1kHz)
            var spectrum = _spectrumService.CalculateSpectrum(e.Buffer, e.BytesRecorded);
            
            double[] newTargets = new double[BarCount];
            double currentFramePeak = 0;

            // 1. Calculate Logarithmic Spectrum
            // We want to map bins to bars logarithmically.
            // Frequency Range: ~20Hz to ~20kHz.
            // Log formula: Index = log(freq)
            
            // Minimal bin index (approx 20Hz). Bin 0 is DC, Bin 1 is ~10Hz, Bin 2 ~20Hz.
            int minBin = 2; 
            int maxBin = spectrum.Length - 1; // Nyquist (approx 22kHz)

            double logMin = Math.Log(minBin);
            double logMax = Math.Log(maxBin);
            
            for (int i = 0; i < BarCount; i++)
            {
                // Determine start and end bin for this bar
                // Map i (0..59) to Log Scale
                double t = (double)i / BarCount;
                double tNext = (double)(i + 1) / BarCount;
                
                // Interpolate in log domain
                double logStart = logMin + (logMax - logMin) * t;
                double logEnd = logMin + (logMax - logMin) * tNext;
                
                int binStart = (int)Math.Exp(logStart);
                int binEnd = (int)Math.Exp(logEnd);
                
                if (binEnd <= binStart) binEnd = binStart + 1; // Ensure at least 1 bin

                // Sum energy in this range
                double sum = 0;
                int count = 0;
                for (int bin = binStart; bin < binEnd && bin < spectrum.Length; bin++)
                {
                    sum += spectrum[bin];
                    count++;
                }
                
                double average = count > 0 ? sum / count : 0;
                
                // Frequency Compensation (Pink Noise requires ~3dB/octave boost to look flat)
                // Bass naturally has more energy. We don't need to boost it much.
                // Treble needs massive boost.
                // New Boost Curve: 0.5 (Bass) -> 6.0 (Treble)
                double boost = 0.5 + (t * t * 8.0); 
                
                // Raw Value
                newTargets[i] = average * boost;
                
                // Track Peak for AGC
                if (newTargets[i] > currentFramePeak) currentFramePeak = newTargets[i];
            }

            // Update AGC
            if (currentFramePeak > _maxPeak)
            {
                _maxPeak = currentFramePeak; // Attack: Instant adaptation to loud sounds
            }
            else
            {
                _maxPeak -= _maxPeak * GainDecay; // Decay: Slowly increase sensitivity
                if (_maxPeak < 0.001) _maxPeak = 0.001; // Floor
            }

            // Publish new targets safely
            lock (_lock)
            {
                _latestTargetBars = newTargets;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
