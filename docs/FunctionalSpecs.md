# Spécifications Fonctionnelles - Module CRM Industriel

## 1. Contexte & Rôle du Système
Au sein d'un environnement manufacturier opérant selon les principes du **Just In Time (JIT)** et de l'**Industrie 4.0**, le module CRM agit comme le cœur d'orchestration commerciale et logistique. Il fait le pont entre les commandes transmises par les clients (via protocoles EDI) et les chaînes d'assemblage de l'usine (gérées par l'ERP et les systèmes MES).

---

## 2. Processus Métier & Contrôle de Solvabilité

### 2.1 Éligibilité des Commandes
Tout donneur d'ordre partenaire (ex: usine cliente, assembleur tiers) doit disposer d'un contrat commercial actif avant qu'une fabrication ne puisse débuter.

### 2.2 Règles de Gestion et Validation Automatisée
Pour chaque commande entrante (message type `ContratValid` ou standard EDI 850), le module évalue :
* **Période d'effet :** La date de la commande doit impérativement être comprise entre `DateDebut` et `DateFin`.
* **Gamme de produits :** La référence demandée doit figurer dans la liste des pièces homologuées (`ProduitsAutorises`).
* **Plafond d'encours financier :** Le montant de la commande ajouté au solde actuel du client ne doit pas excéder le crédit alloué (`MontantMaxCredit`).

### 2.3 Traitement des Exceptions & Rejets
Si l'une des conditions n'est pas remplie :
* L'ordre est immédiatement stoppé pour préserver les capacités machines et stocks de l'usine.
* Un événement de rejet (`ContratRefuse`) est notifié au partenaire via l'EDI avec le motif explicite (dépassement d'encours, contrat expiré).

---

## 3. Orchestration de la Saga Industrielle (Workflow JIT)

Le CRM orchestre le cycle de vie complet de la commande :
1. **Ingestion :** Réception de l'ordre d'achat via la file `crm`.
2. **Arbitrage :** Interrogation synchrone du référentiel métier (`CrmRepository`).
3. **Ordonnancement Usine :** Envoi d'un ordre de planification de fabrication (`Ceduler`) vers la file `erp`.
4. **Validation Qualité :** Réception de la notification d'achèvement et du certificat de conformité de fabrication (`CertificatQualite` / EDI 855) en provenance du MES d'atelier.
5. **Dénouement Commercial :** Transmission de la facture certifiée au client via la file `edi`.

---

## 4. Comptabilité Auxiliaire & Règlement

* **Enregistrement des Débits :** Chaque émission de facture incrémente le compte client.
* **Enregistrement des Crédits :** Tout acquittement de paiement entrant (`Paiement`) est réconcilié en temps réel avec l'historique des transactions.
* **Calcul dynamique d'encours :** Mise à jour instantanée du solde pour régir les autorisations des commandes subséquentes.
