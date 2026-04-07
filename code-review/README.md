# Code Review — Projet AQL 420-454-RI

Ce dossier contient les preuves de revue de code pour chaque composant du backend CRM.

## Convention de nommage des fichiers de revue

```
CR_<composant>_<YYYYMMDD>.md
```

## Template de revue

```markdown
# Revue de code : [Composant]
Date       : YYYY-MM-DD
Réviseur   : [Nom Prénom]
Auteur     : [Nom Prénom]
Branche    : feature/xxx

## Éléments révisés
- [ ] Logique métier correcte
- [ ] Gestion des erreurs et cas limites
- [ ] Tests unitaires présents et pertinents
- [ ] Logging des actions critiques
- [ ] Respect de la séparation des couches (pas d'accès BD direct dans Business)
- [ ] Sécurité : droits vérifiés (Agent / DirecteurFinances)
- [ ] Pas de secret dans le code (connexion strings, JWT key)

## Commentaires
[Vos commentaires ici]

## Décision
- [ ] Approuvé
- [ ] Approuvé avec réserves
- [ ] Rejeté (raisons : ...)
```

## Revues effectuées

| Date | Composant | Réviseur | Décision |
|------|-----------|----------|----------|
| (à remplir) | ContratValidationService | — | — |
| (à remplir) | CreditControlService | — | — |
| (à remplir) | JitQuotaService | — | — |
| (à remplir) | MqBackgroundService | — | — |
| (à remplir) | SQL Stored Procedures | — | — |
