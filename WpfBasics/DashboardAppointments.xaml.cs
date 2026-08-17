using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace WpfBasics
{
    public partial class DashboardAppointments : UserControl
    {
        private readonly string ConnectionString =
            "Server=localhost;Database=WpfBasic;Trusted_Connection=True;Encrypt=False;";

        private DateTime _selectedDay;
        private ObservableCollection<RendezVous> _allAppointments = new();
        private List<RendezVous> _filteredAppointments = new();

        public ObservableCollection<Docteur> ListeDocteurs { get; } = new();

        private DispatcherTimer _reminderTimer;
        private HashSet<string> _notificationsDejaEnvoyees = new();

        public DashboardAppointments()
        {
            InitializeComponent();
            DataContext = this;

            _selectedDay = DateTime.Today;

            LoadAppointmentsFromDatabase();
            LoadDoctor();

            
            ListeDocteurs.Insert(0, new Docteur
            {
                IdDocteur = "",
                NomComplet = "Tous les docteurs"
            });

            cbOptions.SelectedIndex = 0;
            DatePickerAgenda.SelectedDate = _selectedDay;

            ApplyFiltersAndRefresh();
            StartReminderTimer();
        }        

        private void LoadAppointmentsFromDatabase()
        {
            _allAppointments.Clear();

            try
            {
                using var conn = new SqlConnection(ConnectionString);
                conn.Open();

                string sql = @"
                    SELECT  r.idRdv,
                            r.idPatient,
                            r.idDocteur,
                            r.dateRdv,
                            r.heureRdv,
                            r.description,
                            r.statut,
                            CONCAT(d.nom, ' ', d.prenom) AS NomDocteur,
                            CONCAT(p.nom, ' ', p.prenom) AS NomPatient
                    FROM    rendezVous r
                    JOIN    docteur  d ON r.idDocteur  = d.idDocteur
                    JOIN    patient  p ON r.idPatient  = p.idPatient
                    ORDER BY r.dateRdv, r.heureRdv";

                using var cmd = new SqlCommand(sql, conn);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    var statutRaw = reader["statut"]?.ToString()?.Trim().ToLower() ?? "prevu";

                    var appt = new RendezVous
                    {
                        IdRdv = reader["idRdv"].ToString(),
                        IdPatient = reader["idPatient"].ToString(),
                        IdDocteur = reader["idDocteur"].ToString(),
                        NomDocteur = reader["NomDocteur"].ToString(),
                        NomPatient = reader["NomPatient"].ToString(),
                        DateRdv = Convert.ToDateTime(reader["dateRdv"]),
                        HeureRdv = (TimeSpan)reader["heureRdv"],
                        Description = reader["description"].ToString(),
                        Status = statutRaw switch
                        {
                            "confirme" or "confirmé" or "confirmed" => AppointmentStatus.Confirmed,
                            "annule" or "annulé" or "cancelled" => AppointmentStatus.Cancelled,
                            "termine" or "terminé" or "completed" => AppointmentStatus.Completed,
                            "prevu" or "prévu" or "waiting" => AppointmentStatus.Waiting,
                            _ => AppointmentStatus.Waiting
                        }
                    };

                    _allAppointments.Add(appt);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Erreur SQL :\n" + ex.Message, "Erreur", MessageBoxButton.OK, MessageBoxImage.Error);
            }
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
                           CONCAT(d.nom, ' ', d.prenom) AS NomComplet,
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
                MessageBox.Show("Erreur chargement docteurs : " + ex.Message);
            }
        }

           
        private void ApplyFiltersAndRefresh()
        {
            string selectedDoctorId = cbOptions.SelectedValue as string;

            var dayAppointments = _allAppointments
                .Where(a =>
                    a.DateRdv.Date == _selectedDay.Date
                    && (string.IsNullOrEmpty(selectedDoctorId) || a.IdDocteur == selectedDoctorId)
                )
                .OrderBy(a => a.HeureRdv)
                .ToList();

            // Bloc 1 : prévu + confirmé
            var active = dayAppointments
                .Where(a => a.Status == AppointmentStatus.Waiting || a.Status == AppointmentStatus.Confirmed)
                .ToList();

            // Bloc 2 : terminé + annulé
            var finished = dayAppointments
                .Where(a => a.Status == AppointmentStatus.Completed || a.Status == AppointmentStatus.Cancelled)
                .ToList();

            DailyAppointmentsList.ItemsSource = active;
            CompletedCancelledList.ItemsSource = finished;

            // Titre principal
            DailyAgendaTitle.Text =
                $"Agenda du {_selectedDay:dd MMMM yyyy} · {active.Count} rdv actifs";

            // Afficher / masquer le titre du 2e bloc
            CompletedCancelledTitle.Visibility = finished.Any()
                ? Visibility.Visible
                : Visibility.Collapsed;

            CompletedCancelledTitle.Text = finished.Any()
                ? $"Terminés & Annulés ({finished.Count})"
                : "Terminés & Annulés";
        }

        private void PreviousDay_Click(object sender, RoutedEventArgs e)
        {
            _selectedDay = _selectedDay.AddDays(-1);
            DatePickerAgenda.SelectedDate = _selectedDay;
            ApplyFiltersAndRefresh();
        }

        private void NextDay_Click(object sender, RoutedEventArgs e)
        {
            _selectedDay = _selectedDay.AddDays(1);
            DatePickerAgenda.SelectedDate = _selectedDay;
            ApplyFiltersAndRefresh();
        }

        private void DatePickerAgenda_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DatePickerAgenda.SelectedDate.HasValue)
            {
                _selectedDay = DatePickerAgenda.SelectedDate.Value.Date;
                ApplyFiltersAndRefresh();
            }
        }

        private void DoctorFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;
            ApplyFiltersAndRefresh();
        }

        

        private void PrevMonth_Click(object sender, RoutedEventArgs e) { }
        private void NextMonth_Click(object sender, RoutedEventArgs e) { }
        private void Today_Click(object sender, RoutedEventArgs e)
        {
            _selectedDay = DateTime.Today;
            DatePickerAgenda.SelectedDate = _selectedDay;
            ApplyFiltersAndRefresh();
        }

        private void NewAppointment_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Ouvrir le formulaire de création d'appointment", "New Appointment");
        }

        private void btn_newRdv_Click(object sender, RoutedEventArgs e)
        {
            var modal = new ModalRdv();
            bool? result = modal.ShowDialog();

            if (result == true)
            {
                LoadAppointmentsFromDatabase();
                ApplyFiltersAndRefresh();
            }
        }

        private void EditAppointment_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is RendezVous rdv)
            {
                var modal = new ModalRdv(rdv); 
                bool? result = modal.ShowDialog();

                if (result == true)
                {
                    LoadAppointmentsFromDatabase();
                    ApplyFiltersAndRefresh();
                }
            }
        }

        private void btn_complete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is RendezVous rdv)
            {
                try
                {
                    using var con = new SqlConnection(ConnectionString);
                    con.Open();

                    string query = @"UPDATE rendezVous SET statut = 'termine' WHERE idRdv = @id";
                    using var cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@id", rdv.IdRdv);

                    cmd.ExecuteNonQuery();

                    LoadAppointmentsFromDatabase();
                    ApplyFiltersAndRefresh();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Erreur : " + ex.Message);
                }
            }
        }
        private void btn_cancel_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is RendezVous rdv)
            {
                try
                {
                    using var con = new SqlConnection(ConnectionString);
                    con.Open();

                    string query = @"UPDATE rendezVous SET statut = 'annule' WHERE idRdv = @id";
                    using var cmd = new SqlCommand(query, con);
                    cmd.Parameters.AddWithValue("@id", rdv.IdRdv);

                    cmd.ExecuteNonQuery();

                    LoadAppointmentsFromDatabase();
                    ApplyFiltersAndRefresh();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Erreur : " + ex.Message);
                }
            }
        }




        private void StartReminderTimer()
        {
            _reminderTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(30) 
            };
            _reminderTimer.Tick += ReminderTimer_Tick;
            _reminderTimer.Start();
        }

        private void ReminderTimer_Tick(object sender, EventArgs e)
        {
            DateTime maintenant = DateTime.Now;

            foreach (var rdv in _allAppointments)
            {                
                if (rdv.Status != AppointmentStatus.Confirmed)
                    continue;

                DateTime dateHeureRdv = rdv.DateRdv.Date.Add(rdv.HeureRdv);
                
                double minutesRestantes = (dateHeureRdv - maintenant).TotalMinutes;
                
                if (minutesRestantes >= 14 && minutesRestantes <= 16)
                {
                    string cle = rdv.IdRdv + "_" + dateHeureRdv.ToString("yyyyMMddHHmm");

                    if (!_notificationsDejaEnvoyees.Contains(cle))
                    {
                        _notificationsDejaEnvoyees.Add(cle);
                        AfficherNotification(rdv);
                    }
                }
            }
        }

        private void AfficherNotification(RendezVous rdv)
        {
            MessageBox.Show(
                $"Rappel : Rendez-vous dans environ 15 minutes\n\n" +
                $"Patient : {rdv.PatientName}\n" +
                $"Docteur : {rdv.DoctorAndRoom}\n" +
                $"Heure   : {rdv.Time}\n" +
                $"Description : {rdv.Description}",
                "Rappel de rendez-vous",
                MessageBoxButton.OK,
                MessageBoxImage.Information);

            System.Media.SystemSounds.Exclamation.Play();
        }
    }
}