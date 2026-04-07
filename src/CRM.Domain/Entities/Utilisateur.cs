using CRM.Domain.Enums;

namespace CRM.Domain.Entities;

/// <summary>
/// Utilisateur du système CRM avec son rôle de sécurité.
/// </summary>
public class Utilisateur
{
    public int NoUtilisateur { get; set; }
    public string Login { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Nom { get; set; } = string.Empty;
    public string Prenom { get; set; } = string.Empty;
    public RoleUtilisateur Role { get; set; }
    public bool EstActif { get; set; } = true;
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;
    public DateTime DateModification { get; set; } = DateTime.UtcNow;
}
