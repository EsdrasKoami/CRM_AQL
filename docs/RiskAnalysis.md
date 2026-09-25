# Matrice d'Analyse des Risques & Résilience - Module CRM Industriel

Ce document évalue les risques opérationnels, réseau et applicatifs encourus par le module d'orchestration CRM, ainsi que les mesures d'ingénierie mises en œuvre pour garantir la continuité d'activité de l'usine.

---

## 1. Matrice des Risques

| Réf | Risque Identifié | Impact Usine | Probabilité | Stratégie d'Atténuation & Résilience |
| :---: | :--- | :---: | :---: | :--- |
| **R-01** | **Indisponibilité du broker RabbitMQ** | Élevé (Interruption des flux) | Moyenne | Diagnostic préliminaire via sonde TCP active (`CheckRabbitMqAvailibility`). Arrêt propre et alerte opérateur sans état corrompu. |
| **R-02** | **Charge utile JSON malformée ou corrompue** | Moyen (Rejet de message) | Forte | Décodage défensif encapsulé dans `RMQEnveloppe.Deserialise` avec typage `Unknown/Error`, évitant tout crash d'application. |
| **R-03** | **Dépassement de plafond de crédit client** | Élevé (Risque financier & impayé) | Moyenne | Validation préalable stricte en mémoire (`CrmRepository`) bloquant l'ordre `Ceduler` et déclenchant l'émission d'un `ContratRefuse`. |
| **R-04** | **Bouclage infini d'événements AMQP** | Critique (Surcharge CPU/Réseau) | Faible | Filtrage contextuel basé sur les métadonnées `Sender` et `MessageName` interdisant l'auto-consommation récursive. |
| **R-05** | **Perte de traçabilité / Audit trail** | Moyen (Non-conformité ISO) | Faible | Double redondance d'observabilité : persistance locale structurée sous forme de fichiers horodatés `/logs` et télémétrie distante sur file `logs`. |

---

## 2. Procédures de Reprise après Incident (Disaster Recovery)

* **Garantie At-Least-Once Delivery :** Grâce à l'acquittement manuel explicite (`BasicAckAsync`), tout message en cours de traitement lors d'une coupure électrique ou d'un arrêt de processus reste préservé dans la file d'attente RabbitMQ et est automatiquement retraité au redémarrage.
* **Auto-adaptation aux configurations d'infrastructure :** Le mécanisme de subscription tente une liaison non-durable et bascule automatiquement en mode durable si les files du serveur distant le requièrent.
