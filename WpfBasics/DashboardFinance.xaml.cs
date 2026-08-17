using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Microsoft.Data.SqlClient;

namespace WpfBasics
{
    public partial class DashboardFinance : UserControl
    {
        private readonly string connectionString =
            "Server=localhost;Database=WpfBasic;Trusted_Connection=True;Encrypt=False;";

        public ObservableCollection<FactureViewModel> Factures { get; set; } = new();

        public DashboardFinance()
        {
            InitializeComponent();
            Loaded += DashboardFinance_Loaded;
        }

        private void DashboardFinance_Loaded(object sender, RoutedEventArgs e)
        {
            ChargerDonnees();
        }
        
        // CHARGEMENT DES FACTURES        
        private void ChargerDonnees()
        {
            try
            {
                Factures.Clear();

                using var conn = new SqlConnection(connectionString);
                conn.Open();

                string sql = @"
                    SELECT f.idFacture,
                           p.nom + ' ' + p.prenom AS NomPatient,
                           f.service,
                           f.montant,
                           f.dateFacture,
                           f.statut
                    FROM facture f
                    INNER JOIN patient p ON f.idPatient = p.idPatient
                    ORDER BY f.dateFacture DESC";

                using var cmd = new SqlCommand(sql, conn);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    Factures.Add(new FactureViewModel
                    {
                        IdFacture = reader["idFacture"]?.ToString() ?? "",
                        NomPatient = reader["NomPatient"]?.ToString() ?? "",
                        Service = reader["service"]?.ToString() ?? "",
                        Montant = Convert.ToDecimal(reader["montant"]),
                        DateFacture = Convert.ToDateTime(reader["dateFacture"]),
                        Statut = reader["statut"]?.ToString() ?? ""
                    });
                }

                DgFactures.ItemsSource = Factures;

                if (Factures.Any())
                {
                    DgFactures.SelectedIndex = 0;
                    AfficherDetail(Factures[0]);
                }

                MettreAJourKpi();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur de chargement : " + ex.Message, "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        
        // CHARGEMENT DES PAIEMENTS D'UNE FACTURE
        
        private (decimal DejaPaye, List<PaiementItem> Historique) ChargerPaiements(string idFacture)
        {
            
            decimal dejaPaye = 0;
            var historique = new List<PaiementItem>();

            try
            {
                using var conn = new SqlConnection(connectionString);
                conn.Open();

                string sql = @"
                    SELECT montantPaye, datePaiement, methode, note
                    FROM paiement
                    WHERE idFacture = @id
                    ORDER BY datePaiement DESC";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@id", idFacture);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    decimal montant = Convert.ToDecimal(reader["montantPaye"]);
                    dejaPaye += montant;

                    historique.Add(new PaiementItem
                    {
                        MontantPaye = montant,
                        DatePaiement = Convert.ToDateTime(reader["datePaiement"]),
                        Methode = reader["methode"]?.ToString() ?? "",
                        Note = reader["note"]?.ToString()
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"Erreur lors du chargement des paiements de la facture {idFacture} :\n\n{ex.Message}",
                    "Erreur",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }

            return (dejaPaye, historique);
        }

        
        // MISE À JOUR DU STATUT SELON LES PAIEMENTS
        
        private void MettreAJourStatutFacture(string idFacture, decimal montantTotal)
        {
            var (dejaPaye, _) = ChargerPaiements(idFacture);
            decimal reste = montantTotal - dejaPaye;

            string nouveauStatut;
            if (reste <= 0)
                nouveauStatut = "Payée";
            else if (dejaPaye > 0)
                nouveauStatut = "Partielle";
            else
                nouveauStatut = "En attente";

            using var conn = new SqlConnection(connectionString);
            conn.Open();

            string sql = "UPDATE facture SET statut = @statut WHERE idFacture = @id";
            using var cmd = new SqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@statut", nouveauStatut);
            cmd.Parameters.AddWithValue("@id", idFacture);
            cmd.ExecuteNonQuery();
        }

        
        // AFFICHAGE DU DÉTAIL + HISTORIQUE
        
        private void AfficherDetail(FactureViewModel f)
        {
            TxtDetailNumero.Text = $"Facture #{f.IdFacture}";
            TxtDetailDate.Text = $"Émise le {f.DateFacture:dd MMM yyyy}";
            TxtDetailPatient.Text = f.NomPatient;
            TxtDetailService.Text = f.Service;
            TxtDetailSousTotal.Text = $"Ar {f.Montant:N2}";
            TxtDetailTva.Text = "Ar 0.00";
            TxtDetailTotal.Text = $"Ar {f.Montant:N2}";

            // Charger les paiements
            var (dejaPaye, historique) = ChargerPaiements(f.IdFacture);
            decimal reste = f.Montant - dejaPaye;

            TxtDetailDejaPaye.Text = $"Ar {dejaPaye:N2}";
            TxtDetailReste.Text = $"Ar {reste:N2}";
            ListePaiements.ItemsSource = historique;

            // Badge de statut
            switch (f.Statut)
            {
                case "Payée":
                    BadgeDetailStatut.Background = (Brush)new BrushConverter().ConvertFrom("#D1FAE5")!;
                    TxtDetailStatut.Text = "● Payée";
                    TxtDetailStatut.Foreground = (Brush)new BrushConverter().ConvertFrom("#065F46")!;
                    break;

                case "En attente":
                case "Partielle":
                    BadgeDetailStatut.Background = (Brush)new BrushConverter().ConvertFrom("#FEF3C7")!;
                    TxtDetailStatut.Text = "● " + f.Statut;
                    TxtDetailStatut.Foreground = (Brush)new BrushConverter().ConvertFrom("#92400E")!;
                    break;

                case "En retard":
                    BadgeDetailStatut.Background = (Brush)new BrushConverter().ConvertFrom("#FEE2E2")!;
                    TxtDetailStatut.Text = "● En retard";
                    TxtDetailStatut.Foreground = (Brush)new BrushConverter().ConvertFrom("#991B1B")!;
                    break;
            }
        }

        
        // KPI
        
        private void MettreAJourKpi()
        {
            decimal revenuMois = Factures
                .Where(f => f.DateFacture.Month == DateTime.Now.Month && f.Statut == "Payée")
                .Sum(f => f.Montant);

            decimal enAttente = Factures
                .Where(f => f.Statut is "En attente" or "Partielle" or "En retard")
                .Sum(f => f.Montant);

            int payees = Factures.Count(f => f.Statut == "Payée");
            int total = Factures.Count;
            double taux = total > 0 ? (double)payees / total * 100 : 0;

            TxtRevenuMois.Text = $"Ar {revenuMois:N0}";
            TxtEnAttente.Text = $"Ar {enAttente:N0}";
            TxtFacturesPayees.Text = payees.ToString();
            TxtNbFacturesAttente.Text = $"{Factures.Count(f => f.Statut != "Payée")} facture(s)";
            TxtTauxRecouvrement.Text = $"{taux:N0} % recouvrement";
        }

        
        private void DgFactures_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DgFactures.SelectedItem is FactureViewModel facture)
                AfficherDetail(facture);
        }

        
        private void BtnMarquerPayee_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is FactureViewModel facture)
            {
                var (dejaPaye, _) = ChargerPaiements(facture.IdFacture);

                ModalPaiement.ChargerFacture(
                    facture.IdFacture,
                    facture.NomPatient,
                    facture.Montant,   
                    dejaPaye 
                );

                ModalPaiement.PaiementConfirme -= ModalPaiement_PaiementConfirme;
                ModalPaiement.PaiementConfirme += ModalPaiement_PaiementConfirme;

                ModalPaiement.Annule -= ModalPaiement_Annule;
                ModalPaiement.Annule += ModalPaiement_Annule;

                ModalOverlay.Visibility = Visibility.Visible;
            }
        }

      
        private void ModalPaiement_PaiementConfirme(object sender, PaiementEventArgs e)
        {
            try
            {
                using var conn = new SqlConnection(connectionString);
                conn.Open();

                // 1. Enregistrer le paiement
                string sqlPaiement = @"
                    INSERT INTO paiement (idFacture, montantPaye, datePaiement, methode, note)
                    VALUES (@idFacture, @montant, @date, @methode, @note)";

                using (var cmd = new SqlCommand(sqlPaiement, conn))
                {
                    cmd.Parameters.AddWithValue("@idFacture", e.IdFacture);
                    cmd.Parameters.AddWithValue("@montant", e.MontantPaye);
                    cmd.Parameters.AddWithValue("@date", e.DatePaiement);
                    cmd.Parameters.AddWithValue("@methode", e.Methode);
                    cmd.Parameters.AddWithValue("@note", (object)e.Note ?? DBNull.Value);
                    cmd.ExecuteNonQuery();
                }

                // 2. Mettre à jour le statut de la facture
                var facture = Factures.FirstOrDefault(f => f.IdFacture == e.IdFacture);
                if (facture != null)
                {
                    MettreAJourStatutFacture(e.IdFacture, facture.Montant);

                    // Recharger le nouveau statut
                    string sqlStatut = "SELECT statut FROM facture WHERE idFacture = @id";
                    using var cmdStatut = new SqlCommand(sqlStatut, conn);
                    cmdStatut.Parameters.AddWithValue("@id", e.IdFacture);
                    facture.Statut = cmdStatut.ExecuteScalar()?.ToString() ?? facture.Statut;

                    AfficherDetail(facture);
                    MettreAJourKpi();
                }

                ModalOverlay.Visibility = Visibility.Collapsed;

                MessageBox.Show(
                    $"Paiement de Ar {e.MontantPaye:N2} enregistré avec succès\nMéthode : {e.Methode}",
                    "Succès",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors de l'enregistrement : " + ex.Message,
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ModalPaiement_Annule(object sender, EventArgs e)
        {
            ModalOverlay.Visibility = Visibility.Collapsed;
        }

        
        // NOUVELLE FACTURE
        
        private void BtnNouvelleFacture_Click(object sender, RoutedEventArgs e)
        {
            ModalNouvelleFacture.FactureCreee -= ModalNouvelleFacture_FactureCreee;
            ModalNouvelleFacture.FactureCreee += ModalNouvelleFacture_FactureCreee;

            ModalNouvelleFacture.Annule -= (s, args) => ModalOverlayFacture.Visibility = Visibility.Collapsed;
            ModalNouvelleFacture.Annule += (s, args) => ModalOverlayFacture.Visibility = Visibility.Collapsed;

            ModalOverlayFacture.Visibility = Visibility.Visible;
        }

        private void ModalNouvelleFacture_FactureCreee(object sender, NouvelleFactureEventArgs e)
        {
            try
            {
                using var conn = new SqlConnection(connectionString);
                conn.Open();

                string idFacture = "INV-" + DateTime.Now.ToString("yyMMddHHmmss");
                string statut = e.EncaisserMaintenant ? "Payée" : "En attente";

                // 1. Créer la facture
                string sqlFacture = @"
                    INSERT INTO facture (idFacture, idPatient, service, montant, dateFacture, statut)
                    VALUES (@idFacture, @idPatient, @service, @montant, @date, @statut)";

                using (var cmd = new SqlCommand(sqlFacture, conn))
                {
                    cmd.Parameters.AddWithValue("@idFacture", idFacture);
                    cmd.Parameters.AddWithValue("@idPatient", e.IdPatient);
                    cmd.Parameters.AddWithValue("@service", e.Service);
                    cmd.Parameters.AddWithValue("@montant", e.Montant);
                    cmd.Parameters.AddWithValue("@date", e.DateFacture);
                    cmd.Parameters.AddWithValue("@statut", statut);
                    cmd.ExecuteNonQuery();
                }

                // 2. Si on encaisse immédiatement → créer le premier paiement
                if (e.EncaisserMaintenant)
                {
                    string sqlPaiement = @"
                        INSERT INTO paiement (idFacture, montantPaye, datePaiement, methode, note)
                        VALUES (@idFacture, @montant, @date, @methode, @note)";

                    using var cmdPaiement = new SqlCommand(sqlPaiement, conn);
                    cmdPaiement.Parameters.AddWithValue("@idFacture", idFacture);
                    cmdPaiement.Parameters.AddWithValue("@montant", e.Montant);
                    cmdPaiement.Parameters.AddWithValue("@date", e.DateFacture);
                    cmdPaiement.Parameters.AddWithValue("@methode", e.MethodePaiement ?? "Carte");
                    cmdPaiement.Parameters.AddWithValue("@note", (object)e.Note ?? DBNull.Value);
                    cmdPaiement.ExecuteNonQuery();
                }

                ChargerDonnees();
                ModalOverlayFacture.Visibility = Visibility.Collapsed;

                MessageBox.Show($"Facture {idFacture} créée avec succès.", "Succès",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur : " + ex.Message, "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    
    // VIEWMODELS & HELPERS
    
    public class FactureViewModel : INotifyPropertyChanged
    {
        public string IdFacture { get; set; } = "";
        public string NomPatient { get; set; } = "";
        public string Service { get; set; } = "";
        public decimal Montant { get; set; }
        public DateTime DateFacture { get; set; }

        private string _statut = "";
        public string Statut
        {
            get => _statut;
            set
            {
                _statut = value;
                OnPropertyChanged(nameof(Statut));
                OnPropertyChanged(nameof(PeutMarquerPayee));
            }
        }
        

        public bool PeutMarquerPayee => Statut != "Payée";

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string name) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class PaiementItem
    {
        public decimal MontantPaye { get; set; }
        public DateTime DatePaiement { get; set; }
        public string Methode { get; set; } = "";
        public string? Note { get; set; }
    }

    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is true ? Visibility.Visible : Visibility.Collapsed;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}