# Temporary AWS demo infrastructure

This Terraform root describes a disposable SmartExpense portfolio environment.
This branch creates configuration only: no AWS resources have been provisioned,
no images pushed, and no deployment workflow added.

## Architecture and defaults

Internet traffic reaches port 80 on one public EC2 instance. A later deployment
will run frontend/nginx, API, and the existing EF migration container there,
using private ECR images and RDS PostgreSQL in the same VPC. The API has no public
port. Administration uses Systems Manager (SSM), with no SSH key or port 22.

The dedicated `10.42.0.0/16` VPC contains two `/24` subnets in separate standard
Availability Zones. Both use one Internet Gateway/public route table. EC2 gets
an automatic public IPv4 explicitly; the subnets do not assign public IPs by
default. RDS uses both subnets for its required DB subnet group but has
`publicly_accessible = false` and accepts TCP 5432 only from the EC2 security
group. Security groups are stateful; RDS needs no general outbound rule here.
EC2 permits outbound traffic for package downloads, ECR/SSM HTTPS, DNS, and RDS.

| Component | Demo default |
| --- | --- |
| Region | `us-east-1`, configurable |
| EC2 | `t3.micro`, current AWS-managed AL2023 x86_64 AMI, encrypted 12 GiB gp3 root disk |
| RDS | PostgreSQL 18 (latest available matching minor), `db.t3.micro`, single AZ, encrypted 20 GiB gp3 |
| ECR | Private API/frontend repositories, mutable tags, scan on push, five recent images retained |
| Secrets | Two standard SSM SecureString parameters using the AWS-managed SSM key |
| Budget | Account-wide USD 5/month alert threshold, optional email alerts at 50%, 80%, and 100% actual cost |

There is no NAT Gateway, load balancer, ECS/EKS, Elastic IP allocation, hosted
zone, reserved capacity, Multi-AZ database, or deployment automation. Public IPv4
is still billable even without an Elastic IP. EC2 uses standard CPU credits to
avoid surplus-credit charges; RDS burst credits can still incur charges. A
`t3.micro` has limited RAM: use prebuilt images and run migrations before the
API, then increase `ec2_instance_type` if necessary. Do not build images on EC2.

The public URL is HTTP only. Use disposable demo data/accounts and never real
financial information or reused passwords. HTTPS and a production deployment
are separate work. The URL will not serve an application after Terraform apply
alone: bootstrap installs tools but deploys nothing.

## Prerequisites and authentication

- An AWS account with permission to manage these EC2, VPC, RDS, ECR, IAM, SSM,
  and Budgets resources, plus permission to read the referenced AWS data sources.
- AWS CLI v2 and Terraform `>= 1.10, < 2.0`.
- Current AWS provider 6.x; retain the dependency lock file after initialization.
- A separate, protected **non-synced directory outside this checkout** for
  Terraform state and real variable files. This repository can be under OneDrive.

Use standard AWS credential resolution, preferably a short-lived IAM Identity
Center session. Do not use the root user or put credentials in Terraform:

```shell
aws configure sso --profile smart-expense-demo
aws sso login --profile smart-expense-demo
aws sts get-caller-identity --profile smart-expense-demo
```

Set `AWS_PROFILE=smart-expense-demo` in the shell used for Terraform (PowerShell:
`$env:AWS_PROFILE = 'smart-expense-demo'`). Confirm the target account and region
before planning. Terraform's local provisioning identity is separate from the
future GitHub deploy role, which cannot create infrastructure.

GitHub OIDC providers are account-wide. If one already exists for
`token.actions.githubusercontent.com`, set `github_oidc_provider_arn` to that
provider's ARN. Terraform will reuse it without owning or deleting it. Otherwise
the root creates and owns a provider; do not let unrelated roles depend on that
demo-owned provider if the environment will be destroyed. Fixed demo resource
names support one copy per account/region, not multiple parallel environments.

## Inputs, secrets, and state

Copy `terraform.tfvars.example` to a protected file such as
`<NON_SYNCED_DIRECTORY>/terraform.tfvars`. Adjust non-secret options there.
Supply both required secrets through `TF_VAR_db_password` and
`TF_VAR_jwt_signing_key`, or add them to that protected variable file. Use local
secure input tooling instead of pasting values into terminal history. Neither
secret has a default, and this repository contains no real secret values.

The signing key must contain at least 32 ASCII characters. The RDS password must
meet the validation in `variables.tf`. Both inputs are `sensitive = true` and
become SSM SecureString values at apply time. Neither is included in user data
or outputs. The default SSM KMS key avoids a separate customer-managed key; the
deployment host retrieves exact parameter names using `GetParameter` or
`GetParameters` with decryption. Do not print those values or dump environment
files in deployment logs. Serialize/quote secrets safely when building future
connection strings and container configuration.

**Sensitive does not mean absent from state.** RDS passwords and SSM parameter
values are stored in Terraform state and can also be present in saved plans and
backups. Keep those files outside cloud-synced folders, restrict local access,
and use encrypted local storage. Do not attach them to CI logs, issues, or Git.
The repo ignores state, `.terraform`, tfvars, plan, and crash files. A remote
encrypted state backend is deliberately not provisioned in this demo branch.

## Initialize, review, and provision later

Run these commands from `infra/aws`. Commands below are instructions for a
future deliberate provisioning session; do not run `apply` merely to validate.

```shell
terraform fmt -check
terraform init -backend=false
terraform validate
terraform plan -state="<NON_SYNCED_DIRECTORY>/terraform.tfstate" -var-file="<NON_SYNCED_DIRECTORY>/terraform.tfvars"
terraform apply -state="<NON_SYNCED_DIRECTORY>/terraform.tfstate" -var-file="<NON_SYNCED_DIRECTORY>/terraform.tfvars"
```

This root uses the local backend. Always pass the same explicit `-state` path
for state-reading/writing operations, including destroy; never fall back to a
new empty state. Keep the state until teardown and verification are complete.
The `-state` flags here apply to local state, not a future remote backend.

`init -backend=false` downloads the provider and creates `.terraform` and the
dependency lock file; `validate` does not need AWS credentials or create
resources. `plan` queries AWS and requires authentication. Review every create,
replace, and delete operation before applying. AMI and engine data sources
resolve current region-specific versions during planning, so future plans can
propose updates/replacement; inspect them instead of applying blindly.

Check current PostgreSQL version/class availability before provisioning:

```shell
aws rds describe-db-engine-versions --engine postgres --region us-east-1 --query 'DBEngineVersions[].EngineVersion'
aws rds describe-orderable-db-instance-options --engine postgres --db-instance-class db.t3.micro --region us-east-1 --query 'OrderableDBInstanceOptions[].EngineVersion'
```

Use an available version/class combination if AWS does not offer the defaults
in the selected region. The initial RDS database is named `smartexpense` because
RDS initial database naming excludes underscores; future connection settings
must use the output, not the local Compose database name `smart_expense`.

## IAM and future deployment

The EC2 role attaches `AmazonSSMManagedInstanceCore`, permits ECR authentication
and pulls only from the two repositories, and reads only the two secret
parameters. The managed SSM policy itself allows broad parameter reads, so an
explicit deny excludes all other parameters and disallows recursive path reads.
No broad custom KMS permissions are added. SSM Agent is already present in the
selected standard AL2023 AMI; bootstrap starts it and Docker, installs AWS CLI
only if absent, and installs a pinned, checksum-verified official Compose plugin.
IMDSv2 is required; hop limit 1 keeps normal bridged application containers from
using the instance role. Host SSM commands can retrieve configuration as needed.

The GitHub trust policy requires both exact claims:

```text
aud = sts.amazonaws.com
sub = repo:BoarettoFelipe@135382716/smart-expense@1331218846:ref:refs/heads/main
```

PR subjects, other repositories, branches, and GitHub environment subjects do
not match. A later environment-based deployment must deliberately change that
subject and configure environment protection. Protect `main`: Run Command with
`AWS-RunShellScript` effectively grants root command execution on the one demo
instance, including access to its secrets. It is a deployment privilege, not a
read-only role. No CD workflow or GitHub permission changes are included here.

The deploy role can authenticate to ECR, upload to the two repositories, send
only `AWS-RunShellScript` commands to the exact EC2 ARN, and read command status.
Unavoidable wildcard resource permissions are `ecr:GetAuthorizationToken` and
`ssm:GetCommandInvocation` (these APIs do not support resource scoping), plus the
AWS-managed SSM agent control/channel permissions. Command-status reads can
include output of other known command IDs; deployment must not log secrets.
The parameter-read denies use wildcards only to reduce access. There is no
AdministratorAccess, IAM user/access key, or permission to create resources.

The future cloud container definition must use RDS instead of launching the
local PostgreSQL service, bind nginx to host port 80, pass the two retrieved
secrets at runtime, and run the EF migration image before the API. It can use a
future tag in the API ECR repository for the SDK/migration image; no third ECR
repository is needed. Root Compose remains the local-development definition.

## Outputs and verification after a future apply

```shell
terraform output -state="<NON_SYNCED_DIRECTORY>/terraform.tfstate"
```

Outputs include region, EC2 instance ID/public IP, future application URL,
API/frontend repository URLs, GitHub deploy role ARN, private RDS endpoint,
database name/username, and parameter **names only**. No output reveals secret
values or password-bearing connection strings.

Use the AWS console or read-only CLI commands to verify EC2 status checks, RDS
`available` and `PubliclyAccessible=false`, security group rules, and Systems
Manager managed-node status. Through an authorized administrator's SSM session,
check `cloud-init status --wait`, `systemctl status docker amazon-ssm-agent`,
`docker compose version`, and `aws --version`. GitHub's deployment role has only
Run Command access; it does not grant interactive Session Manager access.
Inspect only parameter metadata when checking that SecureStrings exist. ECR
repositories remain empty until a later deployment. A fresh instance's public
URL is not an application health check until that deployment has run.

## Costs and mandatory teardown

This architecture is **not guaranteed to be free**. EC2, RDS, gp3/EBS storage,
public IPv4, ECR storage, requests/data transfer, and potentially RDS CPU credits
can incur charges regardless of account credits. The budget is account-wide so
it also catches costs outside this tagged stack. An optional email enables
alerts at 50%, 80%, and 100% of the configurable monthly threshold. Billing data
and notifications can be delayed; budgets do not stop resources or cap spending.

Destroy after every temporary demonstration:

```shell
terraform destroy -state="<NON_SYNCED_DIRECTORY>/terraform.tfstate" -var-file="<NON_SYNCED_DIRECTORY>/terraform.tfvars"
```

Teardown intentionally deletes RDS without a final snapshot, retains no automated
backups (retention is zero), deletes the EC2 root volume on termination, and
force-deletes repository images with ECR. All demo financial data and parameters
are lost. Manually created snapshots, AMIs, volumes, image copies, or unrelated
resources outside this state are not removed automatically. A reused OIDC
provider is also intentionally left intact.

**Destroy the entire stack to stop ongoing EC2/RDS/EBS/IPv4 costs.** Deleting or
stopping EC2 alone can leave RDS, EBS, ECR, and other chargeable resources behind.
After destroy, verify completion in the AWS console for the selected account
and region, inspect billing for delayed charges, and remove any manually created
demo artifacts. If destroy partially fails, retain state and fix/retry it. Once
verified, securely dispose of local secret-bearing tfvars/state/plan/backups.

## Reference documentation

- [AWS-managed AL2023 AMI parameters](https://docs.aws.amazon.com/linux/al2023/ug/ec2.html)
- [Preinstalled SSM Agent](https://docs.aws.amazon.com/systems-manager/latest/userguide/ami-preinstalled-agent.html)
- [SSM managed-instance core policy](https://docs.aws.amazon.com/aws-managed-policy/latest/reference/AmazonSSMManagedInstanceCore.html)
- [GitHub OIDC subject restrictions](https://docs.github.com/en/actions/how-tos/secure-your-work/security-harden-deployments/oidc-in-aws)
- [Repository-scoped ECR push permissions](https://docs.aws.amazon.com/AmazonECR/latest/userguide/image-push-iam.html)
- [Docker Compose plugin installation](https://docs.docker.com/compose/install/linux/)
- [Terraform sensitive data and state](https://developer.hashicorp.com/terraform/language/manage-sensitive-data)
