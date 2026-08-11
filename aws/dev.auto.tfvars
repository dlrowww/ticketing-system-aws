# Public DNS configuration for the development environment.
route53_zone_id         = "Z049011621PJFDI20HJIY"
application_domain_name = "tickets.xuecoding.com"

# Route the application hostname to the internet-facing ALB created by the
# Kubernetes AWS Load Balancer Controller.
create_route53_alb_record = true
alb_dns_name              = "k8s-ticketin-ticketin-5f542d1eaa-1138980156.us-east-1.elb.amazonaws.com"
alb_zone_id               = "Z35SXDOTRQ7X7K"
