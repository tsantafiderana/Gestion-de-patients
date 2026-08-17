using System;
using System.Collections.Generic;
using System.Text;

namespace WpfBasics
{
    public class LignePrescription
    {
        public string IdLigne { get; set; }
        public string IdPrescription { get; set; }
        public string NomMedicament { get; set; }
        public string Posologie { get; set; }
        public int Quantite { get; set; }
        public int DureeTraitement { get; set; }
        public string Instructions { get; set; }
        public string Unite { get; set; }

        public string NomDocteur {  get; set; }
        public DateTime DatePrescription {  get; set; }

    }
}
