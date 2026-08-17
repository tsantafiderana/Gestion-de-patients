using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Data.SqlClient;

namespace WpfBasics
{
    public partial class ModalNouvelleFacture : UserControl
    {
        public event EventHandler<NouvelleFactureEventArgs> FactureCreee;
        public event EventHandler Annule;

        private readonly string _connectionString =
            "Server=localhost;Database=WpfBasic;Trusted_Connection=True;Encrypt=False;";

        private string _methodeSelectionnee = "Carte";
        private List<PatientItem> _patients = new();

        public ModalNouvelleFacture()
        {
            InitializeComponent();
            Loaded += ModalNouvelleFacture_Loaded;
        }

        private void ModalNouvelleFacture_Loaded(object sender, RoutedEventArgs e)
        {
            ChargerPatients();
            SelectionnerMethode("Carte");
            DpDateFacture.SelectedDate = DateTime.Today;
        }

        private void ChargerPatients()
        {
            try
            {
                _patients.Clear();
                using var conn = new SqlConnection(_connectionString);
                conn.Open();

                string sql = "SELECT idPatient, nom, prenom FROM patient ORDER BY nom, prenom";
                using var cmd = new SqlCommand(sql, conn);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    _patients.Add(new PatientItem
                    {
                        IdPatient = reader["idPatient"].ToString(),
                        Nom = reader["nom"].ToString(),
                        Prenom = reader["prenom"].ToString()
                    });
                }

                CmbPatient.ItemsSource = _patients;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur chargement patients : " + ex.Message);
            }
        }

        private void CmbPatient_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            CmbConsultation.ItemsSource = null;
            CmbConsultation.IsEnabled = false;

            if (CmbPatient.SelectedItem is PatientItem patient)
            {
                ChargerConsultations(patient.IdPatient);
            }
        }

        private void ChargerConsultations(string idPatient)
        {
            try
            {
                var liste = new List<ConsultationItem>();

                using var conn = new SqlConnection(_connectionString);
                conn.Open();

                // Adapte cette requête selon ta table réelle (consultation ou rendezVous)
                string sql = @"
                    SELECT TOP 20 idConsultation, dateConsultation, motif
                    FROM consultation
                    WHERE idPatient = @idPatient
                    ORDER BY dateConsultation DESC";

                using var cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@idPatient", idPatient);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var date = Convert.ToDateTime(reader["dateConsultation"]);
                    var motif = reader["motif"]?.ToString() ?? "Consultation";

                    liste.Add(new ConsultationItem
                    {
                        Id = reader["idConsultation"].ToString(),
                        Libelle = $"{date:dd/MM/yyyy} - {motif}"
                    });
                }

                CmbConsultation.ItemsSource = liste;
                CmbConsultation.IsEnabled = liste.Any();
            }
            catch
            {
                // Si la table consultation n'existe pas encore, on ignore
                CmbConsultation.IsEnabled = false;
            }
        }

        private void ChkEncaisserMaintenant_Checked(object sender, RoutedEventArgs e)
        {
            PanelPaiement.Visibility = Visibility.Visible;
        }

        private void ChkEncaisserMaintenant_Unchecked(object sender, RoutedEventArgs e)
        {
            PanelPaiement.Visibility = Visibility.Collapsed;
        }

        private void Methode_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is string methode)
                SelectionnerMethode(methode);
        }

        private void SelectionnerMethode(string methode)
        {
            _methodeSelectionnee = methode;

            ResetStyleMethode(BtnMethodeCarte, false);
            ResetStyleMethode(BtnMethodeEspeces, false);
            ResetStyleMethode(BtnMethodeAssurance, false);
            ResetStyleMethode(BtnMethodeVirement, false);

            switch (methode)
            {
                case "Carte": ResetStyleMethode(BtnMethodeCarte, true); break;
                case "Espèces": ResetStyleMethode(BtnMethodeEspeces, true); break;
                case "Assurance": ResetStyleMethode(BtnMethodeAssurance, true); break;
                case "Virement": ResetStyleMethode(BtnMethodeVirement, true); break;
            }
        }

        private void ResetStyleMethode(Border border, bool isSelected)
        {
            if (isSelected)
            {
                border.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#EFF6FF"));
                border.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2563EB"));
            }
            else
            {
                border.Background = Brushes.White;
                border.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E5E7EB"));
            }
        }

        private void TxtMontant_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^[0-9.,]+$");
        }

        private void BtnCreer_Click(object sender, RoutedEventArgs e)
        {
            // Validations
            if (CmbPatient.SelectedItem is not PatientItem patient)
            {
                MessageBox.Show("Veuillez sélectionner un patient.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string service = CmbService.Text?.Trim();
            if (string.IsNullOrWhiteSpace(service))
            {
                MessageBox.Show("Veuillez indiquer le service / motif.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            string montantTexte = TxtMontant.Text.Replace(',', '.');
            if (!decimal.TryParse(montantTexte, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal montant) || montant <= 0)
            {
                MessageBox.Show("Veuillez saisir un montant valide.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var args = new NouvelleFactureEventArgs
            {
                IdPatient = patient.IdPatient,
                NomPatient = patient.NomComplet,
                IdConsultation = (CmbConsultation.SelectedItem as ConsultationItem)?.Id,
                Service = service,
                Montant = montant,
                DateFacture = DpDateFacture.SelectedDate ?? DateTime.Today,
                EncaisserMaintenant = ChkEncaisserMaintenant.IsChecked == true,
                MethodePaiement = _methodeSelectionnee,
                Note = TxtNote.Text?.Trim()
            };

            FactureCreee?.Invoke(this, args);
        }

        private void BtnFermer_Click(object sender, RoutedEventArgs e)
        {
            Annule?.Invoke(this, EventArgs.Empty);
        }

        private void Modal_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => e.Handled = true;
        private void Overlay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) => Annule?.Invoke(this, EventArgs.Empty);
    }

    // Classes d'aide
    public class PatientItem
    {
        public string IdPatient { get; set; }
        public string Nom { get; set; }
        public string Prenom { get; set; }
        public string NomComplet => $"{Nom} {Prenom}";
    }

    public class ConsultationItem
    {
        public string Id { get; set; }
        public string Libelle { get; set; }
    }

    public class NouvelleFactureEventArgs : EventArgs
    {
        public string IdPatient { get; set; }
        public string NomPatient { get; set; }
        public string IdConsultation { get; set; }
        public string Service { get; set; }
        public decimal Montant { get; set; }
        public DateTime DateFacture { get; set; }
        public bool EncaisserMaintenant { get; set; }
        public string MethodePaiement { get; set; }
        public string Note { get; set; }
    }
}