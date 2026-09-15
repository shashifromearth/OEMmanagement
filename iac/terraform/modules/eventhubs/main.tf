variable "name" { type = string }
variable "location" { type = string }
variable "resource_group_name" { type = string }
variable "tags" { type = map(string) }

resource "azurerm_eventhub_namespace" "this" {
  name                = "evhns-${var.name}"
  location            = var.location
  resource_group_name = var.resource_group_name
  sku                 = "Standard"
  capacity            = 2
  zone_redundant      = true
  auto_inflate_enabled = true
  maximum_throughput_units = 8
  tags                = var.tags
}

resource "azurerm_eventhub" "topics" {
  for_each            = toset(["agent.turns", "agent.requests", "agent.failures", "tool.executions", "workorders.scheduled", "audit.logs", "ingestion.jobs"])
  name                = replace(each.value, ".", "-")
  namespace_name      = azurerm_eventhub_namespace.this.name
  resource_group_name = var.resource_group_name
  partition_count     = 6
  message_retention   = 7
}

output "kafka_endpoint" {
  value = "${azurerm_eventhub_namespace.this.name}.servicebus.windows.net:9093"
}
