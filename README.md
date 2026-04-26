# GUIDE DE RÉUSSITE - PRÉSENTATION CRM AQL

Ce document est votre script pas-à-pas pour l'évaluation. Suivez chaque étape pour une démonstration sans faille.

---

## ETAPE 0 : PRÉPARATION ET LANCEMENT
1. Assurez-vous d'être connecté au réseau (VPN si nécessaire pour atteindre RabbitMQ).
2. Lancez l'application en double-cliquant sur le fichier **`run.bat`** (à la racine du dossier).
3. (Alternative) Tapez : `dotnet run --project src/CRM.MessagingConsole/CRM.MessagingConsole.csproj`
4. Vérifiez que la console affiche : `[OK] Connexion réussie à 172.16.88.227 !`

---

## ETAPE 1 : RÉCEPTION DE LA COMMANDE (SCÉNARIO STANDARD)
*   **Le prof envoie la demande :** Un message de type "ContratValid" (ou code EDI 850) arrive.
*   **Observation :** Votre écran affiche en bleu `[RÉCEPTION CRM] BINGO ! Le CRM vient de recevoir...`.
*   **Intelligence :** La console analyse le message et vous affiche un conseil en vert : `[CONSEIL] Vous pouvez lancer l'Usine. Tapez la touche '1'`.

---

## ETAPE 2 : ACTION DU CRM (LANCEMENT DE LA PRODUCTION)
*   **Action :** Appuyez sur la touche **1** puis Entrée.
*   **Observation :** Le CRM envoie l'ordre "Ceduler" à l'ERP (l'Usine).
*   **Mouchard :** Le texte magenta `[MOUCHARD RÉSEAU]` apparaît pour confirmer que l'ERP a bien reçu l'ordre.

---

## ETAPE 3 : RETOUR DE L'USINE (QUALITÉ)
*   **L'ERP envoie le certificat :** Un message "CertificatQualite" arrive de l'Usine.
*   **Observation :** Votre écran affiche à nouveau `[RÉCEPTION CRM]`.
*   **Intelligence :** La console vous conseille : `[CONSEIL] Tapez '2' pour renvoyer le certificat, ou '3' pour envoyer la facture`.

---

## ETAPE 4 : FINALISATION (ENVOI À L'EDI)
*   **Action :** Appuyez sur **2** pour le certificat, puis sur **3** pour la facture.
*   **Validation :** Vous venez de prouver que le CRM gère le flux documentaire complet vers le client.

---

## ETAPE 5 : GESTION DES PIÈGES (PIÈCES MANQUANTES OU MAUVAIS CONTRAT)
Si le prof essaie de vous sortir du scénario :
*   **Contrat Invalide :** Si le message reçu contient "invalide", la console affichera une alerte rouge. Utilisez l'option **4** pour répondre manuellement un "ContratRefuse".
*   **Option Experts (Touche 4) :** Vous permet d'écrire n'importe quel nom de message vers n'importe quelle file (edi, erp, crm).
*   **Simulateur (Touche S) :** Si le prof ne veut pas utiliser son propre RabbitMQ, utilisez 'S' pour déclencher vous-même les arrivées de messages.

---

## COMPRENDRE LE CODE (LE CÔTÉ TECHNIQUE)
Si le prof pointe le fichier `Program.cs` et vous demande comment ça marche :

### 1. La Connexion (InitializeRabbitMqAsync)
Nous créons une **Factory** avec l'adresse IP et les identifiants. Elle nous donne une `Connection` (le tuyau principal) et un `Channel` (le canal pour parler).

### 2. L'Écoute (StartConsumersAsync)
Le CRM écoute 3 files en même temps : `crm` (pour recevoir), `edi` et `erp` (pour vérifier que nos messages sont bien arrivés). Nous utilisons un **Consumer** qui tourne en tâche de fond (en asynchrone).

### 3. La Réception (ProcessIncomingMessageAsync)
C'est ici que bat le cœur du CRM. Quand un message arrive :
*   Il est **Désérialisé** (le JSON est transformé en objet C# grâce à la classe `RMQEnveloppe`).
*   Le CRM regarde le nom du message. 
*   **Intelligence d'analyse :** Il cherche des mots clés comme "Contrat" ou "Certificat" pour vous suggérer la meilleure réponse à donner.

### 4. L'Envoi (PublishMessageAsync)
Quand vous appuyez sur une touche, le CRM prépare une nouvelle `RMQEnveloppe`, la transforme en JSON (Sérialisation), et la propulse sur le réseau vers la file de destination.

### 5. Robustesse (EnsureQueueAndSubscribeAsync)
Le code est "intelligent" : si une file sur le serveur est configurée en mode Durable (qui survit au redémarrage) ou Non-Durable, le code s'adapte tout seul pour ne jamais planter.
