# CRM Manufacturier JIT — Guide d'équipe

Ce projet est un système CRM complet pour une entreprise manufacturière travaillant en flux tendu (Just In Time). Il comprend un Backend API sécurisé, une logique métier isolée et une interface client WPF.

## 🏗️ Architecture du Projet

Le projet suit une architecture en couches pour garantir l'isolation et la testabilité :

- **CRM.Domain** : Contient les entités (Client, Contrat, etc.) et les interfaces.
- **CRM.Business** : **Cœur du système**. Contient toute la logique de validation, calculs de solde et de quotas.
- **CRM.DataAccess** : Gestion de la persistance (EF Core + Dapper) avec support **SQLite** (pour plus de portabilité).
- **CRM.Logging** : Système de traçabilité quotidien (Logs BE).
- **CRM.Messaging** : Gestionnaire de messages asynchrones (Background Service).
- **CRM.Api** : Point d'entrée HTTP (ASP.NET Core) gérant l'authentification JWT.
- **CRM.Frontend** : Application WPF déportée utilisant exclusivement l'API.

---

## ✅ Respect des Exigences de Remise

Voici comment le projet répond aux critères demandés par le professeur :

### 1. Logique Métier (DLL & Public)
La logique métier est regroupée dans le projet `CRM.Business`. C'est une **Class Library (.dll)** indépendante. Toutes les classes de services et interfaces nécessaires à l'intégration sont marquées comme `public`.

### 2. Intégration EDI (ContratValid)
L'exigence d'intégration avec l'EDI est respectée via `IContratValidationService`. 
- **Méthode** : `ContratValidAsync(noClient)`
- **Rôle** : Vérifie l'existence d'un contrat actif et retourne un objet de résultat structuré.

### 3. Isolation de la Dette (Découplage WPF)
La logique de calcul du solde (dette) est située dans `CRM.DataAccess` et consommée par `CRM.Business`. 
**Le projet WPF est totalement isolé** : il ne contient aucune logique de calcul et aucune chaîne de connexion SQL. Il consomme les données via l'API, ce qui permet au prof de tester les DLL métier sans avoir besoin de l'interface graphique.

### 4. Logger Quotidien (BEYYYYMMDD.log)
Le système génère des logs automatiques dans le dossier `src/CRM.Api/logs/`.
- **Format** : `BE{Date:yyyyMMdd}.log` (ex: `BE20260407.log`).
- **Niveaux** : Trace les succès et les erreurs critiques avec source et horodatage.

---

## 🚀 Comment Lancer le Projet

### 1. Prérequis
- Visual Studio 2022 avec .NET 8 SDK.
- Support pour WPF et ASP.NET Core.

### 2. Démarrage du Backend (Obligatoire en premier)
1. Ouvrez la solution `Projet-AQL.sln`.
2. Définissez `CRM.Api` comme projet de démarrage.
3. Lancez (F5). Une fenêtre Swagger s'ouvrira pour confirmer que l'API tourne sur `http://localhost:5000`.
   *(Note : La base de données SQLite `crm_local.db` se crée et s'initialise automatiquement au premier lancement).*

### 3. Démarrage du Frontend
1. Une fois l'API lancée, faites un clic droit sur `CRM.Frontend` > `Debug` > `Start New Instance`.
2. Connectez-vous avec les identifiants ci-dessous.

---

## 🔑 Identifiants de Test

| Login | Password | Rôle |
| :--- | :--- | :--- |
| `Directeur1` | `Password123` | Accès complet (Plafonds + Soldes) |
| `Agent1` | `Password123` | Accès limité (Navigation + Validation) |

---

## 🛠️ Notes de Développement
- **SQLite** : Nous avons migré de SQL LocalDB vers SQLite pour s'assurer que le projet fonctionne sur n'importe quel ordinateur sans configuration manuelle de SQL Server.
- **Mode Reveal** : L'écran de connexion dispose d'une icône 👁️ pour voir le mot de passe tapé (synchronisation dynamique PasswordBox/TextBox).