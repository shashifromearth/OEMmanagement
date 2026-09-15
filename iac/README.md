# Infrastructure as Code

All deployment assets live under `iac/`.

```
iac/
  docker/           local compose + Dockerfiles
  kubernetes/       AKS manifests, HPA, PDB, Kafka RF=3
  terraform/        Azure: AKS, SQL, Cosmos, Redis, Search, OpenAI, Event Hubs, APIM, Monitor
```

## Terraform

```bash
cd iac/terraform
terraform init
terraform plan -var-file=environments/prod.tfvars \
  -var tenant_id=$ARM_TENANT_ID \
  -var sql_admin_password=$SQL_PASSWORD
terraform apply -var-file=environments/prod.tfvars
```

Backend is Azure Storage (`backend "azurerm"`). Configure in CI.

Event Hubs exposes **Kafka protocol** (`*.servicebus.windows.net:9093`) so app code keeps `Confluent.Kafka`. In-cluster Kafka StatefulSet is for non-Azure or burst buffering.

## Kubernetes

```bash
kubectl apply -k iac/kubernetes
```

Minimum 3 replicas + PDB minAvailable 2 on gateway, orchestrator, agent-runtime, Kafka.
