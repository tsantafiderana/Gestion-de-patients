using System;
using System.Collections.Generic;
using System.Text;

namespace WpfBasics
{
    public class Consultation
    {
        public string IdConsultation { get; set; }
        public string IdDocteur { get; set; }
        public string NomDocteur { get; set; }   // nom complet du docteur
        public string Motif { get; set; }
        public string Diagnostic { get; set; }
        public string Observation { get; set; }
        public string Conclusion { get; set; }
        public DateTime DateConsultation { get; set; }
    }
}
