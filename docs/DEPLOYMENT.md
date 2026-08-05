# Deployment

This guide takes you from an empty Ubuntu VPS to the a single server running three containers managed by
Docker Compose, running over HTTPS:

- **caddy** : Reverse proxy (terminates TLS with automatic Let's Encrypt certs)
- **api** : The .NET API serving the built Vue SPA (built from `Dockerfile`)
- **db** : PostgreSQL (data stored in a named Docker volume)

Only Caddy is reachable from the internet (ports 80/443). The API and database live on an internal Docker network.
All configuration comes from a `.env` file next to `compose.yaml`.

Prerequisite: A domain (or subdomain) you control, e.g. `cbo.example.com`.

---

## 1. Provision the VPS

1. On your local machine, create an SSH key pair if you don't have one (press Enter through the prompts):

   ```bash
   ssh-keygen -t ed25519
   ```

   Your public key is in `~/.ssh/id_ed25519.pub`. Paste its contents into the provider's console.
   The private key never leaves your machine.

2. In the provider's console, create a server:
   - Image: Ubuntu 26.04 LTS
   - Size: the smallest instance is enough
   - SSH key: add the contents of `~/.ssh/id_ed25519.pub`.

3. Note the server's public IPv4 address. It's used everywhere below as `YOUR_SERVER_IP`.

## 2. Point your domain at the server

Do this early.
DNS changes take time to propagate and Caddy can only obtain a certificate once the domain resolves to your server.

In your DNS provider's panel, create an "A record" for the (sub)domain pointing at `YOUR_SERVER_IP`.

> If your DNS provider proxies traffic through its own network
> (e.g. Cloudflare's orange-cloud "Proxied" mode), turn that off for this record ("DNS only")
> Caddy obtains its own TLS certificate and needs traffic to reach the server directly.

Verify from your local machine (repeat until it returns your server's IP):

```bash
nslookup cbo.example.com
```

## 3. Create a user and harden the server

Log in as root:

```bash
ssh root@YOUR_SERVER_IP
```

Create a user for day-to-day work (choose a password for `sudo`):

```bash
adduser deploy
usermod -aG sudo deploy
```

Let the new user log in with the same SSH key as root:

```bash
install -d -m 700 -o deploy -g deploy /home/deploy/.ssh
cp /root/.ssh/authorized_keys /home/deploy/.ssh/
chown deploy:deploy /home/deploy/.ssh/authorized_keys
chmod 600 /home/deploy/.ssh/authorized_keys
```

Before continuing, verify in a second terminal that `ssh deploy@YOUR_SERVER_IP` works.
The next step locks out password logins and root.

Disable root login and password authentication:

```bash
sed -i 's/^#\?PermitRootLogin.*/PermitRootLogin no/' /etc/ssh/sshd_config
sed -i 's/^#\?PasswordAuthentication.*/PasswordAuthentication no/' /etc/ssh/sshd_config
systemctl restart ssh
```

Enable the firewall, allowing only SSH, HTTP, and HTTPS:

```bash
ufw allow OpenSSH
ufw allow 80/tcp
ufw allow 443/tcp
ufw enable
```

From here on, work as the `deploy` user:

```bash
exit
ssh deploy@YOUR_SERVER_IP
```

## 4. Install Docker

Install Docker Engine and the Compose plugin
([source](https://docs.docker.com/engine/install/ubuntu/)):

```bash
sudo apt-get update
sudo apt-get install -y ca-certificates curl
sudo install -m 0755 -d /etc/apt/keyrings
sudo curl -fsSL https://download.docker.com/linux/ubuntu/gpg -o /etc/apt/keyrings/docker.asc
echo "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.asc] \
  https://download.docker.com/linux/ubuntu $(. /etc/os-release && echo "$VERSION_CODENAME") stable" | \
  sudo tee /etc/apt/sources.list.d/docker.list > /dev/null
sudo apt-get update
sudo apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin
```

Allow your user to run Docker without `sudo`, then re-log so it takes effect:

```bash
sudo usermod -aG docker $USER
exit
ssh deploy@YOUR_SERVER_IP
```

> Note: membership in the `docker` group is effectively root access on the machine.
> That's fine for a single-operator server. Just be aware of it.

Verify:

```bash
docker run --rm hello-world
```

## 5. Deploy the app

Clone the repository:

```bash
git clone https://github.com/thaumaturg/cbo.git
cd cbo
```

The frontend build expects `frontend/.env.local` to exist
(it carries the PrimeUI license key, baked into the bundle at build time).
Create it:

```bash
echo "VITE_PRIMEUI_LICENSE_KEY=your-key-here" > frontend/.env.local
```

Create the environment file and fill it in:

```bash
cp .env.example .env
nano .env
```

- `DOMAIN` (your domain, e.g. `cbo.example.com`)
- `POSTGRES_PASSWORD` (generate one: `openssl rand -base64 24`)
- `JWT_KEY` (generate one: `openssl rand -hex 64`)
- `JWT_ISSUER` / `JWT_AUDIENCE` (your public URL, e.g. `https://cbo.example.com`)

Keep the file private:

```bash
chmod 600 .env
```

Build and start everything (the first build takes a few minutes):

```bash
docker compose up -d --build
```

## 6. Verify

```bash
docker compose ps
```

All three services should be `Up`, db should show `healthy`. Then open `https://cbo.example.com` in a browser.
You should see the app over HTTPS with a valid certificate.
The API answers under `/api/*`, database schema is created automatically on first start
(EF Core migrations run at startup).

If something is off:

```bash
docker compose logs caddy   # certificate issuance problems show up here
docker compose logs api     # app/database errors show up here
```

The most common first-deploy failure is DNS that hasn't propagated yet.
Caddy logs certificate errors and retries automatically.
Simply wait and check again.

## 7. Day-2 operations

All commands run from the repo directory (`~/cbo`).

### Logs

```bash
docker compose logs -f api          # follow the app log (Ctrl+C to stop)
docker compose logs --tail 100 db   # last 100 lines of PostgreSQL
```

### Deploying an update

```bash
git pull
docker compose up -d --build
```

Compose rebuilds the image and replaces only the `api` container with a few seconds of downtime.
The database and its data are untouched, and any new EF migrations apply automatically on startup.
Occasionally reclaim disk space from superseded image layers:

```bash
docker image prune -f
```

### Stop / start

```bash
docker compose down     # stop and remove containers, volumes (data) survive
docker compose up -d    # start again
```

> **Never** run `docker compose down -v` on the server.
> The `-v` flag deletes the volumes, i.e. your database and TLS certificates.

### Backup

Write a plain-SQL dump of the database to a file on the host:

```bash
mkdir -p ~/backups
docker compose exec -T db pg_dump -U postgres -d cbo_db > ~/backups/cbo_$(date +%F).sql
```

For a nightly backup at 03:15, run `crontab -e` and add:

```cron
15 3 * * * cd ~/cbo && docker compose exec -T db pg_dump -U postgres -d cbo_db > ~/backups/cbo_$(date +\%F).sql
```

Copy backups off the server from your local machine:

```bash
scp deploy@YOUR_SERVER_IP:~/backups/cbo_<date>.sql .
```

### Restore

A plain-SQL dump restores into an empty database, so reset the data volume first.
This deletes the current data - that's the point of a restore:

```bash
docker compose down
docker volume rm cbo_pgdata
docker compose up -d --wait db
docker compose exec -T db psql -U postgres -d cbo_db < ~/backups/cbo_<date>.sql
docker compose up -d
```

### Database access (pgAdmin)

The database publishes no port on the server, so a UI tool can't reach it directly. That should stay that way.
The safe pattern is to publish the port on the server's loopback interface only
and let pgAdmin connect through an SSH tunnel. The database remains unreachable from the internet.

One-time setup on the server. The repo already has an override template, so just copy it and apply:

```bash
cp compose.override.yaml.example compose.override.yaml
docker compose up -d
```

This publishes the database on `127.0.0.1:5432` and the API on `127.0.0.1:8080` which is also loopback-only,
(occasionally handy for `curl`ing the API directly on the server). Neither port is reachable from the internet.

Recreating the containers drops connections for a couple of seconds. The API reconnects on its next request.

Then in pgAdmin on your local machine, register a new server with:

- **Connection** tab: Host `127.0.0.1`, Port `5432`, Database `cbo_db`, Username `postgres`,
  Password `POSTGRES_PASSWORD` from the server's `.env`.
- **SSH Tunnel** tab: enabled, Tunnel host `YOUR_SERVER_IP`, Username `deploy`,
  Authentication "Identity file" pointing at your SSH private key (`~/.ssh/id_ed25519`).

pgAdmin logs into the server over SSH and forwards the database connection through that encrypted link.

Without pgAdmin, a SQL prompt directly in the SSH session needs no setup at all:

```bash
docker compose exec db psql -U postgres -d cbo_db
```

> Before any manual data manipulation, take a backup (see above)
