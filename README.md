# ProxyAPI - OAuth Authentication Proxy for Insomnia

Un proxy .NET 9 qui authentifie les utilisateurs Insomnia via OAuth 2.0 (Authorization Code Flow) auprès d'un IDP OIDC générique.

## Architecture DDD

Le projet respecte les principes du Domain-Driven Design avec une séparation claire des responsabilités :

```
- ProxyAPI.Domain        : Entités, value objects, interfaces métier
- ProxyAPI.Application   : Services d'orchestration et DTOs
- ProxyAPI.Infrastructure: Implémentations (cache, clients OAuth)
- ProxyAPI.Presentation  : Controllers, middleware, dépendances ASP.NET
- ProxyAPI.Tests         : Tests unitaires avec xUnit et Moq
```

## Fonctionnalités

### 1. Authentification OAuth 2.0
- **Flow**: Authorization Code Flow
- **Support**: Tout IDP OIDC (Keycloak, Azure AD, etc.)
- **Endpoints**:
  - `GET /api/auth/login` - Initie le flow OAuth
  - `POST /api/auth/callback` - Reçoit le code d'autorisation
  - `POST /api/auth/logout` - Termina la session
  - `GET /api/auth/status` - Vérifie l'état d'authentification

### 2. Gestion du Cache
- **IMemoryCache** : Stockage thread-safe des tokens
- **TTL Automatique** : Éviction des tokens expirés
- **Refresh Token** : Renouvellement automatique des tokens
- **Abstraction ITokenCache** : Permet l'implémentation Redis/autre cache

### 3. Middleware d'Authentification
- Valide les cookies de session
- Injecte automatiquement les tokens Bearer dans les headers
- Gère les rafraîchissements de token transparents

### 4. Proxy HTTP
- Route les requêtes vers un serveur upstream
- Transmet automatiquement le token d'authentification
- Préserve les headers et le contexte de la requête

## Setup & Configuration

### Prérequis
- .NET 9 SDK
- Un IDP OIDC configuré (ex: Keycloak local)

### Installation

```bash
# Cloner le projet
cd /Users/taiebma/dev/proxyAPI

# Restaurer les dépendances
dotnet restore

# Compiler
dotnet build
```

### Configuration

#### 1. IDP Local (Keycloak)

Pour développement local avec Keycloak :

```bash
docker run -p 8080:8080 \
  -e KEYCLOAK_ADMIN=admin \
  -e KEYCLOAK_ADMIN_PASSWORD=admin \
  quay.io/keycloak/keycloak:latest \
  start-dev
```

Puis créer un client OAuth dans le realm `master` :
- Client ID: `insomnia-proxy`
- Client Secret: `local-dev-secret`
- Redirect URIs: `http://localhost:5000/auth/callback`
- Valid post logout redirect URIs: `http://localhost:5000`

#### 2. Fichier appsettings.Development.json

```json
{
  "Oidc": {
    "Authority": "http://localhost:8080/realms/master",
    "ClientId": "insomnia-proxy",
    "ClientSecret": "local-dev-secret",
    "AuthorizationEndpoint": "http://localhost:8080/realms/master/protocol/openid-connect/auth",
    "TokenEndpoint": "http://localhost:8080/realms/master/protocol/openid-connect/token",
    "RedirectUri": "http://localhost:5000/auth/callback"
  }
}
```

### Démarrage

```bash
# Depuis ProxyAPI.Presentation
cd ProxyAPI.Presentation
dotnet run
```

L'application démarre sur `http://localhost:5000`

## Utilisation avec Insomnia

### 1. Démarrer l'authentification

```bash
curl http://localhost:5000/api/auth/login
```

Réponse :
```json
{
  "url": "http://localhost:8080/realms/master/protocol/openid-connect/auth?...",
  "state": "...",
  "sessionId": "..."
}
```

Ouvrir l'URL dans un navigateur et se connecter.

### 2. Callback (automatique)

Après connexion réussie, l'IDP redirige vers `/api/auth/callback` avec un code d'autorisation.

Le proxy :
1. Valide le code
2. L'échange contre un token
3. Le stocke en cache
4. Retourne un cookie de session

### 3. Requêtes Proxifiées

Utiliser le proxy pour accéder aux endpoints protégés :

```bash
curl -b "X-ProxyAPI-ClientId=<client-id>" \
  http://localhost:5000/api/proxy/api/users
```

Le proxy :
- Valide le cookie de session
- Injecte le token Bearer
- Forwarder vers le serveur upstream
- Retourne la réponse

### 4. Vérifier le statut

```bash
curl -b "X-ProxyAPI-ClientId=<client-id>" \
  http://localhost:5000/api/auth/status
```

## Tests Unitaires

```bash
# Lancer tous les tests
dotnet test

# Test spécifique
dotnet test --filter MemoryTokenCacheTests

# Avec couverture de code
dotnet test /p:CollectCoverage=true
```

### Couverture

- **Domain Tests**: Validation value objects, entités
- **Application Tests**: Services d'authentification, orchestration (Mocks)
- **Infrastructure Tests**: Cache memory, TTL, expiration

## Extension & Customisation

### Implémenter un Cache Redis

```csharp
public class RedisTokenCache : ITokenCache
{
    private readonly IConnectionMultiplexer _redis;
    
    public void Set(ClientId clientId, TokenValue token)
    {
        var json = JsonSerializer.Serialize(token);
        _redis.GetDatabase().StringSet(
            clientId.Value, 
            json, 
            token.ExpiresAt - DateTime.UtcNow
        );
    }
    
    // Implémenter les autres méthodes...
}
```

Puis dans `DependencyInjectionExtensions.cs` :
```csharp
services.AddSingleton<ITokenCache, RedisTokenCache>();
```

### Implémenter un IDP Custom

Créer une nouvelle classe implémentant `IOAuthClient` :

```csharp
public class CustomOAuthClient : IOAuthClient
{
    // Implémenter les méthodes du contrat
}
```

## Architecture DDD - Explications

### 1. Domain Layer
- **Pas de dépendances externes** (HttpClient, DB)
- Contient la logique métier pure
- Entités et Value Objects immutables
- Exceptions custom

### 2. Application Layer
- Orchestre les use cases
- Utilise les abstractions du Domain (ITokenCache, IOAuthClient)
- Convertit les DTOs
- Aucun détail d'implémentation

### 3. Infrastructure Layer
- Implémente les interfaces du Domain
- Gère les détails externes (HTTP, cache, DB)
- Aucune logique métier

### 4. Presentation Layer
- Controllers, middleware
- Convertit les requêtes HTTP en DTOs
- Injection des dépendances
- Configuration ASP.NET Core

## Workflow Complet

```
1. [Insomnia] → GET /api/auth/login
2. [Proxy] → Génère State + SessionId
3. [Proxy] → Retourne URL IDP
4. [Insomnia] → Utilisateur clique → IDP login
5. [IDP] → Redirige vers /api/auth/callback?code=...
6. [Proxy] → Échange code → Token
7. [Proxy] → Stocke en cache
8. [Proxy] → Cookie de session
9. [Insomnia] → GET /api/proxy/api/users (+ cookie)
10. [Middleware] → Valide cookie
11. [Middleware] → Récupère token du cache
12. [Middleware] → Injecte Bearer
13. [Proxy] → Forward vers upstream
14. [Upstream] → Répond
15. [Insomnia] → Reçoit réponse
```

## Troubleshooting

### Le cookie n'est pas créé
- Vérifier que `Secure=true` (HTTPS) n'est pas trop strict en développement
- Vérifier `SameSite` settings en configuration

### Token expiration
- Implémenter la logique de refresh_token
- Configurer le TTL dans `appsettings.json`

### IDP connection error
- Vérifier les endpoints OIDC
- Tester : `curl {Authority}/.well-known/openid-configuration`
- Vérifier les credentials ClientId/ClientSecret

## Licences

Ce projet utilise :
- xUnit, Moq, FluentAssertions (tests)
- IdentityModel (JWT parsing)
- ASP.NET Core (framework)
