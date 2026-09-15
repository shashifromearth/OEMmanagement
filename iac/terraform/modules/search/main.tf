variable "name" { type = string }
variable "location" { type = string }
variable "resource_group_name" { type = string }
variable "tags" { type = map(string) }

resource "azurerm_search_service" "this" {
  name                = substr("srch${replace(var.name, "-", "")}", 0, 60)
  location            = var.location
  resource_group_name = var.resource_group_name
  sku                 = "standard"
  replica_count       = 3
  partition_count     = 1
  semantic_search_sku = "standard"
  tags                = var.tags
}

output "endpoint" { value = "https://${azurerm_search_service.this.name}.search.windows.net" }
