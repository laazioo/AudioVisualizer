using System;
using System.Threading.Tasks;
using Windows.Media.Control;

namespace AudioVisualizer.Services
{
    public class MediaInfoService
    {
        private GlobalSystemMediaTransportControlsSessionManager? _manager;
        
        public event EventHandler<MediaInfoEventArgs>? TrackChanged;

        public async Task InitializeAsync()
        {
            _manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            if (_manager != null)
            {
                _manager.CurrentSessionChanged += Manager_CurrentSessionChanged;
                UpdateCurrentTrack();
            }
        }

        private void Manager_CurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager sender, CurrentSessionChangedEventArgs args)
        {
            UpdateCurrentTrack();
        }

        private async void UpdateCurrentTrack()
        {
            var session = _manager?.GetCurrentSession();
            if (session != null)
            {
                // Subscribe to playback info changes
                session.MediaPropertiesChanged += async (s, e) => await UpdateProperties(session);
                await UpdateProperties(session);
            }
            else
            {
               TrackChanged?.Invoke(this, new MediaInfoEventArgs("Waiting for audio...", ""));
            }
        }

        private async Task UpdateProperties(GlobalSystemMediaTransportControlsSession session)
        {
            try 
            {
                var props = await session.TryGetMediaPropertiesAsync();
                if (props != null)
                {
                    TrackChanged?.Invoke(this, new MediaInfoEventArgs(props.Title, props.Artist));
                }
            }
            catch { /* Ignore errors fetching properties */ }
        }
    }

    public class MediaInfoEventArgs : EventArgs
    {
        public string Title { get; }
        public string Artist { get; }

        public MediaInfoEventArgs(string title, string artist)
        {
            Title = title;
            Artist = artist;
        }
    }
}
