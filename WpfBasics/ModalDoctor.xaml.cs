using Microsoft.Data.SqlClient;
using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;

namespace WpfBasics
{
    public partial class ModalDoctor : Window
    {
        private readonly string connexionString =
            @"Data Source=.;Initial Catalog=WpfBasic;Integrated Security=True;TrustServerCertificate=True";

        private readonly Docteur _docteurAModifier;   
        private readonly bool _estModeEdition;

        
        public ModalDoctor()
        {
            InitializeComponent();
            _estModeEdition = false;
            Title = "Nouveau Docteur";
        }

        
        public ModalDoctor(Docteur docteur)
        {
            InitializeComponent();
            _docteurAModifier = docteur;
            _estModeEdition = true;
            Title = "Modifier le docteur";
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (_estModeEdition && _docteurAModifier != null)
            {
                // Mode Édition
                txtIdDocteur.Text = _docteurAModifier.IdDocteur;
                txtIdDocteur.IsEnabled = false;                
                ChargerDonneesDocteur(_docteurAModifier.IdDocteur);

                // Changer le texte du bouton
                BtnSave.Content = "Modifier";
                TitreModal.Text = "Modifier le docteur";
            }
            else
            {
                // Mode Création
                GenererProchainId();
                cbSexe.SelectedIndex = 0;
                cbSpecialite.SelectedIndex = 0;

                BtnSave.Content = "Enregistrer";
                TitreModal.Text = "Nouveau docteur";
            }
        }

        private void GenererProchainId()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(connexionString))
                {
                    conn.Open();
                    string query = "SELECT TOP 1 idDocteur FROM docteur ORDER BY idDocteur DESC";
                    using (SqlCommand cmd = new SqlCommand(query, conn))
                    {
                        object result = cmd.ExecuteScalar();
                        if (result != null)
                        {
                            string lastId = result.ToString();
                            // Format attendu : D-001, D-002...
                            if (lastId.StartsWith("D-") && int.TryParse(lastId.Substring(2), out int num))
                            {
                                txtIdDocteur.Text = $"D-{(num + 1):D3}";
                                return;
                            }
                        }
                    }
                }
            }
            catch { /* silencieux */ }

            txtIdDocteur.Text = "D-001";
        }

        private void ChargerDonneesDocteur(string idDocteur)
        {
            try
            {
                using (var con = new SqlConnection(connexionString))
                {
                    con.Open();
                    string query = @"
                        SELECT idDocteur, nom, prenom, specialite, telephone, email, sexe
                        FROM docteur
                        WHERE idDocteur = @id";

                    using (var cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@id", idDocteur);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                txtNom.Text = reader["nom"]?.ToString();
                                txtPrenom.Text = reader["prenom"]?.ToString();
                                txtEmail.Text = reader["email"]?.ToString();
                                txtTelephone.Text = reader["telephone"]?.ToString();

                                // Sexe
                                string sexe = reader["sexe"]?.ToString();
                                foreach (ComboBoxItem item in cbSexe.Items)
                                {
                                    if (item.Content.ToString() == sexe)
                                    {
                                        cbSexe.SelectedItem = item;
                                        break;
                                    }
                                }

                                // Spécialité
                                string specialite = reader["specialite"]?.ToString();
                                foreach (ComboBoxItem item in cbSpecialite.Items)
                                {
                                    if (item.Content.ToString() == specialite)
                                    {
                                        cbSpecialite.SelectedItem = item;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors du chargement : " + ex.Message);
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => this.Close();
        private void BtnCancel_Click(object sender, RoutedEventArgs e) => this.Close();

        private void BtnAdd_Click(object sender, RoutedEventArgs e)  
        {
            if (!ValiderFormulaire()) return;

            try
            {
                using (var con = new SqlConnection(connexionString))
                {
                    con.Open();

                    string query = _estModeEdition
                        ? @"UPDATE docteur SET
                                nom = @nom,
                                prenom = @prenom,
                                email = @email,
                                telephone = @telephone,
                                sexe = @sexe,
                                specialite = @specialite
                            WHERE idDocteur = @idDocteur"
                        : @"INSERT INTO docteur (idDocteur, nom, prenom, email, telephone, sexe, specialite)
                            VALUES (@idDocteur, @nom, @prenom, @email, @telephone, @sexe, @specialite)";

                    using (var cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@idDocteur", txtIdDocteur.Text.Trim());
                        cmd.Parameters.AddWithValue("@nom", txtNom.Text.Trim());
                        cmd.Parameters.AddWithValue("@prenom", txtPrenom.Text.Trim());
                        cmd.Parameters.AddWithValue("@email", txtEmail.Text.Trim());
                        cmd.Parameters.AddWithValue("@telephone", txtTelephone.Text.Trim());
                        cmd.Parameters.AddWithValue("@sexe", ((ComboBoxItem)cbSexe.SelectedItem).Content.ToString());
                        cmd.Parameters.AddWithValue("@specialite", ((ComboBoxItem)cbSpecialite.SelectedItem).Content.ToString());

                        cmd.ExecuteNonQuery();
                    }
                }

                MessageBox.Show(
                    _estModeEdition ? "Docteur modifié avec succès !" : "Docteur enregistré avec succès !",
                    "Succès",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                this.DialogResult = true;
                this.Close();
            }
            catch (SqlException ex) when (ex.Number == 2627)
            {
                MessageBox.Show("Cet ID Docteur existe déjà.", "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur :\n" + ex.Message, "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ValiderFormulaire()
        {
            if (string.IsNullOrWhiteSpace(txtIdDocteur.Text))
            {
                MessageBox.Show("L'ID Docteur est obligatoire.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtIdDocteur.Focus();
                return false;
            }
            if (string.IsNullOrWhiteSpace(txtNom.Text))
            {
                MessageBox.Show("Le nom est obligatoire.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtNom.Focus();
                return false;
            }
            if (string.IsNullOrWhiteSpace(txtPrenom.Text))
            {
                MessageBox.Show("Le prénom est obligatoire.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtPrenom.Focus();
                return false;
            }
            if (!string.IsNullOrWhiteSpace(txtEmail.Text))
            {
                string pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
                if (!Regex.IsMatch(txtEmail.Text.Trim(), pattern))
                {
                    MessageBox.Show("L'adresse email n'est pas valide.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                    txtEmail.Focus();
                    return false;
                }
            }
            if (string.IsNullOrWhiteSpace(txtTelephone.Text))
            {
                MessageBox.Show("Le numéro de téléphone est obligatoire.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtTelephone.Focus();
                return false;
            }
            if (cbSexe.SelectedItem == null)
            {
                MessageBox.Show("Veuillez sélectionner le sexe.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            if (cbSpecialite.SelectedItem == null)
            {
                MessageBox.Show("Veuillez sélectionner la spécialité.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }
            return true;
        }
    }
}