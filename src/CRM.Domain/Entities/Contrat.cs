namespace CRM.Domain.Entities;

/// <summary>
/// Contrat liant un client au système JIT avec plafond de crédit et période de validité.
/// </summary>
public class Contrat
{
    public int NoContrat { get; set; }
    public string NoClient { get; set; } = string.Empty;
    public DateTime DateDebut { get; set; }
    public DateTime DateFin { get; set; }

    /// <summary>Plafond de crédit autorisé pour ce contrat.</summary>
    public decimal MontantMax { get; set; }

    public string? Description { get; set; }
    public bool EstActif { get; set; } = true;
    public DateTime DateCreation { get; set; } = DateTime.UtcNow;
    public DateTime DateModification { get; set; } = DateTime.UtcNow;

    // Navigation
    public Client? Client { get; set; }
    public ICollection<ItemContrat> Items { get; set; } = new List<ItemContrat>();
    public ICollection<Transaction> Transactions { get; set; } = new List<Transaction>();

    /// <summary>
    /// Indique si le contrat est actif à la date spécifiée.
    /// </summary>
    public bool EstValide(DateTime? asOf = null)
    {
        var date = asOf?.Date ?? DateTime.UtcNow.Date;
        return EstActif && DateDebut.Date <= date && DateFin.Date >= date;
    }
}
