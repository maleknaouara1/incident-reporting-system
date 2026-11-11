# Déploiement en production (Docker)

Ce dossier met toute l'application en ligne sur **un seul serveur Linux**, derrière **un seul domaine en HTTPS** :

```
Internet ──HTTPS──> caddy ──> web (nginx : application Angular + relais /api) ──> api (.NET 10) ──> db (SQL Server)
                                                                      migrate (applique les migrations puis s'arrête)
```

- **caddy** obtient et renouvelle automatiquement le certificat HTTPS (Let's Encrypt) et redirige HTTP vers HTTPS.
- **web** sert l'application Angular et relaie `/api/...` vers l'API. Le site et l'API sont donc sur la **même origine** : c'est ce qu'exige le cookie de session `SameSite=Strict`, et cela rend CORS inutile.
- **migrate** met la base à jour à chaque démarrage, avant l'API.
- Seuls les ports **80** et **443** sont ouverts sur Internet. L'API et la base ne sont accessibles que par le réseau interne Docker.

## 1. Prérequis

- Un serveur Linux 64 bits (x86-64) avec **au moins 4 Go de RAM** (SQL Server en demande 2 Go), Docker et le plugin Compose :
  ```bash
  curl -fsSL https://get.docker.com | sh
  ```
- Un **nom de domaine** dont l'enregistrement DNS `A` (et `AAAA` en IPv6) pointe vers l'adresse IP du serveur.
- Les ports **80** et **443** ouverts dans le pare-feu.

## 2. Récupérer le code et configurer

```bash
git clone https://github.com/maleknaouara1/incident-reporting-system.git
cd incident-reporting-system/deploy
cp .env.example .env
nano .env
```

Valeurs **obligatoires** dans `.env` :

| Variable | Rôle |
|---|---|
| `DOMAIN` | Le domaine public, ex. `incidents.mondomaine.com` |
| `SQL_SA_PASSWORD` | Mot de passe SQL Server : 8 caractères minimum, majuscule, minuscule, chiffre et symbole ; **ni `;` ni guillemets** |
| `JWT_KEY` | Clé de signature des sessions, **32 caractères minimum** : `openssl rand -base64 48` |
| `ADMIN_EMAIL` / `ADMIN_PASSWORD` | Premier compte Admin (créé seulement s'il n'existe aucun Admin) |

Pour envoyer de vrais emails (lien « mot de passe oublié »), mettre `SMTP_MOCK=false` et remplir `SMTP_*` (pour Gmail : un mot de passe d'application de 16 lettres, sans espaces). Sinon les emails sont seulement écrits dans les logs de l'API.

> Ne commitez jamais `.env` : il contient tous les secrets (il est déjà ignoré par git).

## 3. Démarrer

```bash
docker compose up -d --build
```

Le premier démarrage prend quelques minutes (construction des images, démarrage de SQL Server, migrations, certificat HTTPS). Suivre l'avancement :

```bash
docker compose ps
docker compose logs -f api
```

Puis ouvrir `https://<DOMAIN>` et se connecter avec le compte Admin. **Ensuite, supprimer `ADMIN_EMAIL` et `ADMIN_PASSWORD` de `.env`** (le compte existe désormais en base).

## 4. Mettre à jour

```bash
cd incident-reporting-system && git pull
cd deploy && docker compose up -d --build
```

L'image du site est reconstruite depuis la branche `main` du dépôt Angular sur GitHub. Les migrations de base s'appliquent automatiquement avant le redémarrage de l'API.

## 5. Sauvegardes

Les données vivent dans des volumes Docker :

| Volume | Contenu |
|---|---|
| `incident-reporting_sqldata` | Base SQL Server |
| `incident-reporting_attachments` | Pièces jointes des incidents |
| `incident-reporting_caddy_data` | Certificats HTTPS |

Sauvegarde de la base (fichier `.bak` copié hors du conteneur) :

```bash
docker compose exec db bash -c '/opt/mssql-tools18/bin/sqlcmd -C -S localhost -U sa -P "$MSSQL_SA_PASSWORD" \
  -Q "BACKUP DATABASE [IncidentReportingDb] TO DISK = N'"'"'/var/opt/mssql/backup.bak'"'"' WITH INIT"'
docker compose cp db:/var/opt/mssql/backup.bak ./backup-$(date +%F).bak
```

À planifier (cron) et à copier sur une autre machine.

## 6. Points de sécurité

- **Changer les secrets** de développement : la clé JWT, et le mot de passe d'application Gmail s'il a déjà été exposé.
- L'API fait confiance aux en-têtes `X-Forwarded-*` (`ReverseProxy__Enabled=true`) **uniquement** parce qu'elle n'est joignable que par le réseau Docker. Ne publiez jamais son port 8080 sur Internet.
- L'API se connecte à la base avec le compte `sa`. Pour aller plus loin, créer un utilisateur SQL dédié avec les seuls droits sur `IncidentReportingDb`.
- Après la mise en production, chaque utilisateur doit **se reconnecter une fois** (le jeton de renouvellement est passé dans un cookie HttpOnly).
- SQL Server **Express** est gratuit mais limité à 10 Go par base ; au-delà, une licence est nécessaire.

## Dépannage

| Symptôme | Piste |
|---|---|
| `migrate` en erreur | `docker compose logs migrate` : souvent un `SQL_SA_PASSWORD` invalide ou contenant `;` |
| Pas de certificat HTTPS | Le DNS doit pointer vers le serveur et les ports 80/443 être ouverts ; voir `docker compose logs caddy` |
| `api` redémarre en boucle | `docker compose logs api` : souvent `JWT_KEY` manquant ou trop court |
| Emails non reçus | `SMTP_MOCK=false` ? Sinon voir les erreurs SMTP dans `docker compose logs api` |
