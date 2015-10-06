using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Ronnrein.RRadio.Utils;
using RRadio.Annotations;

namespace Ronnrein.RRadio {
    public class RadioSong : INotifyPropertyChanged {

        public String Artist { get; private set; }
        public String ArtistURL { get; private set; }
        public String Name { get; private set; }
        public String TrackURL { get; private set; }
        public String Album { get; private set; }
        public String AlbumURL { get; private set; }
        public String ImageURL { get; private set; }
        public String Genre { get; private set; }

        private string apiKey = "57ee3318536b23ee81d6b27e36997cde";
        private string URL = "http://ws.audioscrobbler.com/2.0/";
        private string defaultImage = "Images/default-thumbnail.png";

        public RadioSong(string info, bool loadMetaInfo = true) {
            string[] split = info.Split('-');
            Artist = split[0].Trim();
            Name = split[1].Trim();
            ImageURL = defaultImage;
            if (loadMetaInfo) {
                LoadMetainfo();
            }
        }

        private async void LoadMetainfo() {
            string json = new WebClient().DownloadString(GetAPIUrl());
            JObject data = JObject.Parse(json);

            JToken track = data["track"];
            if (track == null) {
                return;
            }

            Name = track["name"].ToSafeString();
            TrackURL = track["url"].ToSafeString();

            JToken artist = track["artist"];
            if (artist != null) {
                Artist = artist["name"].ToSafeString();
                ArtistURL = artist["url"].ToSafeString();
            }

            JToken album = track["album"];
            if (album != null) {
                Album = album["title"].ToSafeString();
                AlbumURL = album["url"].ToSafeString();
                ImageURL = album["image"].Single(x => x["size"].ToSafeString() == "medium")["#text"].ToSafeString();
                if (ImageURL == "") {
                    ImageURL = defaultImage;
                }
            }

            JToken tags = track["toptags"];
            if (tags != null) {
                Genre = track["toptags"]["tag"][0].ToSafeString();
            }

            foreach (PropertyInfo propertyInfo in GetType().GetProperties()) {
                OnPropertyChanged(propertyInfo.Name);
            }
        }

        private string GetAPIUrl() {
            NameValueCollection queryString = HttpUtility.ParseQueryString(string.Empty);
            queryString["method"] = "track.getinfo";
            queryString["artist"] = Artist;
            queryString["track"] = Name;
            queryString["api_key"] = apiKey;
            queryString["format"] = "json";
            queryString["limit"] = "1";
            return URL + "?" + queryString;
        }

        public override string ToString() {
            return Artist + " - " + Name;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [NotifyPropertyChangedInvocator]
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null) {
            var handler = PropertyChanged;
            if (handler != null) handler(this, new PropertyChangedEventArgs(propertyName));
        }

        public struct SongInfo {
            
        }
    }
}
