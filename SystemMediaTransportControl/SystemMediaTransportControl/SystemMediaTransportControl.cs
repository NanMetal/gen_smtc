/*
 * Sharpamp version 0.1 beta
 * $Id$
 * Copyright (C) 2009, Daniel Lo Nigro (Daniel15) <daniel at d15.biz>
 * 
 * This file is part of Sharpamp.
 * 
 * Sharpamp is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Lesser General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * Sharpamp is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Lesser General Public License for more details.
 * 
 * You should have received a copy of the GNU Lesser General Public License
 * along with Sharpamp.  If not, see <http://www.gnu.org/licenses/>.
 */
using Windows.Media;
using Windows.Media.Playback;
using Windows.Storage.Streams;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace SMTC
{
    public class SystemMediaTransportControl
    {
        private delegate IntPtr GetAlbumArtDelegate([MarshalAs(UnmanagedType.LPWStr)] string filename, [MarshalAs(UnmanagedType.LPWStr)] string type);

        private GetAlbumArtDelegate GetAlbumArtFunc;

        private static SystemMediaTransportControlsDisplayUpdater updater;
        private static SystemMediaTransportControls player;

        /// <summary>
        /// Access to the Winamp API
        /// </summary>
        protected Winamp Winamp { get; private set; }

        /// <summary>
        /// Configure the plugin. May open a configuration dialog, or just do nothing.
        /// </summary>
        public virtual void Config()
        {
            // By default, show a messagebox
            System.Windows.Forms.MessageBox.Show("This plugin has no configuration options.",
                "Winamp Plugin Configuration",
                System.Windows.Forms.MessageBoxButtons.OK,
                System.Windows.Forms.MessageBoxIcon.Information);
        }

        /// <summary>
        /// Quit the plugin
        /// </summary>
        public virtual void Quit()
        {

        }

        /// <summary>
        /// Internal initialisation routine
        /// </summary>
        public void Init(IntPtr hWnd)
        {
            Winamp = new Winamp(hWnd);
            Initialize();
        }

        public string Name
        {
            get { return "SystemMediaTransportControl for Windows 10"; }
        }

        public void Initialize()
        {
#pragma warning disable CS0618
            player = BackgroundMediaPlayer.Current.SystemMediaTransportControls;
#pragma warning restore CS0618
            player.ButtonPressed += Player_ButtonPressed;
            updater = player.DisplayUpdater;

            player.IsPlayEnabled = true;
            player.IsPauseEnabled = true;
            player.IsStopEnabled = true;
            player.IsPreviousEnabled = true;
            player.IsNextEnabled = true;

            Winamp.StatusChanged += Winamp_StatusChanged;
            Winamp.SongChanged += Winamp_SongChanged;

            SetSong(Winamp.CurrentSong);
        }

        private void Player_ButtonPressed(SystemMediaTransportControls sender, SystemMediaTransportControlsButtonPressedEventArgs args)
        {
            if (Winamp.Status == Status.Stopped)
            {
                Winamp.Play();
                return;
            }
            switch (args.Button)
            {
                case SystemMediaTransportControlsButton.Pause:
                case SystemMediaTransportControlsButton.Play:
                    Winamp.PlayPause();
                    break;
                case SystemMediaTransportControlsButton.Next:
                    Winamp.NextTrack();
                    break;
                case SystemMediaTransportControlsButton.Previous:
                    Winamp.PreviousTrack();
                    break;
            }
        }

        private void Winamp_SongChanged(object sender, SongChangedEventArgs e)
        {
            SetSong(e.Song);
        }

        private void Winamp_StatusChanged(object sender, StatusChangedEventArgs e)
        {
            switch (e.Status)
            {
                case Status.Paused:
                    player.PlaybackStatus = MediaPlaybackStatus.Paused;
                    break;
                case Status.Playing:
                    player.PlaybackStatus = MediaPlaybackStatus.Playing;
                    break;
                case Status.Stopped:
                    player.PlaybackStatus = MediaPlaybackStatus.Stopped;
                    break;
            }
        }

        public void SetTimeline()
        {
            SystemMediaTransportControlsTimelineProperties timelineProperties = new SystemMediaTransportControlsTimelineProperties();
            int current = Winamp.GetCurrentTrackOutputTime(OutputTimeMode.CurrentPositionMilliseconds);
            int lenght = Winamp.GetCurrentTrackOutputTime(OutputTimeMode.TrackLenghtMilliseconds);

            timelineProperties.StartTime = TimeSpan.FromSeconds(0);
            timelineProperties.MinSeekTime = TimeSpan.FromSeconds(0);
            timelineProperties.Position = TimeSpan.FromMilliseconds(current);
            timelineProperties.MaxSeekTime = TimeSpan.FromSeconds(lenght);
            timelineProperties.EndTime = TimeSpan.FromMilliseconds(lenght);

            player.IsFastForwardEnabled = true;
            player.IsRewindEnabled = true;

            player.UpdateTimelineProperties(timelineProperties);
        }

        /// <summary>
        /// Sets the current song.
        /// </summary>
        /// <param name="song">Song to use.</param>
        private void SetSong(Song song)
        {
            updater.Type = MediaPlaybackType.Music;
            MusicDisplayProperties musicProps = updater.MusicProperties;
            musicProps.Title = song.Title;
            musicProps.Artist = song.Artist;
            musicProps.TrackNumber = GetTrack(song.Track);
            musicProps.AlbumTitle = song.Album;
            musicProps.AlbumArtist = song.Artist;

            // Don't wait
#pragma warning disable CS4014
            SetThumbnailAsync(song.Filename);
#pragma warning restore CS4014
            
            updater.Update();
        }

        /// <summary>
        /// Sets the album art of the selected filename. This method is slow.
        /// </summary>
        /// <param name="filename">Path to file.</param>
        private async Task SetThumbnailAsync(string filename)
        {
            IntPtr image = GetAlbumArt(filename, "cover");
            if (image != IntPtr.Zero)
            {
                using (Bitmap bmp = Image.FromHbitmap(image))
                using (MemoryStream stream = new MemoryStream())
                {
                    bmp.Save(stream, ImageFormat.Png);
                    InMemoryRandomAccessStream randomAccessStream = new InMemoryRandomAccessStream();
                    await randomAccessStream.WriteAsync(stream.ToArray().AsBuffer());

                    // Delete HBITMAP
                    Win32.DeleteObject(image);

                    // Dispose current thumbnail
                    if (updater.Thumbnail != null)
                    {
                        IRandomAccessStreamWithContentType oldStream = await updater.Thumbnail.OpenReadAsync();
                        oldStream.Dispose();
                    }
                    updater.Thumbnail = RandomAccessStreamReference.CreateFromStream(randomAccessStream);
                }
            }
            else
                updater.Thumbnail = null;
        }

        /// <summary>
        /// Parses the current track number.
        /// </summary>
        /// <param name="track">String to parse.</param>
        /// <returns>Sanitized track number.</returns>
        private uint GetTrack(string track)
        {
            if(track.Contains("/"))
            {
                uint.TryParse(track.Split('/')[0], out uint trackNumber);
                return trackNumber;
            }
            else
            {
                uint.TryParse(track, out uint trackNumber);
                return trackNumber;
            }
        }


        /// <summary>
        /// Sets the function pointer for GetAlbumArt(string, string, out int, out int, out IntPtr).
        /// </summary>
        /// <param name="ptr">void* function.</param>
        public void SetAlbumArtFunc(IntPtr ptr)
        {
            GetAlbumArtFunc = (GetAlbumArtDelegate)Marshal.GetDelegateForFunctionPointer(ptr, typeof(GetAlbumArtDelegate));
        }

        /// <summary>
        /// Retrieves the pointer of the desired filename album art.
        /// </summary>
        /// <param name="filename">Filename (path) to use.</param>
        /// <param name="ptr">Pointer to the art data. Must use with GetImageData(IntPtr, int, int)</param>
        /// <returns></returns>
        public IntPtr GetAlbumArt(string filename, string type)
        {
            if (GetAlbumArtFunc != null)
                return GetAlbumArtFunc(filename, type);
            return IntPtr.Zero;
        }
    }
}
