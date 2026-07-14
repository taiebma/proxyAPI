# ProxyAPI - Proxy OAuth/OIDC

ProxyAPI est une application ASP.NET Core qui permet d’authentifier un utilisateur via un flux OAuth 2.0/OIDC puis de proxyfier ses requêtes vers un service upstream avec un token d’accès injecté automatiquement.

## Vue d’ensemble

Le projet est actuellement organisé autour de trois grands blocs :

- Domain : logique métier et abstractions
- Infrastructure : implémentations de cache, OAuth/OIDC, configuration et audit
- Presentation : contrôleurs ASP.NET Core, middleware et composition des dépendances

## Composants principaux

### Domain
- `ProxyAPIAuthenticationService` : orchestration du flow OAuth et de la gestion des clients authentifiés
- `SessionManager` : stockage temporaire des sessions OAuth en mémoire
- Interfaces métier : `IProxyAPIAuthenticationService`, `ISessionManager`, `ITokenService`
- DTOs : `AuthorizationUrlResponse`, `AuthorizationCodeRequest`, `ClientContext`

### Infrastructure
- `OIdcClient` : échange de code contre token et refresh token
- `MemoryCacheService<T>` : implémentation générique de cache en mémoire
- `OIdcAuthSettings` et `OAuthSettings` : lecture de la configuration OIDC/OAuth
- modules d’extension : audit BDD, authz, logging, service discovery, cache

### Presentation
- `AuthController` : endpoints `login`, `callback`, `logout`, `status`
- `ProxyController` : proxy de requêtes HTTP vers une URL passée via le paramètre `uri`
- `AuthenticationMiddleware` : ajout d’un mécanisme d’authentification/initialisation des headers
- `Program.cs` : configuration du pipeline ASP.NET Core et injection de dépendances

## Fonctionnalités

### Authentification OAuth
- Flux Authorization Code
- Support de tout fournisseur OIDC compatible
- Gestion de l’état (`state`) et d’une session courte
- Identifiant de session `auth_session` et header client `X-ProxyAPI-ClientId`

### Proxy HTTP
- Réception de requêtes via `GET/POST/PUT/DELETE/PATCH`
- Paramètre obligatoire `uri` pour définir l’URL upstream
- Injection du token Bearer ou d’un header personnalisé
- Transmission des headers et du corps de la requête

### Audit et sécurité
- Intégration d’un mécanisme d’audit via les extensions infrastructure
- Authentification basée sur un header et des rôles configurés via `RoleProvider`
- Support du refresh token si l’IDP le fournit

## Prérequis

- .NET SDK compatible avec la solution
- Un fournisseur OIDC accessible (Keycloak, Azure AD, Auth0, etc.)
- Optionnellement Docker si vous souhaitez tester localement avec Keycloak

## Démarrage rapide

```bash
cd /Users/taiebma/dev/proxyAPI
dotnet restore
dotnet build
```

Puis lancer l’application :

```bash
cd ProxyAPI.Presentation
dotnet run
```

L’application démarre habituellement sur `http://localhost:5000` et peut aussi exposer `https://localhost:5001` selon la configuration de développement.

## Configuration

La configuration principale se trouve dans :

- [ProxyAPI.Presentation/appsettings.json](ProxyAPI.Presentation/appsettings.json)
- [ProxyAPI.Presentation/appsettings.Development.json](ProxyAPI.Presentation/appsettings.Development.json)

Les sections importantes sont :

- `Oidc` : endpoints d’autorisation et de token
- `OAuth` : configuration complémentaire pour les clients OAuth
- `Cache` : expiration des objets cache
- `RoleProvider` : rôles et mapping utilisateur

## Endpoints principaux

- `GET /api/auth/login` : démarre le login OAuth
- `GET /api/auth/callback` : réception du code d’autorisation
- `POST /api/auth/logout` : supprimer le contexte client
- `GET /api/auth/status` : vérifier si la session est toujours valide
- `GET|POST|PUT|DELETE|PATCH /api/proxy/` : proxy vers une URL upstream via le paramètre `uri`

## Exemple de flux

1. L’utilisateur appelle `GET /api/auth/login`
2. Le proxy génère un `state` et enregistre une session temporaire
3. L’URL d’autorisation est renvoyée à l’utilisateur
4. Après connexion, l’IDP redirige vers `GET /api/auth/callback`
5. Le proxy échange le code contre un token
6. Le token est stocké en cache et lié à un `clientId`
7. Le client peut ensuite appeler le proxy avec le header `X-ProxyAPI-ClientId`
8. Le proxy récupère le token et l’injecte dans la requête vers l’upstream

## Tests

Les tests sont organisés sous [ProxyAPI.Tests](ProxyAPI.Tests) et peuvent être exécutés avec :

```bash
dotnet test
```

## Notes de conception

Le projet s’appuie sur une architecture modulaire, avec une séparation claire entre :

- la logique métier pure,
- les adaptations techniques,
- l’exposition HTTP,
- et les extension points fournis par les modules infrastructure.

Cette approche facilite l’ajout de nouveaux fournisseurs OIDC, de nouveaux mécanismes de cache ou de nouveaux modules d’audit sans réécrire le cœur du service.

## Documentation complémentaire

- [ARCHITECTURE.md](ARCHITECTURE.md)
- [GETTING_STARTED.md](GETTING_STARTED.md)
- [SUMMARY.md](SUMMARY.md)
