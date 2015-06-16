using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Ronnrein.RRadio {
    public class RadioFavorites : ObservableCollection<RadioStation> {

        public const string Path = "stations.txt";

        public RadioFavorites() {
            Load();
        }

        public void Load() {
            try {
                string[] lines = File.ReadAllLines(Path);
                Clear();
                foreach (string url in lines) {
                    Add(new RadioStation(url));
                }
            }
            catch (FileNotFoundException) {
                using (File.Create(Path)) { }
            }
        }

        protected override void InsertItem(int index, RadioStation item) {
            base.InsertItem(index, item);
            StackTrace stackTrace = new StackTrace();

            // Check if this was called from outside this object, if so append to file
            if (stackTrace.GetFrame(2).GetMethod().DeclaringType != GetType()) {
                Append(item.URL);
            }
        }

        protected override void RemoveItem(int index) {
            string url = Items[index].URL;
            base.RemoveItem(index);
            Remove(url);
        }

        private static void Append(string url) {
            File.AppendAllText(Path, url + Environment.NewLine);
        }

        private void Remove(string url) {
            string[] lines = File.ReadAllLines(Path);
            using (File.Create(Path)) { }
            File.WriteAllLines(Path, lines.Where(l => l != url).ToArray());
        }

    }
}
