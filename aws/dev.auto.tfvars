# Public DNS configuration for the development environment.
route53_zone_id         = "Z049011621PJFDI20HJIY"
application_domain_name = "tickets.xuecoding.com"

# Enable this only after the Kubernetes Ingress has created its ALB and the
# generated ALB DNS name and canonical hosted zone ID are known.
create_route53_alb_record = false
