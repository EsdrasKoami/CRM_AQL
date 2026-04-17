# Planification des Tâches Restantes (Sprint Final vers l'Interface)

Le moteur `MessagingConsole` et la communication RabbitMQ étant sécurisés, voici la feuille de route et l'attribution des tâches pour la prochaine étape du projet : la création et la liaison de l'interface utilisateur (UI) finale.

### 1. Esdras (Développeur Principal)
**Mission : Développement de l'Interface Utilisateur (UI) et Intégration**
* **Création de la Vue :** Choix et configuration du Framework visuel (WPF, Blazor, ou ASP.NET Web App) pour concevoir l'interface graphique du CRM.
* **Binding et Intégration MQ :** Lier les événements visuels (boutons "Approuver") à l'envoi de messages RabbitMQ en arrière-plan, en remplaçant l'actuel menu CLI en console.
* **Affichage Dynamique :** Implémenter le rafraîchissement en temps réel de l'écran lorsque le système capte une entrée RabbitMQ envoyée par l'EDI ou l'ERP.

### 2. Alice (Base de Données - DAL)
**Mission : Finalisation des Données et Optimisation**
* **Seeding de la Base de Données :** Créer un script d'insertion de fausses données (Clients existants, historiques de Contrats) pour que l'interface affiche des éléments lors de la présentation.
* **Migrations Finales :** Mettre à jour le schéma du `CrmDbContext` si l'interface finale requiert de nouvelles colonnes (comme une date d'approbation d'un utilisateur).
* **Mise au point SQL :**  Optimiser les requêtes Entity Framework pour que la nouvelle interface graphique charge les listes (ex: Liste des transactions) rapidement.

### 3. Emile (Logique Métier - BLL)
**Mission : Interfaces de Communication Interne (API/Services)**
* **Connecteurs Frontend/Backend :** Mettre en place des Contrôleurs (ou ViewModels dynamiques) permettant à l'Interface de communiquer facilement avec le `ContratValidationService`.
* **Règles d'Affichage :** Gérer la logique qui détermine si "Approuvé" ou "Refusé" doit apparaître en vert ou rouge sur l'Interface en fonction des quotas.

### 4. Lilia (Architecture JSON et Validations)
**Mission : Robustesse Frontend et Dictionnaires**
* **Validation Côté Client :** S'assurer que les formulaires cliquables (ex: création manuelle d'un message) gèrent les erreurs si les champs fournis sont vides, avant l'envoi C#.
* **Normalisation des Typages :** Assurer la parfaite compatibilité des champs JSON générés par l'interface avec ce qu'attendent les autres équipes (EDI/ERP).

### 5. Nikita (Assurance Qualité & Tests Finaux)
**Mission : Validation de Bout-en-Bout (E2E)**
* **Tests Interface Utilisateur :** Cliquer et tester tous les chemins de l'application visuelle pour s'assurer d'aucun crash de "NullReference".
* **Simulation Globale :** Lancer le simulateur Prof / EDI en arrière-plan pendant un test réel sur l'Interface, et valider que tout atterrit précisément dans les bons onglets de l'écran.

### 6. Francis (Réseau, Déploiement et Ops)
**Mission : Préparation au Lancement**
* **Déploiement de l'Interface :** Préparer l'exécutable client final (.exe ou conteneur Web) et valider qu'il n'y a pas de problème de chemins d'accès.
* **Gestion des Securités et Ports :** S'assurer que la version graphique parvient toujours à passer à travers le pare-feu du serveur `172.16.88.38` et que la journalisation est redirigée dans un dossier accessible à tous.
