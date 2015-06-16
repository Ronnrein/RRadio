using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Ronnrein.RRadio;

namespace RRadio {

    public partial class MainWindow : Window {

        public RadioPlayer RadioPlayer { get; private set; }
        public RadioFavorites Stations { get; private set; } 

        public MainWindow() {
            InitializeComponent();

            RadioPlayer = new RadioPlayer();
            Stations = new RadioFavorites();

            DataContext = this;
        }

        private void ListFavorites_OnMouseDoubleClick(object sender, MouseButtonEventArgs mouseButtonEventArgs) {
            RadioStation station = ((ListView) sender).SelectedItem as RadioStation;
            if (station.IsUp()) {
                RadioPlayer.Station = station;
                RadioPlayer.Play();
            }
        }

        private void MenuStationsAdd_OnClick(object sender, RoutedEventArgs e) {
            AddStationDialog dialog = new AddStationDialog();
            if (dialog.ShowDialog() == true) {
                Stations.Add(new RadioStation(dialog.ResponseText));
            }
            
        }
    }
}
