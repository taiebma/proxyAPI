# Architecture DDD - ProxyAPI

## Vue d'ensemble

ProxyAPI est architecturé selon les principes du **Domain-Driven Design (DDD)** avec une séparation explicite des responsabilités en 4 couches.

## Les 4 Couches

### 1. Domain Layer (`ProxyAPI.Domain`)

**Responsabilité** : Modéliser la logique métier pure, indépendante de toute infrastructure.

**Contenu** :
- **Entities** : `Client`, `AuthenticationSession`
- **Value Objects** : `ClientId`, `TokenValue`
- **Interfaces** : `ITokenCache`, `IOidcClient` (abstractions métier)
- **Exceptions** : `DomainException` et dérivées

**Principes** :
- ✅ Pas de dépendances externes (HttpClient, EF, etc.)
- ✅ Pas de configuration
- ✅ Logique métier 100% testable
- ✅ Immutabilité des value objects

**Exemple** : `TokenValue` encapsule la logique de validation d'expiration du token.

### 2. Application Layer (`ProxyAPI.Application`)

**Responsabilité** : Orchestrer les use cases en utilisant les abstractions du domain.

**Contenu** :
- **Services** : `AuthenticationService`, `OAuthFlowService`
- **DTOs** : `TokenResponse`, `ClientContext`, `AuthorizationUrlResponse`
- **Interfaces** : `IAuthenticationService` (contrat application)

**Principes** :
- ✅ Utilise uniquement les abstractions du Domain (interfaces)
- ✅ Pas d'implémentations concrètes (except services)
- ✅ Convertit les requêtes en use cases métier
- ✅ Retourne des résultats (DTOs)

**Exemple** : `AuthenticationService.HandleCallbackAsync()` orchestre :
1. Validation du state (Domain)
2. Échange du code (appel IOidcClient)
3. Stockage en cache (appel ITokenCache)
4. Retour du contexte client (DTO)

### 3. Infrastructure Layer (`ProxyAPI.Infrastructure`)

**Responsabilité** : Implémenter les interfaces métier avec les détails techniques externes.

**Contenu** :
- **Cache** : `MemoryTokenCache` (impl. `ITokenCache`)
- **OAuth** : `OidcClient` (impl. `IOidcClient`)
- **Configuration** : `OAuthSettings`, `CacheSettings`

**Principes** :
- ✅ Implémente les interfaces du Domain
- ✅ Gère les détails externes (HttpClient, collection concurrentes)
- ✅ Pas d'appels directs à Services Application

**Exemple** : `MemoryTokenCache` :
- Thread-safe (ConcurrentDictionary)
- TTL automatique
- Éviction des entrées expirées

### 4. Presentation Layer (`ProxyAPI.Presentation`)

**Responsabilité** : Exposer les use cases via HTTP et configurer l'injection de dépendances.

**Contenu** :
- **Controllers** : `AuthController`, `ProxyController`
- **Middleware** : `AuthenticationMiddleware`
- **Extensions** : `DependencyInjectionExtensions`
- **Configuration** : `appsettings.json`

**Principes** :
- ✅ Convertit HTTP → DTOs → Services
- ✅ Gère les cookies, headers HTTP
- ✅ Injection de dépendances centralisée
- ✅ Pas de logique métier

**Exemple** : `AuthController.Login()` :
1. Reçoit requête HTTP
2. Appelle `IAuthenticationService.GetAuthorizationUrlAsync()`
3. Retourne JSON + cookie

## Flux de Données

```
HTTP Request
    ↓
Controller (Presentation)
    ↓ Convertit en DTO
Service (Application)
    ↓ Orchestre
Domain Entities/Value Objects
    ↓ Appelle abstractions
Infrastructure Implementations
    ↓ Détails externes (HTTP, cache)
HTTP/Cache operations
    ↓
Response to Client
```

## Avantages de cette Architecture

### 1. Testabilité
- **Domain** : 100% testable, aucune dépendance externe
- **Application** : Testable avec Mocks (Moq)
- **Infrastructure** : Tests d'intégration
- **Presentation** : WebApplicationFactory

### 2. Maintenabilité
- Logique métier centralisée dans Domain
- Facile de localiser où changer
- Couplage faible entre couches

### 3. Extensibilité
- Changer le cache (Redis) : créer nouvelle classe `ITokenCache`
- Ajouter nouvel IDP : créer nouvelle classe `IOidcClient`
- Ajouter nouveau transport : créer nouveau contrôleur

### 4. Réutilisabilité
- Commandes console peuvent utiliser `AuthenticationService`
- Queue/Background jobs peuvent utiliser `ITokenCache`
- Logic indépendante d'HTTP

## Exemple : Ajouter un Cache Redis

1. **Créer l'implémentation** (Infrastructure) :
```csharp
public class RedisTokenCache : ITokenCache { ... }
```

2. **Enregistrer dans DI** (Presentation) :
```csharp
services.AddSingleton<ITokenCache>(sp =>
    new RedisTokenCache(connectionString));
```

3. **Application + Domain inchangés** ✅

## Structure des Fichiers

```
ProxyAPI.Domain/
├── Entities/
│   ├── Client.cs               # Entité client
│   ├── AuthenticationSession.cs # Session OAuth
├── ValueObjects/
│   ├── ClientId.cs             # ID immuable
│   └── TokenValue.cs           # Token immuable
├── Interfaces/
│   ├── ITokenCache.cs          # Abstraction cache
│   └── IOidcClient.cs         # Abstraction OAuth
└── Exceptions/
    └── DomainException.cs      # Exceptions métier

ProxyAPI.Application/
├── Services/
│   └── AuthenticationService.cs # Orchestration
├── DTOs/
│   └── OAuthDtos.cs            # Data Transfer Objects
└── Interfaces/
    └── IAuthenticationService.cs # Contrat service

ProxyAPI.Infrastructure/
├── Cache/
│   └── MemoryTokenCache.cs     # Impl. cache mémoire
├── OAuth/
│   └── OidcClient.cs           # Impl. OIDC
└── Configuration/
    └── OAuthSettings.cs         # Config settings

ProxyAPI.Presentation/
├── Controllers/
│   ├── AuthController.cs       # API Auth
│   └── ProxyController.cs      # API Proxy
├── Middleware/
│   └── AuthenticationMiddleware.cs # Auth middleware
└── Extensions/
    └── DependencyInjectionExtensions.cs # DI setup
```

## Principes SOLID Appliqués

### Single Responsibility (S)
- `AuthenticationService` : orchestration seulement
- `MemoryTokenCache` : cache seulement
- `AuthController` : HTTP seulement

### Open/Closed (O)
- Ajouter nouvel IDP : nouvelle classe `IOidcClient`
- Pas modifier code existant

### Liskov Substitution (L)
- `RedisTokenCache` remplace `MemoryTokenCache` sans casse
- Respecte contrat `ITokenCache`

### Interface Segregation (I)
- `ITokenCache` : 5 méthodes ciblées
- `IOidcClient` : 3 méthodes ciblées
- Pas de "God Interface"

### Dependency Inversion (D)
- Controllers → IAuthenticationService (abstraction)
- Pas de dépendances directes aux implémentations

## Décisions Architecturales

### 1. Pourquoi Abstractions dans Domain ?
Les interfaces `ITokenCache` et `IOidcClient` sont dans Domain (pas Infrastructure) car :
- Elles définissent les contrats métier
- Le Domain les utilise conceptuellement
- Les implémentations sont interchangeables

### 2. Pourquoi Value Objects ?
`ClientId` et `TokenValue` sont des value objects car :
- Logique d'égalité basée sur la valeur (pas l'identité)
- Immuables et sûrs par construction
- Encapsulent la validation

### 3. Pourquoi Session Storage en Memory ?
`AuthenticationSession` stockée en dictionnaire (pas cache) car :
- Durée très courte (10 minutes)
- Peu de données
- Perdu au redémarrage app (acceptable)

### 4. Pourquoi Middleware vs Filter ?
Middleware au lieu de Filter car :
- Contrôle total du pipeline
- Peut short-circuit la requête (401)
- Accès direct au contexte HTTP

## Évolution Future

### Potential Improvements
1. **Add Specification Pattern** for querying clients
2. **Add Repository Pattern** if adding database
3. **Add CQRS** if read/write separation needed
4. **Add Event Sourcing** for audit trails
5. **Add Domain Events** for notifications

### Migration vers BD
```csharp
// Add to Domain
public interface IClientRepository
{
    Task<Client?> GetByIdAsync(ClientId id);
    Task SaveAsync(Client client);
}

// Implement in Infrastructure
public class SqlClientRepository : IClientRepository { ... }

// Register in DI
services.AddScoped<IClientRepository, SqlClientRepository>();
```

## Conclusion

Cette architecture DDD offre :
- ✅ Logique métier claire et testable
- ✅ Couches bien séparées et responsabilités clairement définies
- ✅ Facile à étendre et maintenir
- ✅ Prête pour l'évolution (DB, events, etc.)
