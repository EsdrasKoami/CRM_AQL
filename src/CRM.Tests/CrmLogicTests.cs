using Microsoft.VisualStudio.TestTools.UnitTesting;
using CRM.MessagingConsole.Data;
using CRM.MessagingConsole.Models;
using System.Text;

namespace CRM.Tests;

[TestClass]
public class CrmLogicTests
{
    [TestMethod]
    public void TestContratValide_ShouldReturnTrue_WhenDatesAndCreditAreOk()
    {
        // Arrange
        var contrat = new Contrat
        {
            DateDebut = DateTime.Now.AddDays(-1),
            DateFin = DateTime.Now.AddDays(1),
            MontantMaxCredit = 1000m,
            SoldeActuel = 500m
        };

        // Act & Assert
        Assert.IsTrue(contrat.EstValide);
    }

    [TestMethod]
    public void TestContratInvalide_ShouldReturnFalse_WhenCreditExceeded()
    {
        // Arrange
        var contrat = new Contrat
        {
            DateDebut = DateTime.Now.AddDays(-1),
            DateFin = DateTime.Now.AddDays(1),
            MontantMaxCredit = 1000m,
            SoldeActuel = 1500m // Trop cher !
        };

        // Act & Assert
        Assert.IsFalse(contrat.EstValide);
    }

    [TestMethod]
    public void TestRepository_ShouldReturnCorrectClient()
    {
        // Arrange
        var repo = new CrmRepository();

        // Act
        var result = repo.GetContratByClient("C1");

        // Assert
        Assert.IsNotNull(result);
        Assert.AreEqual("C1", result.ClientId);
    }

    [TestMethod]
    public void TestDeserialization_ShouldHandleInvalidJsonGracefully()
    {
        // Arrange
        byte[] badJson = Encoding.UTF8.GetBytes("{ invalid json }");

        // Act
        var result = RMQEnveloppe.Deserialise(badJson);

        // Assert
        Assert.AreEqual("Unknown", result.MessageName);
        Assert.AreEqual("Error", result.Status);
    }

    [TestMethod]
    public void TestContratInvalide_ShouldReturnFalse_WhenDateExpired()
    {
        // Arrange
        var contrat = new Contrat
        {
            DateDebut = DateTime.Now.AddDays(-30),
            DateFin = DateTime.Now.AddDays(-1), // Expiré hier
            MontantMaxCredit = 5000m,
            SoldeActuel = 100m
        };

        // Act & Assert
        Assert.IsFalse(contrat.EstValide);
    }

    [TestMethod]
    public void TestSoldeClient_ShouldCalculateCorrectBalance_AfterInvoiceAndPayment()
    {
        // Arrange
        var repo = new CrmRepository();
        string clientId = "TEST_CLIENT";

        // Act
        repo.EnregistrerTransaction(clientId, 1000m, "Facture");
        repo.EnregistrerTransaction(clientId, 250m, "Facture");
        repo.EnregistrerTransaction(clientId, 500m, "Paiement");

        // Assert: 1000 + 250 - 500 = 750
        decimal solde = repo.GetSoldeClient(clientId);
        Assert.AreEqual(750m, solde);
    }

    [TestMethod]
    public void TestSerializationRoundTrip_ShouldPreserveEnvelopeProperties()
    {
        // Arrange
        var envelope = new RMQEnveloppe("Ceduler", "CRM_ENGINE", "Actif", "Production Order #850-2026");

        // Act
        string json = System.Text.Json.JsonSerializer.Serialize(envelope);
        byte[] raw = Encoding.UTF8.GetBytes(json);
        var restored = RMQEnveloppe.Deserialise(raw);

        // Assert
        Assert.AreEqual(envelope.MessageName, restored.MessageName);
        Assert.AreEqual(envelope.Sender, restored.Sender);
        Assert.AreEqual(envelope.Status, restored.Status);
        Assert.AreEqual(envelope.MessageText, restored.MessageText);
    }
}

