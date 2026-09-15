variable "name" { type = string }
variable "location" { type = string }
variable "resource_group_name" { type = string }
variable "subnet_id" { type = string }
variable "log_analytics_id" { type = string }
variable "tags" { type = map(string) }

resource "azurerm_kubernetes_cluster" "this" {
  name                = "aks-${var.name}"
  location            = var.location
  resource_group_name = var.resource_group_name
  dns_prefix          = var.name
  kubernetes_version  = "1.31.2"
  sku_tier            = "Standard"
  oidc_issuer_enabled = true
  workload_identity_enabled = true
  default_node_pool {
    name                         = "system"
    vm_size                      = "Standard_D4s_v5"
    zones                        = ["1", "2", "3"]
    min_count                    = 3
    max_count                    = 6
    enable_auto_scaling          = true
    vnet_subnet_id               = var.subnet_id
    only_critical_addons_enabled = true
  }
  identity { type = "SystemAssigned" }
  network_profile {
    network_plugin = "azure"
    network_policy = "azure"
    load_balancer_sku = "standard"
    outbound_type  = "loadBalancer"
  }
  oms_agent {
    log_analytics_workspace_id = var.log_analytics_id
  }
  azure_policy_enabled = true
  tags                 = var.tags
}

resource "azurerm_kubernetes_cluster_node_pool" "apps" {
  name                  = "apps"
  kubernetes_cluster_id = azurerm_kubernetes_cluster.this.id
  vm_size               = "Standard_D8s_v5"
  zones                 = ["1", "2", "3"]
  min_count             = 3
  max_count             = 12
  enable_auto_scaling   = true
  vnet_subnet_id        = var.subnet_id
  mode                  = "User"
  tags                  = var.tags
}

output "name" { value = azurerm_kubernetes_cluster.this.name }
output "id" { value = azurerm_kubernetes_cluster.this.id }
