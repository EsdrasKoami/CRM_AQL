using System;
using System.Threading;
using System.Threading.Tasks;
using CRM.Business.Services;
using CRM.Domain.Entities;
using CRM.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CRM.Business.Tests;

public class ContratValidationServiceTests
{
    private readonly Mock<IContratRepository> _mockRepo;
    private readonly Mock<ILogger<ContratValidationService>> _mockLogger;
    private readonly ContratValidationService _service;

    public ContratValidationServiceTests()
    {
        _mockRepo = new Mock<IContratRepository>();
        _mockLogger = new Mock<ILogger<ContratValidationService>>();
        _service = new ContratValidationService(_mockRepo.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task ContratValidAsync_NoClientVide_RetourneInvalide()
    {
        // Act
        var result = await _service.ContratValidAsync("");

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal("NoClient ne peut pas être vide.", result.Raison);
    }

    [Fact]
    public async Task ContratValidAsync_ClientSansContratActif_RetourneInvalide()
    {
        // Arrange
        var clientId = "C123456";
        _mockRepo.Setup(r => r.GetContratActifAsync(clientId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync((Contrat)null);

        // Act
        var result = await _service.ContratValidAsync(clientId);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Aucun contrat actif pour le client", result.Raison);
    }

    [Fact]
    public async Task ContratValidAsync_ClientAvecContratActif_RetourneValide()
    {
        // Arrange
        var clientId = "C123456";
        var contrat = new Contrat
        {
            NoContrat = 1001,
            NoClient = clientId,
            EstActif = true,
            DateDebut = DateTime.UtcNow.AddDays(-10),
            DateFin = DateTime.UtcNow.AddDays(10)
        };

        _mockRepo.Setup(r => r.GetContratActifAsync(clientId, It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
                 .ReturnsAsync(contrat);

        // Act
        var result = await _service.ContratValidAsync(clientId);

        // Assert
        Assert.True(result.IsValid);
        Assert.Null(result.Raison);
        Assert.Equal(1001, result.NoContrat);
    }
}
