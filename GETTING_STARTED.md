# Guide de démarrage rapide

Ce guide vous permet de démarrer ProxyAPI localement et de tester le flow OAuth puis le proxy HTTP.

## 1. Prérequis

- .NET SDK installé
- Un fournisseur OIDC accessible
- Optionnellement Docker si vous voulez tester avec Keycloak localement

Vérifiez l’environnement avec :

```bash
dotnet --version
```

## 2. Installer et compiler

```bash
cd /Users/taiebma/dev/proxyAPI
dotnet restore
dotnet build
```

## 3. Lancer l’application

```bash
cd ProxyAPI.Presentation
dotnet run
```

Selon la configuration de développement, l’application peut être disponible sur :

- `http://localhost:5000`
- `https://localhost:5001`

## 4. Configurer l’IDP

La configuration OIDC se trouve dans [ProxyAPI.Presentation/appsettings.Development.json](ProxyAPI.Presentation/appsettings.Development.json).

Le bloc à adapter est le suivant :

```json
{
  "Oidc": {
    "Authority": "https://your-idp.example.com",
    "ClientId": "insomnia-proxy",
    "ClientSecret": "your-client-secret",
    "AuthorizationEndpoint": "https://your-idp.example.com/oauth/authorize",
    "TokenEndpoint": "https://your-idp.example.com/oauth/token",
    "RedirectUri": "http://localhost:5000/api/auth/callback"
  }
}
```

## 5. Tester le flux d’authentification

### Étape A — démarrer le login

```bash
curl http://localhost:5000/api/auth/login
```

La réponse contient l’URL d’autorisation et une session temporaire.

### Étape B — valider le callback

Après la connexion, l’IDP redirige vers :

```text
/api/auth/callback?code=...&state=...
```

Le backend échange le code contre un token et crée un identifiant `X-ProxyAPI-ClientId`.

### Étape C — vérifier l’état

```bash
curl -H "X-ProxyAPI-ClientId=<client-id>" http://localhost:5000/api/auth/status
```

### Étape D — tester le proxy

```bash
curl -H "X-ProxyAPI-ClientId=<client-id>" "http://localhost:5000/api/proxy/?uri=https://example.com"
```

Le paramètre `uri` est obligatoire et définit l’URL de destination de la requête proxy.

## 6. Tester avec Insomnia

1. Créez une requête `GET` vers `http://localhost:5000/api/auth/login`
2. Copiez l’URL renvoyée et ouvrez-la dans un navigateur
3. Une fois connecté, récupérez la valeur du header `X-ProxyAPI-ClientId`
4. Ajoutez ce header à la requête vers `/api/proxy/`

## 7. Déconnexion

```bash
curl -X POST -H "X-ProxyAPI-ClientId=<client-id>" http://localhost:5000/api/auth/logout
```

## 8. Dépannage rapide

### Le login ne démarre pas
- Vérifiez que l’URL de l’IDP et les endpoints OIDC sont corrects.
- Contrôlez les valeurs `ClientId` et `ClientSecret`.

### La session est refusée sur `/api/proxy/`
- Vérifiez que le header `X-ProxyAPI-ClientId` est bien envoyé.
- Vérifiez que la session n’a pas expiré.

### Le proxy ne répond pas
- Vérifiez que l’URL passée dans `uri` est valide.
- Contrôlez le serveur upstream cible.

## 9. Tests

```bash
dotnet test
```

Pour plus de détails, voir [README.md](README.md) et [ARCHITECTURE.md](ARCHITECTURE.md).
