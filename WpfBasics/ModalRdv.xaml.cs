using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Data.SqlClient;

namespace WpfBasics
{
    public partial class ModalRdv : Window
    {
        private readonly string ConnectionString =
            "Server=localhost;Database=WpfBasic;Trusted_Connection=True;Encrypt=False;";

        public ObservableCollection<Docteur> ListeDocteurs { get; } = new();
        public ObservableCollection<Patient> ListePatients { get; set; } = new();

        private readonly RendezVous _rendezVousAModifier = null;
        private readonly bool _estModeEdition;

        public ModalRdv()
        {
            InitializeComponent();
            DataContext = this;
            _estModeEdition = false;
            Title = "Nouveau Rendez-Vous";
            LoadDoctor();
            LoadPatient();
        }

        public ModalRdv(RendezVous rendezVous)
        {
            InitializeComponent();
            DataContext = this;
            _rendezVousAModifier = rendezVous;
            _estModeEdition = true;
            Title = "Modifier le Rendez-Vous";
            LoadDoctor();
            LoadPatient();
        }

        private void LoadDoctor()
        {
            ListeDocteurs.Clear();
            try
            {
                using var conn = new SqlConnection(ConnectionString);
                conn.Open();

                string query = @"
                    SELECT d.idDocteur,
                           CONCAT(d.nom, ' ', d.prenom, ' (', d.specialite, ')') AS NomComplet,
                           d.nom, d.prenom
                    FROM docteur d
                    ORDER BY d.nom, d.prenom";

                using var cmd = new SqlCommand(query, conn);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    ListeDocteurs.Add(new Docteur
                    {
                        IdDocteur = reader["idDocteur"].ToString(),
                        NomComplet = reader["NomComplet"].ToString(),
                        Nom = reader["nom"].ToString(),
                        Prenom = reader["prenom"].ToString()
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur chargement docteurs : " + ex.Message,
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void LoadPatient()
        {
            ListePatients.Clear();
            try
            {
                using var conn = new SqlConnection(ConnectionString);
                conn.Open();

                string query = @"
                    SELECT idPatient,
                           CONCAT(nom, ' ', prenom, ' (', idPatient, ')') AS NomComplet,
                           nom,
                           prenom,
                           dateNaissance,
                           genre,
                           email,
                           telephone,
                           adresse,
                           groupeSanguin
                    FROM patient";

                using var cmd = new SqlCommand(query, conn);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    var naissance = Convert.ToDateTime(reader["dateNaissance"]);
                    int age = DateTime.Today.Year - naissance.Year;
                    if (naissance.Date > DateTime.Today.AddYears(-age)) age--;

                    var patient = new Patient
                    {
                        IdPatient = reader["idPatient"].ToString(),
                        Prenom = reader["prenom"].ToString(),
                        Nom = reader["nom"].ToString(),
                        NomComplet = reader["NomComplet"].ToString(),
                        DateNaissance = naissance,
                        Age = age,
                        Sexe = reader["genre"].ToString(),
                        Contact = reader["email"]?.ToString(),
                        Telephone = reader["telephone"]?.ToString() ?? "",
                        Adresse = reader["adresse"]?.ToString() ?? "",
                        GroupeSanguin = reader["groupeSanguin"]?.ToString() ?? ""
                    };

                    ListePatients.Add(patient);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur chargement patients : " + ex.Message,
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (_estModeEdition && _rendezVousAModifier != null)
            {
                txtIdRdv.Text = _rendezVousAModifier.IdRdv;
                txtIdRdv.IsEnabled = false;
                ChargerDonnéesRdv(_rendezVousAModifier.IdRdv);
            }
            else
            {
                GenererProchainId();
                inputDate.SelectedDate = DateTime.Today;
            }
        }

        private void GenererProchainId()
        {
            try
            {
                using var con = new SqlConnection(ConnectionString);
                con.Open();

                string query = @"
                    SELECT MAX(CAST(SUBSTRING(idRdv, 3, LEN(idRdv)) AS INT))
                    FROM rendezVous
                    WHERE idRdv LIKE 'R-%'";

                using var cmd = new SqlCommand(query, con);
                object result = cmd.ExecuteScalar();

                if (result != null && result != DBNull.Value)
                {
                    int num = Convert.ToInt32(result);
                    txtIdRdv.Text = $"R-{(num + 1):D3}";
                    return;
                }
            }
            catch
            {
                // En cas d'erreur on tombe sur la valeur par défaut
            }

            txtIdRdv.Text = "R-001";
        }

        /// <summary>
        /// Vérifie qu'il n'y a pas de conflit de créneau pour le docteur sélectionné.
        /// Écart minimum : 1 heure.
        /// </summary>
        private bool EstCreneauDisponible(string idDocteur, DateTime dateHeureProposee, string idRdvAExclure = null)
        {
            try
            {
                using var con = new SqlConnection(ConnectionString);
                con.Open();

                // Récupère tous les rendez-vous du docteur le même jour
                string query = @"
                    SELECT idRdv, dateRdv, heureRdv
                    FROM rendezVous
                    WHERE idDocteur = @idDocteur
                      AND CAST(dateRdv AS DATE) = @date
                      AND (@idExclure IS NULL OR idRdv <> @idExclure)";

                using var cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@idDocteur", idDocteur);
                cmd.Parameters.AddWithValue("@date", dateHeureProposee.Date);
                cmd.Parameters.AddWithValue("@idExclure", (object)idRdvAExclure ?? DBNull.Value);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    DateTime dateExistante = Convert.ToDateTime(reader["dateRdv"]).Date;
                    TimeSpan heureExistante = (TimeSpan)reader["heureRdv"];
                    DateTime dateHeureExistante = dateExistante.Add(heureExistante);

                    // Différence absolue en minutes
                    double diffMinutes = Math.Abs((dateHeureProposee - dateHeureExistante).TotalMinutes);

                    // Conflit si moins de 60 minutes
                    if (diffMinutes < 60)
                    {
                        return false;
                    }
                }

                return true; // Aucun conflit
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors de la vérification des créneaux :\n" + ex.Message,
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
                return false; // Par sécurité on bloque
            }
        }

        private bool ValiderFormulaire()
        {
            if (string.IsNullOrWhiteSpace(txtIdRdv.Text))
            {
                MessageBox.Show("L'ID du rendez-vous est obligatoire.",
                    "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                txtIdRdv.Focus();
                return false;
            }

            if (cbPatient.SelectedItem == null)
            {
                MessageBox.Show("Veuillez sélectionner un patient.",
                    "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                cbPatient.Focus();
                return false;
            }

            if (cbDocteur.SelectedItem == null)
            {
                MessageBox.Show("Veuillez sélectionner un docteur.",
                    "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                cbDocteur.Focus();
                return false;
            }

            if (cbStatut.SelectedItem == null)
            {
                MessageBox.Show("Veuillez sélectionner un statut.",
                    "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                cbStatut.Focus();
                return false;
            }

            if (inputDate.SelectedDate == null)
            {
                MessageBox.Show("Veuillez sélectionner une date valide.",
                    "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                inputDate.Focus();
                return false;
            }

            if (tpHeureRdv.SelectedDateTime == null)
            {
                MessageBox.Show("Veuillez sélectionner une heure valide.",
                    "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                tpHeureRdv.Focus();
                return false;
            }

            // Construction de la date + heure complète
            DateTime dateRdv = inputDate.SelectedDate.Value.Date;
            TimeSpan heureRdv = tpHeureRdv.SelectedDateTime.Value.TimeOfDay;
            DateTime dateHeureComplete = dateRdv.Add(heureRdv);

            // 1. Doit être dans le futur
            if (dateHeureComplete <= DateTime.Now)
            {
                MessageBox.Show("La date et l'heure du rendez-vous doivent être dans le futur.",
                    "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                inputDate.Focus();
                return false;
            }

            // 2. Vérification de disponibilité (écart minimum 1 heure par docteur)
            string idDocteur = cbDocteur.SelectedValue?.ToString();
            string idAExclure = _estModeEdition ? txtIdRdv.Text.Trim() : null;

            if (!EstCreneauDisponible(idDocteur, dateHeureComplete, idAExclure))
            {
                MessageBox.Show(
                    "Ce créneau n'est pas disponible.\n" +
                    "Il existe déjà un rendez-vous pour ce docteur à moins d'1 heure d'intervalle.",
                    "Conflit de créneau",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                tpHeureRdv.Focus();
                return false;
            }

            return true;
        }

        private void ChargerDonnéesRdv(string idRdv)
        {
            try
            {
                using var con = new SqlConnection(ConnectionString);
                con.Open();

                string query = @"
                    SELECT r.idPatient, r.idDocteur, r.dateRdv, r.heureRdv,
                           r.description, r.statut
                    FROM rendezVous r
                    WHERE r.idRdv = @id";

                using var cmd = new SqlCommand(query, con);
                cmd.Parameters.AddWithValue("@id", idRdv);

                using var reader = cmd.ExecuteReader();
                if (reader.Read())
                {
                    txtDescription.Text = reader["description"]?.ToString() ?? "";

                    // Patient & Docteur
                    cbPatient.SelectedValue = reader["idPatient"].ToString();
                    cbDocteur.SelectedValue = reader["idDocteur"].ToString();

                    // Statut
                    string statut = reader["statut"]?.ToString() ?? "";
                    foreach (ComboBoxItem item in cbStatut.Items)
                    {
                        if (item.Tag?.ToString().Equals(statut, StringComparison.OrdinalIgnoreCase) == true)
                        {
                            cbStatut.SelectedItem = item;
                            break;
                        }
                    }

                    // Date
                    if (reader["dateRdv"] != DBNull.Value)
                        inputDate.SelectedDate = Convert.ToDateTime(reader["dateRdv"]);

                    // Heure
                    if (reader["heureRdv"] != DBNull.Value)
                    {
                        TimeSpan heure = (TimeSpan)reader["heureRdv"];
                        tpHeureRdv.SelectedDateTime = DateTime.Today.Add(heure);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur lors du chargement du rendez-vous :\n" + ex.Message,
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (!ValiderFormulaire()) return;

            try
            {
                using var con = new SqlConnection(ConnectionString);
                con.Open();

                string query;

                if (_estModeEdition)
                {
                    query = @"
                        UPDATE rendezVous SET
                            idPatient = @idPatient,
                            idDocteur = @idDocteur,
                            dateRdv = @dateRdv,
                            heureRdv = @heureRdv,
                            description = @description,
                            statut = @statut
                        WHERE idRdv = @idRdv";
                }
                else
                {
                    query = @"
                        INSERT INTO rendezVous
                            (idRdv, idPatient, idDocteur, dateRdv, heureRdv, description, statut)
                        VALUES
                            (@idRdv, @idPatient, @idDocteur, @dateRdv, @heureRdv, @description, @statut)";
                }

                using var cmd = new SqlCommand(query, con);

                cmd.Parameters.AddWithValue("@idRdv", txtIdRdv.Text.Trim());
                cmd.Parameters.AddWithValue("@idPatient", cbPatient.SelectedValue);
                cmd.Parameters.AddWithValue("@idDocteur", cbDocteur.SelectedValue);
                cmd.Parameters.AddWithValue("@description", txtDescription.Text.Trim());
                cmd.Parameters.AddWithValue("@statut", ((ComboBoxItem)cbStatut.SelectedItem).Tag.ToString());

                // Date uniquement
                cmd.Parameters.AddWithValue("@dateRdv", inputDate.SelectedDate.Value.Date);

                // Heure uniquement (TimeSpan)
                cmd.Parameters.AddWithValue("@heureRdv", tpHeureRdv.SelectedDateTime.Value.TimeOfDay);

                cmd.ExecuteNonQuery();

                MessageBox.Show(
                    _estModeEdition
                        ? "Rendez-vous modifié avec succès !"
                        : "Rendez-vous enregistré avec succès !",
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
                    MessageBox.Show("Cet ID de rendez-vous existe déjà.",
                        "Erreur", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
                else
                {
                    MessageBox.Show("Erreur SQL :\n" + ex.Message,
                        "Erreur base de données", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur :\n" + ex.Message,
                    "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}