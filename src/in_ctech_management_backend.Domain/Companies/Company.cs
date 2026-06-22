using in_ctech_management_backend.Domain.Common;

namespace in_ctech_management_backend.Domain.Companies
{
    public class Company : AuditableEntity
    {
        public CompanyId CompanyId  { get; private set; }
        public string Nom { get; private set; }
        public string? Adresse { get; private set; }
        public string? Contact { get; private set; }
        public string Code { get; private set; }
        public string Pays { get; private set; }
        public string SocietyType { get; private set; }

        private Company()
        {
            CompanyId = new CompanyId(Guid.NewGuid());
            Nom = string.Empty;
            Code = string.Empty;
            Pays = string.Empty;
            SocietyType = string.Empty;
        }

        private Company(string nom, string? adresse, string? contact, string code, string pays, string societyType, string? createdBy)
        {
            CompanyId = new CompanyId(Guid.NewGuid());
            Nom = nom;
            Adresse = adresse;
            Contact = contact;
            Code = code;
            Pays = pays;
            SocietyType = societyType;
            CreatedBy = createdBy;
        }

        public static Company Create(string nom, string? adresse, string? contact, string code, string pays, string societyType, string? createdBy = null)
        {
            ValidateNom(nom);
            ValidateCode(code);
            ValidatePays(pays);
            ValidateSocietyType(societyType);
            return new Company(nom, adresse, contact, code, pays, societyType, createdBy);
        }

        public void Update(string? nom, string? adresse, string? contact, string? code, string? pays, string? societyType, string? updatedBy = null)
        {
            if (nom is not null)
            {
                ValidateNom(nom);
                Nom = nom;
            }
            if (adresse is not null) Adresse = adresse;
            if (contact is not null) Contact = contact;
            if (code is not null)
            {
                ValidateCode(code);
                Code = code;
            }
            if (pays is not null)
            {
                ValidatePays(pays);
                Pays = pays;
            }
            if (societyType is not null)
            {
                ValidateSocietyType(societyType);
                SocietyType = societyType;
            }
            UpdatedAt = DateTime.UtcNow;
            UpdatedBy = updatedBy;
        }

        private static void ValidateNom(string nom)
        {
            if (string.IsNullOrWhiteSpace(nom))
                throw new DomainException("Le nom de la société est obligatoire");
            if (nom.Length > 150)
                throw new DomainException("Le nom ne peut pas dépasser 150 caractères");
        }

        private static void ValidateCode(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
                throw new DomainException("Le code (ICE/SIREN) est obligatoire");
        }

        private static void ValidatePays(string pays)
        {
            if (string.IsNullOrWhiteSpace(pays))
                throw new DomainException("Le pays est obligatoire");
        }

        private static void ValidateSocietyType(string societyType)
        {
            if (string.IsNullOrWhiteSpace(societyType))
                throw new DomainException("Le type de société est obligatoire");
        }
    }
}
