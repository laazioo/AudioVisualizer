using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Media.Control;
using Windows.Storage.Streams;

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
                    byte[]? thumbData = null;
                    if (props.Thumbnail != null)
                    {
                        try
                        {
                            using var stream = await props.Thumbnail.OpenReadAsync();
                            using var netStream = stream.AsStreamForRead();
                            using var ms = new MemoryStream();
                            await netStream.CopyToAsync(ms);
                            thumbData = ms.ToArray();
                        }
                        catch { }
                    }
                    TrackChanged?.Invoke(this, new MediaInfoEventArgs(props.Title, props.Artist, thumbData));
                }
            }
            catch { /* Ignore errors fetching properties */ }
        }

        public async Task PlayPauseAsync()
        {
            var session = _manager?.GetCurrentSession();
            if (session != null)
            {
                try { await session.TryTogglePlayPauseAsync(); }
                catch { }
            }
        }

        public async Task SkipNextAsync()
        {
            var session = _manager?.GetCurrentSession();
            if (session != null)
            {
                try { await session.TrySkipNextAsync(); }
                catch { }
            }
        }

        public async Task SkipPreviousAsync()
        {
            var session = _manager?.GetCurrentSession();
            if (session != null)
            {
                try { await session.TrySkipPreviousAsync(); }
                catch { }
            }
        }
    }

    public class MediaInfoEventArgs : EventArgs
    {
        public string Title { get; }
        public string Artist { get; }
        public byte[]? ThumbnailData { get; }

        public MediaInfoEventArgs(string title, string artist, byte[]? thumbnailData = null)
        {
            Title = title;
            Artist = artist;
            ThumbnailData = thumbnailData;
        }
    }
}
