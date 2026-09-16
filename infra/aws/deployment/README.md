# First manual AWS deployment

Preparation only: no push, SSM command, deployment, or Terraform change is
performed by adding these files. The commands below are for a later explicitly
approved deployment. No Git operations or CD workflow are required.

## Architecture and release contract

Host port 80 -> frontend/nginx:8080 -> `/api/` -> api:8080 -> private RDS.
Only the frontend publishes a host port. There is no PostgreSQL container.
The existing API Dockerfile supplies both runtime and EF migration targets.
Use one explicit release tag built from the same source for all three images:

- `smart-expense-api:<release>`: API runtime.
- `smart-expense-api:<release>-migrations`: existing SDK/EF migration target.
- `smart-expense-frontend:<release>`: nginx/frontend runtime.

The migration tag uses the existing API repository, not a third repository.
Do not overwrite a release tag: ECR tags are mutable, but operational releases
should be stable. ECR retains only five images **per repository**; API runtime
and migration images both count toward that total. Keep rollback candidates in
mind before uploading additional releases.

## Prerequisites and non-secret inputs

Locally: Docker Desktop with Linux containers, AWS CLI v2, PowerShell, and a
valid `smart-expense-demo` SSO session in account `739275443630`. Renew SSO
manually if needed; the sender does not initiate interactive login.

On EC2: Docker, Compose >=2.30, AWS CLI, Python 3, curl, and flock. The AL2023
bootstrap supplies Docker/Compose/AWS tools and SSM Agent. The deployment checks
for Python; if absent, stop and explicitly install it on the host later with
`dnf install -y python3`. No Terraform modification is necessary. EC2 must remain
Online in SSM and have ECR pull and exact SSM parameter-read permissions.

Copy `deployment.env.example` to a local file, preferably outside OneDrive, such
as `C:/terraform/smart-expense-demo/deployment.env`. It contains non-secret
inputs only. Never add `db_password`, `jwt_signing_key`, AWS credentials, or
connection strings to it. Use plain `KEY=value`, without shell quoting:

| Input | Meaning |
| --- | --- |
| `AWS_REGION` | `us-east-1` |
| `ECR_REGISTRY` | Existing account registry |
| `IMAGE_TAG` | Explicit release tag, e.g. `demo-001` |
| `RDS_HOST` | Actual Terraform `rds_endpoint` hostname, without `:5432` |
| `RDS_PORT` | `5432` |
| `RDS_DATABASE`, `RDS_USERNAME` | Actual Terraform database outputs |
| `DB_PASSWORD_PARAMETER` | `/smart-expense/demo/db-password` |
| `JWT_SIGNING_KEY_PARAMETER` | `/smart-expense/demo/jwt-signing-key` |

Read only the named, non-sensitive Terraform outputs if needed:
`terraform -chdir=infra/aws output -raw rds_endpoint`, `database_name`, and
`database_username`. Do not dump state or retrieve secrets locally.

## Local validation before any push

From the repository root:

```powershell
dotnet build backend/SmartExpense.slnx --configuration Release
dotnet test backend/SmartExpense.slnx --configuration Release --no-build
Push-Location frontend
npm run lint
npm run build
Pop-Location
python -B -m unittest discover -s infra/aws/deployment -p 'test_*.py' -v
# After building the three local aws-validation image tags:
./infra/aws/deployment/smoke-local.ps1
```

The Python Compose tests use temporary fake values, require a Docker CLI, and
do not run containers or query AWS. `DOCKER_EXE` can specify an absolute CLI path.
The separate smoke script uses only a disposable local PostgreSQL container,
loopback/random frontend port, and fake credentials; it removes its own containers
and files afterward. It deliberately disables TLS only in that local test.
Build its tags using the commands below with local names
`smart-expense-api:aws-validation`, `smart-expense-api:aws-validation-migrations`,
and `smart-expense-frontend:aws-validation`; never supply RDS or actual secrets.
For schema-only validation, use the public example file as a stand-in env/CA
path (do not run this configuration):

```powershell
$env:ECR_REGISTRY = '739275443630.dkr.ecr.us-east-1.amazonaws.com'
$env:IMAGE_TAG = 'validation'
$env:RUNTIME_ENV_FILE = (Resolve-Path .env.example).Path
$env:RDS_CA_FILE = (Resolve-Path .env.example).Path
docker compose --env-file infra/aws/deployment/deployment.env.example -f infra/aws/deployment/compose.yaml --profile tools config --quiet
Remove-Item Env:RUNTIME_ENV_FILE, Env:RDS_CA_FILE, Env:ECR_REGISTRY, Env:IMAGE_TAG
```

Use `config --quiet` with actual runtime files; ordinary `config`, JSON config,
`docker inspect`, debug tracing, and environment dumps can reveal secrets.
`.dockerignore` excludes infrastructure and Terraform artifacts from backend
build contexts. No credentials/secrets are build arguments or copied into images.

## Future build and push (not executed during preparation)

From the root, build for the EC2's x86_64 architecture:

```powershell
$registry = '739275443630.dkr.ecr.us-east-1.amazonaws.com'
$release = 'demo-001'
docker build --platform linux/amd64 --target api -f backend/Dockerfile -t "${registry}/smart-expense-api:${release}" .
docker build --platform linux/amd64 --target migrations -f backend/Dockerfile -t "${registry}/smart-expense-api:${release}-migrations" .
docker build --platform linux/amd64 --target runtime -f frontend/Dockerfile -t "${registry}/smart-expense-frontend:${release}" frontend

# Confirm account 739275443630 before authenticating/pushing.
aws sts get-caller-identity --profile smart-expense-demo --region us-east-1
aws ecr get-login-password --profile smart-expense-demo --region us-east-1 | docker login --username AWS --password-stdin $registry
docker push "${registry}/smart-expense-api:${release}"
docker push "${registry}/smart-expense-api:${release}-migrations"
docker push "${registry}/smart-expense-frontend:${release}"
docker logout $registry
```

Stop on any failing build/login/push; do not deploy a partly uploaded release.
Buildx provenance/manifest metadata may consume additional ECR entries. Review
repository retention and local EC2 disk space before accumulating releases.

## Prepare and later send via SSM

First generate a reviewable, non-secret payload **without contacting AWS**:

```powershell
./infra/aws/deployment/deploy-via-ssm.ps1 -ConfigPath C:/terraform/smart-expense-demo/deployment.env -ParametersPath C:/terraform/smart-expense-demo/ssm-deployment.json
```

After separate approval and all pushes, send it explicitly:

```powershell
./infra/aws/deployment/deploy-via-ssm.ps1 -ConfigPath C:/terraform/smart-expense-demo/deployment.env -ParametersPath C:/terraform/smart-expense-demo/ssm-deployment.json -InstanceId i-0604d67bb952209ae -Profile smart-expense-demo -Execute
```

The sender first verifies account identity, then uses `aws ssm send-command`
with `AWS-RunShellScript`. It embeds only scripts, Compose, and non-secret inputs
as Base64, writes a unique root-only release directory under
`/opt/smart-expense/releases/`, and invokes `bash deploy.sh deployment.env`.
No clone, S3 bucket, SSH, access key, or remote Git operation is needed.

The EC2 script, serialized by a host lock, executes this sequence:

1. Validate inputs/tools and use the EC2 instance profile, not a local profile.
2. Authenticate ECR into a temporary `/run` Docker config; pull all three tags.
3. Download the public regional RDS CA bundle over HTTPS to a persistent public
   certificate file under `/opt/smart-expense/certificates/`.
4. Fetch the two exact SecureStrings with decryption. Pipe JSON directly to
   `prepare-runtime.py`; no secret values are shell arguments or printed.
5. Create a mode-0600 raw env file in a root-only temporary `/run` directory.
   Quote Npgsql connection values safely, preserving special characters.
   Use `SSL Mode=VerifyFull` and `Root Certificate=/run/rds-ca.pem`.
6. Quietly validate Compose, then `docker compose run --rm --no-deps migrations`.
   Migration output is suppressed and its Docker logging driver is `none`.
   If migrations fail, do not start/update API or frontend.
7. `docker compose up -d --force-recreate --remove-orphans --wait --wait-timeout 180 api frontend`.
   The stable project name is `smart-expense-aws`; only its obsolete containers
   are eligible for orphan removal. Recreate nginx so it resolves the new API.
8. Verify both container health checks, host HTTP `/`, and unauthenticated
   `/api/transactions` returning 401 through nginx. Delete temporary env/token
   files via the exit trap on success or failure.

Monitor the returned command ID (submission alone is not deployment success):

```powershell
aws ssm get-command-invocation --profile smart-expense-demo --region us-east-1 --command-id <COMMAND_ID> --instance-id i-0604d67bb952209ae --query '{Status:Status,ResponseCode:ResponseCode}' --no-cli-pager
```

Repeat until a terminal status. Require `Success` with response code 0. SSM
execution timeout is 900 seconds. Verify `/` in the browser and perform a manual
disposable-user login/category/transaction smoke test only after deployment is
approved. These health checks are not full business/database readiness tests;
successful migrations validate database access, not every future query.

## Secrets, failure modes, and remaining demo risks

- Secrets are absent from source, image layers, payload, user data, and scripted
  logs. They reside transiently in `/run` and in Docker's container environment
  metadata. Root/Docker administrators can inspect them; Compose is not a secret
  vault. Never run `set -x`, print config, retrieve SSM values to logs, or publish
  container inspection output. Default application logging must remain free of
  sensitive-data logging. No automatic diagnostic dump on failure.
- Container restarts retain their environment. After host reboot the temporary
  file is absent; rerun the deployment to recreate containers or rotate secrets.
  The public CA file persists for automatic container restarts.
- This is an in-place deployment with possible downtime, not rolling/atomic.
  Existing containers remain during migrations; future migrations must be
  backward-compatible or be deployed during a deliberate maintenance window.
  No automatic DB rollback. Never delete volumes/databases or use global prune.
- On migration failure, investigate privately in a controlled session without
  logging secrets. On startup/health failure the new release may be partly
  running; fix and rerun. Rolling back image tags does not roll back schema.
- On `t3.micro`, SDK/EF migrations can pressure RAM, especially with an existing
  API running. All builds happen locally, not on EC2. Check disk space before
  pulling; old images/releases are retained for manual rollback, not pruned.
- Port 80 is HTTP-only: use disposable demo accounts/data. Do not send real
  financial data or reused passwords. TLS termination is future separate work.
- The initial RDS master username is used for this disposable first deployment.
  A separate least-privilege runtime database role is future deliberate work;
  do not treat this master-user setup as production-ready.
- Existing EC2/RDS/EBS/IPv4/ECR continue accruing costs; deployments do not alter
  Budget limits or stop resources. No Terraform apply/destroy is part of this flow.

References: [Compose raw env files](https://docs.docker.com/reference/compose-file/services/#format),
[Npgsql TLS verification](https://www.npgsql.org/doc/security), and
[RDS certificate bundles](https://docs.aws.amazon.com/AmazonRDS/latest/UserGuide/UsingWithRDS.SSL.html).
