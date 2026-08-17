using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WpfBasics
{
    public class Patient : INotifyPropertyChanged
    {
        private string _idPatient;
        private string _nomComplet;
        private string _idDossier;
        private string _sexe;
        public string _nom;
        public string _prenom;
        private int _age;
        private string _email;
        private DateTime _dateNaissance;
        private string _groupeSanguin;
        private string _telephone;
        private string _adresse;

        public string IdPatient
        {
            get => _idPatient;
            set { _idPatient = value; OnPropertyChanged(); }
        }
        public string IdDossier
        {
            get => _idDossier;
            set { _idDossier = value; OnPropertyChanged(); }
        }
        public string Nom
        {
            get => _nom;
            set { _nom = value; OnPropertyChanged(); }
        }
        public string Prenom
        {
            get => _prenom;
            set { _prenom = value; OnPropertyChanged(); }
        }

        public string NomComplet
        {
            get => _nomComplet;
            set { _nomComplet = value; OnPropertyChanged(); }
        }

        public string Sexe
        {
            get => _sexe;
            set { _sexe = value; OnPropertyChanged(); }
        }

        public int Age
        {
            get => _age;
            set { _age = value; OnPropertyChanged(); }
        }

        public string Contact
        {
            get => _email;
            set { _email = value; OnPropertyChanged(); }
        }

        public DateTime DateNaissance
        {
            get => _dateNaissance;
            set { _dateNaissance = value; OnPropertyChanged(); OnPropertyChanged(nameof(DateNaissanceAffichee)); }
        }

        public string GroupeSanguin
        {
            get => _groupeSanguin;
            set { _groupeSanguin = value; OnPropertyChanged(); }
        }

        public string Initials
        {
            get
            {
                if (string.IsNullOrWhiteSpace(NomComplet)) return "?";
                var parts = NomComplet.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 2)
                    return $"{char.ToUpper(parts[0][0])}{char.ToUpper(parts[1][0])}";
                return char.ToUpper(parts[0][0]).ToString();
            }
        }

        public string Telephone
        {
            get => _telephone;
            set { _telephone = value; OnPropertyChanged(); }
        }

        public string Adresse
        {
            get => _adresse;
            set { _adresse = value; OnPropertyChanged(); }
        }
        
        public string DateNaissanceAffichee
        {
            get
            {
                if (DateNaissance == default) return "";
                return $"{DateNaissance:dd MMMM yyyy} ({Age} ans)";
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}