﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using System.Web;
using Newtonsoft.Json;

namespace Ronnrein.RRadio {
    public class RadioStation : INotifyPropertyChanged {

        /// <summary>
        /// Name of this radio station
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// URL of this radio station
        /// </summary>
        public string URL { get; private set; }

        /// <summary>
        /// The current Http status code returned by the station URL
        /// </summary>
        public int HttpStatus { get; private set; }

        public RadioSong CurrentSong { get; private set; }

        public string Format { get; private set; }

        public string Genre { get; private set; }

        public int Bitrate { get; private set; }

        public int MetaInt { get; private set; }

        public delegate void ReceivedHttpStatusCodeHandler(RadioStation sender, int status);

        /// <summary>
        /// Gets called whenever a Http status code is received
        /// </summary>
        public static event ReceivedHttpStatusCodeHandler ReceivedHttpStatusCode;

        /// <summary>
        /// Create a new RadioStation object
        /// </summary>
        /// <param name="name">Name of radio station</param>
        /// <param name="url">URL of radio station</param>
        public RadioStation(string url) {
            URL = url;
            Name = url;

            HttpStatus = (int) HttpStatusCode.NotFound;
            LoadMetainfo();
        }

        /// <summary>
        /// Returns whether the radio station appears to be online
        /// </summary>
        /// <returns></returns>
        public bool IsUp() {
            return HttpStatus == (int)HttpStatusCode.OK;
        }

        private async void LoadMetainfo() {
            HttpStatusCode result = HttpStatusCode.NotFound;
            byte[] buffer = new byte[512];

            try {
                HttpWebRequest request = (HttpWebRequest) WebRequest.Create(URL);
                request.Method = "GET";
                request.Headers.Add("Icy-MetaData:1");
                request.UserAgent = "WinampMPEG/5.09";
                using (Task<WebResponse> responseTask = request.GetResponseAsync()) {
                    HttpWebResponse response = (HttpWebResponse) await responseTask;
                    result = response.StatusCode;
                    HttpStatus = (int)result;
                    OnPropertyChanged("HttpStatus");
                    Name = response.GetResponseHeader("icy-name");
                    if (Name == "") {
                        Name = URL;
                    }
                    else {
                        OnPropertyChanged("Name");
                    }
                    if (result == HttpStatusCode.OK) {
                        Genre = response.GetResponseHeader("icy-genre");
                        Format = response.GetResponseHeader("Content-Type");
                        int metaInt;
                        Int32.TryParse(response.GetResponseHeader("icy-metaint"), out metaInt);
                        MetaInt = metaInt;
                        int bitrate;
                        Int32.TryParse(response.GetResponseHeader("icy-br"), out bitrate);
                        Bitrate = bitrate;

                        if (Genre != "") {
                            Genre = Genre.First().ToString().ToUpper() + String.Join("", Genre.Skip(1)).ToLower();
                        }

                        OnPropertyChanged("Genre");
                        OnPropertyChanged("Format");
                        OnPropertyChanged("MetaInt");
                        OnPropertyChanged("Bitrate");

                        try {

                            int metadataLength = 0;
                            string metadataHeader = "";
                            string oldMetadataHeader = "";
                            int count = 0;

                            Stream socketStream = response.GetResponseStream();
                            while (true) {
                                int bufLen = socketStream.Read(buffer, 0, buffer.Length);
                                if (bufLen < 0) {
                                    return;
                                }
                                
                                for (int i = 0; i < bufLen; i++) {
                                    if (metadataLength != 0) {
                                        metadataHeader += Convert.ToChar(buffer[i]);
                                        metadataLength--;
                                        if (metadataLength == 0) {
                                            string fileName = "";
                                            if (!metadataHeader.Equals(oldMetadataHeader)) {
                                                string title = Regex.Match(metadataHeader, "(StreamTitle=')(.+?)(?: \\((.+)\\))?(';)").Groups[2].Value.Trim();
                                                CurrentSong = new RadioSong(title);
                                                OnPropertyChanged("CurrentSong");
                                                return;
                                            }
                                            metadataHeader = "";
                                        }
                                    }
                                    else {
                                        if (count++ >= MetaInt) {
                                            metadataLength = Convert.ToInt32(buffer[i])*16;
                                            if (metadataLength == 0) {
                                                return;
                                            }
                                            count = 0;
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception e) {
                            Console.WriteLine(e.Message);
                        }
                    }
                }
            }
            catch (WebException e) {
                Console.WriteLine(e.Message);
                HttpStatus = (int)((HttpWebResponse) e.Response).StatusCode;
            }
            catch (UriFormatException) { }

            
        }

        public override string ToString() {
            return Name;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
