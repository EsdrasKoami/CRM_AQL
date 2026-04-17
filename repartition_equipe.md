# Répartition des Tâches - Projet CRM AQL

L'équipe a collaboré de manière Agile sur l'architecture et l'implémentation du système `CRM.MessagingConsole` et de ses dépendances. Voici la répartition des contributions de chaque membre :

### 1. Alice - Base de Données et Accès aux Données (DAL)
* **Conception de la BD** : Modélisation des Entités du domaine (Client, Contrat, Transaction, ItemContrat, etc.).
* **Implémentation d'Entity Framework** : Configuration du `CrmDbContext` et gestion de la chaîne de connexion.
* **Repositories** : Programmation des interfaces et classes d'accès aux données (`ClientRepository`, `ItemContratRepository`, `TransactionRepository`).

### 2. Esdras - Développeur Principal & Intégration RabbitMQ
* **Console Interactive CLI** : Conception de l'interface en ligne de commande (menu interactif avec gestion des envois en temps réel sans blocage de thread).
* **Consommateur Asynchrone** : Programmation du moteur principal de réception RabbitMQ (`AsyncEventingBasicConsumer` sur les files `crm`, `edi`, `erp`, etc.).
* **Publication MQ** : Gestion de l'émission des messages JSON vers les différentes instances tierces via la méthode robuste `SendAsync`.

### 3. Emile - Logique d'Affaire et Validations (BLL)
* **Couche de Services** : Implémentation du cœur du métier, notamment le pipeline de validation des contrats (`ContratValidationService`) et le service de facturation (`FacturationService`).
* **Règles d'approbation** : Création de la logique de vérification structurelle (JitQuotaService) avant de déclencher un message de `ReponseCommande`.
* **Routage logistique** : Intégration de la prise de décisions du système basée sur le contenu des messages entrants.

### 4. Lilia - Structuration des Messages et Tolérance aux Pannes
* **Gestion du Format de l'Enveloppe** : Intégration et manipulation du standard de payload C# (`RMQEnveloppe` et mapping vers JSON).
* **Sérialisation/Désérialisation Sécurisée** : Protection du pipeline de réception avec des englobages `try-catch` pour rejeter proprement les formats invalides sans crasher l'application.
* **Résilience des Files (Durable/Non-Durable)** : Écriture de la méthode `AssureQueueAndSubscribeAsync` pour que le système s'adapte automatiquement et ne crash pas si le serveur s'attend à une file avec une signature différente.

### 5. Nikita - Assurance Qualité et Tests d'Intégration
* **Simulation et Débogage** : Configuration des cas de tests RabbitMQ en local pour envoyer de "faux" messages et confirmer le circuit de communication (ping-pong local).
* **Affichage des Traces en Direct** : Implémentation de la capture de décodage (`[MQ MSG BRUT REÇU]` en jaune) permettant au programmeur d'auditer le contenu avant même le traitement C#.
* **Ergonomie et Automatisation** : Création des étapes du script batch orienté utilisateur (`run.bat`) avec affichage des couleurs et diagnostique.

### 6. Francis - Infrastructures, Logs et Réseau
* **Diagnostic Réseau (TCP/IP)** : Programmation du vérificateur de connexion automatique sur le port 5672 (host `172.16.88.38`) au démarrage du service, pour ne pas attendre le time-out RabbitMQ.
* **Système de Journalisation (Logging)** : Conception du mécanisme autonome d'écriture sur disque (`logs/BExxxx.log`) afin de conserver l'historique complet des transits des messages.
* **Stabilité et Aquittement AQ** : Sécurisation du cycle de vie des paquets via les acquittements dynamiques (`BasicAck`) et refus manuels (`BasicNack` / `requeue: false`) pour empêcher les messages empoisonnés de boucler.

---
**Note de soumission** : Bien que chacun ait eu une responsabilité d'expert (Database, Reseau, MQ, Logique), la construction du Mastermind global a été faite en coordination constante avec notre développeur principal Esdras pour centraliser toutes les composantes dans la `CRM.MessagingConsole`.
