#!/bin/bash
# Run only on the EC2 host through an explicitly authorized SSM command.
set +x
set -euo pipefail
umask 077

[[ $EUID -eq 0 ]] || { echo 'Deployment requires root.' >&2; exit 1; }
for tool in aws docker python3 curl flock mktemp; do
  command -v "$tool" >/dev/null || { echo 'Missing deployment prerequisite.' >&2; exit 1; }
done
script_dir=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)
config_file=${1:?Supply the non-secret deployment.env file}

# Read allowlisted literal values, never source/eval configuration as shell code.
while IFS= read -r line || [[ -n $line ]]; do
  line=${line%$'\r'}
  [[ -z $line || $line == \#* ]] && continue
  [[ $line == *=* ]] || { echo 'Invalid deployment config.' >&2; exit 1; }
  config_key=${line%%=*}
  case "$config_key" in
    AWS_REGION|ECR_REGISTRY|IMAGE_TAG|RDS_HOST|RDS_PORT|RDS_DATABASE|RDS_USERNAME|DB_PASSWORD_PARAMETER|JWT_SIGNING_KEY_PARAMETER)
      export "$line" ;;
    *) echo 'Unsupported deployment config key.' >&2; exit 1 ;;
  esac
done < "$config_file"

: "${AWS_REGION:?}" "${ECR_REGISTRY:?}" "${IMAGE_TAG:?}" "${RDS_HOST:?}" "${RDS_PORT:?}" "${RDS_DATABASE:?}" "${RDS_USERNAME:?}" "${DB_PASSWORD_PARAMETER:?}" "${JWT_SIGNING_KEY_PARAMETER:?}"
[[ $AWS_REGION == us-east-1 && $ECR_REGISTRY =~ ^[0-9]{12}\.dkr\.ecr\.us-east-1\.amazonaws\.com$ && $IMAGE_TAG =~ ^[A-Za-z0-9_][A-Za-z0-9_.-]{0,99}$ && $RDS_HOST =~ ^[A-Za-z0-9.-]+\.us-east-1\.rds\.amazonaws\.com$ && $RDS_HOST != replace-* && $RDS_PORT == 5432 ]] || { echo 'Invalid deployment inputs.' >&2; exit 1; }
[[ $DB_PASSWORD_PARAMETER == /smart-expense/demo/db-password && $JWT_SIGNING_KEY_PARAMETER == /smart-expense/demo/jwt-signing-key ]] || { echo 'Unexpected parameter names.' >&2; exit 1; }

# Use the EC2 instance profile, never workstation credentials/profile.
unset AWS_PROFILE AWS_DEFAULT_PROFILE AWS_ACCESS_KEY_ID AWS_SECRET_ACCESS_KEY AWS_SESSION_TOKEN
export AWS_DEFAULT_REGION=$AWS_REGION AWS_EC2_METADATA_DISABLED=false AWS_PAGER=''
exec 9>/run/smart-expense-deploy.lock
flock -n 9 || { echo 'Another deployment is running.' >&2; exit 1; }
runtime_dir=$(mktemp -d /run/smart-expense-deploy.XXXXXX)
export RUNTIME_ENV_FILE=$runtime_dir/runtime.env
export DOCKER_CONFIG=$runtime_dir/docker
# This public CA bundle must remain available for container restarts.
install -d -m 0755 /opt/smart-expense/certificates
export RDS_CA_FILE=/opt/smart-expense/certificates/us-east-1-bundle.pem
cleanup() {
  rm -f "$RUNTIME_ENV_FILE" "$DOCKER_CONFIG/config.json" "$runtime_dir/rds-ca.pem"
  rmdir "$DOCKER_CONFIG" "$runtime_dir" 2>/dev/null || true
}
trap cleanup EXIT
mkdir -m 0700 "$DOCKER_CONFIG"
compose=(docker compose --project-name smart-expense-aws --env-file "$config_file" -f "$script_dir/compose.yaml")

echo 'Authenticating and pulling release images.'
aws ecr get-login-password --region "$AWS_REGION" | docker login --username AWS --password-stdin "$ECR_REGISTRY" >/dev/null 2>&1
docker pull "$ECR_REGISTRY/smart-expense-api:$IMAGE_TAG"
docker pull "$ECR_REGISTRY/smart-expense-api:$IMAGE_TAG-migrations"
docker pull "$ECR_REGISTRY/smart-expense-frontend:$IMAGE_TAG"
curl --fail --silent --show-error --location --retry 3 'https://truststore.pki.rds.amazonaws.com/us-east-1/us-east-1-bundle.pem' -o "$runtime_dir/rds-ca.pem"
grep -q -- '-----BEGIN CERTIFICATE-----' "$runtime_dir/rds-ca.pem"
install -m 0644 "$runtime_dir/rds-ca.pem" "$RDS_CA_FILE"

echo 'Preparing private runtime configuration.'
aws ssm get-parameters --region "$AWS_REGION" --names "$DB_PASSWORD_PARAMETER" "$JWT_SIGNING_KEY_PARAMETER" --with-decryption --output json | python3 "$script_dir/prepare-runtime.py" "$RUNTIME_ENV_FILE"
"${compose[@]}" --profile tools config --quiet
echo 'Applying existing EF migrations; diagnostic output withheld.'
# No migration stdout/stderr in SSM/daemon logs: exceptions can contain details.
if ! "${compose[@]}" run --rm --no-deps migrations >/dev/null 2>&1; then
  echo 'Migrations failed; API/frontend update was not started.' >&2
  exit 1
fi
echo 'Starting API and frontend.'
"${compose[@]}" up -d --force-recreate --remove-orphans --wait --wait-timeout 180 api frontend
curl --fail --silent --show-error --retry 6 --retry-connrefused http://127.0.0.1/ -o /dev/null
proxy_status=$(curl --silent --show-error --output /dev/null --write-out '%{http_code}' http://127.0.0.1/api/transactions)
[[ $proxy_status == 401 ]] || { echo 'Authenticated API proxy check failed.' >&2; exit 1; }
echo 'Deployment healthy: frontend, API health, and authenticated proxy verified.'
# Runtime env and temporary ECR credentials are deleted by the EXIT trap.
