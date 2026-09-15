variable "name" { type = string }
variable "location" { type = string }
variable "failover_location" { type = string }
variable "resource_group_name" { type = string }
variable "tags" { type = map(string) }

resource "azurerm_cosmosdb_account" "this" {
  name                = "cosmos-${var.name}"
  location            = var.location
  resource_group_name = var.resource_group_name
  offer_type          = "Standard"
  kind                = "GlobalDocumentDB"
  automatic_failover_enabled = true
  multiple_write_locations_enabled = false
  consistency_policy {
    consistency_level = "Session"
  }
  geo_location {
    location          = var.location
    failover_priority = 0
    zone_redundant    = true
  }
  geo_location {
    location          = var.failover_location
    failover_priority = 1
  }
  backup {
    type                = "Continuous"
    tier                = "Continuous7Days"
  }
  tags = var.tags
}

resource "azurerm_cosmosdb_sql_database" "car" {
  name                = "car"
  resource_group_name = var.resource_group_name
  account_name        = azurerm_cosmosdb_account.this.name
}

resource "azurerm_cosmosdb_sql_container" "conversations" {
  name                = "conversations"
  resource_group_name = var.resource_group_name
  account_name        = azurerm_cosmosdb_account.this.name
  database_name       = azurerm_cosmosdb_sql_database.car.name
  partition_key_path = "/sessionId"
  throughput          = 400
}

output "endpoint" { value = azurerm_cosmosdb_account.this.endpoint }
