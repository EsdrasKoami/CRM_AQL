namespace CRM.Domain.Entities;

/// <summary>
/// Représente un client. L'identifiant suit le format C###### (ex: C000001).
/// </summary>
public class Client
{
    /// <summary>Format : C######</summary>
    public string NoClient { get; set; } = string.Empty;

    public string NomEntreprise { get; set; } = string.Empty;
    public string? Adresse { get; set; }
    public string? Ville { get; set; }
    public string? CodePostal { get; set; }
    public string? Telephone { get; set; }
    public string? Courriel { get; set; }
    public bool EstActif { get; set; } = true;
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;

    // Navigation
    public ICollection<Contrat> Contrats { get; set; } = new List<Contrat>();
}
