# Spécifications Techniques - Module CRM Industriel

## 1. Stack Technologique & Architecture Logicielle
* **Plateforme :** .NET 8.0 (C# 12.0)
* **Modèle d'Architecture :** Architecture orientée événements (EDA - Event-Driven Architecture) asynchrone, découplage micro-services via AMQP.
* **Patron d'Orchestration :** Saga distribuée avec orchestrateur centralisé.
* **Contrat d'échange de données :** JSON structuré encapsulé dans le modèle `RMQEnveloppe`.

---

## 2. Topologie de Messagerie RabbitMQ

Le module s'appuie sur un bus de messages RabbitMQ pour assurer la communication asynchrone et résiliente :

| File d'Attente | Rôle & Direction | Type d'Événements Traités |
| :--- | :--- | :--- |
| `crm` | **Entrante** (Souscription active) | Ordres d'achat (`850`), approbations de contrat, certificats qualité (`855`), accusés de paiement. |
| `erp` | **Sortante** + Mouchard d'écoute | Ordres d'ordonnancement et lancement de fabrication (`Ceduler`). |
| `edi` | **Sortante** + Mouchard d'écoute | Rejets de contrat (`ContratRefuse`), certificats qualité transmis au client, factures commerciales. |
| `logs` | **Sortante (Télémétrie)** | Piste d'audit distribuée (Audit Trail), horodatage des réceptions et transitions d'état. |

---

## 3. Conception des Composants Applicatifs

### 3.1 `Program.cs` - Moteur d'Événements & Supervision
* Initialise les canaux de consommation et d'émission via la bibliothèque officielle `RabbitMQ.Client`.
* Gère les cycles de vie des connexions AMQP, avec vérification TCP préalable pour diagnostiquer immédiatement toute indisponibilité réseau.
* Démarre des consommateurs d'événements asynchrones (`AsyncEventingBasicConsumer`).

### 3.2 `CrmRepository.cs` - Couche Métier & Persistance en Mémoire
* Implémente le pattern Repository pour encapsuler les accès aux entités `Contrat` et `Transaction`.
* Exécute des requêtes déclaratives (LINQ) pour calculer la validité temporelle, vérifier les limites de crédit et consolider le solde net comptable.

### 3.3 `RMQEnveloppe.cs` - Enveloppe Standardisée de Données
* Garantit l'interopérabilité et la sérialisation bidirectionnelle UTF-8.
* Inclut une méthode `Deserialise(byte[] rawBody)` sécurisée dotée d'un fallback `Unknown/Error` protégeant l'application contre les charges utiles malformées.

---

## 4. Stratégie de Résilience & Fiabilité Industrielle

* **Acquittements Manuels (`BasicAckAsync`) :** Aucun message n'est retiré de la file tant que son traitement d'affaires n'est pas finalisé avec succès.
* **Gestion d'Erreurs Non Bloquante (`BasicNackAsync`) :** En cas d'anomalie critique de désérialisation, le message est rejeté de manière contrôlée sans bloquer le pipeline de traitement.
* **Résilience de Déclaration de File :** Fallback dynamique sur le mode durable / non-durable afin de s'adapter sans interruption à l'état préexistant des files sur le courtier RabbitMQ.
