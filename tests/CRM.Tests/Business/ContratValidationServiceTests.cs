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
/// Tests unitaires pour ContratValidationService (ContratValid()).
/// </summary>
public class ContratValidationServiceTests
{
    private readonly Mock<IContratRepository> _contratRepoMock;
    private readonly ContratValidationService _service;

    public ContratValidationServiceTests()
    {
        _contratRepoMock = new Mock<IContratRepository>();
        _service = new ContratValidationService(
            _contratRepoMock.Object,
            NullLogger<ContratValidationService>.Instance);
    }

    [Fact]
    public async Task ContratValid_ClientActif_ContratActif_ReturnsTrue()
    {
        // Arrange
        var contrat = new Contrat
        {
            NoContrat = 1,
            NoClient  = "C000001",
            DateDebut  = DateTime.Today.AddDays(-30),
            DateFin    = DateTime.Today.AddDays(30),
            MontantMax = 50_000,
            EstActif   = true
        };

        _contratRepoMock
            .Setup(r => r.GetContratActifAsync("C000001", It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(contrat);

        // Act
        var result = await _service.ContratValidAsync("C000001");

        // Assert
        result.IsValid.Should().BeTrue();
        result.NoContrat.Should().Be(1);
        result.Raison.Should().BeNull();
    }

    [Fact]
    public async Task ContratValid_AucunContratActif_ReturnsFalse()
    {
        // Arrange
        _contratRepoMock
            .Setup(r => r.GetContratActifAsync("C000099", It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Contrat?)null);

        // Act
        var result = await _service.ContratValidAsync("C000099");

        // Assert
        result.IsValid.Should().BeFalse();
        result.NoContrat.Should().BeNull();
        result.Raison.Should().Contain("C000099");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null!)]
    public async Task ContratValid_NoClientVide_ReturnsFalse(string noClient)
    {
        // Act
        var result = await _service.ContratValidAsync(noClient);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Raison.Should().NotBeNullOrEmpty();
    }
}
