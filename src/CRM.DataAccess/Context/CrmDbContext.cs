using CRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CRM.DataAccess.Context;

public class CrmDbContext : DbContext
{
    public CrmDbContext(DbContextOptions<CrmDbContext> options) : base(options) { }

    public DbSet<Client>      Clients      => Set<Client>();
    public DbSet<Contrat>     Contrats     => Set<Contrat>();
    public DbSet<ItemContrat> ItemsContrat => Set<ItemContrat>();
    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Utilisateur> Utilisateurs => Set<Utilisateur>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // ── Client ──────────────────────────────────────────────
        modelBuilder.Entity<Client>(e =>
        {
            e.ToTable("Clients");
            e.HasKey(c => c.NoClient);
            e.Property(c => c.NoClient).HasMaxLength(7).IsFixedLength();
            e.Property(c => c.NomEntreprise).HasMaxLength(150).IsRequired();
            e.Property(c => c.Adresse).HasMaxLength(250);
            e.Property(c => c.Ville).HasMaxLength(100);
            e.Property(c => c.CodePostal).HasMaxLength(7);
            e.Property(c => c.Telephone).HasMaxLength(20);
            e.Property(c => c.Courriel).HasMaxLength(150);
            e.Property(c => c.DateCreation).HasDefaultValueSql("SYSUTCDATETIME()");
        });

        // ── Contrat ─────────────────────────────────────────────
        modelBuilder.Entity<Contrat>(e =>
        {
            e.ToTable("Contrats");
            e.HasKey(c => c.NoContrat);
            e.Property(c => c.MontantMax).HasColumnType("decimal(18,2)");
            e.Property(c => c.Description).HasMaxLength(300);
            e.Property(c => c.DateCreation).HasDefaultValueSql("SYSUTCDATETIME()");
            e.Property(c => c.DateModification).HasDefaultValueSql("SYSUTCDATETIME()");

            e.HasOne(c => c.Client)
             .WithMany(cl => cl.Contrats)
             .HasForeignKey(c => c.NoClient)
             .OnDelete(DeleteBehavior.Restrict);
        });

        // ── ItemContrat ──────────────────────────────────────────
        modelBuilder.Entity<ItemContrat>(e =>
        {
            e.ToTable("ItemsContrat");
            e.HasKey(i => i.NoItemContrat);
            e.Property(i => i.NoProduit).HasMaxLength(50).IsRequired();
            e.Property(i => i.DescriptionProduit).HasMaxLength(200);
            e.Property(i => i.PrixUnitaire).HasColumnType("decimal(18,4)");

            e.HasIndex(i => new { i.NoContrat, i.NoProduit }).IsUnique();

            e.HasOne(i => i.Contrat)
             .WithMany(c => c.Items)
             .HasForeignKey(i => i.NoContrat)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // ── Transaction ──────────────────────────────────────────
        modelBuilder.Entity<Transaction>(e =>
        {
            e.ToTable("Transactions");
            e.HasKey(t => t.NoTransaction);
            e.Property(t => t.TypeTransaction)
             .HasConversion<string>()
             .HasMaxLength(20);
            e.Property(t => t.Montant).HasColumnType("decimal(18,2)");
            e.Property(t => t.Reference).HasMaxLength(100);
            e.Property(t => t.NoProduit).HasMaxLength(50);
            e.Property(t => t.Commentaire).HasMaxLength(500);
            e.Property(t => t.DateTransaction).HasDefaultValueSql("SYSUTCDATETIME()");

            e.HasOne(t => t.Contrat)
             .WithMany(c => c.Transactions)
             .HasForeignKey(t => t.NoContrat)
             .OnDelete(DeleteBehavior.Restrict);

            e.HasOne(t => t.Utilisateur)
             .WithMany()
             .HasForeignKey(t => t.NoUtilisateur)
             .IsRequired(false)
             .OnDelete(DeleteBehavior.SetNull);
        });

        // ── Utilisateur ──────────────────────────────────────────
        modelBuilder.Entity<Utilisateur>(e =>
        {
            e.ToTable("Utilisateurs");
            e.HasKey(u => u.NoUtilisateur);
            e.HasIndex(u => u.Login).IsUnique();
            e.Property(u => u.Login).HasMaxLength(50).IsRequired();
            e.Property(u => u.PasswordHash).HasMaxLength(256).IsRequired();
            e.Property(u => u.Nom).HasMaxLength(100).IsRequired();
            e.Property(u => u.Prenom).HasMaxLength(100).IsRequired();
            e.Property(u => u.Role)
             .HasConversion<string>()
             .HasMaxLength(30);
            e.Property(u => u.DateCreation).HasDefaultValueSql("SYSUTCDATETIME()");
            e.Property(u => u.DateModification).HasDefaultValueSql("SYSUTCDATETIME()");
        });
    }
}
