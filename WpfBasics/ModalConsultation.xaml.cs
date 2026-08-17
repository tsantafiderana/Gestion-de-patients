using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Data.SqlClient;

namespace WpfBasics
{
    public partial class ModalConsultation : UserControl
    {
        public event EventHandler<ConsultationEventArgs> ConsultationEnregistree;
        public event EventHandler Annule;

        private readonly string _connectionString =
            "Server=localhost;Database=WpfBasic;Trusted_Connection=True;Encrypt=False;";

        // ========== NOUVEAU : Patient reçu depuis ConsultationHistory ==========
        public Patient Patient { get; set; }

        public ModalConsultation()
        {
            InitializeComponent();
            Loaded += ModalConsultation_Loaded;
        }

        private void ModalConsultation_Loaded(object sender, RoutedEventArgs e)
        {
            ChargerDocteurs();
            ChargerRendezVousDuPatient();          // ← version filtrée
            DpDateConsultation.SelectedDate = DateTime.Today;

            // Pré-remplir le dossier médical si possible
            if (Patient != null && !string.IsNullOrEmpty(Patient.IdDossier))
            {
                TxtIdDossier.Text = Patient.IdDossier;
            }
        }

        private void ChargerDocteurs()
        {
            try
            {
                var liste = new List<DocteurItem>();

                using var conn = new SqlConnection(_connectionString);
                conn.Open();

                string sql = "SELECT idDocteur, nom, prenom, specialite FROM docteur ORDER BY nom";

                using var cmd = new SqlCommand(sql, conn);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    liste.Add(new DocteurItem
                    {
                        IdDocteur = reader["idDocteur"].ToString(),
                        Nom = reader["nom"].ToString(),
                        Prenom = reader["prenom"].ToString(),
                        Specialite = reader["specialite"]?.ToString()
                    });
                }

                CmbDocteur.ItemsSource = liste;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur chargement médecins : " + ex.Message, "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        //private void ChargerRendezVousDuPatient()
        //{
        //    try
        //    {
        //        if (Patient == null)
        //        {
        //            CmbRendezVous.ItemsSource = null;
        //            return;
        //        }

        //        var liste = new List<RendezVousItem>();

        //        using var conn = new SqlConnection(_connectionString);
        //        conn.Open();

        //        // On prend les RDV du patient (tu peux ajouter un filtre de date si tu veux)
        //        string sql = @"
        //            SELECT r.idRdv, r.dateRdv, p.nom, p.prenom, d.idDossier
        //            FROM rendezVous r
        //            INNER JOIN patient p ON r.idPatient = p.idPatient
        //            LEFT JOIN dossierMedical d ON p.idPatient = d.idPatient
        //            WHERE r.idPatient = @idPatient
        //            ORDER BY r.dateRdv DESC";

        //        using var cmd = new SqlCommand(sql, conn);
        //        cmd.Parameters.AddWithValue("@idPatient", Patient.IdPatient);

        //        using var reader = cmd.ExecuteReader();

        //        while (reader.Read())
        //        {
        //            var date = Convert.ToDateTime(reader["dateRdv"]);
        //            liste.Add(new RendezVousItem
        //            {
        //                IdRendezVous = reader["idRdv"].ToString(),
        //                IdDossier = reader["idDossier"]?.ToString(),
        //                Libelle = $"{date:dd/MM/yyyy HH:mm} - {reader["nom"]} {reader["prenom"]}"
        //            });
        //        }

        //        CmbRendezVous.ItemsSource = liste;

        //        // Si un seul RDV → on le sélectionne automatiquement
        //        if (liste.Count == 1)
        //        {
        //            CmbRendezVous.SelectedIndex = 0;
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        MessageBox.Show("Erreur chargement rendez-vous du patient : " + ex.Message,
        //            "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
        //    }
        //}

        //private void CmbRendezVous_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    if (CmbRendezVous.SelectedItem is RendezVousItem rv)
        //    {
        //        TxtIdDossier.Text = rv.IdDossier ?? Patient?.IdDossier ?? "";
        //    }
        //}

        // ========== VERSION FILTRÉE : uniquement les RDV du patient AUJOURD'HUI ==========
        private void ChargerRendezVousDuPatient()
        {
            try
            {
                if (Patient == null)
                {
                    CmbRendezVous.ItemsSource = null;
                    return;
                }

                var liste = new List<RendezVousItem>();

                using var conn = new SqlConnection(_connectionString);
                conn.Open();

                // On prend UNIQUEMENT les RDV du patient pour AUJOURD'HUI
                string sql = @"
            SELECT r.idRdv, 
                   r.dateRdv, 
                   r.idDocteur,
                   p.nom, 
                   p.prenom, 
                   d.idDossier,
                   CONCAT(doc.nom, ' ', doc.prenom) AS NomDocteur
            FROM rendezVous r
            INNER JOIN patient p ON r.idPatient = p.idPatient
            LEFT JOIN dossierMedical d ON p.idPatient = d.idPatient
            LEFT JOIN docteur doc ON r.idDocteur = doc.idDocteur
            WHERE r.idPatient = @idPatient
              AND CAST(r.dateRdv AS DATE) = CAST(GETDATE() AS DATE)
            ORDER BY r.dateRdv ASC";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@idPatient", Patient.IdPatient);

                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    var date = Convert.ToDateTime(reader["dateRdv"]);

                    liste.Add(new RendezVousItem
                    {
                        IdRendezVous = reader["idRdv"].ToString(),
                        IdDossier = reader["idDossier"]?.ToString(),
                        IdDocteur = reader["idDocteur"]?.ToString(),          // ← important
                        NomDocteur = reader["NomDocteur"]?.ToString(),
                        Libelle = $"{date:HH:mm} - {reader["nom"]} {reader["prenom"]}" +
                                  (reader["NomDocteur"] != DBNull.Value ? $" ({reader["NomDocteur"]})" : "")
                    });
                }

                CmbRendezVous.ItemsSource = liste;

                // S’il n’y a qu’un seul RDV aujourd’hui → on le sélectionne automatiquement
                if (liste.Count == 1)
                {
                    CmbRendezVous.SelectedIndex = 0;
                }
                else if (liste.Count == 0)
                {
                    MessageBox.Show("Aucun rendez-vous trouvé pour ce patient aujourd’hui.",
                        "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur chargement rendez-vous du patient : " + ex.Message,
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void CmbRendezVous_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbRendezVous.SelectedItem is not RendezVousItem rv)
                return;

            // 1. Remplir le dossier médical
            TxtIdDossier.Text = rv.IdDossier ?? Patient?.IdDossier ?? "";

            // 2. Sélectionner automatiquement le médecin
            if (!string.IsNullOrEmpty(rv.IdDocteur))
            {
                // On cherche le docteur dans la liste déjà chargée
                foreach (DocteurItem doc in CmbDocteur.Items)
                {
                    if (doc.IdDocteur == rv.IdDocteur)
                    {
                        CmbDocteur.SelectedItem = doc;
                        break;
                    }
                }
            }
        }

        private void ChkGenererFacture_Checked(object sender, RoutedEventArgs e)
        {
            PanelFacture.Visibility = Visibility.Visible;
            if (!string.IsNullOrWhiteSpace(TxtMotif.Text))
                CmbServiceFacture.Text = TxtMotif.Text;
        }

        private void ChkGenererFacture_Unchecked(object sender, RoutedEventArgs e)
        {
            PanelFacture.Visibility = Visibility.Collapsed;
        }

        private void TxtMontant_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^[0-9.,]+$");
        }

        private void BtnEnregistrer_Click(object sender, RoutedEventArgs e)
        {
            if (CmbDocteur.SelectedItem is not DocteurItem docteur)
            {
                MessageBox.Show("Veuillez sélectionner un médecin.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (CmbRendezVous.SelectedItem is not RendezVousItem rendezVous)
            {
                MessageBox.Show("Veuillez sélectionner un rendez-vous.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrWhiteSpace(TxtMotif.Text))
            {
                MessageBox.Show("Le motif est obligatoire.", "Validation",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            decimal? montantFacture = null;
            string serviceFacture = null;
            bool encaisser = false;

            if (ChkGenererFacture.IsChecked == true)
            {
                serviceFacture = CmbServiceFacture.Text?.Trim();

                if (string.IsNullOrWhiteSpace(serviceFacture))
                {
                    MessageBox.Show("Veuillez indiquer le service de facturation.", "Validation",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                string montantTexte = TxtMontantFacture.Text.Replace(',', '.');

                if (!decimal.TryParse(montantTexte, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal montant)
                    || montant <= 0)
                {
                    MessageBox.Show("Veuillez saisir un montant valide.", "Validation",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                montantFacture = montant;
                encaisser = ChkEncaisserMaintenant.IsChecked == true;
            }

            var args = new ConsultationEventArgs
            {
                IdDocteur = docteur.IdDocteur,
                IdRendezVous = rendezVous.IdRendezVous,
                IdDossier = string.IsNullOrWhiteSpace(TxtIdDossier.Text) ? null : TxtIdDossier.Text.Trim(),
                Motif = TxtMotif.Text.Trim(),
                Diagnostic = TxtDiagnostic.Text?.Trim(),
                Observation = TxtObservation.Text?.Trim(),
                Conclusion = TxtConclusion.Text?.Trim(),
                DateConsultation = DpDateConsultation.SelectedDate ?? DateTime.Today,
                GenererFacture = ChkGenererFacture.IsChecked == true,
                ServiceFacture = serviceFacture,
                MontantFacture = montantFacture,
                EncaisserMaintenant = encaisser
            };

            // Enregistrement
            EnregistrerConsultation(args);
        }

        public void EnregistrerConsultation(ConsultationEventArgs e)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                conn.Open();
                using var transaction = conn.BeginTransaction();

                try
                {
                    string idConsultation = GenererNouvelIdConsultation(conn, transaction);

                    string sqlConsultation = @"
                        INSERT INTO consultation
                            (idConsultation, idDocteur, idRendezVous, idDossier,
                             motif, diagnostic, observation, conclusion, dateConsultation)
                        VALUES
                            (@idConsultation, @idDocteur, @idRendezVous, @idDossier,
                             @motif, @diagnostic, @observation, @conclusion, @dateConsultation)";

                    using (var cmd = new SqlCommand(sqlConsultation, conn, transaction))
                    {
                        cmd.Parameters.AddWithValue("@idConsultation", idConsultation);
                        cmd.Parameters.AddWithValue("@idDocteur", e.IdDocteur);
                        cmd.Parameters.AddWithValue("@idRendezVous", e.IdRendezVous);
                        cmd.Parameters.AddWithValue("@idDossier", (object)e.IdDossier ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@motif", e.Motif);
                        cmd.Parameters.AddWithValue("@diagnostic", (object)e.Diagnostic ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@observation", (object)e.Observation ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@conclusion", (object)e.Conclusion ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@dateConsultation", e.DateConsultation);
                        cmd.ExecuteNonQuery();
                    }

                    string message = $"Consultation {idConsultation} enregistrée avec succès.";

                    if (e.GenererFacture && e.MontantFacture.HasValue)
                    {
                        string idFacture = "INV-" + DateTime.Now.ToString("yyMMddHHmmss");
                        string statutFacture = e.EncaisserMaintenant ? "Payée" : "En attente";
                        string idPatient = RecupererIdPatientDepuisRendezVous(e.IdRendezVous, conn, transaction);

                        string sqlFacture = @"
                            INSERT INTO facture
                                (idFacture, idPatient, idConsultation, service, montant, dateFacture, statut)
                            VALUES
                                (@idFacture, @idPatient, @idConsultation, @service, @montant, @dateFacture, @statut)";

                        using (var cmd = new SqlCommand(sqlFacture, conn, transaction))
                        {
                            cmd.Parameters.AddWithValue("@idFacture", idFacture);
                            cmd.Parameters.AddWithValue("@idPatient", idPatient);
                            cmd.Parameters.AddWithValue("@idConsultation", idConsultation);
                            cmd.Parameters.AddWithValue("@service", e.ServiceFacture);
                            cmd.Parameters.AddWithValue("@montant", e.MontantFacture.Value);
                            cmd.Parameters.AddWithValue("@dateFacture", e.DateConsultation);
                            cmd.Parameters.AddWithValue("@statut", statutFacture);
                            cmd.ExecuteNonQuery();
                        }

                        message += $"\nFacture {idFacture} créée.";

                        if (e.EncaisserMaintenant)
                        {
                            string sqlPaiement = @"
                                INSERT INTO paiement (idFacture, montantPaye, datePaiement, methode, note)
                                VALUES (@idFacture, @montant, @date, @methode, @note)";

                            using var cmdPaiement = new SqlCommand(sqlPaiement, conn, transaction);
                            cmdPaiement.Parameters.AddWithValue("@idFacture", idFacture);
                            cmdPaiement.Parameters.AddWithValue("@montant", e.MontantFacture.Value);
                            cmdPaiement.Parameters.AddWithValue("@date", e.DateConsultation);
                            cmdPaiement.Parameters.AddWithValue("@methode", "Carte");
                            cmdPaiement.Parameters.AddWithValue("@note", "Paiement automatique à la consultation");
                            cmdPaiement.ExecuteNonQuery();

                            message += "\nPaiement enregistré.";
                        }
                    }

                    transaction.Commit();

                    MessageBox.Show(message, "Succès", MessageBoxButton.OK, MessageBoxImage.Information);

                    // On notifie le parent que c’est enregistré
                    ConsultationEnregistree?.Invoke(this, e);
                    Annule?.Invoke(this, EventArgs.Empty);   // ferme aussi le modal
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors de l'enregistrement : " + ex.Message,
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GenererNouvelIdConsultation(SqlConnection conn, SqlTransaction transaction)
        {
            string sql = "SELECT MAX(idConsultation) FROM consultation";
            using var cmd = new SqlCommand(sql, conn, transaction);
            var result = cmd.ExecuteScalar()?.ToString();

            if (string.IsNullOrEmpty(result))
                return "C-001";

            if (result.StartsWith("C-") && int.TryParse(result.Substring(2), out int numero))
                return $"C-{(numero + 1):D3}";

            return "C-" + DateTime.Now.ToString("HHmmss");
        }

        private string RecupererIdPatientDepuisRendezVous(string idRendezVous, SqlConnection conn, SqlTransaction transaction)
        {
            // Attention : selon ta table c’est idRdv ou idRendezVous
            string sql = "SELECT idPatient FROM rendezVous WHERE idRdv = @id";
            using var cmd = new SqlCommand(sql, conn, transaction);
            cmd.Parameters.AddWithValue("@id", idRendezVous);

            var result = cmd.ExecuteScalar()?.ToString();
            return result ?? throw new Exception("Impossible de trouver le patient lié au rendez-vous.");
        }

        private void BtnFermer_Click(object sender, RoutedEventArgs e)
        {
            Annule?.Invoke(this, EventArgs.Empty);
        }

        private void Modal_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => e.Handled = true;

        private void Overlay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Annule?.Invoke(this, EventArgs.Empty);
        }
    }

    // ==================== CLASSES D'AIDE ====================

    public class DocteurItem
    {
        public string IdDocteur { get; set; }
        public string Nom { get; set; }
        public string Prenom { get; set; }
        public string Specialite { get; set; }

        public string NomComplet =>
            $"{Nom} {Prenom}" + (string.IsNullOrEmpty(Specialite) ? "" : $" ({Specialite})");
    }

    public class RendezVousItem
    {
        public string IdRendezVous { get; set; }
        public string IdDossier { get; set; }
        public string IdDocteur { get; set; }        
        public string NomDocteur { get; set; }       
        public string Libelle { get; set; }
    }

    public class ConsultationEventArgs : EventArgs
    {
        public string IdDocteur { get; set; }
        public string IdRendezVous { get; set; }
        public string IdDossier { get; set; }
        public string Motif { get; set; }
        public string Diagnostic { get; set; }
        public string Observation { get; set; }
        public string Conclusion { get; set; }
        public DateTime DateConsultation { get; set; }

        public bool GenererFacture { get; set; }
        public string ServiceFacture { get; set; }
        public decimal? MontantFacture { get; set; }
        public bool EncaisserMaintenant { get; set; }
    }
}