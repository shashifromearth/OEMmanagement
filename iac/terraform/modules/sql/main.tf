variable "name" { type = string }
variable "location" { type = string }
variable "resource_group_name" { type = string }
variable "admin_login" {
  type = string
}

variable "admin_password" {
  type      = string
  sensitive = true
}
variable "subnet_id" { type = string }
variable "tags" { type = map(string) }

resource "azurerm_mssql_server" "this" {
  name                         = "sql-${var.name}"
  resource_group_name          = var.resource_group_name
  location                     = var.location
  version                      = "12.0"
  administrator_login          = var.admin_login
  administrator_login_password = var.admin_password
  minimum_tls_version          = "1.2"
  public_network_access_enabled = false
  tags                         = var.tags
}

resource "azurerm_mssql_database" "ops" {
  name           = "CarOps"
  server_id      = azurerm_mssql_server.this.id
  sku_name       = "S2"
  zone_redundant = false
  short_term_retention_policy {
    retention_days = 14
  }
  long_term_retention_policy {
    weekly_retention  = "P4W"
    monthly_retention = "P12M"
  }
  tags = var.tags
}

output "fqdn" { value = azurerm_mssql_server.this.fully_qualified_domain_name }
output "database_name" { value = azurerm_mssql_database.ops.name }
