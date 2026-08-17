using Microsoft.Data.SqlClient;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace WpfBasics
{
    public partial class ConsultationHistory : UserControl
    {
        private string connectionString = "Server=localhost;Database=WpfBasic;Trusted_Connection=True;Encrypt=False;";

        public ObservableCollection<Consultation> Consultation { get; set; } = new();

        private Patient _selectedPatient;
        public Patient SelectedPatient
        {
            get => _selectedPatient;
            set
            {
                _selectedPatient = value;
                LoadConsultation();
                DataContext = null;
                DataContext = this;
            }
        }

        public ConsultationHistory()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void LoadConsultation()
        {
            Consultation.Clear();
            if (SelectedPatient == null) return;

            try
            {
                using var conn = new SqlConnection(connectionString);
                conn.Open();

                string query = @"
                    SELECT c.idConsultation,
                           c.idDocteur,
                           CONCAT(d.nom, ' ', d.prenom) AS NomDocteur,
                           c.motif,
                           c.diagnostic,
                           c.observation,
                           c.conclusion,
                           c.dateConsultation
                    FROM consultation c
                    INNER JOIN docteur d ON c.idDocteur = d.idDocteur
                    INNER JOIN dossierMedical dos ON c.idDossier = dos.idDossier
                    WHERE dos.idPatient = @idPatient
                    ORDER BY c.dateConsultation DESC";

                using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@idPatient", SelectedPatient.IdPatient);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    Consultation.Add(new Consultation
                    {
                        IdConsultation = reader["idConsultation"].ToString(),
                        IdDocteur = reader["idDocteur"].ToString(),
                        NomDocteur = reader["NomDocteur"].ToString(),
                        Motif = reader["motif"]?.ToString() ?? "",
                        Diagnostic = reader["diagnostic"]?.ToString() ?? "",
                        Observation = reader["observation"]?.ToString() ?? "",
                        Conclusion = reader["conclusion"]?.ToString() ?? "",
                        DateConsultation = Convert.ToDateTime(reader["dateConsultation"])
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors du chargement des consultations :\n" + ex.Message);
            }
        }

        private void btn_ajouterConsultation_click(object sender, RoutedEventArgs e)
        {
            if (SelectedPatient == null)
            {
                MessageBox.Show("Aucun patient sélectionné.", "Attention",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Création du modal et passage du patient
            var modal = new ModalConsultation
            {
                Patient = SelectedPatient          // ← on passe le patient ici
            };

            var window = new Window
            {
                Title = "Nouvelle consultation",
                Content = modal,
                Width = 560,
                Height = 780,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Owner = Window.GetWindow(this),
                ResizeMode = ResizeMode.NoResize,
                WindowStyle = WindowStyle.None,          // pour un vrai effet modal moderne
                AllowsTransparency = true,
                Background = System.Windows.Media.Brushes.Transparent
            };

            // Fermeture du modal
            modal.Annule += (s, args) =>
            {
                window.Close();
            };

            // Après enregistrement réussi → on rafraîchit la liste
            modal.ConsultationEnregistree += (s, args) =>
            {
                LoadConsultation();     // rafraîchit l’historique
                window.Close();
            };

            window.ShowDialog();
        }
    }
}