using Microsoft.Data.SqlClient;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml.Linq;

namespace WpfBasics
{
    public partial class DashboardPatient : UserControl, INotifyPropertyChanged
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
                _selectedPatient = value;
                OnPropertyChanged();
            }
        }

        public ICommand ViewPatientCommand { get; }
        public ICommand DeletePatientCommand { get; }
        public ICommand EditPatientCommand { get; }

        public DashboardPatient()
        {
            InitializeComponent();

            ViewPatientCommand = new RelayCommand(ViewPatient);
            DeletePatientCommand = new RelayCommand(DeletePatient);
            EditPatientCommand = new RelayCommand(EditPatient);

            LoadPatient();

            this.Loaded += Patient_record;
            txtNombrePatients.Text = $"{GetNombrePatients()} patients enregistrés dans la base de donnée";

            DataContext = this;
        }

        //Chargement des données de patient
        private void LoadPatient()
        {
            //Patients.Clear();
            FilteredPatients.Clear();
            using var conn = new SqlConnection(connectionString);
            conn.Open();
            string query = @"
                SELECT idPatient,
                       CONCAT(nom, ' ', prenom) AS NomComplet,
                       nom,
                       prenom,
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
                    Prenom = reader["prenom"].ToString(),
                    Nom = reader["nom"].ToString(),
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

        private int GetNombrePatients()
        {
            using var conn = new SqlConnection(connectionString);
            conn.Open();
            using var cmd = new SqlCommand("SELECT COUNT(*) FROM patient", conn);
            return (int)cmd.ExecuteScalar();
        }

        private void ViewPatient(object parameter)
        {
            if (parameter is Patient p)
                SelectedPatient = p;
        }

        //Supprimer Patient
        private void DeletePatient(object parameter)
        {
            if (parameter is Patient patient)
            {
                var result = MessageBox.Show(
                    $"Voulez-vous vraiment supprimer {patient.NomComplet} ?",
                    "Confirmation de suppression",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    using var conn = new SqlConnection(connectionString);
                    conn.Open();

                    string query = "DELETE FROM patient WHERE idPatient = @id";
                    using var cmd = new SqlCommand(query, conn);
                    cmd.Parameters.AddWithValue("@id", patient.IdPatient);
                    cmd.ExecuteNonQuery();

                    Patients.Remove(patient);

                    txtNombrePatients.Text = $"{GetNombrePatients()} patients enregistrés dans la base de donnée";

                    if (SelectedPatient == patient)
                        SelectedPatient = null;
                }
            }

            LoadPatient();
        }

        private void Patient_record(object sender, RoutedEventArgs e)
        {
            // Sélectionne le premier patient
            if (Patients.Count > 0)
                SelectedPatient = Patients[0];


        }



        //private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        //{
        //    string filter = (SearchBox.Text ?? "").Trim().ToLowerInvariant();

        //    FilteredPatients.Clear();

        //    if (string.IsNullOrWhiteSpace(filter))
        //    {
        //        foreach (var p in Patients)
        //            FilteredPatients.Add(p);
        //    }
        //    else
        //    {
        //        foreach (var p in Patients)
        //        {
        //            if ((p.NomComplet?.ToLowerInvariant().Contains(filter) == true) ||
        //                (p.GroupeSanguin?.ToLowerInvariant().Contains(filter) == true) ||
        //                (p.Initials?.ToLowerInvariant().Contains(filter) == true) ||
        //                (p.Telephone?.Contains(filter) == true))
        //            {
        //                FilteredPatients.Add(p);
        //            }
        //        }
        //    }

        //    // Garde la sélection si le patient est toujours visible
        //    if (SelectedPatient != null && !FilteredPatients.Contains(SelectedPatient))
        //    {
        //        SelectedPatient = FilteredPatients.FirstOrDefault();
        //    }
        //}
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string search = SearchBox.Text?.Trim().ToLower() ?? "";

            FilteredPatients.Clear();

            if (string.IsNullOrEmpty(search))
            {
                foreach (var p in Patients)
                    FilteredPatients.Add(p);
                return;
            }

            
            var terms = search.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var p in Patients)
            {
                
                bool matches = terms.All(term =>
                    (p.Nom?.ToLower().Contains(term) == true) ||
                    (p.Prenom?.ToLower().Contains(term) == true) ||
                    (p.Contact?.ToLower().Contains(term) == true) ||
                    (p.IdPatient?.ToLower().Contains(term) == true) ||
                    ((p.Nom + " " + p.Prenom).ToLower().Contains(term)) 
                );

                if (matches)
                    FilteredPatients.Add(p);
            }
        }
        private void EditPatient(object parameter)
        {
            if (parameter is Patient patient)
            {
                var modal = new ModalPatient(patient);
                bool? result = modal.ShowDialog();

                if (result == true)
                {
                    LoadPatient();
                    txtNombrePatients.Text = $"{GetNombrePatients()} patients enregistrés dans la base de donnée";

                    SelectedPatient = Patients.FirstOrDefault(p => p.IdPatient == patient.IdPatient);
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ModalPatient modal = new ModalPatient();
            bool? result = modal.ShowDialog();

            if (result == true)
            {
                LoadPatient();
                txtNombrePatients.Text = $"{GetNombrePatients()} patients enregistrés dans la base de donnée";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }



    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Func<object, bool> _canExecute;

        public RelayCommand(Action<object> execute, Func<object, bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute == null || _canExecute(parameter);
        public void Execute(object parameter) => _execute(parameter);

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    } 


    }

