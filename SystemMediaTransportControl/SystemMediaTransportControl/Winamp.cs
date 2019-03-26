using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SMTC
{
    /// <summary>
    /// Winamp API for C#. Allows controlling of Winamp.
    /// </summary>
    public class Winamp
    {
        /// <summary>
        /// Window handle of Winamp.
        /// </summary>
        private IntPtr WinampHwnd { get; }

        private IntPtr _oldWndProc = IntPtr.Zero;
        private Win32.Win32WndProc _winampWndProc;

        #region Winamp API

        private const int WM_WA_IPC = Win32.WM_USER;

        // Callbacks for subclassing the winamp window
        private const int IPC_PLAYING_FILE = 3003;
        private const int IPC_CB_MISC = 603;
        private const int IPC_CB_MISC_TITLE = 0;
        private const int IPC_CB_MISC_STATUS = 2;

        /// <summary>
        /// IPC commands we can send to Winamp
        /// </summary>
        protected enum IPCCommand
        {
            /// <summary>
            /// Get the Winamp version number.
            /// </summary>
            GetVersion = 0,
            /// <summary>
            /// Check whether Winamp is playing, paused, or stopped.
            /// </summary>
            IsPlaying = 104,
            /// <summary>
            /// Get the file name of the currently playing file.
            /// </summary>
            GetFilename = 3031,
            /// <summary>
            /// Get the title of the currently playing song.
            /// </summary>
            GetTitle = 3034,
            /// <summary>
            /// Get information about the currently playing song.
            /// </summary>
            ExtendedFileInfo = 3026,
            /// <summary>
            /// Get the output time of the currently playing song.
            /// </summary>
            GetOutputTime = 105
        }

        /// <summary>
        /// Misc commands we can send to Winamp
        /// </summary>
        protected enum Command
        {
            /// <summary>
            /// Play the current song
            /// </summary>
            Play = 40045,
            /// <summary>
            /// Play or pause the current song
            /// </summary>
            PlayPause = 40046,
            /// <summary>
            /// Stop the current song
            /// </summary>
            Stop = 40047,
            /// <summary>
            /// Go to the previous track
            /// </summary>
            PrevTrack = 40198,
            /// <summary>
            /// Go to the next track
            /// </summary>
            NextTrack = 40048,
        }
        #endregion

        /// <summary>
        /// Event that fires when the current song is changed.
        /// </summary>
        public event SongChangedEventHandler SongChanged;

        /// <summary>
        /// Event that fires when the status is changed.
        /// </summary>
        public event StatusChangedEventHandler StatusChanged;

        /// <summary>
        /// Gets the current song.
        /// </summary>
        public Song CurrentSong { get; private set; }

        private Status _status;
        /// <summary>
        /// Gets the current Winamp status.
        /// </summary>
        public Status Status
        {
            get
            {
                return _status;
            }

            private set
            {
                // Was it not actually changed?
                if (_status == value)
                    return;

                _status = value;

                StatusChanged?.Invoke(this, new StatusChangedEventArgs(value));
            }
        }

        /// <summary>
        /// Create a new instance of the Winamp class.
        /// </summary>
        /// <param name="hWnd">Window handle of Winamp.</param>
        public Winamp(IntPtr hWnd)
        {
            WinampHwnd = hWnd;
            Init();
        }

        /// <summary>
        /// Initialise the Winamp class. Called from the constructor
        /// </summary>
        private void Init()
        {
            CurrentSong = new Song();

            UpdateSongData();
            
            _winampWndProc = new Win32.Win32WndProc(WinampWndProc);

            // Make sure it doesn't get garbage collected
            GC.KeepAlive(_winampWndProc);

            _oldWndProc = Win32.SetWindowLong(WinampHwnd, Win32.GWL_WNDPROC, _winampWndProc);
        }

        /// <summary>
        /// Destructor for Winamp API. Removes the subclassing.
        /// </summary>
        ~Winamp()
        {
            Win32.SetWindowLong(WinampHwnd, Win32.GWL_WNDPROC, _oldWndProc);
        }

        /// <summary>
        /// Handle a message from the Winamp window.
        /// </summary>
        /// <param name="hWnd">Window handle.</param>
        /// <param name="msg">Message type.</param>
        /// <param name="wParam">wParam</param>
        /// <param name="lParam">lParam</param>
        /// <returns></returns>
        private int WinampWndProc(IntPtr hWnd, int msg, int wParam, int lParam)
        {
            if (msg == WM_WA_IPC)
            {
                if (lParam == IPC_CB_MISC)
                {
                    // Start of playing/stop/pause
                    //if (wParam == IPC_CB_MISC_TITLE)
                    //    UpdateSongData();
                    // Start playing/stop/pause/ffwd/rwd
                    /*else*/ if (wParam == IPC_CB_MISC_STATUS || wParam == IPC_CB_MISC_TITLE)
                    {
                        Status = (Status)SendIPCCommandInt(IPCCommand.IsPlaying);
                        UpdateSongData();
                    }
                }
            }

            // Call original handler
            return Win32.CallWindowProc(_oldWndProc, hWnd, msg, wParam, lParam);
        }

        #region Commands
        /// <summary>
        /// Send a command to Winamp via SendMessage().
        /// </summary>
        /// <param name="command">Command to send</param>
        /// <returns>Return value of command</returns>
        protected IntPtr SendIPCCommand(IPCCommand command)
        {
            //return SendMessage(_WinampWindow, WM_WA_IPC, (Int32) command, 0);
            return Win32.SendMessage(WinampHwnd, WM_WA_IPC, (IntPtr)0, (int)command);
        }

        /// <summary>
        /// Send an IPC command to Winamp via SendMessage(), and return an int result.
        /// </summary>
        /// <param name="command">Command to send</param>
        /// <returns>Return value of command</returns>
        protected int SendIPCCommandInt(IPCCommand command)
        {
            return Win32.SendMessage(WinampHwnd, WM_WA_IPC, 0, (int)command);
        }

        /// <summary>
        /// Send a command to Winamp via SendMessage(), and receive a string result.
        /// </summary>
        /// <param name="command">Command to send</param>
        /// <returns>Return value of command</returns>
        protected string SendIPCCommandString(IPCCommand command)
        {
            return Marshal.PtrToStringAuto(Win32.SendMessage(WinampHwnd, WM_WA_IPC, IntPtr.Zero, (Int32)command));
        }

        /// <summary>
        /// Send a command to Winamp via SendMessage().
        /// </summary>
        /// <param name="command"></param>
        protected void SendCommand(Command command)
        {
            Debug.WriteLine("Sending command " + command);
            Win32.SendMessage(WinampHwnd, Win32.WM_COMMAND, (Int32)command, 0);
        }

        /// <summary>
        /// Start playing the current song.
        /// </summary>
        public void Play()
        {
            SendCommand(Command.Play);
        }

        /// <summary>
        /// Stop playing the current song.
        /// </summary>
        public void Stop()
        {
            SendCommand(Command.Stop);
        }

        /// <summary>
        /// If currently playing, pause playback. If currently paused or stopped,
        /// start playing.
        /// </summary>
        public void PlayPause()
        {
            SendCommand(Command.PlayPause);
        }

        /// <summary>
        /// Go to the previous track.
        /// </summary>
        public void PreviousTrack()
        {
            SendCommand(Command.PrevTrack);
        }

        /// <summary>
        /// Go to the next track.
        /// </summary>
        public void NextTrack()
        {
            SendCommand(Command.NextTrack);
        }

        /// <summary>
        /// Get the version of Winamp.
        /// </summary>
        /// <returns>Version number (eg. 5.56)</returns>
        public string GetVersion()
        {
            int version = SendIPCCommand(IPCCommand.GetVersion).ToInt32();
            return string.Format("{0}.{1}", (version & 0x0000FF00) >> 12, version & 0x000000FF);
        }
        #endregion

        /// <summary>
        /// Update the data about the currently playing song
        /// </summary>
        private void UpdateSongData()
        {
            string filename = SendIPCCommandString(IPCCommand.GetFilename);
            
            if (CurrentSong.Filename == filename)
                return;

            bool hasMetadata = true;
            string title = GetMetadata(filename, "title");
            
            if (string.IsNullOrEmpty(title))
            {
                title = SendIPCCommandString(IPCCommand.GetTitle);
                hasMetadata = false;
            }

            string artist = string.Empty;
            string year = string.Empty;
            string album = string.Empty;
            string track = string.Empty;

            if (hasMetadata)
            {
                artist = GetMetadata(filename, "Artist");
                year = GetMetadata(filename, "Year");
                album = GetMetadata(filename, "Album");
                track = GetMetadata(filename, "Track");
            }
            
            Song song = new Song
            {
                HasMetadata = hasMetadata,
                Filename = filename,
                Title = title,
                Artist = artist,
                Album = album,
                Year = year,
                Track = track
            };

            CurrentSong = song;

            SongChanged?.Invoke(this, new SongChangedEventArgs(song));
        }

        /// <summary>
        /// Get metadata about a song.
        /// </summary>
        /// <param name="filename">Filename.</param>
        /// <param name="field">Field to get (artist, album, etc).</param>
        /// <returns>Data contained in this tag.</returns>
        private string GetMetadata(string filename, string field)
        {
            // Create our struct
            Win32.extendedFileInfoStructW data = new Win32.extendedFileInfoStructW
            {
                Metadata = field,
                Filename = filename,
                Ret = new string('\0', 256),
                RetLen = 256
            };
            Win32.SendMessage(WinampHwnd, WM_WA_IPC, ref data, (int)IPCCommand.ExtendedFileInfo);
            return data.Ret;
        }

        public int GetCurrentTrackOutputTime(OutputTimeMode mode)
        {
            return Win32.SendMessage(WinampHwnd, WM_WA_IPC, (int)mode, (int)IPCCommand.GetOutputTime);
        }
    }

    #region Events
    /// <summary>
    /// Represents the method that will handle the SongChangedEvent
    /// </summary>
    /// <param name="sender">Winamp object that sent the event</param>
    /// <param name="e">Arguments for the event</param>
    public delegate void SongChangedEventHandler(object sender, SongChangedEventArgs e);

    /// <summary>
    /// Provides data for the SongChanged event
    /// </summary>
    public class SongChangedEventArgs : EventArgs
    {
        /// <summary>
        /// The song that is currently playing
        /// </summary>
        public Song Song { get; private set; }
        /// <summary>
        /// Create a new instance of SongChangedEventArgs for a specified song
        /// </summary>
        /// <param name="song">The current song</param>
        public SongChangedEventArgs(Song song)
        {
            Song = song;
        }
    }

    /// <summary>
    /// Represents the method that will handle the StatusChangedEvent
    /// </summary>
    /// <param name="sender">Winamp object that sent the event</param>
    /// <param name="e">Arguments for the event</param>
    public delegate void StatusChangedEventHandler(object sender, StatusChangedEventArgs e);

    /// <summary>
    /// Provides data for the StatusChanged event
    /// </summary>
    public class StatusChangedEventArgs : EventArgs
    {
        /// <summary>
        /// The current Winamp status
        /// </summary>
        public Status Status { get; private set; }
        /// <summary>
        /// Create a new instance of StatusChangedEventArgs
        /// </summary>
        /// <param name="status">The current status</param>
        public StatusChangedEventArgs(Status status)
        {
            Status = status;
        }

    }
    #endregion

    /// <summary>
    /// Winamp status
    /// </summary>
    public enum Status
    {
        /// <summary>
        /// Winamp is currently not playing
        /// </summary>
        Stopped = 0,
        /// <summary>
        /// Winamp is currently playing
        /// </summary>
        Playing = 1,
        /// <summary>
        /// Winamp is currently paused
        /// </summary>
        Paused = 3
    }

    public enum OutputTimeMode
    {
        CurrentPositionMilliseconds = 0,
        TrackLenghtSeconds = 1,
        TrackLenghtMilliseconds = 2
    }

    /// <summary>
    /// Contains song data.
    /// </summary>
    public class Song
    {
        public string Title { get; internal set; }

        public string Artist { get; internal set; }
        
        public string Album { get; internal set; }
        
        public string Year { get; internal set; }
        /// <summary>
        /// Whether the song has any metadata. If false, only the title will be
        /// available.
        /// </summary>
        public bool HasMetadata { get; internal set; }
        
        public string Filename { get; internal set; }
    
        public string Track { get; internal set; }
    }
}
