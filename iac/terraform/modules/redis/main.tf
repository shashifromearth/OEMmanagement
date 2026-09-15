variable "name" { type = string }
variable "location" { type = string }
variable "resource_group_name" { type = string }
variable "subnet_id" { type = string }
variable "tags" { type = map(string) }

resource "azurerm_redis_cache" "this" {
  name                 = "redis-${var.name}"
  location             = var.location
  resource_group_name  = var.resource_group_name
  capacity             = 1
  family               = "P"
  sku_name             = "Premium"
  non_ssl_port_enabled = false
  minimum_tls_version  = "1.2"
  replica_count        = 1
  shard_count          = 1
  redis_configuration {
    aof_backup_enabled = true
  }
  tags = var.tags
}

output "hostname" { value = azurerm_redis_cache.this.hostname }
