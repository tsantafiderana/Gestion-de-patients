using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace WpfBasics
{
    public partial class ModalPayement : UserControl
    {
        public event EventHandler<PaiementEventArgs> PaiementConfirme;
        public event EventHandler Annule;

        private string _methodeSelectionnee = "Carte";
        private string _idFacture;
        private decimal _montantDu;

        public ModalPayement()
        {
            InitializeComponent();
            DpDatePaiement.SelectedDate = DateTime.Today;
        }

        public void ChargerFacture(string idFacture, string nomPatient, decimal montantTotal, decimal dejaPaye)
        {
            _idFacture = idFacture;
            _montantDu = montantTotal - dejaPaye;   // reste à payer

            TxtSousTitre.Text = $"Facture #{idFacture}";
            TxtPatient.Text = nomPatient;

            TxtMontantTotal.Text = $"Ar {montantTotal:N2}";
            TxtDejaPaye.Text = $"Ar {dejaPaye:N2}";
            TxtMontantDu.Text = $"Ar {_montantDu:N2}";

            // Propose le reste à payer par défaut
            TxtMontantPaye.Text = _montantDu > 0
                ? _montantDu.ToString("N2", CultureInfo.InvariantCulture)
                : "0.00";

            SelectionnerMethode("Carte");
            TxtNote.Text = string.Empty;
            DpDatePaiement.SelectedDate = DateTime.Today;
        }

        private void Methode_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is string methode)
            {
                SelectionnerMethode(methode);
            }
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
                case "Carte":
                    ResetStyleMethode(BtnMethodeCarte, true);
                    break;
                case "Espèces":
                    ResetStyleMethode(BtnMethodeEspeces, true);
                    break;
                case "Assurance":
                    ResetStyleMethode(BtnMethodeAssurance, true);
                    break;
                case "Virement":
                    ResetStyleMethode(BtnMethodeVirement, true);
                    break;
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

        private void BtnConfirmer_Click(object sender, RoutedEventArgs e)
        {
            string montantTexte = TxtMontantPaye.Text.Replace(',', '.');
            if (!decimal.TryParse(montantTexte, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal montantPaye)
                || montantPaye <= 0)
            {
                MessageBox.Show("Veuillez saisir un montant valide.", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (montantPaye > _montantDu)
            {
                var result = MessageBox.Show(
                    "Le montant saisi est supérieur au montant dû.\nVoulez-vous continuer ?",
                    "Attention",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes) return;
            }

            var args = new PaiementEventArgs
            {
                IdFacture = _idFacture,
                MontantPaye = montantPaye,
                Methode = _methodeSelectionnee,
                DatePaiement = DpDatePaiement.SelectedDate ?? DateTime.Today,
                Note = TxtNote.Text?.Trim()
            };

            PaiementConfirme?.Invoke(this, args);
        }

        private void BtnFermer_Click(object sender, RoutedEventArgs e)
        {
            Annule?.Invoke(this, EventArgs.Empty);
        }

        // Empêche la fermeture quand on clique à l’intérieur du modal
        private void Modal_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        // Ferme le modal si on clique sur l’overlay sombre
        private void Overlay_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Annule?.Invoke(this, EventArgs.Empty);
        }
    }

    public class PaiementEventArgs : EventArgs
    {
        public string IdFacture { get; set; }
        public decimal MontantPaye { get; set; }
        public string Methode { get; set; }
        public DateTime DatePaiement { get; set; }
        public string Note { get; set; }
    }
}