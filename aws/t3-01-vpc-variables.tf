variable "vpc_name" {
  description = "The name of the VPC"
  type        = string
  default     = "ticketing-system-vpc"
}

variable "vpc_cidr" {
  description = "The CIDR block for the VPC"
  type        = string
  default     = "10.20.0.0/16"
}

variable "vpc_public_subnets" {
  description = "List of CIDR blocks for public subnets"
  type        = list(string)
  default     = ["10.20.1.0/24", "10.20.2.0/24"]
}

variable "vpc_private_subnets" {
  description = "List of CIDR blocks for private subnets"
  type        = list(string)
  default     = ["10.20.16.0/20", "10.20.32.0/20"]
}

variable "vpc_database_subnets" {
  description = "List of CIDR blocks for database subnets"
  type        = list(string)
  default     = ["10.20.48.0/24", "10.20.49.0/24"]
}

variable "vpc_create_database_subnet_group" {
  description = "Whether to create a database subnet group"
  type        = bool
  default     = true
}

variable "vpc_create_database_subnet_route_table" {
  description = "VPC Create Database Subnet Route Table"
  type        = bool
  default     = true
}

variable "vpc_enable_nat_gateway" {
  description = "Enable NAT Gateways for Private Subnets Outbound Communication"
  type        = bool
  default     = true
}

variable "vpc_single_nat_gateway" {
  description = "Enable only single NAT Gateway in one Availability Zone to save costs during our demos"
  type        = bool
  default     = true
}

variable "vpc_one_nat_gateway_per_az" {
  description = "Enable one NAT Gateway per Availability Zone"
  type        = bool
  default     = false
}