using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Data.SqlClient;

namespace WpfBasics
{
    public partial class RapportsMedicaux : UserControl
    {
        private readonly string connectionString =
            @"Data Source=.;Initial Catalog=WpfBasic;Integrated Security=True;TrustServerCertificate=True";

        public RapportsMedicaux()
        {
            InitializeComponent();
            Loaded += RapportsMedicaux_Loaded;
        }

        private void RapportsMedicaux_Loaded(object sender, RoutedEventArgs e)
        {
            // Période par défaut : 30 derniers jours
            dpDebut.SelectedDate = DateTime.Today.AddDays(-30);
            dpFin.SelectedDate = DateTime.Today;
            ChargerDonnees();
        }

        private void Filter_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tag)
            {
                DateTime fin = DateTime.Today;
                DateTime debut = tag switch
                {
                    "today" => DateTime.Today,
                    "7days" => DateTime.Today.AddDays(-7),
                    "30days" => DateTime.Today.AddDays(-30),
                    "year" => new DateTime(DateTime.Today.Year, 1, 1),
                    _ => DateTime.Today.AddDays(-30)
                };

                dpDebut.SelectedDate = debut;
                dpFin.SelectedDate = fin;
                ChargerDonnees();
            }
        }

        private void Date_Changed(object sender, SelectionChangedEventArgs e)
        {
            if (dpDebut.SelectedDate != null && dpFin.SelectedDate != null)
                ChargerDonnees();
        }

        private void ChargerDonnees()
        {
            if (dpDebut.SelectedDate == null || dpFin.SelectedDate == null) return;

            DateTime debut = dpDebut.SelectedDate.Value.Date;
            DateTime fin = dpFin.SelectedDate.Value.Date.AddDays(1).AddSeconds(-1);

            try
            {
                using var con = new SqlConnection(connectionString);
                con.Open();

                ChargerCartes(con, debut, fin);
                //ChargerTopDiagnostics(con, debut, fin);
                ChargerConsultationsRecentes(con, debut, fin);
                ChargerActiviteMedecins(con, debut, fin);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors du chargement des rapports :\n" + ex.Message,
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ChargerCartes(SqlConnection con, DateTime debut, DateTime fin)
        {
            // Total consultations
            string q1 = @"SELECT COUNT(*) 
                          FROM consultation 
                          WHERE dateConsultation BETWEEN @d1 AND @d2";
            using (var cmd = new SqlCommand(q1, con))
            {
                cmd.Parameters.AddWithValue("@d1", debut);
                cmd.Parameters.AddWithValue("@d2", fin);
                txtTotalConsultations.Text = cmd.ExecuteScalar()?.ToString() ?? "0";
            }

            // Patients uniques (via dossierMedical)
            string q2 = @"SELECT COUNT(DISTINCT dm.idPatient) 
                          FROM consultation c
                          INNER JOIN dossierMedical dm ON c.idDossier = dm.idDossier
                          WHERE c.dateConsultation BETWEEN @d1 AND @d2";
            using (var cmd = new SqlCommand(q2, con))
            {
                cmd.Parameters.AddWithValue("@d1", debut);
                cmd.Parameters.AddWithValue("@d2", fin);
                txtPatientsUniques.Text = cmd.ExecuteScalar()?.ToString() ?? "0";
            }

            // RDV réalisés
            try
            {
                string q3 = @"SELECT COUNT(*) FROM rendezVous 
                              WHERE dateRendezVous BETWEEN @d1 AND @d2 AND statut = 'Terminé'";
                using var cmd = new SqlCommand(q3, con);
                cmd.Parameters.AddWithValue("@d1", debut);
                cmd.Parameters.AddWithValue("@d2", fin);
                int realises = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
                txtRdvRealises.Text = realises.ToString();

                string q4 = @"SELECT COUNT(*) FROM rendezVous 
                              WHERE dateRendezVous BETWEEN @d1 AND @d2";
                using var cmd2 = new SqlCommand(q4, con);
                cmd2.Parameters.AddWithValue("@d1", debut);
                cmd2.Parameters.AddWithValue("@d2", fin);
                int total = Convert.ToInt32(cmd2.ExecuteScalar() ?? 0);
                double taux = total > 0 ? (realises * 100.0 / total) : 0;
                txtRdvTaux.Text = $"Taux de présence : {taux:0}%";
            }
            catch
            {
                txtRdvRealises.Text = "0";
                txtRdvTaux.Text = "Taux de présence : —";
            }

            // Diagnostics
            string q5 = @"SELECT COUNT(*) 
                          FROM consultation 
                          WHERE dateConsultation BETWEEN @d1 AND @d2 
                            AND diagnostic IS NOT NULL AND diagnostic <> ''";
            using (var cmd = new SqlCommand(q5, con))
            {
                cmd.Parameters.AddWithValue("@d1", debut);
                cmd.Parameters.AddWithValue("@d2", fin);
                txtTotalDiagnostics.Text = cmd.ExecuteScalar()?.ToString() ?? "0";
            }
        }

    

        private void ChargerConsultationsRecentes(SqlConnection con, DateTime debut, DateTime fin)
        {
            var list = new List<ConsultationItem>();
            
            string query = @"
                SELECT TOP 15 
                    c.dateConsultation,
                    p.nom + ' ' + p.prenom AS patient,
                    ISNULL(d.nom + ' ' + d.prenom, '—') AS medecin,
                    c.motif,
                    c.diagnostic
                FROM consultation c
                INNER JOIN dossierMedical dm ON c.idDossier = dm.idDossier
                INNER JOIN patient p ON dm.idPatient = p.idPatient
                LEFT JOIN docteur d ON c.idDocteur = d.idDocteur
                WHERE c.dateConsultation BETWEEN @d1 AND @d2
                ORDER BY c.dateConsultation DESC";

            using var cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@d1", debut);
            cmd.Parameters.AddWithValue("@d2", fin);

            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new ConsultationItem
                {
                    Date = Convert.ToDateTime(reader["dateConsultation"]),
                    Patient = reader["patient"]?.ToString() ?? "",
                    Medecin = reader["medecin"]?.ToString() ?? "—",
                    Motif = reader["motif"]?.ToString() ?? "",
                    Diagnostic = reader["diagnostic"]?.ToString() ?? "—"
                });
            }
            dgConsultations.ItemsSource = list;
        }

        private void ChargerActiviteMedecins(SqlConnection con, DateTime debut, DateTime fin)
        {
            var list = new ObservableCollection<MedecinActivite>();
            string[] couleurs = { "#3B82F6", "#EC4899", "#8B5CF6", "#10B981", "#F59E0B", "#EF4444" };

            string query = @"
                SELECT d.idDocteur, d.nom, d.prenom, d.specialite, COUNT(c.idConsultation) as nb
                FROM docteur d
                LEFT JOIN consultation c ON d.idDocteur = c.idDocteur 
                    AND c.dateConsultation BETWEEN @d1 AND @d2
                GROUP BY d.idDocteur, d.nom, d.prenom, d.specialite
                ORDER BY nb DESC";

            using var cmd = new SqlCommand(query, con);
            cmd.Parameters.AddWithValue("@d1", debut);
            cmd.Parameters.AddWithValue("@d2", fin);

            using var reader = cmd.ExecuteReader();
            int i = 0;
            while (reader.Read())
            {
                string nom = reader["nom"]?.ToString() ?? "";
                string prenom = reader["prenom"]?.ToString() ?? "";
                string initiales = (prenom.Length > 0 ? prenom[0].ToString() : "") +
                                   (nom.Length > 0 ? nom[0].ToString() : "");

                list.Add(new MedecinActivite
                {
                    Nom = $"{prenom} {nom}",
                    Specialite = reader["specialite"]?.ToString() ?? "",
                    NbConsultations = Convert.ToInt32(reader["nb"]),
                    Initiales = initiales.ToUpper(),
                    CouleurAvatar = new SolidColorBrush((Color)ColorConverter.ConvertFromString(couleurs[i % couleurs.Length]))
                });
                i++;
            }
            lstMedecins.ItemsSource = list;
        }

        // ========== Classes helper ==========
        public class DiagnosticItem
        {
            public string Nom { get; set; }
            public int Nombre { get; set; }
            public Brush Couleur { get; set; }
        }

        public class ConsultationItem
        {
            public DateTime Date { get; set; }
            public string Patient { get; set; }
            public string Medecin { get; set; }
            public string Motif { get; set; }
            public string Diagnostic { get; set; }
        }

        public class MedecinActivite
        {
            public string Nom { get; set; }
            public string Specialite { get; set; }
            public int NbConsultations { get; set; }
            public string Initiales { get; set; }
            public Brush CouleurAvatar { get; set; }
        }
    }
}