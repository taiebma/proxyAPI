# ProxyAPI

ProxyAPI est une solution ASP.NET Core destinée à faciliter l’authentification OAuth/OIDC de requêtes API vers des services qui sont dans des zones protégées et non accessible en direct.
Il vérifie l'authentification et les authorisations de l'utilisateur gr^ace au Code flow OIDC qui est fait au préalable.

## Présentation

Cette application permet de :
- démarrer un flux d’authentification OAuth 2.0 / OIDC,
- gérer une session utilisateur temporaire,
- stocker et rafraîchir des jetons d’accès,
- proxyfier des requêtes HTTP vers une cible externe tout en conservant l’identité authentifiée.

## Architecture générale

Le projet repose sur une séparation claire entre trois couches principales :

- Domain : logique métier, services d’authentification et abstractions
- Infrastructure : implémentations techniques (OIDC, cache, audit, configuration)
- Presentation : contrôleurs HTTP, middleware et composition des dépendances

## Fonctionnalités principales

### Authentification OAuth/OIDC
- support du flux Authorization Code,
- intégration avec un fournisseur OIDC externe,
- gestion du paramètre `state` et des sessions temporaires,
- endpoints de login, callback, logout et vérification de statut.

### Proxy HTTP
- réception de requêtes via les méthodes GET, POST, PUT, DELETE et PATCH,
- paramètre obligatoire `uri` pour définir la cible upstream,
- injection automatique du jeton d’accès dans la requête sortante,
- transmission du corps et des headers du client initial.

### Audit et extensibilité
- mécanisme d’audit configurable,
- support de modules d’extension pour logging, authz, discovery et cache,
- architecture conçue pour évoluer vers des implémentations plus robustes en environnement de production.

## Prérequis

- SDK .NET compatible avec la solution,
- un fournisseur OIDC atteignable,
- éventuellement Docker si vous souhaitez tester avec Keycloak localement.

## Démarrage rapide

```bash
cd /Users/taiebma/dev/proxyAPI
dotnet restore
dotnet build
```

Lancer ensuite l’application :

```bash
cd ProxyAPI.Presentation
dotnet run
```

L’application peut être accessible via `http://localhost:5000` ou `https://localhost:5001` selon la configuration locale.

## Configuration

La configuration principale est définie dans :

- [ProxyAPI.Presentation/appsettings.json](ProxyAPI.Presentation/appsettings.json)
- [ProxyAPI.Presentation/appsettings.Development.json](ProxyAPI.Presentation/appsettings.Development.json)

Sections importantes :
- `Oidc` : endpoints et paramètres de connexion à l’IDP,
- `OAuth` : paramètres complémentaires pour les clients OAuth,
- `Cache` : configuration du cache mémoire,
- `RoleProvider` : rôles et mappage utilisateur.

## Endpoints principaux

- `GET /api/auth/login` : initie le flux OAuth
- `GET /api/auth/callback` : reçoit le code d’autorisation
- `POST /api/auth/logout` : termine la session client
- `GET /api/auth/status` : vérifie la validité de la session
- `GET|POST|PUT|DELETE|PATCH /api/proxy/` : proxy une requête vers une URL upstream

## Flux de fonctionnement

1. Le client démarre l’authentification via `GET /api/auth/login`.
2. Le service génère un `state` de sécurité et enregistre une session temporaire.
3. L’URL d’autorisation est renvoyée au client.
4. Après validation par l’IDP, le callback est traité et un jeton est échangé.
5. Le jeton est stocké et associé à un identifiant client.
6. Les requêtes suivantes passent par le proxy, qui injecte le jeton dans la requête sortante.

## Tests

La suite de tests est localisée dans [ProxyAPI.Tests](ProxyAPI.Tests) et peut être exécutée avec :

```bash
dotnet test
```

## Documentation associée

- [ARCHITECTURE.md](ARCHITECTURE.md)
- [GETTING_STARTED.md](GETTING_STARTED.md)
- [SUMMARY.md](SUMMARY.md)

## Notes de conception

L’architecture favorise la séparation des responsabilités, la testabilité et l’extensibilité. Elle permet d’ajouter de nouveaux fournisseurs OIDC, de nouveaux mécanismes de cache ou de nouveaux modules d’audit sans remettre en cause le cœur du système.
