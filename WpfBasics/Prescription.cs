using System;
using System.Collections.Generic;
using System.Text;

namespace WpfBasics
{
    public class Prescription
    {
        public string IdPrescription { get; set; }
        public string IdConsultation { get; set; }
        public string IdDoteur { get; set; }
        public string IdPatient { get; set; }

        public DateTime DatePrescription { get; set; }
    }
}
