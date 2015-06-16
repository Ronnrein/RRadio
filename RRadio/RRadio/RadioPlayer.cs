using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Timers;
using System.Windows;
using NAudio.Wave;
using Timer = System.Timers.Timer;

namespace Ronnrein.RRadio {

    public class RadioPlayer : INotifyPropertyChanged {

        /// <summary>
        /// The different playbach states possible
        /// </summary>
        public enum StreamPlaybackState {
            Stopped,
            Playing,
            Buffering,
            Paused
        }

        /// <summary>
        /// The volume of the player
        /// </summary>
        public float Volume {
            get { return volume; }
            set {
                volume = (value < 0) ? 0 : (value > 1.0f) ? 1.0f : value;
                volumeProvider.Volume = volume;
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// The number of seconds currently buffered
        /// </summary>
        public double BufferedSeconds { get; private set; }

        /// <summary>
        /// The percent currently buffered of the maximum
        /// </summary>
        public int BufferedPercent {
            get { return (int) (bufferMaxMilliseconds != 0 ? ((BufferedSeconds*1000)/bufferMaxMilliseconds)*100 : 0); }
        }

        /// <summary>
        /// The current station being streamed from
        /// </summary>
        public RadioStation Station {
            get { return currentStation; }
            set {
                currentStation = value;
                Stop();
                OnPropertyChanged();
            }
        }

        /// <summary>
        /// The current playbach state
        /// </summary>
        public StreamPlaybackState PlaybackState {
            get { return playbackState; }
        }

        private volatile StreamPlaybackState playbackState;
        private BufferedWaveProvider bufferedWaveProvider;
        private IWavePlayer waveOut;
        private volatile bool fullyDownloaded;
        private HttpWebRequest webRequest;
        private VolumeWaveProvider16 volumeProvider;
        private RadioStation currentStation;
        private Timer timer = new Timer();
        private float volume;
        private int bufferMaxMilliseconds;
        private RadioStream readFullyStream;

        /// <summary>
        /// Create a new RadioPlayer
        /// </summary>
        public RadioPlayer() {
            timer.Interval = 250;
            timer.AutoReset = true;
            timer.Elapsed += timer_Elapsed;

            volume = 1;
        }

        /// <summary>
        /// Start playback of stream
        /// </summary>
        public void Play() {
            if (playbackState == StreamPlaybackState.Stopped) {
                playbackState = StreamPlaybackState.Buffering;
                bufferedWaveProvider = null;
                ThreadPool.QueueUserWorkItem(Stream, currentStation.URL);
                timer.Enabled = true;
                OnPropertyChanged("PlaybackState");
            }
            else if (playbackState == StreamPlaybackState.Paused) {
                playbackState = StreamPlaybackState.Buffering;
                OnPropertyChanged("PlaybackState");
            }
        }

        /// <summary>
        /// Stop playback of stream
        /// </summary>
        public void Stop() {
            if (playbackState != StreamPlaybackState.Stopped) {
                if (!fullyDownloaded) {
                    webRequest.Abort();
                }
                playbackState = StreamPlaybackState.Stopped;
                OnPropertyChanged("PlaybackState");
                if (waveOut != null) {
                    waveOut.Stop();
                    waveOut.Dispose();
                    waveOut = null;
                }
                timer.Enabled = false;
                playbackState = StreamPlaybackState.Stopped;
                Thread.Sleep(500);
            }
        }

        /// <summary>
        /// Pause playback of stream
        /// </summary>
        public void Pause() {
            if (playbackState == StreamPlaybackState.Playing || playbackState == StreamPlaybackState.Buffering) {
                waveOut.Pause();
                playbackState = StreamPlaybackState.Paused;
                OnPropertyChanged("PlaybackState");
            }
        }

        private void Stream(object urlObj) {
            fullyDownloaded = false;
            string url = (string)urlObj;
            webRequest = (HttpWebRequest)WebRequest.Create(url);
            HttpWebResponse response;
            try {
                response = (HttpWebResponse)webRequest.GetResponse();
            }
            catch (WebException e) {
                if (e.Status != WebExceptionStatus.RequestCanceled) {
                    MessageBox.Show(e.Message);
                }
                return;
            }
            byte[] buffer = new byte[16384 * 4];

            IMp3FrameDecompressor decompressor = null;
            try {
                using (Stream responseStream = response.GetResponseStream()) {
                    readFullyStream = new RadioStream(responseStream);
                    readFullyStream.MetaInt = Station.MetaInt;
                    do {
                        if (IsBufferNearlyFull()) {
                            // Take a break
                            Thread.Sleep(500);
                        }
                        else {
                            Mp3Frame frame;
                            try {
                                frame = Mp3Frame.LoadFromStream(readFullyStream);
                            }
                            catch (EndOfStreamException) {
                                // End of stream reached, break
                                fullyDownloaded = true;
                                break;
                            }
                            catch (WebException) {
                                // Silently catch exception, probably aborted from other thread
                                break;
                            }
                            if (decompressor == null) {
                                decompressor = CreateFrameDecompressor(frame);
                                bufferedWaveProvider = new BufferedWaveProvider(decompressor.OutputFormat) {
                                    BufferDuration = TimeSpan.FromSeconds(20)
                                };
                            }
                            int decompressed = decompressor.DecompressFrame(frame, buffer, 0);
                            bufferedWaveProvider.AddSamples(buffer, 0, decompressed);
                        }
                    } while (playbackState != StreamPlaybackState.Stopped);
                    if (decompressor != null) decompressor.Dispose();
                }
            }
            finally {
                if (decompressor != null) {
                    decompressor.Dispose();
                }
            }
        }

        private void timer_Elapsed(object sender, ElapsedEventArgs e) {
            if (playbackState != StreamPlaybackState.Stopped) {
                if (waveOut == null && bufferedWaveProvider != null) {
                    waveOut = new WaveOut();
                    waveOut.PlaybackStopped += OnPlaybackStopped;
                    volumeProvider = new VolumeWaveProvider16(bufferedWaveProvider) {Volume = volume};
                    waveOut.Init(volumeProvider);
                    bufferMaxMilliseconds = (int) bufferedWaveProvider.BufferDuration.TotalMilliseconds;
                }
                else if (bufferedWaveProvider != null) {
                    BufferedSeconds = bufferedWaveProvider.BufferDuration.TotalSeconds;
                    // Make sure enough is buffered
                    if (BufferedSeconds < 0.5 && playbackState == StreamPlaybackState.Playing && !fullyDownloaded) {
                        Pause();
                    }
                    else if (BufferedSeconds > 4 && playbackState == StreamPlaybackState.Buffering) {
                        StartPlayback();
                    }
                    else if (fullyDownloaded && BufferedSeconds == 0) {
                        Stop();
                    }
                }
                OnPropertyChanged("BufferedSeconds");
                OnPropertyChanged("BufferedPercent");
            }
        }

        private void StartPlayback() {
            waveOut.Play();
            playbackState = StreamPlaybackState.Playing;
            OnPropertyChanged("PlaybackState");
        }

        private void OnPlaybackStopped(object sender, StoppedEventArgs e) {
            if (e.Exception != null) {
                MessageBox.Show(e.Exception.Message);
            }
        }

        private bool IsBufferNearlyFull() {
            return bufferedWaveProvider != null &&
                   bufferedWaveProvider.BufferLength - bufferedWaveProvider.BufferedBytes
                   < bufferedWaveProvider.WaveFormat.AverageBytesPerSecond / 4;
        }

        private static IMp3FrameDecompressor CreateFrameDecompressor(Mp3Frame frame) {
            WaveFormat waveFormat = new Mp3WaveFormat(frame.SampleRate, frame.ChannelMode == ChannelMode.Mono ? 1 : 2,
                                                      frame.FrameLength, frame.BitRate);
            return new AcmMp3FrameDecompressor(waveFormat);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}