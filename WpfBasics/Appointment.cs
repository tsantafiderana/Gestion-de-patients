using System;
using System.Collections.Generic;
using System.Text;

namespace WpfBasics
{
    public class Appointment
    {
        public string IdRdv { get; set; }
        public string IdPatient { get; set; }
        public string IdDocteur { get; set; }

        public string NomDocteur { get; set; }
        public DateTime DateRdv { get; set; }
        public TimeSpan HeureRdv { get; set; }
        public string Description { get; set; }

        // Temporaire : on force Confirmed tant qu'il n'y a pas de colonne Status
        public AppointmentStatus Status { get; set; } = AppointmentStatus.Confirmed;
    }

    public enum AppointmentStatus
    {
        Confirmed,
        Waiting,
        Completed,
        Cancelled
    }
}
