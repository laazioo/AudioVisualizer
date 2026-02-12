using System;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Windows.Media.Control;
using Windows.Storage.Streams;

namespace AudioVisualizer.Services
{
    public class MediaInfoService
    {
        private GlobalSystemMediaTransportControlsSessionManager? _manager;
        private static readonly HttpClient _httpClient = new HttpClient();
        
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
                    // Try to get high-res YouTube thumbnail first
                    byte[]? thumbData = await TryGetYouTubeThumbnailAsync(props.Title, props.Artist);

                    // Fall back to SMTC thumbnail
                    if (thumbData == null && props.Thumbnail != null)
                    {
                        try
                        {
                            using var stream = await props.Thumbnail.OpenReadAsync();
                            stream.Seek(0);
                            var bytes = new byte[stream.Size];
                            using var reader = new DataReader(stream);
                            await reader.LoadAsync((uint)stream.Size);
                            reader.ReadBytes(bytes);
                            thumbData = bytes;
                        }
                        catch { }
                    }
                    TrackChanged?.Invoke(this, new MediaInfoEventArgs(props.Title, props.Artist, thumbData));
                }
            }
            catch { /* Ignore errors fetching properties */ }
        }

        private async Task<byte[]?> TryGetYouTubeThumbnailAsync(string title, string artist)
        {
            try
            {
                // Build search query from title and artist
                var query = string.IsNullOrEmpty(artist) ? title : $"{artist} {title}";
                if (string.IsNullOrWhiteSpace(query)) return null;

                var encoded = Uri.EscapeDataString(query);
                var searchUrl = $"https://www.youtube.com/results?search_query={encoded}";

                var request = new HttpRequestMessage(HttpMethod.Get, searchUrl);
                request.Headers.Add("User-Agent", "Mozilla/5.0");
                var response = await _httpClient.SendAsync(request);
                
                if (!response.IsSuccessStatusCode) return null;

                var html = await response.Content.ReadAsStringAsync();

                // Extract first video ID from search results
                var match = Regex.Match(html, @"""videoId""\s*:\s*""([a-zA-Z0-9_-]{11})""");
                if (!match.Success) return null;

                var videoId = match.Groups[1].Value;

                // Try maxresdefault first, then hqdefault
                foreach (var quality in new[] { "maxresdefault", "hqdefault", "mqdefault" })
                {
                    try
                    {
                        var thumbUrl = $"https://i.ytimg.com/vi/{videoId}/{quality}.jpg";
                        var thumbResponse = await _httpClient.GetAsync(thumbUrl);
                        if (thumbResponse.IsSuccessStatusCode && thumbResponse.Content.Headers.ContentLength > 1000)
                        {
                            return await thumbResponse.Content.ReadAsByteArrayAsync();
                        }
                    }
                    catch { }
                }
            }
            catch { }
            return null;
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
