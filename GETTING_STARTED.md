# Getting Started - ProxyAPI

Guide rapide pour démarrer avec le proxy OAuth.

## Démarrage Rapide (5 minutes)

### 1. Prérequis
```bash
dotnet --version  # Assurez-vous d'avoir .NET 9
```

### 2. Compiler la solution
```bash
cd /Users/taiebma/dev/proxyAPI
dotnet build
```

### 3. Lancer les tests
```bash
dotnet test
# Sortie attendue : 31 tests réussis
```

### 4. Démarrer le serveur (mode développement)
```bash
cd ProxyAPI.Presentation
dotnet run
```

Le proxy démarre sur `http://localhost:5000`

---

## Configuration Locale (Keycloak)

### Étape 1 : Démarrer Keycloak

```bash
docker run -p 8080:8080 \
  -e KEYCLOAK_ADMIN=admin \
  -e KEYCLOAK_ADMIN_PASSWORD=admin \
  quay.io/keycloak/keycloak:latest \
  start-dev
```

Accès : http://localhost:8080
- Admin console : http://localhost:8080/admin
- Username: `admin`
- Password: `admin`

### Étape 2 : Créer un Client OIDC

1. Aller à **Realm: master** → **Clients**
2. Créer un nouveau client :
   - **Client ID**: `insomnia-proxy`
   - **Client type**: OpenID Connect
3. Configuration :
   - **Valid redirect URIs**: `http://localhost:5000/auth/callback`
   - **Valid post logout redirect URIs**: `http://localhost:5000/auth/logout`
   - **Access type**: Confidential
4. Récupérer le **Client Secret** dans l'onglet **Credentials**

### Étape 3 : Configurer le Proxy

Le proxy utilise automatiquement `appsettings.Development.json` :

```json
{
  "Oidc": {
    "Authority": "http://localhost:8080/realms/master",
    "ClientId": "insomnia-proxy",
    "ClientSecret": "xxxxxxxxxxxxxxxx",  // À remplacer
    "AuthorizationEndpoint": "http://localhost:8080/realms/master/protocol/openid-connect/auth",
    "TokenEndpoint": "http://localhost:8080/realms/master/protocol/openid-connect/token",
    "RedirectUri": "http://localhost:5000/auth/callback"
  }
}
```

---

## Workflow de Test

### 1. Obtenir l'URL OAuth

```bash
curl http://localhost:5000/api/auth/login
```

Réponse :
```json
{
  "url": "http://localhost:8080/realms/master/protocol/openid-connect/auth?client_id=insomnia-proxy&...",
  "state": "abc123...",
  "sessionId": "xyz789..."
}
```

### 2. Se connecter (dans le navigateur)

Ouvrir l'URL retournée et se connecter avec un compte Keycloak.

Après connexion réussie, vous verrez :
```json
{
  "message": "Authentication successful",
  "clientId": "550e8400-e29b-41d4-a716-446655440000"
}
```

**Copier le `clientId`** - c'est votre session.

### 3. Vérifier le statut

```bash
curl -H "Cookie: X-ProxyAPI-ClientId=550e8400-e29b-41d4-a716-446655440000" \
  http://localhost:5000/api/auth/status
```

Réponse :
```json
{
  "authenticated": true,
  "clientId": "550e8400-e29b-41d4-a716-446655440000",
  "expiresAt": "2026-06-18T15:30:00Z"
}
```

### 4. Accéder aux ressources protégées

```bash
curl -H "Cookie: X-ProxyAPI-ClientId=550e8400-e29b-41d4-a716-446655440000" \
  http://localhost:5000/api/proxy/api/users
```

Le proxy :
- ✅ Valide votre session
- ✅ Récupère le token du cache
- ✅ Injecte le Bearer token
- ✅ Forward vers le serveur upstream (http://localhost:3000)

### 5. Se déconnecter

```bash
curl -X POST \
  -H "Cookie: X-ProxyAPI-ClientId=550e8400-e29b-41d4-a716-446655440000" \
  http://localhost:5000/api/auth/logout
```

---

## Avec Insomnia

### Étape 1 : Authentification

1. **New Request** → Type: `GET`
2. URL: `http://localhost:5000/api/auth/login`
3. Send
4. Copier la réponse `url`
5. Ouvrir dans navigateur, se connecter
6. Copier le `clientId` de la réponse de callback

### Étape 2 : Ajouter Cookie

1. **New Request** pour protéger
2. Headers → **Cookie**:
   ```
   X-ProxyAPI-ClientId=<clientId-from-step-1>
   ```

### Étape 3 : Accéder aux ressources

```
GET http://localhost:5000/api/proxy/api/users
Cookie: X-ProxyAPI-ClientId=550e8400-e29b-41d4-a716-446655440000
```

---

## Structure du Projet

```
ProxyAPI/
├── ProxyAPI.Domain/           # Logique métier (DDD)
├── ProxyAPI.Application/      # Services & DTOs
├── ProxyAPI.Infrastructure/   # Cache, OAuth clients
├── ProxyAPI.Presentation/     # Controllers, middleware
├── ProxyAPI.Tests/            # Tests unitaires
├── README.md                  # Documentation complète
├── ARCHITECTURE.md            # Explications DDD
└── appsettings.json           # Configuration production
```

---

## Commandes Útiles

### Build & Test
```bash
# Compiler
dotnet build

# Tests
dotnet test

# Tests avec couverture
dotnet test /p:CollectCoverage=true

# Voir les tests en détail
dotnet test --verbosity normal
```

### Développement
```bash
# Démarrer le serveur (hot reload)
cd ProxyAPI.Presentation
dotnet watch run

# Ou simplement
dotnet run
```

### Débogge
```bash
# Sur VS Code
# Mettre un breakpoint et démarrer avec
dotnet run --configuration Debug

# Ou
Ctrl+Shift+D en VS Code
```

---

## Troubleshooting

### 1. "Connection refused" à Keycloak
```
✓ Vérifier que Keycloak est lancé : http://localhost:8080
✓ Vérifier le port dans appsettings.Development.json
```

### 2. "Invalid state parameter"
```
✓ La session a expiré (10 minutes)
✓ Relancer le flow depuis /api/auth/login
```

### 3. "Token expired"
```
✓ Le proxy tente de rafraîchir automatiquement
✓ Si aucun refresh_token : relancer l'authentification
```

### 4. "401 Unauthorized" sur proxy
```
✓ Vérifier que le cookie X-ProxyAPI-ClientId est présent
✓ Vérifier le format du cookie
✓ Relancer l'authentification
```

### 5. "Upstream server not found"
```
✓ Le proxy essaie de forwarder à http://localhost:3000
✓ Démarrer votre serveur upstream sur ce port
✓ Ou configurer l'adresse dans ProxyController.cs
```

---

## Architecture DDD Expliquée

La solution suit **Domain-Driven Design** :

- **Domain** (`ProxyAPI.Domain`)
  - Entités métier : `Client`, `AuthenticationSession`
  - Abstractions : `ITokenCache`, `IOAuthClient`
  - Logique métier pure

- **Application** (`ProxyAPI.Application`)
  - Orchestration des use cases
  - `AuthenticationService` = coordinateur
  - DTOs = format des requêtes/réponses

- **Infrastructure** (`ProxyAPI.Infrastructure`)
  - Implémentations : `MemoryTokenCache`, `OidcClient`
  - Détails externes (HTTP, cache)

- **Presentation** (`ProxyAPI.Presentation`)
  - Controllers : `AuthController`, `ProxyController`
  - Middleware : extraction du cookie, injection du token

Pour plus de détails, lire [ARCHITECTURE.md](./ARCHITECTURE.md).

---

## Prochaines Étapes

### 1. Tester avec votre IDP
Remplacer les configs Keycloak par vos endpoints OIDC (Azure AD, Auth0, etc.)

### 2. Implémenter Redis Cache
Créer `RedisTokenCache : ITokenCache` pour production

### 3. Ajouter une BD
Créer `IClientRepository` pour persister les sessions

### 4. Monitoring & Logging
Ajouter Serilog pour production

### 5. Tests d'Intégration
Ajouter `IntegrationTests` avec WebApplicationFactory

---

## Support

- 📖 Documentation complète : [README.md](./README.md)
- 🏗️ Architecture : [ARCHITECTURE.md](./ARCHITECTURE.md)
- ✅ Tests : Voir dossier `ProxyAPI.Tests/`
- 🔧 Configuration : `appsettings.*.json`

Bon développement ! 🚀
