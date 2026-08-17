
using Microsoft.Data.SqlClient;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;


namespace WpfBasics
{
    /// <summary>
    /// Logique d'interaction pour PrescriptionsHistory.xaml
    /// </summary>
    public partial class PrescriptionsHistory : UserControl
    {

        private string connectionString = "Server=localhost;Database=WpfBasic;Trusted_Connection=True;Encrypt=False;";

        public ObservableCollection<LignePrescription> Prescription { get; set; } = new();

        private Patient _selectedPatient;
        public Patient SelectedPatient
        {
            get => _selectedPatient;
            set
            {
                _selectedPatient = value;
                LoadPrescriptionsAperçu();
                DataContext = null;
                DataContext = this;
            }
        }
        public PrescriptionsHistory()
        {
            InitializeComponent();
        }

        private void LoadPrescriptionsAperçu()
        {
            Prescription.Clear();
            if (SelectedPatient == null) return;

            try
            {
                using var conn = new SqlConnection(connectionString);
                conn.Open();

                string query = @"
                    SELECT l.idLigne,
                           l.idPrescription,
                           l.nomMedicament,
                           l.posologie,
                           l.quantite,
                           l.dureeTraitement,
                           l.instructions,
                           l.unite,
                           CONCAT(doc.nom, ' ', doc.prenom) AS NomDocteur,
                            p.datePrescription
                    FROM lignePrescription l 
                    INNER JOIN prescription p ON l.idPrescription = p.idPrescription
                    INNER JOIN docteur doc ON p.idDocteur = doc.idDocteur
                    WHERE p.idPatient = @idPatient
                    ORDER BY p.datePrescription DESC, l.idLigne DESC";

                using var cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@idPatient", SelectedPatient.IdPatient);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    Prescription.Add(new LignePrescription
                    {
                        IdLigne = reader["idLigne"].ToString(),
                        IdPrescription = reader["idPrescription"].ToString(),
                        NomMedicament = reader["nomMedicament"].ToString(),
                        Posologie = reader["posologie"].ToString(),
                        Quantite = Convert.ToInt32(reader["quantite"]),
                        DureeTraitement = Convert.ToInt32(reader["dureeTraitement"]),
                        Instructions = reader["instructions"].ToString(),
                        Unite = reader["unite"].ToString(),
                        NomDocteur = reader["NomDocteur"].ToString(),
                        DatePrescription = Convert.ToDateTime(reader["datePrescription"])
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
