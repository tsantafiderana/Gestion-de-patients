using Microsoft.Data.SqlClient;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace WpfBasics
{
    public partial class OverViewMedicalRecords : UserControl
    {
        private string connectionString = "Server=localhost;Database=WpfBasic;Trusted_Connection=True;Encrypt=False;";

        public ObservableCollection<Consultation> ConsultationsApercu { get; set; } = new();
        public ObservableCollection<LignePrescription> PrescriptionsAperçu { get; set; } = new();

        private Patient _selectedPatient;
        public Patient SelectedPatient
        {
            get => _selectedPatient;
            set
            {
                _selectedPatient = value;
                LoadConsultationsApercu();
                LoadPrescriptionsAperçu();

         
                DataContext = null;
                DataContext = this;
            }
        }

        public OverViewMedicalRecords()
        {
            InitializeComponent();
            DataContext = this;
        }

        private void LoadConsultationsApercu()
        {
            ConsultationsApercu.Clear();
            if (SelectedPatient == null) return;

            try
            {
                using var conn = new SqlConnection(connectionString);
                conn.Open();

                string query = @"
                    SELECT TOP 3 
                           c.idConsultation,
                           c.idDocteur,
                           CONCAT(d.nom, ' ', d.prenom) AS NomDocteur,
                           c.motif,
                           c.diagnostic,
                           c.observation,
                           c.conclusion,
                           c.dateConsultation
                    FROM Consultation c
                    INNER JOIN Docteur d ON c.idDocteur = d.idDocteur
                    INNER JOIN dossierMedical dos ON c.idDossier = dos.idDossier
                    WHERE dos.idPatient = @idPatient
                    ORDER BY c.dateConsultation DESC";

                using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@idPatient", SelectedPatient.IdPatient);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    ConsultationsApercu.Add(new Consultation
                    {
                        IdConsultation = reader["idConsultation"].ToString(),
                        IdDocteur = reader["idDocteur"].ToString(),
                        NomDocteur = reader["NomDocteur"].ToString(),
                        Motif = reader["motif"].ToString(),
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

        private void LoadPrescriptionsAperçu()
        {
            PrescriptionsAperçu.Clear();
            if (SelectedPatient == null) return;

            try
            {
                using var conn = new SqlConnection(connectionString);
                conn.Open();

                string query = @"
                    SELECT TOP 3 
                           l.idLigne,
                           l.idPrescription,
                           l.nomMedicament,
                           l.posologie,
                           l.quantite,
                           l.dureeTraitement,
                           l.instructions,
                           l.unite
                    FROM lignePrescription l 
                    INNER JOIN prescription p ON l.idPrescription = p.idPrescription
                    WHERE p.idPatient = @idPatient
                    ORDER BY p.datePrescription DESC, l.idLigne DESC";

                using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@idPatient", SelectedPatient.IdPatient);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    PrescriptionsAperçu.Add(new LignePrescription
                    {
                        IdLigne = reader["idLigne"].ToString(),
                        IdPrescription = reader["idPrescription"].ToString(),
                        NomMedicament = reader["nomMedicament"].ToString(),
                        Posologie = reader["posologie"].ToString(),
                        Quantite = Convert.ToInt32(reader["quantite"]),
                        DureeTraitement = Convert.ToInt32(reader["dureeTraitement"]),
                        Instructions = reader["instructions"].ToString(),
                        Unite = reader["unite"].ToString()
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors du chargement des prescriptions :\n" + ex.Message);
            }
        }
    }
}