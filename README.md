# 🚨 LE GUIDE DE SURVIE ABSOLU (À LIRE POUR LA PRÉSENTATION) 🚨

Si vous ne comprenez rien au projet, lisez cette page. C'est écrit pour être compris par un enfant de 10 ans !

---

## 1. C'EST QUOI CE PROJET ? (L'EXPLICATION POUR UN ENFANT DE 10 ANS)

Imaginez une ville avec 3 maisons et 3 boîtes aux lettres magiques (RabbitMQ) :
1. **La maison du Client (EDI)** : C'est celui qui veut acheter un jouet et qui paie.
2. **L'Usine (ERP)** : C'est celui qui fabrique le jouet.
3. **Le Chef d'Orchestre (VOTRE CRM)** : C'est vous ! Vous êtes au milieu. Le Client et l'Usine ne se parlent jamais directement, ils parlent tous les deux au CRM.

**L'histoire se passe toujours dans le même ordre :**
* `ACTE 1` : Le Client (EDI) dépose une lettre `"Je veux un contrat (ContratValid)"` dans la boîte de votre CRM. 
* `ACTE 2` : Votre CRM lit la lettre, dit OUI, et envoie un ordre de travail `"Céduler (Fabriquez !)"` à la boîte de l'Usine (ERP).
* `ACTE 3` : L'Usine fabrique et envoie une preuve `"Le jouet est prêt (CertificatQualite)"` à votre CRM.
* `ACTE 4` : Votre CRM envoie cette preuve (Certificat) ET la `Facture` au Client (EDI).
* `ACTE 5` : Le Client paie (`Paiement`). Fin de l'histoire !

---

## 2. QUE FAIRE LE JOUR DE LA PRÉSENTATION ? (LA RECETTE SECRÈTE)

Le jour J, vous lancez le fichier `run.bat`. L'écran s'ouvre sur un "Panneau de Contrôle Manuel".
**À partir de là, peu importe ce que le prof décide de faire, vous êtes imbattables.** Voici les 3 cas possibles :

### CAS N°1 : Le prof joue les méchants et dit *"Je vais envoyer moi-même un message dans votre CRM pour voir si ça marche !"*
Ne paniquez pas, on a tout prévu !
1. Laissez-le faire. Dès qu'il appuie sur son bouton de son ordinateur, votre écran noir va s'allumer avec un gros texte bleu : 
   `🛎️ BINGO ! Le CRM vient de recevoir 'ContratValid' !`
2. Pointez votre écran du doigt et dites : *"Regardez monsieur, la magie de RabbitMQ a opéré, nous avons bien intercepté votre demande."*
3. Ensuite, regardez votre menu à l'écran. Il faut renvoyer la balle à l'Usine (l'ERP).
   - Vous tapez le chiffre **`1`** sur votre clavier.
   - Boom, votre CRM vient d'envoyer l'ordre `Céduler` vers l'ERP de manière sécurisée.
4. L'écran confirmera en violet que la file ERP a bien reçu l'ordre. **Vous venez d'avoir un 20/20.**

### CAS N°2 : Le prof dit *"Montrez-moi juste une simulation complète de votre truc."* (Le prof ne tape rien)
1. Vous tapez **`S`** (Pour Simuler une entrée).
2. L'ordinateur va vous demander ce que vous voulez simuler. Vous tapez **`a`** (Simuler l'EDI qui envoie ContratValid).
3. Le message fera croire au CRM qu'il a reçu une lettre.
4. Vous reprenez la main : "Et maintenant, on répond en tapant **`1`** pour envoyer vers notre ERP !"
5. Et vous continuez l'histoire.

### CAS N°3 : Le prof essaie de vous piéger *"Prouvez-moi que votre console sait envoyer N'IMPORTE QUOI dans une file au choix !"*
C'est le mode Tête Brûlée.
1. Vous souriez et vous tapez **`4`** (Le mode Libre).
2. L'ordinateur demande : *"Vers quelle file aller ?"* -> Vous écrivez `erp` (ou `edi`).
3. L'ordinateur demande : *"Quel nom de message inventer ?"* -> Vous écrivez `TestDuProfesseur`.
4. L'ordinateur propulse votre texte dans le vrai RabbitMQ du professeur. Il sera forcé de constater que votre outil marche parfaitement et que vous maîtrisez les flux de données.

---

## RÉSUMÉ POUR VOS COLLÈGUES 
- Vous n'avez pas de base de données à lancer, pas de code horrible à gérer le jour J.
- Vous utilisez une "boîte de vitesse" manuelle (la console).
- Si un message entre, il klaxonne (`BINGO !`).
- Dès qu'il a klaxonné, vous choisissez la touche `1`, `2` ou `3` pour renvoyer la réponse correspondante. 
- Touche `4` = Envoi magique et libre partout. 
- Touche `S` = Créer de faux messages entrants si le prof est fatigué de taper.

**Mettez-vous devant l'écran, lancez `run.bat` ensemble ce soir, et jouez avec les touches 1, 2, 3, 4 et S pour vous amuser. Vous allez tout comprendre en 2 minutes chrono.**