---
name: authdeep-development
description: >
  Everything an AI coding agent needs to integrate AuthDeep without a browser —
  session login + CSRF, tenant/headless settings, users, gateway services,
  API keys (including notifications_email), SMTP/notification email config and
  templates, proxy S2S, frontend↔AuthDeep, backend↔gateway HMAC verification.
  Use when asked to add AuthDeep login, configure SMTP for signup/signin emails,
  create API keys, register services, send notification email, connect
  frontend/backend through the gateway, or self-host AuthDeep.
---

# AuthDeep Development (agent-first)

> Install: `curl -fsSL https://data.authdeep.com/skills/install.sh | sh -s gateway-integration`
> PowerShell: `iwr https://data.authdeep.com/skills/install.ps1 -useb | iex; Install-AuthDeepSkill gateway-integration`
> Stable URL: `https://data.authdeep.com/skills/gateway-integration`

This skill is the contract map for coding agents. Prefer **HTTP APIs** over the
admin UI when automating. Examples under `examples/` are the runnable source of
truth. **Pick the auth mode that matches the situation** — do not force one
pattern onto every use case.

### Decision matrix (all situations)

| Situation | Auth to use | Do not use |
|-----------|-------------|------------|
| End-user login in a browser / SPA | Session cookie `auth.sid` + CSRF (details: [browser-auth-integration](../browser-auth-integration/SKILL.md)) | `sak_` in the browser |
| **Public self-service signup + login for YOUR customer-facing app** (blog, SaaS end users) | **Hosted auth redirect** — send the user to the tenant's hosted signup/login, which creates their account + membership and returns them. See [recipe G](#14-prompt-recipes-copy-paste) | A confidential OIDC client for the public — OIDC authorize requires the user to already be a tenant member, so public users get `access_denied` |
| Customer’s own login page (headless) | Headless login + CSRF (`white_label`) — see [browser-auth-integration](../browser-auth-integration/SKILL.md) | Hosted login assumptions; inventing `SameSite=None` |
| OIDC client (AuthDeep as IdP) | Only for **known, provisioned** users who are already tenant members | Public/self-registering end users — they are not members yet |
| One-time admin setup (SMTP, keys, services, users) | Admin session + CSRF **or** AuthDeep admin UI | Embedding admin password in an app |
| App backend sends transactional email | `sak_` + HMAC → `/api/gateway/notifications/email` | User password; `X-Internal-Secret` |
| Browser/mobile calling gateway APIs | `cak_` (+ origin/IP locks) or user session | `sak_` / `ssk_` in client bundles |
| Service A → Service B via gateway | `sak_` + HMAC → `/api/gateway/proxy/{slug}/…` | Cross-tenant keys |
| Your backend verifies gateway proxy inbound | Verify `X-Gateway-Signature` with `ssk_` | Trusting unverified headers |
| Registered service calling notifications | `gwk_` + `X-Gateway-Signature` (`ssk_`) | Customer-facing `X-Internal-Secret` |
| AuthDeep Mail mailbox send (platform internal) | `authdeep_mail` provider / internal S2S | Exposing internal secret to customers |
| Self-hosted deploy | Compose/Helm + license | Unlicensed forever mode |

Mail **delivery** provider (tenant setting) is independent of send auth:

| Provider | When |
|----------|------|
| `smtp` | Tenant’s own SMTP (common for customer apps now) |
| `authdeep_mail` | Deliver via AuthDeep Mail |

## TOC

0. [Decision matrix](#decision-matrix-all-situations)
1. [Non-negotiable contracts](#1-non-negotiable-contracts)
2. [Bootstrap — login, CSRF, session](#2-bootstrap--login-csrf-session)
3. [Tenant settings & headless auth](#3-tenant-settings--headless-auth)
4. [Users](#4-users)
5. [Gateway services](#5-gateway-services)
6. [API keys (sak_/cak_)](#6-api-keys-sak_-cak_)
7. [Notification email — SMTP, templates, send](#7-notification-email--smtp-templates-send)
8. [Connect frontend → AuthDeep](#8-connect-frontend--authdeep)
9. [Connect backend → gateway (verify inbound)](#9-connect-backend--gateway-verify-inbound)
10. [Call another service via gateway proxy](#10-call-another-service-via-gateway-proxy)
11. [Plans / feature gates](#11-plans--feature-gates)
12. [Self-hosted](#12-self-hosted)
13. [Verify before done](#13-verify-before-done)
14. [Prompt recipes (copy-paste)](#14-prompt-recipes-copy-paste)
15. [Browser session / cookies / headless / cache](../browser-auth-integration/SKILL.md) — see **browser-auth-integration** skill

Base URL = AuthDeep API origin (examples: `https://app-dev.authdeep.net`,
`https://app.authdeep.com`). All paths below are relative to that origin.

---

## 1. Non-negotiable contracts

- **Session cookie `auth.sid` is HttpOnly.** Never put it in `localStorage`.
  Use a cookie jar (`credentials: 'include'` / curl `-c/-b`).
  For SameSite=Strict, CSRF, headless Origin allowlist, cache `no-store`, and
  same-site vs cross-domain handoff/OIDC, see the
  [browser-auth-integration](../browser-auth-integration/SKILL.md) skill.
- **CSRF on every mutation** when using a session: header `X-CSRF-Token` from
  `session.csrfToken` (or `GET /api/auth/csrf` for headless).
- **S2S uses HMAC**, never a user’s session cookie as a service credential.
- **API keys**: `sak_…` (HMAC required) and `cak_…` (HTTPS bearer-style key).
  Issued by AuthDeep only.
- **Tenant scope**: send `X-Tenant-Id: <uuid>` on gateway admin calls when the
  frontend/agent uses `tenantScoped` flows.
- **Do not invent endpoints.** If it is not in this skill or `examples/`, stop
  and check the live OpenAPI / handlers.

HMAC string (SAK outbound to AuthDeep):

```
METHOD\n<path+query>\n<unix_timestamp>\n<sha256hex(body)>
```

Header: `X-HMAC-Signature: t=<unix>,v1=<hex>`  
Header: `X-API-Key: sak_…`  
Clock skew > 5 minutes → reject/retry.  
Examples: `examples/backend/hmac/{python,nodejs,nextjs,java,golang}/`.

---

## 2. Bootstrap — login, CSRF, session

> Browser cookie/session details (SameSite=Strict, cache, headless Origin, cross-domain):
> [browser-auth-integration](../browser-auth-integration/SKILL.md).

### Local login (agent / script)

```http
POST /api/auth/local/login
Content-Type: application/json

{"email":"admin@tenant.example","password":"..."}
```

- No CSRF on this endpoint.
- Response `200`: `{ "session": { "userId", "tenantId", "csrfToken", "tenantRoles", "globalRole", "features", ... } }`
- Sets `Set-Cookie: auth.sid=…; HttpOnly; …`
- If `session.mfaPending: true` → complete MFA before other calls.

### Refresh session / CSRF

```http
GET /api/auth/session
Cookie: auth.sid=…
```

→ `{ "session": { …, "csrfToken": "…" } }` or `{ "session": null }`.

### Logout

```http
POST /api/auth/signout
Cookie: auth.sid=…
X-CSRF-Token: <csrfToken>
```

→ `{ "ok": true }`

### Headless login (customer’s own login UI)

Requires plan feature `white_label`, tenant `headlessAuthEnabled: true`, and
request `Origin` ∈ `headlessAllowedOrigins` (see browser-auth-integration).

```http
GET /api/auth/csrf
→ { "csrfToken": "…" }   # ephemeral if no session

POST /api/auth/headless/login
Content-Type: application/json
X-CSRF-Token: <csrfToken>

{"tenantId":"<uuid>","email":"…","password":"…","mfaCode":"optional"}
```

Examples: `examples/frontend/headless-integration/`.

**Agent rule:** keep cookie jar + `csrfToken` in memory for the whole run.
Every POST/PUT/PATCH/DELETE with the session must send `X-CSRF-Token`.

---

## 3. Tenant settings & headless auth

```http
GET /api/admin/tenants/{tenantId}/settings
Cookie: auth.sid=…
```

Important fields: `headlessAuthEnabled`, `headlessAllowedOrigins`,
`hostedAuthUiEnabled`, `hostedSigninEnabled`, `passwordLoginEnabled`,
`selfServiceSignupEnabled`, `notificationsEmail`, …

```http
PUT /api/admin/tenants/{tenantId}/settings
Cookie: auth.sid=…
X-CSRF-Token: <csrf>
Content-Type: application/json

{
  "headlessAuthEnabled": true,
  "headlessAllowedOrigins": ["https://app.customer.com"],
  "passwordLoginEnabled": true,
  "notificationsEmail": true
}
```

Branding (hosted login look):

| Method | Path |
|--------|------|
| GET/PUT | `/api/admin/tenants/{tenantId}/branding` |
| POST | `/api/admin/tenants/{tenantId}/branding/logo` (multipart) |
| GET | `/api/branding` (public, no auth) |

---

## 4. Users

```http
GET /api/admin/users?filter=&offset=0&limit=50&includeContext=true
Cookie: auth.sid=…
```

Optional (global_admin): `&tenantId=<uuid>`.

Response: `{ "users": [ { "id","email","displayName","role","tenantId","active","status",… } ], "total": N }`

Requires `tenant_admin` | `global_admin` | `super_admin`.

---

## 5. Gateway services

Register your backend so the gateway can proxy to it and issue `gwk_` / `ssk_`.

```http
GET /api/gateway/services
Cookie: auth.sid=…
X-Tenant-Id: <tenantId>
```

```http
POST /api/gateway/services
Cookie: auth.sid=…
X-CSRF-Token: <csrf>
X-Tenant-Id: <tenantId>
Content-Type: application/json

{
  "slug": "orders-api",
  "name": "Orders API",
  "backendUrl": "https://orders.internal.example/api",
  "openApiPath": "/openapi.json",
  "healthPath": "/health"
}
```

**201 — secrets shown once:**

```json
{
  "service": {
    "id": "…",
    "slug": "orders-api",
    "gatewayApiKey": "gwk_…",
    "serviceSecretKey": "ssk_…"
  }
}
```

Store `ssk_` in the backend secrets manager. `gwk_` identifies the service on
inbound gateway→backend requests.

Also: `GET/PUT/DELETE /api/gateway/services/{serviceId}`,
`GET /api/gateway/services/{serviceId}/integration`.

---

## 6. API keys (sak_/cak_)

There is **no** separate “notification key” prefix. Use `sak_` (recommended for
servers) or `cak_`.

### Permission model

Each permission row is **XOR**:

| Field | Use |
|-------|-----|
| `serviceId` + `httpMethod` | Proxy access to that registered service |
| `capability: "notifications_email"` + `httpMethod` | Platform route `/api/gateway/notifications/*` only |

### Create SAK for notifications (no service required) — preferred

Requires backend ≥ **0.314.0** (migration V100).

```http
POST /api/gateway/api-keys/service
Cookie: auth.sid=…
X-CSRF-Token: <csrf>
X-Tenant-Id: <tenantId>
Content-Type: application/json

{
  "label": "notification-sender",
  "hmacAlgorithm": "sha256",
  "replayWindowSecs": 300,
  "rateLimitRequests": 100,
  "rateLimitWindowSecs": 60,
  "permissions": [
    { "capability": "notifications_email", "httpMethod": "POST" }
  ]
}
```

**201 — copy once:**

```json
{
  "id": "…",
  "key": "sak_…",
  "hmacSecret": "…",
  "note": "Store key and hmacSecret securely — they will NOT be shown again"
}
```

### Create SAK scoped to a gateway service

```json
"permissions": [
  { "serviceId": "<service-uuid>", "httpMethod": "*" }
]
```

### Create CAK

```http
POST /api/gateway/api-keys/client
…
{ "label": "partner", "permissions": [ … ], "ipWhitelist": [], "allowedOrigins": [] }
```

CAK: send `X-API-Key: cak_…` only (no HMAC). Prefer SAK for servers.

### List / rotate / permissions

| Method | Path |
|--------|------|
| GET | `/api/gateway/api-keys/service` · `/client` |
| PUT | `/api/gateway/api-keys/service/{id}/permissions` |
| POST | `/api/gateway/api-keys/service/{id}/rotate` |

### UI note (admin console)

Frontend **≥ 0.104.0**: Create Service Key → **Access scope** →
**Platform APIs → Notification email**.

Older UI still shows **Service access (required)** only. Use the **API** above,
or pick any existing service + **POST** (back-compat: service POST still
authorises notification send). Deploy FE `0.104.0` to get the Platform option.

---

## 7. Notification email — SMTP, templates, send

**Law (AuthDeep platform):** signup / sign-in / password-reset / MFA email OTP /
welcome **always** deliver from `noreply@authdeep.com` (system sender). Tenant
Notification Email SMTP / AuthDeep Mail does **not** control those paths —
only `POST /api/gateway/notifications/email` (and API/app mail). Per-tenant
**templates** still brand auth subject/body. Settings + templates are always
tenant-scoped (`X-Tenant-Id` / effective tenant). AuthDeep SaaS uses the
`default` tenant for platform admin; that must not override system auth From.

AuthDeep can deliver **your own transactional** messages through tenant
notification config. Auth emails use platform delivery + optional templates.

### 7a. Enable + configure provider

```http
GET /api/gateway/notifications/settings
Cookie: auth.sid=…
X-Tenant-Id: <tenantId>
```

```http
PUT /api/gateway/notifications/settings
Cookie: auth.sid=…
X-CSRF-Token: <csrf>
X-Tenant-Id: <tenantId>
Content-Type: application/json

{
  "notifications_email": true,
  "provider": "smtp",
  "smtp_host": "smtp.example.com",
  "smtp_port": 587,
  "smtp_username": "user",
  "smtp_password": "secret",
  "smtp_from": "noreply@example.com",
  "email_rate_limit": 100
}
```

`provider`:

| Value | Meaning |
|-------|---------|
| `smtp` | Tenant’s own SMTP (signup/signin/OTP via this SMTP) |
| `authdeep_mail` | Route through AuthDeep Mail (`mail_from` mailbox) |

Omit `smtp_password` to keep existing; `""` clears.

Also enable tenant flag via settings if needed: `"notificationsEmail": true`
on `/api/admin/tenants/{id}/settings`.

### 7b. Templates

```http
GET /api/gateway/notifications/templates
Cookie: auth.sid=…
X-Tenant-Id: <tenantId>

PUT /api/gateway/notifications/templates/welcome
Cookie: auth.sid=…
X-CSRF-Token: <csrf>
X-Tenant-Id: <tenantId>
Content-Type: application/json

{
  "subject": "Welcome {{first_name}}",
  "html_body": "<p>Hi {{first_name}}</p>",
  "text_body": "Hi {{first_name}}"
}

DELETE /api/gateway/notifications/templates/welcome
Cookie: auth.sid=…
X-CSRF-Token: <csrf>
X-Tenant-Id: <tenantId>
```

### 7c. Send (your application backend — no browser)

```http
POST /api/gateway/notifications/email
Content-Type: application/json
X-API-Key: sak_…
X-HMAC-Signature: t=<unix>,v1=<hex>

{
  "to": "user@example.com",
  "subject": "Your code",
  "html_body": "<p>Code {{code}}</p>",
  "text_body": "Code {{code}}",
  "variables": { "code": "482915" }
}
```

Or template:

```json
{ "to": "…", "template": "welcome", "variables": { "first_name": "Ada" } }
```

**Success:** `202` `{ "accepted": true }`

**Auth alternatives for send:**

1. `sak_` + HMAC (preferred)
2. `cak_` + `X-API-Key` only
3. `X-Gateway-Key: gwk_…` + `X-Gateway-Signature` (signed with service `ssk_`)
4. Admin session + CSRF

Runnable: `examples/mail/{nodejs,python,go}/send_notification.*`

### 7d. Signup / signin emails from YOUR app

Two different paths — do not confuse them:

| Goal | How |
|------|-----|
| AuthDeep-hosted auth emails (OTP, magic link, password reset) | Configure §7a SMTP/`authdeep_mail`. AuthDeep sends them itself. |
| Emails YOUR backend sends after signup/signin in **your** product | Create SAK with `notifications_email` (§6) and call §7c from your backend. |

Do **not** use mail-backend `X-Internal-Secret` / `POST /v1/transactional/send`
from customer apps — that is AuthDeep-internal only.

---

## 8. Connect frontend → AuthDeep

Pattern (SPA / Next.js):

1. `POST /api/auth/local/login` or headless login with `credentials: 'include'`.
2. `GET /api/auth/session` → store `csrfToken` in memory.
3. Every mutation: `X-CSRF-Token` + cookies.
4. Protected UI reads identity from session, not from a JWT in localStorage.

Examples: `examples/frontend/{nextjs-integration,react-integration,headless-integration}/`.

To call **your** API through the gateway from the browser, prefer:

- Session cookie toward AuthDeep + gateway proxy, **or**
- Short-lived `cak_` with origin/IP restrictions — never embed `sak_` / `ssk_` in FE.

---

## 9. Connect backend → gateway (verify inbound)

When a client hits:

```
/api/gateway/proxy/{slug}/…  →  your backendUrl + …
```

Your backend must verify:

| Header | Meaning |
|--------|---------|
| `X-Gateway-Key` | `gwk_…` |
| `X-Gateway-Signature` | `t=…,v1=…` HMAC with **ssk_** |
| `X-Gateway-Timestamp` | unix seconds |
| `X-AuthDeep-User-ID` / `X-AuthDeep-Tenant-ID` | caller identity |

Signed payload for inbound verification (path only, trailing slash stripped):

```
METHOD\npath\ntimestamp\nhex(SHA256(body))
```

Reject invalid signatures. Never trust client-supplied tenant IDs over these
headers after verification.

---

## 10. Call another service via gateway proxy

**Correct proxy path (code):**

```
/api/gateway/proxy/{slug}/{upstreamPath}
```

Example:

```http
POST /api/gateway/proxy/orders-api/v1/orders
Content-Type: application/json
X-API-Key: sak_…
X-HMAC-Signature: t=<unix>,v1=<hex>

{"sku":"…"}
```

HMAC is over `RequestURI` (path **including** query string).

Service A → Service B: both registered under the same tenant; caller holds a
`sak_` with permission on B’s `serviceId` (or `*`).

---

## 11. Plans / feature gates

- Feature not on plan → `403`/`402`; do not invent client-side unlocks.
- Check entitlements from `session.features` or admin tenant APIs.
- Gateway routes require plan feature `api_gateway`.
- Headless login requires `white_label`.

---

## 12. Self-hosted

```bash
git clone https://git.authdeep.net/authdeep/authdeep-public
cd authdeep-public/examples/self-hosted
cp .env.example .env   # real secrets only
docker compose up -d
```

Helm: `helm/authdeep/`. Valid license required.

---

## 13. Verify before done

1. Real HTTP capture against a live AuthDeep env (DEV/QA).
2. CSRF missing → fail; wrong tenant → fail.
3. No secrets in git, logs, or FE bundles.
4. Diff against the matching `examples/` folder.

---

## 14. Prompt recipes (copy-paste)

Use the [decision matrix](#decision-matrix-all-situations). Recipes below are
**alternatives**, not a single mandatory path.

### A. App backend → send via tenant SMTP (`sak_` runtime)

```
Situation: customer application sends signup/signin/OTP/custom mail.
Runtime auth: sak_ + HMAC only (no end-user password in the app).

One-time setup (admin UI OR admin session API — sections 2+7):
- PUT notifications/settings: notifications_email=true, provider=smtp,
  smtp_host, smtp_port, smtp_username, smtp_password, smtp_from
- Create SAK with permissions:
  [{ "capability":"notifications_email", "httpMethod":"POST" }]
  (FE >= 0.104.0: Platform APIs → Notification email)
  Older FE: any gateway service + POST
- Store in app secrets:
  AUTHDEEP_GATEWAY_URL, AUTHDEEP_SERVICE_KEY=sak_…, AUTHDEEP_SIGNING_SECRET

Runtime send:
  POST /api/gateway/notifications/email
  X-API-Key + X-HMAC-Signature
  Body: to + subject/bodies OR template + variables
  Expect 202 { "accepted": true }
  Delivery uses the configured SMTP.

Examples: examples/mail/*/send_notification.*; examples/backend/hmac/
Forbidden in app: user password login, X-Internal-Secret, sak_ in frontend
```

### B. Admin UI — SMTP + Platform API key (human QA)

```
1. Notification Email → Provider SMTP → fill host/port/user/password/from → Save
2. API Keys → Create service key → Platform APIs → Notification email → POST
3. Copy sak_ + HMAC secret into app env
4. App uses recipe A
FE must be >= 0.104.0 for Platform APIs row (QA/PROD tags deployed).
```

### C. Admin session API — provision SMTP + SAK (automation / agents)

```
Situation: CI/onboarding agent with a short-lived admin session (not app runtime).
1. POST /api/auth/local/login (or headless) → cookie + csrfToken  [admin only]
2. PUT /api/gateway/notifications/settings (SMTP fields) + CSRF + X-Tenant-Id
3. POST /api/gateway/api-keys/service with capability notifications_email
4. Hand sak_/hmac to the app secret store; discard admin session
5. App forever uses recipe A
```

### D. Gateway service + proxy + inbound verify

```
1. Admin: POST /api/gateway/services → save gwk_ + ssk_
2. Backend: verify X-Gateway-Signature with ssk_ (section 9)
3. Admin: mint sak_ with { serviceId, httpMethod:"*" }
4. Callers: POST /api/gateway/proxy/{slug}/… with sak_ + HMAC
```

### E. Frontend session / headless

```
Hosted login: examples/frontend/{nextjs,react}-integration/
Headless: enable headlessAuthEnabled + origins; GET /api/auth/csrf;
  POST /api/auth/headless/login; keep csrf in memory; cookie HttpOnly
Mail from the SPA’s backend still uses recipe A — never sak_ in the browser.
```

### F. CAK / gateway-signature notification send

```
CAK: X-API-Key: cak_… (no HMAC); lock origins/IPs; same POST body as A
Registered service: X-Gateway-Key: gwk_… + X-Gateway-Signature with ssk_
```

### G. Public self-service signup + login for YOUR customer-facing app

Use this when **the public** (your blog readers, your SaaS end users) create
accounts and sign in on your site. Do NOT create an OIDC client for anonymous
self-registration — OIDC authorize requires an existing tenant member, so new
users get `access_denied` until they join via hosted signup.

**Requires** AuthDeep build that includes host-bind fix (bug-0421) and tenant app
handoff (feature-0523). Until then, use admin invite
`POST /api/admin/tenants/{tenantId}/users/invite` only.

```
One-time tenant setup (admin UI → Tenant Settings → Auth surfaces & origins):
- hostedAuthUiEnabled: true
- selfServiceSignupEnabled: true
- Web app origins: your app origin (https://blog.example.com)
- Allowed redirect URIs: your exact callback
  (https://blog.example.com/auth/callback) — used by /api/auth/app/start

Runtime (hosted redirect — no OIDC client):
1. Send new users to:
   https://<tenant>.authdeep.com/auth/signup?next=/api/auth/app/start?redirect_uri=<urlencoded exact allowedRedirectUri>&state=<opaque>
   Returning users:
   https://<tenant>.authdeep.com/auth/login?next=/api/auth/app/start?redirect_uri=<urlencoded exact allowedRedirectUri>&state=<opaque>

   IMPORTANT:
   - `next` MUST be a relative path starting with /api/auth/app/start
   - Absolute https:// URLs in `next` are rejected (open-redirect protection)
   - `redirect_uri` query value MUST exact-match a tenant allowedRedirectUri

2. Hosted UI joins the user to THAT tenant (selfServiceSignupEnabled), then
   resumes `next` → AuthDeep validates redirect_uri → 302 to your callback
   with ?code=…&state=…

3. Your app backend exchanges the code for identity (not an AuthDeep session):
   POST https://<tenant>.authdeep.com/api/auth/app/exchange
   { "code": "…" }
   → { userId, email, name, tenantId, tenantRoles, globalRole }
   Code is single-use (~2 minutes). Build YOUR app session from that identity.
   Never expect auth.sid on your domain (SameSite=Strict on .authdeep.com).

Rules:
- NEVER create a confidential OIDC client for anonymous public signup.
- OIDC (AuthDeep as IdP) is for already-provisioned members / Access.
- selfServiceSignupEnabled must be true or hosted join-signup 403s.
- Headless login is a separate Scale/Enterprise surface; do not enable it as a
  workaround for Recipe G.
```
