output "resource_group" {
  value = azurerm_resource_group.main.name
}

output "aks_name" {
  value = module.aks.name
}

output "sql_fqdn" {
  value = module.sql.fqdn
}

output "cosmos_endpoint" {
  value = module.cosmos.endpoint
}

output "search_endpoint" {
  value = module.search.endpoint
}

output "eventhubs_kafka_endpoint" {
  value = module.eventhubs.kafka_endpoint
}

output "apim_gateway_url" {
  value = module.apim.gateway_url
}
