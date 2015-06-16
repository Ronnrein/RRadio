using System.Windows;

namespace RRadio {
    
    public partial class AddStationDialog : Window {
        public AddStationDialog() {
            InitializeComponent();
        }

        public string ResponseText {
            get { return TextBoxAddStation.Text; }
            set { TextBoxAddStation.Text = value; }
        }

        private void ButtonOK_OnClick(object sender, RoutedEventArgs e) {
            DialogResult = true;
        }
    }
}
