using Microsoft.Data.SqlClient;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;

namespace WpfBasics
{
    public partial class MedicalRecords : UserControl, INotifyPropertyChanged
    {
        private string connectionString = "Server=localhost;Database=WpfBasic;Trusted_Connection=True;Encrypt=False;";

        public ObservableCollection<Patient> Patients { get; set; } = new();
        public ObservableCollection<Patient> FilteredPatients { get; set; } = new();

        private Patient _selectedPatient;
        public Patient SelectedPatient
        {
            get => _selectedPatient;
            set
            {
                if (_selectedPatient == value) return;

                _selectedPatient = value;
                OnPropertyChanged();

                // Mise à jour de la vue actuellement affichée
                if (ChangedContent?.Content is OverViewMedicalRecords overview)
                    overview.SelectedPatient = value;
                else if (ChangedContent?.Content is ConsultationHistory history)
                    history.SelectedPatient = value;
                else if (ChangedContent?.Content is PrescriptionsHistory prescriptions)
                    prescriptions.SelectedPatient = value;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public MedicalRecords()
        {
            InitializeComponent();
            DataContext = this;
            LoadPatient();

            this.Loaded += MedicalRecords_Loaded;
        }

        private void MedicalRecords_Loaded(object sender, RoutedEventArgs e)
        {
            // Sélectionne le premier patient
            if (FilteredPatients.Count > 0)
                SelectedPatient = FilteredPatients[0];

            // Sélectionne l’onglet Aperçu par défaut
            if (NavListBox.Items.Count > 0)
                NavListBox.SelectedIndex = 0;
        }

        private void LoadPatient()
        {
            Patients.Clear();
            FilteredPatients.Clear();

            using var conn = new SqlConnection(connectionString);
            conn.Open();

            string query = @"
                SELECT idPatient,
                       CONCAT(nom, ' ', prenom) AS NomComplet,
                       dateNaissance,
                       genre,
                       email,
                       telephone,
                       adresse,
                       groupeSanguin
                FROM patient";

            using var cmd = new SqlCommand(query, conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var naissance = Convert.ToDateTime(reader["dateNaissance"]);
                int age = DateTime.Today.Year - naissance.Year;
                if (naissance.Date > DateTime.Today.AddYears(-age)) age--;

                var patient = new Patient
                {
                    IdPatient = reader["idPatient"].ToString(),
                    NomComplet = reader["NomComplet"].ToString(),
                    DateNaissance = naissance,
                    Age = age,
                    Sexe = reader["genre"].ToString(),
                    Contact = reader["email"]?.ToString(),
                    Telephone = reader["telephone"]?.ToString() ?? "",
                    Adresse = reader["adresse"]?.ToString() ?? "",
                    GroupeSanguin = reader["groupeSanguin"]?.ToString() ?? ""
                };

                Patients.Add(patient);
                FilteredPatients.Add(patient);
            }
        }

        // Recherche en temps réel
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string filter = (SearchBox.Text ?? "").Trim().ToLowerInvariant();

            FilteredPatients.Clear();

            if (string.IsNullOrWhiteSpace(filter))
            {
                foreach (var p in Patients)
                    FilteredPatients.Add(p);
            }
            else
            {
                foreach (var p in Patients)
                {
                    if ((p.NomComplet?.ToLowerInvariant().Contains(filter) == true) ||
                        (p.GroupeSanguin?.ToLowerInvariant().Contains(filter) == true) ||
                        (p.Initials?.ToLowerInvariant().Contains(filter) == true) ||
                        (p.Telephone?.Contains(filter) == true))
                    {
                        FilteredPatients.Add(p);
                    }
                }
            }

            // Garde la sélection si le patient est toujours visible
            if (SelectedPatient != null && !FilteredPatients.Contains(SelectedPatient))
            {
                SelectedPatient = FilteredPatients.FirstOrDefault();
            }
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Protection contre les appels trop précoces
            if (!IsLoaded) return;

            if (sender is not ListBox listBox || listBox.SelectedItem is not ListBoxItem item)
                return;

            string tag = item.Tag?.ToString();

            switch (tag)
            {
                case "overview":
                    var overview = new OverViewMedicalRecords();
                    overview.SelectedPatient = this.SelectedPatient;
                    ChangedContent.Content = overview;
                    break;

                case "consultations":
                    var consultation = new ConsultationHistory();
                    consultation.SelectedPatient = this.SelectedPatient;
                    ChangedContent.Content = consultation;
                    break;

                case "prescriptions":
                    var prescription = new PrescriptionsHistory();
                    prescription.SelectedPatient = this.SelectedPatient;
                    ChangedContent.Content = prescription;
                    break;
            }
        }
    }
}