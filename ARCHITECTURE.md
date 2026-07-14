# Architecture actuelle de ProxyAPI

## Vue d’ensemble

ProxyAPI est une solution ASP.NET Core orientée OAuth/OIDC. Son objectif est de permettre à un client externe d’initier une authentification, de récupérer un token et d’utiliser ce token pour proxyfier des requêtes vers un service upstream.

## Structure du code

Le projet est organisé autour de trois niveaux fonctionnels :

### 1. Domaine (`ProxyAPI.Domain`)

Ce niveau contient la logique métier et les abstractions utilisées par les autres couches.

Composants principaux :
- `ProxyAPIAuthenticationService` : orchestration du flow OAuth
- `SessionManager` : gestion des sessions OAuth temporaires
- `IProxyAPIAuthenticationService` et `ISessionManager` : contrats métier
- DTOs de domaine : `AuthorizationUrlResponse`, `AuthorizationCodeRequest`, `ClientContext`

Responsabilités :
- valider les étapes du flow OAuth,
- gérer les sessions de courte durée,
- préparer les contextes clients à partir des tokens récupérés.

### 2. Infrastructure (`ProxyAPI.Infrastructure`)

Ce niveau implémente les détails techniques nécessaires au fonctionnement du service.

Composants principaux :
- `OidcClient` : appels HTTP vers l’IDP pour l’authorization code flow, le refresh token et l’échange de code
- `MemoryCacheService<T>` : stockage des tokens et sessions en mémoire
- `OIdcAuthSettings` et `OAuthSettings` : configuration OIDC/OAuth
- extensions d’infrastructure : audit, logging, cache, service discovery, authz

Responsabilités :
- communiquer avec l’IDP,
- stocker les tokens avec un TTL,
- exposer des services techniques réutilisables.

### 3. Présentation (`ProxyAPI.Presentation`)

Cette couche expose le service via HTTP et compose les dépendances.

Composants principaux :
- `AuthController` : endpoints d’authentification
- `ProxyController` : proxy HTTP vers un service upstream
- `AuthenticationMiddleware` : logique transverse d’authentification
- `DependencyInjectionExtensions` : enregistrement des services et configuration du pipeline

Responsabilités :
- convertir les requêtes HTTP en opérations métier,
- gérer les cookies de session,
- transmettre les tokens aux appels upstream.

## Flux d’authentification

1. Le client appelle `GET /api/auth/login`
2. Le service crée un `state` et une session temporaire
3. L’URL d’autorisation est générée et renvoyée
4. L’utilisateur se connecte chez l’IDP
5. L’IDP redirige vers `GET /api/auth/callback`
6. Le service échange le code contre un token
7. Le token est stocké en cache et associé à un `clientId`
8. Le client reçoit un identifiant `X-ProxyAPI-ClientId` pour les appels suivants qu'il pourra mettre dans ses headers

## Flux de proxy

1. Le client appelle `GET|POST|PUT|DELETE|PATCH /api/proxy/`
2. Le contrôleur vérifie la présence du header `X-ProxyAPI-ClientId`
3. Le contexte client est récupéré ou rafraîchi si nécessaire
4. Le token d’accès est injecté dans la requête
5. La requête est envoyée vers l’URL fournie via le paramètre `uri`
6. La réponse est renvoyée au client

## Décisions architecturales

### Séparation des responsabilités
La logique métier ne dépend pas directement des détails HTTP ou OIDC. Les composants techniques sont encapsulés dans l’infrastructure.

### Stockage en mémoire
Les sessions et les tokens sont conservés en mémoire pour simplifier le fonctionnement local et éviter une dépendance à une base de données.

### Extension par modules
Les projets d’extension comme `ProxyAPI.Infrastructure.ExtAuditBdd`, `ProxyAPI.Infrastructure.ExtAuthz`, `ProxyAPI.Infrastructure.ExtLogging` et `ProxyAPI.Infrastructure.ExtSdCoac` permettent d’ajouter des fonctionnalités transverses sans modifier le cœur du service.

## Points d’attention

- La configuration OIDC est centralisée dans [ProxyAPI.Presentation/appsettings.json](ProxyAPI.Presentation/appsettings.json) et [ProxyAPI.Presentation/appsettings.Development.json](ProxyAPI.Presentation/appsettings.Development.json).
- Les cookies de session sont utilisés pour maintenir l’état côté client.
- Le proxy dépend fortement de la valeur du paramètre `uri` pour déterminer la destination upstream.

## Évolutions possibles

- remplacer le stockage en mémoire par Redis ou une base de données,
- ajouter des tests d’intégration autour des contrôleurs,
- sécuriser davantage le proxy avec rate limiting et validation stricte des URLs,
- externaliser la configuration de l’IDP et la politique de rôles.
