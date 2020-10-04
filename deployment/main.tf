# vim: ft=hcl

terraform {
  required_providers {
    azurerm = {
      source = "hashicorp/azurerm"
      version = ">= 2.26"
    }
  }
}

variable "app_name" {
  type        = string
  description = "The name for the app service (e.g. filemanager)"
  validation {
    condition     = can(regex("^[a-z0-9][-a-z0-9]*[a-z0-9]$", var.app_name))
    error_message = "The web app name can contain only dashes, lowercase letters, and numbers. It must be at least 2 characters and can neither start nor end with a dash."
  }
}

variable "storage_name" {
  type        = string
  description = "The name for the storage account (e.g. filemanagerstorage)"
  validation {
    condition     = can(regex("^[a-z0-9]{3,24}$", var.storage_name))
    error_message = "The storage account name can contain only lowercase letters and numbers. It must be between 3 and 24 characters."
  }
}

variable "app_service_plan_tier" {
  type    = string
  default = "Basic"
}

variable "app_service_plan_size" {
  type    = string
  default = "B1"
}

variable "region" {
  type    = string
  default = "westus2"
}

provider "azurerm" {
  features {}
}

resource "azurerm_resource_group" "rg" {
  name     = "${var.app_name}-rg"
  location = var.region
}

resource "azurerm_storage_account" "blob" {
  name                     = var.storage_name
  resource_group_name      = azurerm_resource_group.rg.name
  location                 = azurerm_resource_group.rg.location
  min_tls_version          = "TLS1_2"
  account_tier             = "Standard"
  account_replication_type = "LRS"
}

resource "azurerm_storage_container" "blob" {
  name                  = "files"
  storage_account_name  = azurerm_storage_account.blob.name
  container_access_type = "private"
}

resource "azurerm_app_service_plan" "asp" {
  name                = "${var.app_name}-asp"
  location            = azurerm_resource_group.rg.location
  kind                = "Linux"
  reserved            = true
  resource_group_name = azurerm_resource_group.rg.name

  sku {
    tier = var.app_service_plan_tier
    size = var.app_service_plan_size
  }
}

resource "azurerm_app_service" "app" {
  name                = var.app_name
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  app_service_plan_id = azurerm_app_service_plan.asp.id

  site_config {
    use_32_bit_worker_process = false
    ftps_state                = "Disabled"
    min_tls_version           = "1.2"
  }

  https_only   = true

  identity {
    type = "SystemAssigned"
  }

  app_settings = {
    "Storage__Account"   = azurerm_storage_account.blob.name
    "Storage__Container" = azurerm_storage_container.blob.name
  }
}

resource "azurerm_role_assignment" "access" {
  scope                = azurerm_storage_container.blob.resource_manager_id
  role_definition_name = "Storage Blob Data Contributor"
  principal_id         = azurerm_app_service.app.identity[0].principal_id
}

