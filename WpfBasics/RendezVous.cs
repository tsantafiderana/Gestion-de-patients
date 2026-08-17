using System;
using System.Windows.Media;

namespace WpfBasics
{
    

    public class RendezVous
    {
        public string IdRdv { get; set; }
        public string IdPatient { get; set; }
        public string IdDocteur { get; set; }
        public string NomDocteur { get; set; }
        public string NomPatient { get; set; }
        public DateTime DateRdv { get; set; }
        public TimeSpan HeureRdv { get; set; }
        public string Description { get; set; }
        public AppointmentStatus Status { get; set; }

        // ===== Properties used by the Daily Agenda template =====
        public string TimeLabel => HeureRdv.ToString(@"hh\:mm");
        public string Time => HeureRdv.ToString(@"hh\:mm");
        public string PatientName => NomPatient ?? IdPatient;
        public string AppointmentType => Description;
        public string DoctorAndRoom => NomDocteur;

        public Brush StatusColor => Status switch
        {
            AppointmentStatus.Confirmed => new SolidColorBrush(Color.FromRgb(0x3B, 0x82, 0xF6)),
            AppointmentStatus.Waiting => new SolidColorBrush(Color.FromRgb(0xF5, 0x9E, 0x0B)),
            AppointmentStatus.Completed => new SolidColorBrush(Color.FromRgb(0x10, 0xB9, 0x81)),
            AppointmentStatus.Cancelled => new SolidColorBrush(Color.FromRgb(0xEF, 0x44, 0x44)),
            _ => Brushes.Gray
        };

        public Brush StatusBg => Status switch
        {
            AppointmentStatus.Confirmed => new SolidColorBrush(Color.FromRgb(0xDB, 0xEA, 0xFE)),
            AppointmentStatus.Waiting => new SolidColorBrush(Color.FromRgb(0xFE, 0xF3, 0xC7)),
            AppointmentStatus.Completed => new SolidColorBrush(Color.FromRgb(0xD1, 0xFA, 0xE5)),
            AppointmentStatus.Cancelled => new SolidColorBrush(Color.FromRgb(0xFE, 0xE2, 0xE2)),
            _ => Brushes.LightGray
        };

        public string StatusText => Status switch
        {
            AppointmentStatus.Confirmed => "Confirmé",
            AppointmentStatus.Waiting => "Prévu",
            AppointmentStatus.Completed => "Terminé",
            AppointmentStatus.Cancelled => "Annulé",
            _ => "—"
        };
    }
}