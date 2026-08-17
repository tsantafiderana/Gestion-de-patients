using System.Windows;
using System.Windows.Controls;
using System.Data.SQLite;

namespace WpfBasics
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();            
        }

        private void ListeBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ListBox listBox && listBox.SelectedItem is ListBoxItem item)
            {
                string tag = item.Tag?.ToString();

                switch (tag)
                {
                    case "DashboardOverview":
                        MainContent.Content = new DashboardOverview();
                        break;
                    case "DashboardPatient":
                        MainContent.Content = new DashboardPatient();
                        break;

                    case "MedicalRecords":
                        MainContent.Content = new MedicalRecords();
                        break;
                    case "docteurs":
                        MainContent.Content = new DashboardDocteur();
                        break;
                    case "appointments":
                        MainContent.Content = new DashboardAppointments();
                        break;
                    case "finance":
                        MainContent.Content = new DashboardFinance();
                        break;
                    case "rapportsMedicaux":
                        MainContent.Content = new RapportsMedicaux();
                        break;
                }
            }
        }

        private void ListBoxItem_Selected(object sender, RoutedEventArgs e)
        {

        }
    }
}
