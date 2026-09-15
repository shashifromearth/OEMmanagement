variable "name" { type = string }
variable "location" { type = string }
variable "resource_group_name" { type = string }
variable "publisher_email" { type = string }
variable "publisher_name" { type = string }
variable "tags" { type = map(string) }

resource "azurerm_api_management" "this" {
  name                = "apim-${var.name}"
  location            = var.location
  resource_group_name = var.resource_group_name
  publisher_name      = var.publisher_name
  publisher_email     = var.publisher_email
  sku_name            = "Premium_1"
  tags                = var.tags
  identity { type = "SystemAssigned" }
}

resource "azurerm_api_management_api" "agent" {
  name                = "agent-api"
  resource_group_name = var.resource_group_name
  api_management_name = azurerm_api_management.this.name
  revision            = "1"
  display_name        = "CAR Agent API"
  path                = "v1"
  protocols           = ["https"]
}

output "gateway_url" { value = azurerm_api_management.this.gateway_url }
