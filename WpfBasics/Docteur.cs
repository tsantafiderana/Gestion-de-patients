using System;
using System.Collections.Generic;
using System.Text;

namespace WpfBasics
{
    public class Docteur
    {
        public string IdDocteur { get; set; }
        public string NomComplet {  get; set; }

        public string Nom {  get; set; }
        public string Prenom { get; set; }
        public string Specialite { get; set; }
        public string Telephone { get; set; }
        public string Email {  get; set; }
        public string Sexe { get; set; }

        public int NombreConsultation { get; set; }

        public int NombreRdv { get; set; }

        public string Initials { get; set; }
    }
}
