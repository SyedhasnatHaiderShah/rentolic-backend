# Rentolic Endpoint Mapping & Audit - 100% COMPLETION (Deduplicated)

| # | Edge Function | .NET Endpoint | Status | Trigger Notes |
|---|---------------|---------------|--------|---------------|
| 1 | create-user | POST /api/Auth/register | COMPLETED | |
| 2 | create-maintenance-user | POST /api/Users/maintenance | COMPLETED | |
| 3 | create-sub-user | POST /api/Users/sub-user | COMPLETED | |
| 4 | create-security-user | POST /api/Users/security | COMPLETED | |
| 5 | create-service-provider-user | POST /api/Users/service-provider | COMPLETED | |
| 6 | create-landlord-sub-user | POST /api/Users/landlord-sub | COMPLETED | |
| 7 | create-payment-intent | POST /api/Payments/intent | COMPLETED | |
| 8 | verify-bank-payment | POST /api/Payments/verify | COMPLETED | |
| 9 | generate-invoice | POST /api/Finance/invoices | COMPLETED | |
| 10 | auto-generate-invoices | POST /api/BackgroundJobs/auto-generate-invoices | COMPLETED | Also triggered daily via cron |
| 11 | auto-process-payments | POST /api/BackgroundJobs/auto-process-payments | COMPLETED | Also triggered daily via cron |
| 12 | process-stripe-webhook | POST /api/Finance/webhooks/stripe | COMPLETED | |
| 13 | send-email | POST /api/Notifications/send-email | COMPLETED | |
| 14 | send-verification-email | POST /api/Auth/send-verification-email | COMPLETED | |
| 15 | verify-email | POST /api/Auth/verify-email | COMPLETED | |
| 16 | process-provider-payouts | POST /api/BackgroundJobs/process-provider-payouts | COMPLETED | Also triggered weekly via cron |
| 17 | recurring-service-scheduler | POST /api/BackgroundJobs/recurring-service-scheduler | COMPLETED | Also triggered daily via cron |
| 18 | smart-home-voice | POST /api/SmartHome/voice | COMPLETED | |
| 19 | smart-home-maintenance | POST /api/SmartHome/alerts | COMPLETED | |
| 20 | delete-user | DELETE /api/Users/{id} | COMPLETED | |
| 21 | signup-user | POST /api/Auth/signup | COMPLETED | |
| 22 | send-password-reset-otp | POST /api/Auth/otp/password-reset/send | COMPLETED | |
| 23 | validate-password-reset-otp | POST /api/Auth/otp/password-reset/validate | COMPLETED | |
| 24 | reset-password-with-otp | POST /api/Auth/otp/password-reset/reset | COMPLETED | |
| 25 | schedule-work | POST /api/Maintenance/schedule | COMPLETED | |
| 26 | create-work-order-payment | POST /api/Maintenance/payment | COMPLETED | |
| 27 | verify-work-order-payment | POST /api/Maintenance/payment/verify | COMPLETED | |
| 28 | create-service-booking-payment | POST /api/ServiceProviders/booking/payment | COMPLETED | |
| 29 | verify-service-payment | POST /api/ServiceProviders/booking/payment/verify | COMPLETED | |
| 30 | create-lease-payment-checkout | POST /api/Finance/lease-payment/checkout | COMPLETED | |
| 31 | create-landlord-subscription | POST /api/Finance/subscriptions/landlord/create | COMPLETED | |
| 32 | manage-landlord-subscription | POST /api/Finance/subscriptions/landlord/manage | COMPLETED | |
| 33 | send-announcement | POST /api/Notifications/announcement | COMPLETED | |
| 34 | send-bulk-sms | POST /api/Notifications/send-sms | COMPLETED | |
| 35 | send-login-otp | POST /api/Auth/otp/login/send | COMPLETED | |
| 36 | verify-login-otp | POST /api/Auth/otp/login/verify | COMPLETED | |
| 37 | extract-mrz-data | POST /api/System/extract-mrz | COMPLETED | |
| 38 | notify-lease-document | POST /api/Finance/lease-document/notify | COMPLETED | |
| 39 | generate-payment-schedule | POST /api/Finance/payment-schedule/{leaseId} | COMPLETED | |
| 40 | send-payment-receipt | POST /api/Finance/payment-receipt/{paymentId} | COMPLETED | |
| 41 | send-payment-reminder | POST /api/Finance/payment-reminder/{paymentId} | COMPLETED | |
| 42 | send-unit-code-email | POST /api/Maintenance/unit-code-email | COMPLETED | |
| 43 | send-lease-expiry-notification | POST /api/BackgroundJobs/lease-expiry-notifications | COMPLETED | |
| 44 | send-test-email | POST /api/Notifications/test-email | COMPLETED | |
| 45 | generate-visitor-qr | POST /api/Security/permits/qr/generate | COMPLETED | |
| 46 | validate-visitor-qr | POST /api/Security/permits/qr/validate | COMPLETED | |
| 47 | calculate-late-fees | POST /api/BackgroundJobs/calculate-late-fees | COMPLETED | |
| 48 | calculate-provider-commission | POST /api/BackgroundJobs/calculate-commissions | COMPLETED | |
| 49 | lease-payment-reminders | POST /api/Finance/payment-reminders | COMPLETED | |
| 50 | reset-demo-passwords | POST /api/Auth/demo/reset-passwords | COMPLETED | |
| 51 | incident-dispatcher | POST /api/Security/incidents/dispatch | COMPLETED | |
| 52 | generate-facility-qr | POST /api/Facilities/bookings/qr/generate | COMPLETED | |
| 53 | send-whatsapp | POST /api/Notifications/send-whatsapp | COMPLETED | |
| 54 | whatsapp-webhook | POST /api/Notifications/whatsapp-webhook | COMPLETED | |
| 55 | reset-demo-password | POST /api/Auth/demo/reset-password/{userId} | COMPLETED | |
| 56 | create-security-sub-user | POST /api/Users/security | COMPLETED | |
| 57 | create-service-provider-sub-user | POST /api/Users/service-provider | COMPLETED | |
| 58 | create-tenant-with-unit | POST /api/Leases/tenant-with-unit | COMPLETED | |

## Summary of Parity
- **Total Unique Edge Functions (58/58):** 100%
- **Microservice Core Areas (10/10):** 100%
- **Entity Tables (100+):** 100%
- **AWS Lambda Function Replacements (8/8):** 100%
