terraform {
  required_version = ">= 1.7.0"
  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 4.14"
    }
    random = {
      source  = "hashicorp/random"
      version = "~> 3.6"
    }
  }
  backend "azurerm" {}
}

provider "azurerm" {
  features {
    key_vault {
      purge_soft_delete_on_destroy = false
    }
  }
}

locals {
  name = "${var.prefix}-${var.environment}"
  tags = {
    system      = "car-agentic"
    environment = var.environment
    slo         = "99.9"
  }
}

resource "azurerm_resource_group" "main" {
  name     = "rg-${local.name}"
  location = var.location
  tags     = local.tags
}

module "network" {
  source              = "./modules/network"
  name                = local.name
  location            = var.location
  resource_group_name = azurerm_resource_group.main.name
  tags                = local.tags
}

module "keyvault" {
  source              = "./modules/keyvault"
  name                = local.name
  location            = var.location
  resource_group_name = azurerm_resource_group.main.name
  tenant_id           = var.tenant_id
  tags                = local.tags
}

module "monitor" {
  source              = "./modules/monitor"
  name                = local.name
  location            = var.location
  resource_group_name = azurerm_resource_group.main.name
  tags                = local.tags
}

module "sql" {
  source              = "./modules/sql"
  name                = local.name
  location            = var.location
  resource_group_name = azurerm_resource_group.main.name
  admin_login         = var.sql_admin_login
  admin_password      = var.sql_admin_password
  subnet_id           = module.network.data_subnet_id
  tags                = local.tags
}

module "cosmos" {
  source              = "./modules/cosmos"
  name                = local.name
  location            = var.location
  failover_location   = var.failover_location
  resource_group_name = azurerm_resource_group.main.name
  tags                = local.tags
}

module "redis" {
  source              = "./modules/redis"
  name                = local.name
  location            = var.location
  resource_group_name = azurerm_resource_group.main.name
  subnet_id           = module.network.data_subnet_id
  tags                = local.tags
}

module "search" {
  source              = "./modules/search"
  name                = local.name
  location            = var.location
  resource_group_name = azurerm_resource_group.main.name
  tags                = local.tags
}

module "openai" {
  source              = "./modules/openai"
  name                = local.name
  location            = var.location
  resource_group_name = azurerm_resource_group.main.name
  tags                = local.tags
}

module "eventhubs" {
  source              = "./modules/eventhubs"
  name                = local.name
  location            = var.location
  resource_group_name = azurerm_resource_group.main.name
  tags                = local.tags
}

module "aks" {
  source              = "./modules/aks"
  name                = local.name
  location            = var.location
  resource_group_name = azurerm_resource_group.main.name
  subnet_id           = module.network.aks_subnet_id
  log_analytics_id    = module.monitor.log_analytics_id
  tags                = local.tags
}

module "apim" {
  source              = "./modules/apim"
  name                = local.name
  location            = var.location
  resource_group_name = azurerm_resource_group.main.name
  publisher_email     = var.publisher_email
  publisher_name      = var.publisher_name
  tags                = local.tags
}
