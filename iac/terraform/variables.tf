variable "prefix" {
  type    = string
  default = "car"
}

variable "environment" {
  type    = string
  default = "prod"
}

variable "location" {
  type    = string
  default = "eastus2"
}

variable "failover_location" {
  type    = string
  default = "centralus"
}

variable "tenant_id" {
  type = string
}

variable "sql_admin_login" {
  type    = string
  default = "caradmin"
}

variable "sql_admin_password" {
  type      = string
  sensitive = true
}

variable "publisher_email" {
  type = string
}

variable "publisher_name" {
  type    = string
  default = "CAR Agentic"
}
