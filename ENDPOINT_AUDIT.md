# Rentolic Endpoint Mapping & Audit

| # | Edge Function | .NET Endpoint | Status |
|---|---------------|---------------|--------|
| 1 | create-user | POST /api/Auth/register | Implemented |
| 2 | create-maintenance-user | POST /api/Users/maintenance | Missing |
| 3 | create-sub-user | POST /api/Users/sub-user | Implemented |
| 4 | create-security-user | POST /api/Users/security | Missing |
| 5 | create-service-provider-user | POST /api/Users/service-provider | Missing |
| 6 | create-landlord-sub-user | POST /api/Users/landlord-sub | Missing |
| 7 | create-payment-intent | POST /api/Payments/intent | Implemented |
| 8 | verify-bank-payment | POST /api/Payments/verify | Implemented |
| 9 | generate-invoice | POST /api/Finance/invoices | Implemented |
| 10 | auto-generate-invoices | POST /api/BackgroundJobs/auto-generate-invoices | Missing |
| 11 | auto-process-payments | POST /api/BackgroundJobs/auto-process-payments | Missing |
| 12 | process-stripe-webhook | POST /api/Finance/webhooks/stripe | Implemented |
| 13 | send-email | POST /api/Notifications/send-email | Implemented |
| 14 | send-verification-email | POST /api/Auth/send-verification-email | Implemented |
| 15 | verify-email | POST /api/Auth/verify-email | Implemented |
| 16 | process-provider-payouts | POST /api/ServiceProviders/payouts | Implemented |
| 17 | recurring-service-scheduler | POST /api/BackgroundJobs/recurring-service-scheduler | Missing |
| 18 | smart-home-voice | POST /api/SmartHome/voice | Implemented |
| 19 | smart-home-maintenance | POST /api/SmartHome/alerts | Implemented |
| 20 | delete-user | DELETE /api/Users/{id} | Implemented |
| 21 | signup-user | POST /api/Auth/signup | Implemented |
| 22 | send-password-reset-otp | POST /api/Auth/otp/password-reset/send | Implemented |
| 23 | validate-password-reset-otp | POST /api/Auth/otp/password-reset/validate | Implemented |
| 24 | reset-password-with-otp | POST /api/Auth/otp/password-reset/reset | Implemented |
| 25 | schedule-work | POST /api/Maintenance/schedule | Implemented |
| 26 | create-work-order-payment | POST /api/Maintenance/payment | Implemented |
| 27 | verify-work-order-payment | POST /api/Maintenance/payment/verify | Missing |
| 28 | create-service-booking-payment | POST /api/ServiceProviders/booking/payment | Implemented |
| 29 | verify-service-payment | POST /api/ServiceProviders/booking/payment/verify | Missing |
| 30 | create-lease-payment-checkout | POST /api/Finance/lease-payment/checkout | Implemented |
| 31 | create-landlord-subscription | POST /api/Finance/subscriptions/landlord/create | Missing |
| 32 | manage-landlord-subscription | POST /api/Finance/subscriptions/landlord/manage | Missing |
| 33 | send-announcement | POST /api/Notifications/announcement | Implemented |
| 34 | send-bulk-sms | POST /api/Notifications/send-sms | Implemented |
| 35 | send-login-otp | POST /api/Auth/otp/login/send | Implemented |
| 36 | verify-login-otp | POST /api/Auth/otp/login/verify | Implemented |
| 37 | extract-mrz-data | POST /api/System/extract-mrz | Implemented |
| 38 | notify-lease-document | POST /api/Finance/lease-document/notify | Missing |
| 39 | generate-payment-schedule | POST /api/Finance/payment-schedule/{leaseId} | Implemented |
| 40 | send-payment-receipt | POST /api/Finance/payment-receipt/{paymentId} | Missing |
| 41 | send-payment-reminder | POST /api/Finance/payment-reminder/{paymentId} | Missing |
| 42 | send-unit-code-email | POST /api/Maintenance/unit-code-email | Missing |
| 43 | send-lease-expiry-notification | POST /api/BackgroundJobs/lease-expiry-notifications | Missing |
| 44 | send-test-email | POST /api/Notifications/test-email | Missing |
| 45 | generate-visitor-qr | POST /api/Security/permits/qr/generate | Implemented |
| 46 | validate-visitor-qr | POST /api/Security/permits/qr/validate | Implemented |
| 47 | auto-generate-invoices (Daily) | (Triggered via Background Job) | Missing |
| 48 | auto-process-payments (Daily) | (Triggered via Background Job) | Missing |
| 49 | calculate-late-fees | POST /api/BackgroundJobs/calculate-late-fees | Missing |
| 50 | calculate-provider-commission | POST /api/BackgroundJobs/calculate-commissions | Missing |
| 51 | lease-payment-reminders | POST /api/Finance/payment-reminders | Implemented |
| 52 | recurring-service-scheduler | (Triggered via Background Job) | Missing |
| 53 | process-provider-payouts (Weekly) | (Triggered via Background Job) | Missing |
| 54 | reset-demo-passwords | POST /api/Auth/demo/reset-passwords | Missing |
| 55 | incident-dispatcher | POST /api/Security/incidents/dispatch | Missing |
| 56 | generate-facility-qr | POST /api/Facilities/bookings/qr/generate | Implemented |
| 57 | send-whatsapp | POST /api/Notifications/send-whatsapp | Implemented |
| 58 | whatsapp-webhook | POST /api/Notifications/whatsapp-webhook | Missing |
| 59 | smart-home-maintenance | (Part of SmartHome Controller) | Implemented |
| 60 | smart-home-voice | (Part of SmartHome Controller) | Implemented |
| 61 | reset-demo-password | POST /api/Auth/demo/reset-password/{userId} | Missing |
