using CRM.Business.Services;
using CRM.Domain.Entities;
using CRM.Domain.Interfaces.Repositories;
using CRM.Domain.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace CRM.Tests.Business;

/// <summary>
/// Tests unitaires pour CreditControlService.
/// </summary>
public class CreditControlServiceTests
{
    private readonly Mock<IContratRepository>     _contratRepoMock;
    private readonly Mock<ITransactionRepository> _transactionRepoMock;
    private readonly CreditControlService         _service;

    public CreditControlServiceTests()
    {
        _contratRepoMock     = new Mock<IContratRepository>();
        _transactionRepoMock = new Mock<ITransactionRepository>();
        _service = new CreditControlService(
            _contratRepoMock.Object,
            _transactionRepoMock.Object,
            NullLogger<CreditControlService>.Instance);
    }

    [Fact]
    public async Task CommandeAutorisee_SoldePlusCommandeSousPlafond_ReturnsTrue()
    {
        // Arrange — solde 10 000 $, plafond 50 000 $, commande 5 000 $
        _transactionRepoMock
            .Setup(r => r.GetSoldeClientAsync("C000001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new SoldeClientDto
                {
                    NoClient      = "C000001",
                    SoldeContrat  = 10_000m,
                    MontantMax    = 50_000m,
                    ContratActif  = true
                }
            });

        // Act
        var result = await _service.CommandeAutoriseeAsync("C000001", 5_000m);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task CommandeAutorisee_SoldePlusCommandeDepassePlafond_ReturnsFalse()
    {
        // Arrange — solde 48 000 $, plafond 50 000 $, commande 3 000 $ → dépasse
        _transactionRepoMock
            .Setup(r => r.GetSoldeClientAsync("C000001", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new SoldeClientDto
                {
                    NoClient      = "C000001",
                    SoldeContrat  = 48_000m,
                    MontantMax    = 50_000m,
                    ContratActif  = true
                }
            });

        // Act
        var result = await _service.CommandeAutoriseeAsync("C000001", 3_000m);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task CommandeAutorisee_AucunContratActif_ReturnsFalse()
    {
        // Arrange — aucun contrat actif
        _transactionRepoMock
            .Setup(r => r.GetSoldeClientAsync("C000099", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<SoldeClientDto>());

        // Act
        var result = await _service.CommandeAutoriseeAsync("C000099", 1_000m);

        // Assert
        result.Should().BeFalse();
    }
}
