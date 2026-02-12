using NAudio.Wave;
using System;

namespace AudioVisualizer.Services
{
    public class AudioCaptureService : IDisposable
    {
        private WasapiLoopbackCapture? _capture;
        
        public event EventHandler<WaveInEventArgs>? DataAvailable;

        public bool IsRecording { get; private set; }

        public void Start()
        {
            if (IsRecording) return;

            try
            {
                _capture = new WasapiLoopbackCapture();
                _capture.DataAvailable += (s, e) => DataAvailable?.Invoke(this, e);
                _capture.RecordingStopped += (s, e) => IsRecording = false;
                
                _capture.StartRecording();
                IsRecording = true;
            }
            catch (Exception ex)
            {
                // Handle or log error (e.g., no audio device found)
                System.Diagnostics.Debug.WriteLine($"Error starting capture: {ex.Message}");
            }
        }

        public void Stop()
        {
            if (!IsRecording || _capture == null) return;

            _capture.StopRecording();
            _capture.Dispose();
            _capture = null;
            IsRecording = false;
        }

        public void Dispose()
        {
            Stop();
        }
    }
}
