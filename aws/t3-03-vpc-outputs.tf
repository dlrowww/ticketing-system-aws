
output "vpc_id" {
  description = "The ID of the VPC"
  value       = module.vpc.vpc_id
}

output "vpc_cidr_block" {
  description = "The CIDR block of the VPC"
  value       = module.vpc.vpc_cidr_block
}

output "vpc_public_subnets" {
  description = "The list of public subnet IDs"
  value       = module.vpc.public_subnets
}

output "vpc_private_subnets" {
  description = "The list of private subnet IDs"
  value       = module.vpc.private_subnets
}

output "vpc_database_subnets" {
  description = "The list of database subnet IDs"
  value       = module.vpc.database_subnets
}

output "nat_public_ips" {
  description = "The public IP address of the NAT Gateway"
  value       = module.vpc.nat_public_ips
}

output "azs" {
  description = "The list of availability zones used in the VPC"
  value       = module.vpc.azs
}