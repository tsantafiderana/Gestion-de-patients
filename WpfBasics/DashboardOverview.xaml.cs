using Microsoft.Data.SqlClient;
using System.Collections.ObjectModel;
using System.Windows.Controls;

namespace WpfBasics
{
    public partial class DashboardOverview : UserControl
    {
        public ObservableCollection<RendezVous> Planning { get; set; }

        private string connectionString = "Server=localhost;Database=WpfBasic;Trusted_Connection=True;Encrypt=False;";

        public DashboardOverview()
        {
            InitializeComponent();

            Planning = new ObservableCollection<RendezVous>();
            LoadRendezVous();

            DataContext = this;
        }

        private void LoadRendezVous()
        {
            Planning.Clear();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string query = @"SELECT 
                                    heureRdv, 
                                    description, 
                                    patientId, 
                                    CONCAT(p.nom, ' ', p.prenom) AS Identifiant
                                FROM RendezVous r
                                JOIN Patient p ON r.patientId = p.idPatient
                                WHERE r.dateRdv = CAST(GETDATE() AS DATE);
                                ";

                using (SqlCommand cmd = new SqlCommand(query, conn))
                using (SqlDataReader reader = cmd.ExecuteReader())
                {
                    int index = 0;
                    while (reader.Read())
                    {
                        string heure = reader["heureRdv"].ToString();
                        string description = reader["description"].ToString();
                        string patient = reader["identifiant"].ToString();

                        string bg = (index == 0) ? "#f7faff" : "#fafefc";
                        string border = (index == 0) ? "#00355f" : "#1da548";

                        Planning.Add(new RendezVous
                        {
                            //Heure = heure,
                            //Nom = patient,
                            Description = description,
                            //Background = bg,
                            //BorderColor = border
                        });

                        index++;
                    }
                }
            }
        }

       
    }

 
}
