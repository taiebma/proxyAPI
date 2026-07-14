# Résumé de la solution ProxyAPI

## État actuel

ProxyAPI est une solution ASP.NET Core orientée OAuth/OIDC, avec un cœur de service dédié à l’authentification et au proxy d’appels vers un upstream.

## Composants clés

- ProxyAPI.Domain : services d’authentification, gestion des sessions et contrats métier
- ProxyAPI.Infrastructure : clients OIDC, cache mémoire, configuration et extensions transverses
- ProxyAPI.Presentation : contrôleurs HTTP et middleware d’authentification
- ProxyAPI.Tests : tests autour du domaine et des composants infrastructure

## Fonctionnalités livrées

- flow OAuth Authorization Code
- endpoints de login, callback, logout et status
- proxy HTTP vers une URL fournie via le paramètre uri
- injection de token d’accès dans les requêtes upstream
- support de modules additionnels pour l’audit, l’authz, le logging et la découverte de services

## Points de vigilance

- la configuration OIDC doit être ajustée selon l’environnement cible,
- le stockage des sessions et tokens est actuellement en mémoire,
- la sécurité du proxy doit être renforcée si l’usage en production devient important.

## Prochaines améliorations possibles

1. migrer le stockage vers Redis ou une base de données,
2. ajouter davantage de tests d’intégration,
3. renforcer la validation des URLs proxy,
4. ajouter de la télémétrie et du monitoring.

Pour plus de détails, voir README.md, ARCHITECTURE.md et GETTING_STARTED.md.
