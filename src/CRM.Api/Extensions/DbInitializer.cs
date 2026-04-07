using CRM.DataAccess.Context;
using CRM.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CRM.Api.Extensions;

public static class DbInitializer
{
    public static void Initialize(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<CrmDbContext>();

        // Crée les tables si elles n'existent pas selon le mapping EF Core
        context.Database.EnsureCreated();

        // Création des utilisateurs de test
        if (!context.Set<Utilisateur>().Any())
        {
            var pwdHash = BCrypt.Net.BCrypt.HashPassword("Password123");
            context.Set<Utilisateur>().AddRange(
                new Utilisateur { Login = "Agent1", PasswordHash = pwdHash, Role = CRM.Domain.Enums.RoleUtilisateur.Agent, EstActif = true, Nom = "Test", Prenom = "Agent" },
                new Utilisateur { Login = "Directeur1", PasswordHash = pwdHash, Role = CRM.Domain.Enums.RoleUtilisateur.DirecteurFinances, EstActif = true, Nom = "Test", Prenom = "Directeur" }
            );
            context.SaveChanges();
        }

        // Création d'un client et d'un contrat de test pour le Frontend
        if (!context.Set<Client>().Any(c => c.NoClient == "C000001"))
        {
            context.Set<Client>().Add(new Client { NoClient = "C000001", NomEntreprise = "Industries Stark", EstActif = true });
            context.Set<Contrat>().Add(new Contrat { NoClient = "C000001", DateDebut = DateTime.Today, DateFin = DateTime.Today.AddYears(1), MontantMax = 5000.00m, EstActif = true });
            context.SaveChanges();
        }
    }
}
