using System;

using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.SqlClient;

namespace WpfBasics
{
    public partial class ModalPatient : Window
    {
        private readonly string connectionString =
            @"Data Source=.;Initial Catalog=WpfBasic;Integrated Security=True;TrustServerCertificate=True";

        private readonly Patient _patientAModifier = null;
        private readonly bool _estModeEdition;

        // Constructeur classique → mode création
        public ModalPatient()
        {
            InitializeComponent();
            _estModeEdition = false;
            Title = "Nouveau Patient";
        }

        // Constructeur → mode édition
        public ModalPatient(Patient patient)
        {
            InitializeComponent();
            _patientAModifier = patient;
            _estModeEdition = true;
            Title = "Modifier le patient";
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (_estModeEdition && _patientAModifier != null)
            {
                // Mode édition
                txtIdPatient.Text = _patientAModifier.IdPatient;
                txtIdPatient.IsEnabled = false;

                ChargerDonneesPatient(_patientAModifier.IdPatient);
            }
            else
            {
                // Mode création
                GenererProchainId();
                cbSexe.SelectedIndex = 0;
                cbGroupeSanguin.SelectedIndex = 0;
                dpNaissance.SelectedDate = DateTime.Today.AddYears(-25);
            }
        }

        private void GenererProchainId()
        {
            try
            {
                using (SqlConnection con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = "SELECT TOP 1 idPatient FROM patient ORDER BY idPatient DESC";
                    using (SqlCommand cmd = new SqlCommand(query, con))
                    {
                        object result = cmd.ExecuteScalar();
                        if (result != null)
                        {
                            string lastId = result.ToString();
                            if (lastId.StartsWith("P-") && int.TryParse(lastId.Substring(2), out int num))
                            {
                                txtIdPatient.Text = $"P-{(num + 1):D3}";
                                return;
                            }
                        }
                    }
                }
            }
            catch
            {
                
            }

            txtIdPatient.Text = "P-001";
        }

        private void ChargerDonneesPatient(string idPatient)
        {
            try
            {
                using (var con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query = @"
                        SELECT nom, prenom, adresse, dateNaissance, genre, 
                               telephone, email, cin, groupeSanguin
                        FROM patient 
                        WHERE idPatient = @id";

                    using (var cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@id", idPatient);
                        using (var reader = cmd.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                txtNom.Text = reader["nom"]?.ToString();
                                txtPrenom.Text = reader["prenom"]?.ToString();
                                txtAdresse.Text = reader["adresse"]?.ToString();
                                dpNaissance.SelectedDate = Convert.ToDateTime(reader["dateNaissance"]);

                                // Sexe
                                string genre = reader["genre"]?.ToString();
                                foreach (ComboBoxItem item in cbSexe.Items)
                                {
                                    if (item.Content.ToString() == genre)
                                    {
                                        cbSexe.SelectedItem = item;
                                        break;
                                    }
                                }

                                txtTelephone.Text = reader["telephone"]?.ToString();
                                txtEmail.Text = reader["email"]?.ToString();
                                txtCin.Text = reader["cin"]?.ToString();

                                // Groupe sanguin
                                string groupe = reader["groupeSanguin"]?.ToString();
                                foreach (ComboBoxItem item in cbGroupeSanguin.Items)
                                {
                                    if (item.Content.ToString() == groupe)
                                    {
                                        cbGroupeSanguin.SelectedItem = item;
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

        private void BtnAnnuler_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        //Création Patient et modification
       private void BtnEnregistrer_Click(object sender, RoutedEventArgs e)
        {
            if (!ValiderFormulaire())
                return;

            try
            {
                using (var con = new SqlConnection(connectionString))
                {
                    con.Open();
                    string query;

                    if (_estModeEdition)
                    {
                        // UPDATE
                        query = @"
                    UPDATE patient SET
                        nom = @nom,
                        prenom = @prenom,
                        adresse = @adresse,
                        dateNaissance = @dateNaissance,
                        genre = @genre,
                        telephone = @telephone,
                        email = @email,
                        cin = @cin,
                        groupeSanguin = @groupeSanguin
                    WHERE idPatient = @idPatient";
                    }
                    else
                    {
                        // INSERT patient
                        query = @"
                    INSERT INTO patient
                    (idPatient, nom, prenom, adresse, dateNaissance, genre, telephone, email, cin, groupeSanguin)
                    VALUES
                    (@idPatient, @nom, @prenom, @adresse, @dateNaissance, @genre, @telephone, @email, @cin, @groupeSanguin)";
                    }

                    using (var cmd = new SqlCommand(query, con))
                    {
                        cmd.Parameters.AddWithValue("@idPatient", txtIdPatient.Text.Trim());
                        cmd.Parameters.AddWithValue("@nom", txtNom.Text.Trim());
                        cmd.Parameters.AddWithValue("@prenom", txtPrenom.Text.Trim());
                        cmd.Parameters.AddWithValue("@adresse", txtAdresse.Text.Trim());
                        cmd.Parameters.AddWithValue("@dateNaissance", dpNaissance.SelectedDate.Value);
                        cmd.Parameters.AddWithValue("@genre", ((ComboBoxItem)cbSexe.SelectedItem).Content.ToString());
                        cmd.Parameters.AddWithValue("@telephone", txtTelephone.Text.Trim());
                        cmd.Parameters.AddWithValue("@email", txtEmail.Text.Trim());
                        cmd.Parameters.AddWithValue("@cin", txtCin.Text.Trim());
                        cmd.Parameters.AddWithValue("@groupeSanguin", ((ComboBoxItem)cbGroupeSanguin.SelectedItem).Content.ToString());

                        cmd.ExecuteNonQuery();
                    }

                    // === Création automatique d'un dossier médical vide (uniquement en mode création) ===
                    if (!_estModeEdition)
                    {
                        string idDossier = GenererProchainIdDossier(con);
                        string queryDossier = @"
                    INSERT INTO dossierMedical
                    (idDossier, idPatient, dateCreation, allergy, antecedentsMedicaux)
                    VALUES
                    (@idDossier, @idPatient, @dateCreation, @allergy, @antecedentsMedicaux)";

                        using (var cmdDossier = new SqlCommand(queryDossier, con))
                        {
                            cmdDossier.Parameters.AddWithValue("@idDossier", idDossier);
                            cmdDossier.Parameters.AddWithValue("@idPatient", txtIdPatient.Text.Trim());
                            cmdDossier.Parameters.AddWithValue("@dateCreation", DateTime.Today);
                            cmdDossier.Parameters.AddWithValue("@allergy", DBNull.Value);          
                            cmdDossier.Parameters.AddWithValue("@antecedentsMedicaux", DBNull.Value);
                            cmdDossier.ExecuteNonQuery();
                        }
                    }
                }

                MessageBox.Show(
                    _estModeEdition ? "Patient modifié avec succès !" : "Patient et dossier médical créés avec succès !",
                    "Succès",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                this.DialogResult = true;
                this.Close();
            }
            catch (SqlException ex)
            {
                if (ex.Number == 2627)
                {
                    MessageBox.Show("Cet ID Patient existe déjà.", "Erreur",
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    MessageBox.Show("Erreur SQL :\n" + ex.Message, "Erreur base de données",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur :\n" + ex.Message, "Erreur",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GenererProchainIdDossier(SqlConnection con)
        {
            try
            {
                string query = "SELECT TOP 1 idDossier FROM dossierMedical ORDER BY idDossier DESC";
                using (SqlCommand cmd = new SqlCommand(query, con))
                {
                    object result = cmd.ExecuteScalar();
                    if (result != null)
                    {
                        string lastId = result.ToString();
                        if (lastId.StartsWith("DS-") && int.TryParse(lastId.Substring(3), out int num))
                        {
                            return $"DS-{(num + 1):D3}";
                        }
                    }
                }
            }
            catch
            {
                
            }

            return "DS-001";
        }

        private bool ValiderFormulaire()
        {
            if (string.IsNullOrWhiteSpace(txtIdPatient.Text))
            {
                MessageBox.Show("L'ID Patient est obligatoire.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtIdPatient.Focus();
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

            if (dpNaissance.SelectedDate == null)
            {
                MessageBox.Show("La date de naissance est obligatoire.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                dpNaissance.Focus();
                return false;
            }

            if (cbSexe.SelectedItem == null)
            {
                MessageBox.Show("Veuillez sélectionner le sexe.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            if (cbGroupeSanguin.SelectedItem == null)
            {
                MessageBox.Show("Veuillez sélectionner le groupe sanguin.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // Validation email
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

            return true;
        }
    }
}