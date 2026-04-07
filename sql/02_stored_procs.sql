-- ============================================================
-- Projet AQL 420-454-RI — Procédures Stockées
-- ============================================================

USE CRM_AQL;
GO

-- ============================================================
-- SP 1 : usp_GetSoldeClient
-- Calcule le solde temps réel d'un client :
--   Somme des Factures  - Somme des Paiements  (sur contrats actifs)
-- Retourne aussi les détails par contrat.
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.usp_GetSoldeClient
    @NoClient   CHAR(7),
    @AsOfDate   DATE = NULL         -- NULL = aujourd'hui
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    DECLARE @RefDate DATE = ISNULL(@AsOfDate, CAST(SYSUTCDATETIME() AS DATE));

    -- Validation du client
    IF NOT EXISTS (SELECT 1 FROM dbo.Clients WHERE NoClient = @NoClient AND EstActif = 1)
    BEGIN
        RAISERROR('Client %s introuvable ou inactif.', 16, 1, @NoClient);
        RETURN;
    END

    -- Résultat global (1 ligne) + détail par contrat (N lignes)
    SELECT
        c.NoClient,
        cl.NomEntreprise,
        c.NoContrat,
        c.DateDebut,
        c.DateFin,
        c.MontantMax,
        ISNULL(SUM(CASE WHEN t.TypeTransaction = 'Facture'  THEN t.Montant ELSE 0 END), 0) AS TotalFactures,
        ISNULL(SUM(CASE WHEN t.TypeTransaction = 'Paiement' THEN t.Montant ELSE 0 END), 0) AS TotalPaiements,
        ISNULL(SUM(CASE WHEN t.TypeTransaction = 'Facture'  THEN t.Montant ELSE 0 END), 0)
        - ISNULL(SUM(CASE WHEN t.TypeTransaction = 'Paiement' THEN t.Montant ELSE 0 END), 0) AS SoldeContrat,
        c.MontantMax
        - (
            ISNULL(SUM(CASE WHEN t.TypeTransaction = 'Facture'  THEN t.Montant ELSE 0 END), 0)
          - ISNULL(SUM(CASE WHEN t.TypeTransaction = 'Paiement' THEN t.Montant ELSE 0 END), 0)
         ) AS CreditDisponible,
        CASE
            WHEN c.DateDebut <= @RefDate AND c.DateFin >= @RefDate AND c.EstActif = 1
            THEN 1 ELSE 0
        END AS ContratActif
    FROM dbo.Contrats c
    INNER JOIN dbo.Clients cl ON cl.NoClient = c.NoClient
    LEFT  JOIN dbo.Transactions t ON t.NoContrat = c.NoContrat
    WHERE c.NoClient = @NoClient
    GROUP BY
        c.NoClient, cl.NomEntreprise, c.NoContrat,
        c.DateDebut, c.DateFin, c.MontantMax, c.EstActif;
END
GO

-- ============================================================
-- SP 2 : usp_ConsommationMensuelleJIT
-- Retourne la consommation mensuelle par produit pour un contrat,
-- comparée aux quotas min/max.
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.usp_ConsommationMensuelleJIT
    @NoContrat  INT,
    @Annee      INT,
    @Mois       INT
AS
BEGIN
    SET NOCOUNT ON;

    SELECT
        ic.NoProduit,
        ic.DescriptionProduit,
        ic.QuotaMin,
        ic.QuotaMax,
        ISNULL(SUM(t.Quantite), 0)                          AS QuantiteCommandee,
        ic.QuotaMin - ISNULL(SUM(t.Quantite), 0)           AS EcartMin,   -- négatif = OK
        ic.QuotaMax - ISNULL(SUM(t.Quantite), 0)           AS EcartMax,   -- positif = OK
        CASE
            WHEN ISNULL(SUM(t.Quantite), 0) > ic.QuotaMax THEN 'DEPASSEMENT'
            WHEN ISNULL(SUM(t.Quantite), 0) < ic.QuotaMin THEN 'SOUS_QUOTA'
            ELSE 'OK'
        END AS StatutQuota
    FROM dbo.ItemsContrat ic
    LEFT JOIN dbo.Transactions t
        ON  t.NoContrat       = ic.NoContrat
        AND t.NoProduit       = ic.NoProduit
        AND t.TypeTransaction = 'Facture'
        AND YEAR(t.DateTransaction)  = @Annee
        AND MONTH(t.DateTransaction) = @Mois
    WHERE ic.NoContrat = @NoContrat
    GROUP BY
        ic.NoProduit, ic.DescriptionProduit,
        ic.QuotaMin, ic.QuotaMax;
END
GO

-- ============================================================
-- SP 3 : usp_CreerFacture
-- Crée une transaction de type Facture de manière atomique.
-- Vérifie le plafond de crédit AVANT insertion.
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.usp_CreerFacture
    @NoContrat      INT,
    @NoProduit      NVARCHAR(50),
    @Quantite       INT,
    @Reference      NVARCHAR(100) = NULL,
    @NoUtilisateur  INT           = NULL,
    @Commentaire    NVARCHAR(500) = NULL,
    @NoTransaction  INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRY
        BEGIN TRANSACTION;

        -- 1. Récupérer le prix unitaire + MontantMax
        DECLARE @PrixUnitaire DECIMAL(18,4);
        DECLARE @MontantMax   DECIMAL(18,2);
        DECLARE @NoClient     CHAR(7);

        SELECT
            @PrixUnitaire = ic.PrixUnitaire,
            @MontantMax   = co.MontantMax,
            @NoClient     = co.NoClient
        FROM dbo.ItemsContrat ic
        INNER JOIN dbo.Contrats co ON co.NoContrat = ic.NoContrat
        WHERE ic.NoContrat = @NoContrat AND ic.NoProduit = @NoProduit;

        IF @PrixUnitaire IS NULL
        BEGIN
            ROLLBACK;
            RAISERROR('Produit %s introuvable dans le contrat %d.', 16, 1, @NoProduit, @NoContrat);
            RETURN;
        END

        DECLARE @MontantFacture DECIMAL(18,2) = @Quantite * @PrixUnitaire;

        -- 2. Calculer le solde actuel (avec verrou)
        DECLARE @SoldeActuel DECIMAL(18,2);
        SELECT @SoldeActuel =
            ISNULL(SUM(CASE WHEN TypeTransaction = 'Facture'  THEN Montant ELSE 0 END), 0)
          - ISNULL(SUM(CASE WHEN TypeTransaction = 'Paiement' THEN Montant ELSE 0 END), 0)
        FROM dbo.Transactions WITH (UPDLOCK, ROWLOCK)
        WHERE NoContrat = @NoContrat;

        -- 3. Contrôle de crédit
        IF (@SoldeActuel + @MontantFacture) > @MontantMax
        BEGIN
            ROLLBACK;
            RAISERROR(
                'CREDIT_DEPASSE: Solde actuel %.2f + Facture %.2f > Plafond %.2f pour le contrat %d.',
                16, 1,
                @SoldeActuel, @MontantFacture, @MontantMax, @NoContrat
            );
            RETURN;
        END

        -- 4. Insérer la facture
        INSERT INTO dbo.Transactions
            (NoContrat, TypeTransaction, Montant, Reference, NoProduit, Quantite, NoUtilisateur, Commentaire)
        VALUES
            (@NoContrat, 'Facture', @MontantFacture, @Reference, @NoProduit, @Quantite, @NoUtilisateur, @Commentaire);

        SET @NoTransaction = SCOPE_IDENTITY();

        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH;
END
GO

-- ============================================================
-- SP 4 : usp_EnregistrerPaiement
-- Enregistre un paiement partiel ou total.
-- ============================================================
CREATE OR ALTER PROCEDURE dbo.usp_EnregistrerPaiement
    @NoContrat      INT,
    @Montant        DECIMAL(18,2),
    @Reference      NVARCHAR(100) = NULL,
    @NoUtilisateur  INT           = NULL,
    @Commentaire    NVARCHAR(500) = NULL,
    @NoTransaction  INT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    IF @Montant <= 0
    BEGIN
        RAISERROR('Le montant du paiement doit être positif.', 16, 1);
        RETURN;
    END

    BEGIN TRY
        BEGIN TRANSACTION;

        INSERT INTO dbo.Transactions
            (NoContrat, TypeTransaction, Montant, Reference, NoUtilisateur, Commentaire)
        VALUES
            (@NoContrat, 'Paiement', @Montant, @Reference, @NoUtilisateur, @Commentaire);

        SET @NoTransaction = SCOPE_IDENTITY();

        COMMIT;
    END TRY
    BEGIN CATCH
        IF @@TRANCOUNT > 0 ROLLBACK;
        THROW;
    END CATCH;
END
GO

-- ============================================================
-- Données de test initiales
-- ============================================================
-- Utilisateurs (mots de passe : hasher via BCrypt dans l'app)
INSERT INTO dbo.Utilisateurs (Login, PasswordHash, Nom, Prenom, Role)
VALUES
    ('agent.demo',    'HASH_A_REMPLACER', 'Demo',  'Agent',    'Agent'),
    ('finances.demo', 'HASH_A_REMPLACER', 'Demo',  'Finances', 'DirecteurFinances');

-- Client de test
INSERT INTO dbo.Clients (NoClient, NomEntreprise, Courriel)
VALUES ('C000001', 'Industries ABC inc.', 'contact@abc.com');

-- Contrat actif
INSERT INTO dbo.Contrats (NoClient, DateDebut, DateFin, MontantMax, Description)
VALUES ('C000001', '2026-01-01', '2026-12-31', 50000.00, 'Contrat annuel 2026');

-- Item de contrat (produit P001)
INSERT INTO dbo.ItemsContrat (NoContrat, NoProduit, DescriptionProduit, PrixUnitaire, QuotaMin, QuotaMax)
VALUES (1, 'P001', 'Pièce mécanique XB-42', 12.50, 100, 500);

PRINT '=== Procédures stockées et données de test créées. ===';
GO
