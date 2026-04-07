-- ============================================================
-- Projet AQL 420-454-RI — Script de création de la base de données
-- Base   : CRM_AQL
-- Date   : 2026-04-07
-- ============================================================

USE master;
GO

IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = N'CRM_AQL')
BEGIN
    CREATE DATABASE CRM_AQL;
END
GO

USE CRM_AQL;
GO

-- ============================================================
-- TABLE : Utilisateurs
-- Deux rôles : Agent | DirecteurFinances
-- ============================================================
IF OBJECT_ID('dbo.Utilisateurs', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Utilisateurs (
        NoUtilisateur   INT IDENTITY(1,1) PRIMARY KEY,
        Login           NVARCHAR(50)  NOT NULL UNIQUE,
        PasswordHash    NVARCHAR(256) NOT NULL,   -- BCrypt hash
        Nom             NVARCHAR(100) NOT NULL,
        Prenom          NVARCHAR(100) NOT NULL,
        Role            NVARCHAR(30)  NOT NULL     -- 'Agent' | 'DirecteurFinances'
            CHECK (Role IN ('Agent', 'DirecteurFinances')),
        EstActif        BIT           NOT NULL DEFAULT 1,
        DateCreation    DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
        DateModification DATETIME2    NOT NULL DEFAULT SYSUTCDATETIME()
    );
END
GO

-- ============================================================
-- TABLE : Clients
-- ID format C######  (ex: C000001)
-- ============================================================
IF OBJECT_ID('dbo.Clients', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Clients (
        NoClient        CHAR(7)       NOT NULL PRIMARY KEY   -- C######
            CHECK (NoClient LIKE 'C[0-9][0-9][0-9][0-9][0-9][0-9]'),
        NomEntreprise   NVARCHAR(150) NOT NULL,
        Adresse         NVARCHAR(250) NULL,
        Ville           NVARCHAR(100) NULL,
        CodePostal      CHAR(7)       NULL,
        Telephone       NVARCHAR(20)  NULL,
        Courriel        NVARCHAR(150) NULL,
        EstActif        BIT           NOT NULL DEFAULT 1,
        DateCreation    DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME()
    );
END
GO

-- ============================================================
-- TABLE : Contrats
-- ============================================================
IF OBJECT_ID('dbo.Contrats', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Contrats (
        NoContrat       INT IDENTITY(1,1) PRIMARY KEY,
        NoClient        CHAR(7)         NOT NULL
            REFERENCES dbo.Clients(NoClient),
        DateDebut       DATE            NOT NULL,
        DateFin         DATE            NOT NULL,
        MontantMax      DECIMAL(18, 2)  NOT NULL   -- Plafond de crédit
            CHECK (MontantMax >= 0),
        Description     NVARCHAR(300)   NULL,
        EstActif        BIT             NOT NULL DEFAULT 1,
        DateCreation    DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        DateModification DATETIME2      NOT NULL DEFAULT SYSUTCDATETIME(),
        CONSTRAINT CK_Contrats_Dates CHECK (DateFin > DateDebut)
    );
    CREATE INDEX IX_Contrats_NoClient ON dbo.Contrats(NoClient);
END
GO

-- ============================================================
-- TABLE : ItemsContrat
-- Prix fixe + Quotas mensuels min/max par produit
-- ============================================================
IF OBJECT_ID('dbo.ItemsContrat', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.ItemsContrat (
        NoItemContrat   INT IDENTITY(1,1) PRIMARY KEY,
        NoContrat       INT             NOT NULL
            REFERENCES dbo.Contrats(NoContrat),
        NoProduit       NVARCHAR(50)    NOT NULL,
        DescriptionProduit NVARCHAR(200) NULL,
        PrixUnitaire    DECIMAL(18, 4)  NOT NULL
            CHECK (PrixUnitaire >= 0),
        QuotaMin        INT             NOT NULL DEFAULT 0
            CHECK (QuotaMin >= 0),
        QuotaMax        INT             NOT NULL
            CHECK (QuotaMax > 0),
        CONSTRAINT CK_Items_Quotas CHECK (QuotaMax >= QuotaMin),
        CONSTRAINT UQ_Items_Contrat_Produit UNIQUE (NoContrat, NoProduit)
    );
    CREATE INDEX IX_ItemsContrat_NoContrat ON dbo.ItemsContrat(NoContrat);
END
GO

-- ============================================================
-- TABLE : Transactions
-- Factures (positif) + Paiements partiels (négatif)
-- ============================================================
IF OBJECT_ID('dbo.Transactions', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.Transactions (
        NoTransaction   INT IDENTITY(1,1) PRIMARY KEY,
        NoContrat       INT             NOT NULL
            REFERENCES dbo.Contrats(NoContrat),
        TypeTransaction NVARCHAR(20)    NOT NULL
            CHECK (TypeTransaction IN ('Facture', 'Paiement')),
        Montant         DECIMAL(18, 2)  NOT NULL,  -- toujours positif
        Reference       NVARCHAR(100)   NULL,       -- No commande / No chèque
        NoProduit       NVARCHAR(50)    NULL,       -- Rempli pour une facture
        Quantite        INT             NULL,       -- Qté facturée
        DateTransaction DATETIME2       NOT NULL DEFAULT SYSUTCDATETIME(),
        NoUtilisateur   INT             NULL        -- Agent qui a saisi
            REFERENCES dbo.Utilisateurs(NoUtilisateur),
        Commentaire     NVARCHAR(500)   NULL
    );
    CREATE INDEX IX_Transactions_NoContrat       ON dbo.Transactions(NoContrat);
    CREATE INDEX IX_Transactions_DateTransaction ON dbo.Transactions(DateTransaction);
    CREATE INDEX IX_Transactions_Type            ON dbo.Transactions(TypeTransaction);
END
GO

-- ============================================================
-- TABLE : LogMessages  (trace persistante des messages MQ)
-- ============================================================
IF OBJECT_ID('dbo.LogMessages', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.LogMessages (
        NoLog           INT IDENTITY(1,1) PRIMARY KEY,
        DateReception   DATETIME2     NOT NULL DEFAULT SYSUTCDATETIME(),
        TypeMessage     NVARCHAR(100) NOT NULL,
        Payload         NVARCHAR(MAX) NULL,
        Resultat        NVARCHAR(50)  NULL,
        Erreur          NVARCHAR(MAX) NULL
    );
END
GO

PRINT '=== Schema CRM_AQL créé avec succès. ===';
GO
