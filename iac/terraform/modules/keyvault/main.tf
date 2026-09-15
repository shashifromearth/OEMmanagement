variable "name" { type = string }
variable "location" { type = string }
variable "resource_group_name" { type = string }
variable "tenant_id" { type = string }
variable "tags" { type = map(string) }

resource "azurerm_key_vault" "this" {
  name                        = substr("kv${replace(var.name, "-", "")}", 0, 24)
  location                    = var.location
  resource_group_name         = var.resource_group_name
  tenant_id                   = var.tenant_id
  sku_name                    = "standard"
  purge_protection_enabled    = true
  soft_delete_retention_days  = 90
  enable_rbac_authorization   = true
  public_network_access_enabled = false
  tags                        = var.tags
}

output "id" { value = azurerm_key_vault.this.id }
