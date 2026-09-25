# Plan d'Assurance Qualité & Stratégie de Tests - Module CRM

Ce document définit la stratégie de vérification et de validation logicielle mise en place pour certifier la fiabilité du module CRM dans un contexte de production industrielle continue.

---

## 1. Tests Unitaires Automatisés (MSTest / xUnit)

La suite de tests automatisée est intégrée dans le projet `src/CRM.Tests` et s'exécute via la commande `dotnet test` :

| Identifiant | Cas de Test | Objectif de Validation | Statut |
| :---: | :--- | :--- | :---: |
| **TU-01** | `TestContratValide_ShouldReturnTrue_WhenDatesAndCreditAreOk` | Vérifie l'approbation d'une commande si les dates et le solde respectent les seuils. | ✅ Validé |
| **TU-02** | `TestContratInvalide_ShouldReturnFalse_WhenCreditExceeded` | Vérifie le blocage immédiat de la fabrication si le solde dépasse le plafond de crédit. | ✅ Validé |
| **TU-03** | `TestContratInvalide_ShouldReturnFalse_WhenDateExpired` | Vérifie le rejet systématique si la date d'échéance du contrat est dépassée. | ✅ Validé |
| **TU-04** | `TestRepository_ShouldReturnCorrectClient` | Valide l'isolation et la récupération exacte des données du client dans le Repository. | ✅ Validé |
| **TU-05** | `TestDeserialization_ShouldHandleInvalidJsonGracefully` | Garantit qu'un payload altéré ou malformé ne provoque aucun crash applicatif. | ✅ Validé |
| **TU-06** | `TestSoldeClient_ShouldCalculateCorrectBalance_AfterInvoiceAndPayment` | Valide le calcul exact du solde comptable après une série de facturations et paiements. | ✅ Validé |
| **TU-07** | `TestSerializationRoundTrip_ShouldPreserveEnvelopeProperties` | Confirme l'intégrité bidirectionnelle des enveloppes de données échangées via RabbitMQ. | ✅ Validé |

---

## 2. Tests d'Intégration & Protocole AMQP (RabbitMQ)

* **TI-01 - Connectivité Réseau Préalable :** Validation du handshake TCP sur le port `5672` avant l'instanciation des canaux de messagerie.
* **TI-02 - Observabilité par Mouchards :** Souscription aux files `edi` et `erp` pour confirmer en temps réel l'acheminement des ordres.
* **TI-03 - Piste d'Audit Distribuée :** Vérification de l'injection systématique des journaux d'événements sur la file centralisée `logs`.

---

## 3. Scénarios d'Acceptation & Simulation Métier de Plancher d'Usine

* **Scénario A (Cycle Nominal de Fabrication) :**
  Réception d'un ordre d'achat -> Validation de contrat conforme -> Déclenchement de la planification usine (`Ceduler`) -> Réception du `CertificatQualite` -> Facturation automatique transmise à l'EDI.
* **Scénario B (Gestion d'Anomalie Financière) :**
  Réception d'un ordre d'un client dont l'encours est saturé -> Interruption de la chaîne de fabrication -> Notification de refus `ContratRefuse`.
* **Scénario C (Simulation Autonome Hors Réseau) :**
  Mise en œuvre du menu de simulation interactif (Touche **S**) permettant aux équipes d'intégration de rejouer des scénarios industriels complets en circuit fermé.
