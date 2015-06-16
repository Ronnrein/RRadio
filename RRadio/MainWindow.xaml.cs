using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Ronnrein.RRadio;

namespace RRadio {

    public partial class MainWindow : Window {

        public RadioPlayer Player { get; private set; }
        public RadioFavorites Stations { get; private set; } 

        public MainWindow() {
            InitializeComponent();

            Player = new RadioPlayer();
            Stations = new RadioFavorites();

            DataContext = this;
        }

        private RadioStation GetSelectedStation() {
            return (RadioStation) ListFavorites.SelectedItem;
        }

        private void ListFavorites_OnMouseDoubleClick(object sender, MouseButtonEventArgs mouseButtonEventArgs) {
            RadioStation station = GetSelectedStation();
            if (station == null || !station.IsUp()) return;
            Player.Station = station;
            Player.Play();
        }

        private void MenuStationsAdd_OnClick(object sender, RoutedEventArgs e) {
            AddStationDialog dialog = new AddStationDialog();
            if (dialog.ShowDialog() == true) {
                Stations.Add(new RadioStation(dialog.ResponseText));
            }
        }

        private void MenuStationsRemove_OnClick(object sender, RoutedEventArgs e) {
            RadioStation station = GetSelectedStation();
            if (station == null) return;
            Stations.Remove(station);
            if (Player.Station == station) {
                Player.Stop();
                Player.Station = null;
            }
        }

        private void MenuStationsPlay_OnClick(object sender, RoutedEventArgs e) {
            RadioStation station = GetSelectedStation();
            if (station == null || !station.IsUp()) return;
            Player.Station = station;
            Player.Play();
        }

        private void MenuStationRefresh_OnClick(object sender, RoutedEventArgs e) {
            Stations.Load();
        }

        private void MenuStationOpenFile(object sender, RoutedEventArgs e) {
            System.Diagnostics.Process.Start(RadioFavorites.Path);
        }

        private void MenuStationCopyURL_OnClick(object sender, RoutedEventArgs e) {
            RadioStation station = GetSelectedStation();
            if (station == null) return;
            Clipboard.SetText(station.URL);
        }
    }
}
