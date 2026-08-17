using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.SqlClient;

namespace WpfBasics
{
    public partial class DashboardDocteur : UserControl
    {
        private string connectionString = "Server=localhost;Database=WpfBasic;Trusted_Connection=True;Encrypt=False;";

        // Liste complète (source)
        public ObservableCollection<Docteur> AllDocteurs { get; set; } = new();

        // Liste affichée (filtrée)
        public ObservableCollection<Docteur> Docteurs { get; set; } = new();

        public DashboardDocteur()
        {
            InitializeComponent();
            LoadDoctor();
            DataContext = this;
        }

        private string GetInitials(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName)) return string.Empty;

            var parts = fullName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            StringBuilder sb = new StringBuilder();

            foreach (var p in parts)
            {
                if (p.Length > 0)
                    sb.Append(char.ToUpper(p[0]));
            }

            return sb.ToString();
        }

        private void LoadDoctor()
        {
            AllDocteurs.Clear();
            Docteurs.Clear();

            using var conn = new SqlConnection(connectionString);
            conn.Open();

            string query = @"
                SELECT 
                    d.idDocteur,
                    CONCAT(d.nom, ' ', d.prenom) AS NomComplet,
                    d.nom, d.prenom,
                    d.specialite,
                    d.telephone,
                    d.email,
                    d.sexe,
                    COUNT(DISTINCT c.idConsultation) AS nombreConsultation,
                    COUNT(DISTINCT CASE WHEN CAST(r.dateRdv AS DATE) = CAST(GETDATE() AS DATE) THEN r.idRdv END) AS nombreRendezVousAujourdHui
                FROM docteur d
                LEFT JOIN consultation c ON d.idDocteur = c.idDocteur
                LEFT JOIN rendezVous r ON d.idDocteur = r.idDocteur
                GROUP BY d.idDocteur, d.nom, d.prenom, d.specialite, d.telephone, d.email, d.sexe;";

            using var cmd = new SqlCommand(query, conn);
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var docteur = new Docteur
                {
                    IdDocteur = reader["idDocteur"].ToString(),
                    NomComplet = reader["NomComplet"].ToString(),
                    Nom = reader["nom"].ToString(),
                    Prenom = reader["prenom"].ToString(),
                    Specialite = reader["specialite"].ToString(),
                    Telephone = reader["telephone"].ToString(),
                    Sexe = reader["sexe"].ToString(),
                    Email = reader["email"]?.ToString(),
                    NombreConsultation = Convert.ToInt32(reader["nombreConsultation"]),
                    NombreRdv = Convert.ToInt32(reader["nombreRendezVousAujourdHui"]),
                    Initials = GetInitials(reader["NomComplet"].ToString())
                };

                AllDocteurs.Add(docteur);
                Docteurs.Add(docteur);
            }

            // Mise à jour du compteur
            txtNombrePatients.Text = $"{Docteurs.Count} docteur(s)";
        }

        // ===================== RECHERCHE =====================
        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // On ignore le texte placeholder
            string search = SearchBox.Text?.Trim().ToLower() ?? "";

            if (search == "rechercher..." || string.IsNullOrWhiteSpace(search))
            {
                // Afficher tout
                Docteurs.Clear();
                foreach (var d in AllDocteurs)
                    Docteurs.Add(d);
            }
            else
            {
                // On découpe la recherche en mots ("rakoto jean" → ["rakoto", "jean"])
                var terms = search.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                Docteurs.Clear();

                foreach (var d in AllDocteurs)
                {
                    // Tous les mots doivent être trouvés quelque part
                    bool matches = terms.All(term =>
                        (d.Nom?.ToLower().Contains(term) == true) ||
                        (d.Prenom?.ToLower().Contains(term) == true) ||
                        (d.NomComplet?.ToLower().Contains(term) == true) ||
                        (d.Specialite?.ToLower().Contains(term) == true) ||
                        (d.Email?.ToLower().Contains(term) == true) ||
                        (d.Telephone?.ToLower().Contains(term) == true) ||
                        (d.IdDocteur?.ToLower().Contains(term) == true)
                    );

                    if (matches)
                        Docteurs.Add(d);
                }
            }

            txtNombrePatients.Text = $"{Docteurs.Count} docteur(s)";
        }

        
        private void SearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            if (SearchBox.Text == "Rechercher...")
            {
                SearchBox.Text = "";
                SearchBox.Foreground = System.Windows.Media.Brushes.Black;
            }
        }

        private void SearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(SearchBox.Text))
            {
                SearchBox.Text = "Rechercher...";
                SearchBox.Foreground = System.Windows.Media.Brushes.Gray;
            }
        }

        
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var modal = new ModalDoctor();
            if (modal.ShowDialog() == true)
            {
                LoadDoctor();
            }
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Docteur docteur)
            {
                var modal = new ModalDoctor(docteur);
                if (modal.ShowDialog() == true)
                {
                    LoadDoctor();
                }
            }
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is Docteur docteur)
            {
                var result = MessageBox.Show(
                    $"Supprimer le docteur {docteur.NomComplet} ?",
                    "Confirmation",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        using var conn = new SqlConnection(connectionString);
                        conn.Open();
                        using var cmd = new SqlCommand("DELETE FROM docteur WHERE idDocteur = @id", conn);
                        cmd.Parameters.AddWithValue("@id", docteur.IdDocteur);
                        cmd.ExecuteNonQuery();

                        LoadDoctor();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Erreur lors de la suppression : " + ex.Message);
                    }
                }
            }
        }
    }
}