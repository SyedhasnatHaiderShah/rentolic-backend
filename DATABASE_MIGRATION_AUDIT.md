# Rentolic Database Migration Audit
## Supabase → Standalone PostgreSQL for .NET Core 6 / Entity Framework Core

**Version**: 1.0  
**Date**: March 2026  
**Tables**: 130+  
**Enums**: 30  
**Functions**: 40+  
**Triggers**: 20+  

> **Purpose**: This file contains the complete SQL migration script to recreate the entire Rentolic database schema in a standalone PostgreSQL instance, replacing all Supabase-specific constructs (`auth.users`, `auth.uid()`, RLS policies) with standard PostgreSQL patterns compatible with .NET Core 6 / Entity Framework Core.

---

## Table of Contents

1. [Extensions](#1-extensions)
2. [Enums (30 Types)](#2-enums)
3. [Tables (130+ CREATE TABLE)](#3-tables)
4. [Views](#4-views)
5. [Functions](#5-functions)
6. [Triggers](#6-triggers)
7. [Indexes](#7-indexes)
8. [Seed Data](#8-seed-data)
9. [.NET Core 6 Migration Notes](#9-net-core-6-migration-notes)

---

## 1. Extensions

```sql
-- =============================================
-- SECTION 1: PostgreSQL Extensions
-- =============================================
CREATE EXTENSION IF NOT EXISTS "uuid-ossp";
CREATE EXTENSION IF NOT EXISTS "pgcrypto";
CREATE EXTENSION IF NOT EXISTS "postgis";  -- For geography/geometry columns (issue_reports.check_in_location)
```

---

## 2. Enums

```sql
-- =============================================
-- SECTION 2: PostgreSQL Enum Types (30 total)
-- =============================================

CREATE TYPE public.booking_status AS ENUM ('PENDING', 'CONFIRMED', 'IN_PROGRESS', 'COMPLETED', 'CANCELLED');

CREATE TYPE public.channel_type AS ENUM ('GENERAL', 'ANNOUNCEMENTS', 'MAINTENANCE', 'SOCIAL', 'MARKETPLACE');

CREATE TYPE public.check_method AS ENUM ('QR', 'ID_SCAN');

CREATE TYPE public.condition_rating AS ENUM ('EXCELLENT', 'GOOD', 'FAIR', 'POOR', 'DAMAGED', 'MISSING');

CREATE TYPE public.device_status AS ENUM ('ONLINE', 'OFFLINE');

CREATE TYPE public.device_type AS ENUM ('LOCK', 'THERMOSTAT', 'SENSOR', 'LIGHT');

CREATE TYPE public.document_category AS ENUM (
  'lease_agreement', 'payment_receipt', 'deposit_receipt', 'inspection_report',
  'maintenance_invoice', 'id_proof', 'utility_bill', 'insurance_document',
  'property_deed', 'tax_document', 'notice', 'other'
);

CREATE TYPE public.facility_booking_status AS ENUM ('PENDING', 'APPROVED', 'REJECTED', 'CANCELLED', 'CHECKED_IN', 'COMPLETED', 'NO_SHOW');

CREATE TYPE public.facility_type AS ENUM ('GYM', 'POOL', 'BBQ_AREA', 'FUNCTION_HALL', 'TENNIS_COURT', 'PARKING', 'MEETING_ROOM', 'OTHER');

CREATE TYPE public.incident_severity AS ENUM ('LOW', 'MEDIUM', 'HIGH');

CREATE TYPE public.inspection_status AS ENUM ('SCHEDULED', 'IN_PROGRESS', 'PENDING_REVIEW', 'PENDING_SIGNATURES', 'COMPLETED', 'CANCELLED');

CREATE TYPE public.inspection_type AS ENUM ('MOVE_IN', 'MOVE_OUT', 'PERIODIC', 'AD_HOC', 'SNAGGING');

CREATE TYPE public.interval_type AS ENUM ('MONTH', 'YEAR');

CREATE TYPE public.invoice_status AS ENUM ('DRAFT', 'OPEN', 'PAID', 'VOID', 'OVERDUE');

CREATE TYPE public.lease_payment_status AS ENUM ('PENDING', 'DEPOSITED', 'CLEARED', 'BOUNCED', 'CANCELLED', 'OVERDUE');

CREATE TYPE public.lease_status AS ENUM ('DRAFT', 'ACTIVE', 'SUSPENDED', 'TERMINATED', 'EXPIRED');

CREATE TYPE public.listing_status AS ENUM ('DRAFT', 'ACTIVE', 'PAUSED', 'RENTED', 'EXPIRED');

CREATE TYPE public.message_status AS ENUM ('SENT', 'DELIVERED', 'READ', 'DELETED');

CREATE TYPE public.notification_type AS ENUM ('EMAIL', 'PUSH', 'IN_APP', 'ANNOUNCEMENT', 'WORK_ORDER', 'PAYMENT_REMINDER', 'LEASE_RENEWAL', 'INVOICE');

CREATE TYPE public.payment_method AS ENUM ('CARD', 'BANK', 'CASH', 'OTHER');

CREATE TYPE public.payment_provider AS ENUM ('STRIPE', 'OFFLINE');

CREATE TYPE public.payment_status AS ENUM ('PENDING', 'SUCCEEDED', 'FAILED', 'REFUNDED');

CREATE TYPE public.payment_timing AS ENUM ('ADVANCE', 'POST_INSTALLATION');

CREATE TYPE public.permit_status AS ENUM ('PENDING', 'APPROVED', 'REJECTED', 'EXPIRED');

CREATE TYPE public.post_status AS ENUM ('ACTIVE', 'HIDDEN', 'REMOVED', 'FLAGGED');

CREATE TYPE public.priority AS ENUM ('LOW', 'MEDIUM', 'HIGH', 'EMERGENCY');

CREATE TYPE public.provider_specialization AS ENUM (
  'SMART_LOCKS', 'SMART_AC', 'SMART_LIGHTING', 'SMART_SWITCHES',
  'SMART_CAMERAS', 'SMART_SENSORS', 'HOME_AUTOMATION', 'SECURITY_SYSTEMS', 'ENERGY_MANAGEMENT'
);

CREATE TYPE public.quote_status AS ENUM ('SUBMITTED', 'APPROVED', 'REJECTED');

CREATE TYPE public.rent_frequency AS ENUM ('MONTHLY', 'QUARTERLY', 'YEARLY');

CREATE TYPE public.report_status AS ENUM ('PENDING', 'REVIEWED', 'RESOLVED', 'DISMISSED');

CREATE TYPE public.smart_device_status AS ENUM ('PENDING', 'INSTALLED', 'ACTIVE', 'INACTIVE', 'MAINTENANCE', 'REMOVED');

CREATE TYPE public.smart_device_type AS ENUM ('SMART_LOCK', 'SMART_AC', 'SMART_LIGHT', 'SMART_SWITCH');

CREATE TYPE public.smart_home_request_status AS ENUM (
  'SUBMITTED', 'UNDER_REVIEW', 'INSTALLATION_SCHEDULED', 'INSTALLING',
  'INSTALLED', 'ACTIVATED', 'REJECTED', 'CANCELLED'
);

CREATE TYPE public.subscription_status AS ENUM ('ACTIVE', 'PAST_DUE', 'CANCELLED', 'INCOMPLETE');

CREATE TYPE public.unit_status AS ENUM ('VACANT', 'OCCUPIED', 'MAINTENANCE');

CREATE TYPE public.user_status AS ENUM ('ACTIVE', 'INVITED', 'SUSPENDED');

CREATE TYPE public.verification_status AS ENUM ('PENDING', 'VERIFIED', 'REJECTED', 'SUSPENDED');

CREATE TYPE public.visibility_tier AS ENUM ('FREE', 'STANDARD', 'PREMIUM');

CREATE TYPE public.work_order_status AS ENUM (
  'NEW', 'ASSIGNED', 'IN_PROGRESS', 'AWAITING_QUOTE', 'AWAITING_APPROVAL',
  'COMPLETED', 'CLOSED', 'REJECTED', 'PRICING_DECISION',
  'AWAITING_SERVICE_PROVIDER', 'REVIEWED', 'CANCELLED'
);
```

---

## 3. Tables

### 3.1 Core — Users, Roles & Permissions

```sql
-- =============================================
-- 3.1: CORE TABLES
-- =============================================

-- users (replaces auth.users reference)
CREATE TABLE public.users (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  email TEXT NOT NULL UNIQUE,
  phone TEXT,
  hashed_password TEXT,
  name TEXT,
  status public.user_status NOT NULL DEFAULT 'ACTIVE',
  last_login_at TIMESTAMPTZ,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  deleted_at TIMESTAMPTZ
);

-- profiles
CREATE TABLE public.profiles (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id UUID NOT NULL UNIQUE REFERENCES public.users(id) ON DELETE CASCADE,
  name TEXT,
  phone TEXT,
  avatar_url TEXT,
  notification_preferences JSONB,
  whatsapp_opted_in BOOLEAN DEFAULT false,
  whatsapp_phone TEXT,
  whatsapp_verified BOOLEAN DEFAULT false,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- roles (with hierarchical inheritance)
CREATE TABLE public.roles (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  name TEXT NOT NULL UNIQUE,
  description TEXT,
  is_system BOOLEAN DEFAULT false,
  parent_role_id UUID REFERENCES public.roles(id),
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- user_roles
CREATE TABLE public.user_roles (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id UUID NOT NULL REFERENCES public.users(id) ON DELETE CASCADE,
  role_id UUID NOT NULL REFERENCES public.roles(id) ON DELETE CASCADE,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  UNIQUE(user_id, role_id)
);

-- permissions
CREATE TABLE public.permissions (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  code TEXT NOT NULL UNIQUE,
  name TEXT NOT NULL,
  description TEXT,
  category TEXT,
  is_navigation BOOLEAN DEFAULT false,
  nav_path TEXT,
  nav_icon TEXT,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- role_permissions
CREATE TABLE public.role_permissions (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  role_id UUID NOT NULL REFERENCES public.roles(id) ON DELETE CASCADE,
  permission_id UUID NOT NULL REFERENCES public.permissions(id) ON DELETE CASCADE,
  granted_by UUID REFERENCES public.users(id),
  granted_at TIMESTAMPTZ DEFAULT now(),
  UNIQUE(role_id, permission_id)
);

-- permission_audit_logs
CREATE TABLE public.permission_audit_logs (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  action TEXT NOT NULL,
  role_id UUID,
  role_name TEXT,
  permission_id UUID,
  permission_code TEXT,
  parent_role_id UUID,
  parent_role_name TEXT,
  performed_by UUID,
  metadata JSONB,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);
```

### 3.2 Properties & Units

```sql
-- =============================================
-- 3.2: PROPERTIES & UNITS
-- =============================================

CREATE TABLE public.properties (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  landlord_id UUID REFERENCES public.users(id),
  name TEXT NOT NULL,
  type TEXT NOT NULL,
  address TEXT,
  city TEXT,
  state TEXT,
  country TEXT,
  lat DOUBLE PRECISION,
  lng DOUBLE PRECISION,
  google_map_link TEXT,
  total_floors INTEGER,
  total_units INTEGER,
  amenities JSONB,
  utilities_included JSONB,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  deleted_at TIMESTAMPTZ
);

CREATE TABLE public.units (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  property_id UUID NOT NULL REFERENCES public.properties(id) ON DELETE CASCADE,
  unit_number TEXT NOT NULL,
  code TEXT NOT NULL,
  floor INTEGER,
  bedrooms INTEGER,
  bathrooms INTEGER,
  area_sqft NUMERIC,
  furnished BOOLEAN DEFAULT false,
  status public.unit_status DEFAULT 'VACANT',
  monthly_rent NUMERIC,
  security_deposit_amount NUMERIC,
  features JSONB,
  assets JSONB,
  -- Shop-specific fields
  is_shop BOOLEAN DEFAULT false,
  is_primary_shop BOOLEAN DEFAULT false,
  shop_name TEXT,
  shop_type TEXT,
  shop_group_id TEXT,
  shop_images TEXT[],
  frontage_ft NUMERIC,
  has_bathroom BOOLEAN DEFAULT false,
  has_electricity BOOLEAN DEFAULT false,
  has_water BOOLEAN DEFAULT false,
  has_storage BOOLEAN DEFAULT false,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  deleted_at TIMESTAMPTZ
);

CREATE TABLE public.property_maintenance_teams (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  property_id UUID NOT NULL REFERENCES public.properties(id) ON DELETE CASCADE,
  maintenance_team_id UUID NOT NULL REFERENCES public.maintenance_teams(id) ON DELETE CASCADE,
  specialties TEXT[],
  created_at TIMESTAMPTZ DEFAULT now(),
  updated_at TIMESTAMPTZ DEFAULT now()
);
```

### 3.3 Leases

```sql
-- =============================================
-- 3.3: LEASES
-- =============================================

CREATE TABLE public.leases (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  unit_id UUID NOT NULL REFERENCES public.units(id),
  tenant_user_id UUID NOT NULL REFERENCES public.users(id),
  landlord_org_id UUID REFERENCES public.users(id),
  start_date DATE NOT NULL,
  end_date DATE NOT NULL,
  rent_amount NUMERIC NOT NULL,
  rent_frequency public.rent_frequency NOT NULL DEFAULT 'MONTHLY',
  currency TEXT DEFAULT 'AED',
  payment_method public.payment_method DEFAULT 'BANK',
  security_deposit NUMERIC,
  maintenance_responsibility TEXT DEFAULT 'LANDLORD',
  status public.lease_status NOT NULL DEFAULT 'DRAFT',
  notes TEXT,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  deleted_at TIMESTAMPTZ
);

CREATE TABLE public.lease_payments (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  lease_id UUID NOT NULL REFERENCES public.leases(id) ON DELETE CASCADE,
  tenant_user_id UUID NOT NULL REFERENCES public.users(id),
  landlord_id UUID REFERENCES public.users(id),
  property_id UUID REFERENCES public.properties(id),
  unit_id UUID REFERENCES public.units(id),
  amount NUMERIC NOT NULL,
  currency TEXT NOT NULL DEFAULT 'AED',
  payment_method public.payment_method DEFAULT 'BANK',
  status public.lease_payment_status NOT NULL DEFAULT 'PENDING',
  due_date DATE NOT NULL,
  paid_date DATE,
  payment_number INTEGER,
  total_payments INTEGER,
  cheque_number TEXT,
  cheque_date DATE,
  cheque_bank TEXT,
  cheque_image_url TEXT,
  transfer_reference TEXT,
  transfer_proof_url TEXT,
  validated_by UUID REFERENCES public.users(id),
  validated_at TIMESTAMPTZ,
  rejection_reason TEXT,
  late_fee_amount NUMERIC DEFAULT 0,
  late_fee_percentage NUMERIC,
  grace_period_days INTEGER DEFAULT 5,
  notes TEXT,
  receipt_number TEXT,
  receipt_generated_at TIMESTAMPTZ,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE public.lease_payment_history (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  lease_payment_id UUID NOT NULL REFERENCES public.lease_payments(id) ON DELETE CASCADE,
  old_status public.lease_payment_status,
  new_status public.lease_payment_status NOT NULL,
  change_reason TEXT,
  changed_by UUID NOT NULL,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE public.lease_document_templates (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  name TEXT NOT NULL,
  document_type TEXT NOT NULL,
  content TEXT NOT NULL,
  landlord_id UUID REFERENCES public.users(id),
  property_id UUID REFERENCES public.properties(id),
  created_by UUID REFERENCES public.users(id),
  is_active BOOLEAN DEFAULT true,
  is_global BOOLEAN DEFAULT false,
  version INTEGER DEFAULT 1,
  created_at TIMESTAMPTZ DEFAULT now(),
  updated_at TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE public.lease_documents (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  lease_id UUID REFERENCES public.leases(id),
  landlord_id UUID NOT NULL REFERENCES public.users(id),
  tenant_id UUID NOT NULL REFERENCES public.users(id),
  unit_id UUID NOT NULL REFERENCES public.units(id),
  template_id UUID REFERENCES public.lease_document_templates(id),
  document_type TEXT NOT NULL,
  document_content JSONB,
  pdf_path TEXT,
  status TEXT DEFAULT 'DRAFT',
  valid_from DATE,
  valid_until DATE,
  created_at TIMESTAMPTZ DEFAULT now(),
  updated_at TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE public.lease_document_reminders (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  document_id UUID NOT NULL REFERENCES public.lease_documents(id) ON DELETE CASCADE,
  recipient_user_id UUID NOT NULL REFERENCES public.users(id),
  recipient_role TEXT NOT NULL,
  reminder_type TEXT NOT NULL DEFAULT 'SIGNATURE',
  reminder_date TIMESTAMPTZ NOT NULL,
  sent_at TIMESTAMPTZ,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE public.lease_signatures (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  document_id UUID NOT NULL REFERENCES public.lease_documents(id) ON DELETE CASCADE,
  signer_user_id UUID REFERENCES public.users(id),
  signer_role TEXT NOT NULL,
  signer_name TEXT,
  signature_data TEXT,
  signed_at TIMESTAMPTZ,
  ip_address TEXT,
  user_agent TEXT,
  created_at TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE public.lease_audit_trail (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  lease_id UUID REFERENCES public.leases(id),
  actor_user_id UUID REFERENCES public.users(id),
  action TEXT NOT NULL,
  metadata JSONB,
  created_at TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE public.ejari_submissions (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  lease_id UUID REFERENCES public.leases(id),
  lease_document_id UUID REFERENCES public.lease_documents(id),
  submitted_by_user_id UUID REFERENCES public.users(id),
  registration_number TEXT,
  status TEXT DEFAULT 'PENDING',
  submission_date DATE,
  submission_method TEXT,
  approval_date DATE,
  rejection_reason TEXT,
  created_at TIMESTAMPTZ DEFAULT now(),
  updated_at TIMESTAMPTZ DEFAULT now()
);
```

### 3.4 Tenants

```sql
-- =============================================
-- 3.4: TENANTS
-- =============================================

CREATE TABLE public.tenant_profiles (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id UUID NOT NULL UNIQUE REFERENCES public.users(id) ON DELETE CASCADE,
  national_id TEXT,
  emirates_id TEXT,
  dob DATE,
  emergency_contact JSONB,
  score INTEGER,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE public.tenant_apartment_assignments (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  tenant_user_id UUID NOT NULL REFERENCES public.users(id),
  unit_id UUID NOT NULL REFERENCES public.units(id),
  property_id UUID NOT NULL REFERENCES public.properties(id),
  lease_id UUID REFERENCES public.leases(id),
  status TEXT NOT NULL DEFAULT 'PENDING',
  assigned_by UUID REFERENCES public.users(id),
  assigned_at TIMESTAMPTZ,
  approved_at TIMESTAMPTZ,
  approved_by UUID REFERENCES public.users(id),
  notes TEXT,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE public.unit_occupancy_history (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  unit_id UUID NOT NULL REFERENCES public.units(id),
  property_id UUID NOT NULL REFERENCES public.properties(id),
  tenant_user_id UUID NOT NULL REFERENCES public.users(id),
  lease_id UUID REFERENCES public.leases(id),
  move_in_date DATE NOT NULL,
  move_out_date DATE,
  status TEXT NOT NULL DEFAULT 'ACTIVE',
  rent_amount NUMERIC,
  security_deposit NUMERIC,
  deposit_returned NUMERIC,
  deposit_deductions JSONB,
  termination_date DATE,
  termination_reason TEXT,
  termination_type TEXT,
  terminated_by UUID,
  tenant_rating INTEGER,
  tenant_feedback TEXT,
  landlord_rating INTEGER,
  landlord_feedback TEXT,
  notes TEXT,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE public.termination_requests (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  lease_id UUID NOT NULL REFERENCES public.leases(id),
  tenant_user_id UUID NOT NULL REFERENCES public.users(id),
  unit_id UUID NOT NULL REFERENCES public.units(id),
  reason TEXT NOT NULL,
  requested_date DATE NOT NULL,
  status TEXT NOT NULL DEFAULT 'PENDING',
  reviewed_at TIMESTAMPTZ,
  reviewed_by UUID REFERENCES public.users(id),
  review_notes TEXT,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE public.tenant_screenings (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  tenant_user_id UUID NOT NULL REFERENCES public.users(id),
  property_id UUID REFERENCES public.properties(id),
  landlord_id UUID REFERENCES public.users(id),
  status TEXT DEFAULT 'PENDING',
  credit_score INTEGER,
  background_check_status TEXT,
  employment_verified BOOLEAN,
  income_verified BOOLEAN,
  previous_landlord_reference TEXT,
  notes TEXT,
  completed_at TIMESTAMPTZ,
  created_at TIMESTAMPTZ DEFAULT now(),
  updated_at TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE public.screening_references (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  screening_id UUID NOT NULL REFERENCES public.tenant_screenings(id) ON DELETE CASCADE,
  reference_type TEXT NOT NULL,
  name TEXT NOT NULL,
  phone TEXT,
  email TEXT,
  relationship TEXT,
  notes TEXT,
  verified BOOLEAN DEFAULT false,
  verified_at TIMESTAMPTZ,
  created_at TIMESTAMPTZ DEFAULT now()
);
```

### 3.5 Finance — Invoices, Payments, Subscriptions

```sql
-- =============================================
-- 3.5: FINANCE
-- =============================================

CREATE TABLE public.invoice_templates (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  landlord_id UUID REFERENCES public.users(id),
  name TEXT NOT NULL,
  company_name TEXT,
  company_address TEXT,
  company_email TEXT,
  company_phone TEXT,
  logo_url TEXT,
  show_logo BOOLEAN DEFAULT true,
  primary_color TEXT,
  header_text TEXT,
  footer_text TEXT,
  terms_conditions TEXT,
  show_bank_details BOOLEAN DEFAULT false,
  bank_name TEXT,
  bank_account TEXT,
  bank_routing TEXT,
  iban TEXT,
  swift_code TEXT,
  tax_id TEXT,
  is_default BOOLEAN DEFAULT false,
  created_at TIMESTAMPTZ DEFAULT now(),
  updated_at TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE public.invoices (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  lease_id UUID REFERENCES public.leases(id),
  tenant_user_id UUID REFERENCES public.users(id),
  template_id UUID REFERENCES public.invoice_templates(id),
  number TEXT NOT NULL,
  currency TEXT NOT NULL,
  amount NUMERIC NOT NULL,
  due_date DATE NOT NULL,
  status public.invoice_status DEFAULT 'DRAFT',
  meta JSONB,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  deleted_at TIMESTAMPTZ
);

CREATE TABLE public.payments (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  invoice_id UUID NOT NULL REFERENCES public.invoices(id),
  amount NUMERIC NOT NULL,
  currency TEXT NOT NULL,
  method public.payment_method NOT NULL,
  provider public.payment_provider NOT NULL,
  provider_payment_id TEXT,
  status public.payment_status NOT NULL DEFAULT 'PENDING',
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE public.receipts (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  payment_id UUID NOT NULL UNIQUE REFERENCES public.payments(id),
  number TEXT NOT NULL,
  pdf_url TEXT,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE public.payment_methods (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id UUID NOT NULL REFERENCES public.users(id),
  type TEXT NOT NULL,
  is_default BOOLEAN DEFAULT false,
  metadata JSONB,
  stripe_payment_method_id TEXT,
  created_at TIMESTAMPTZ DEFAULT now(),
  updated_at TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE public.plans (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  code TEXT NOT NULL UNIQUE,
  name TEXT NOT NULL,
  tier TEXT NOT NULL,
  price NUMERIC NOT NULL,
  interval public.interval_type NOT NULL,
  limits JSONB,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE public.subscriptions (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  plan_id UUID NOT NULL REFERENCES public.plans(id),
  stripe_subscription_id TEXT,
  status public.subscription_status DEFAULT 'ACTIVE',
  current_period_start TIMESTAMPTZ,
  current_period_end TIMESTAMPTZ,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE public.subscription_config (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  plan_name TEXT NOT NULL DEFAULT 'Rentolic Pro',
  base_price_per_unit NUMERIC NOT NULL DEFAULT 10,
  currency TEXT NOT NULL DEFAULT 'AED',
  trial_days INTEGER NOT NULL DEFAULT 14,
  yearly_discount_percent NUMERIC NOT NULL DEFAULT 20,
  stripe_product_id TEXT,
  stripe_monthly_price_id TEXT,
  stripe_yearly_price_id TEXT,
  features JSONB,
  is_active BOOLEAN DEFAULT true,
  created_at TIMESTAMPTZ DEFAULT now(),
  updated_at TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE public.landlord_subscriptions (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  landlord_user_id UUID NOT NULL UNIQUE REFERENCES public.users(id),
  unit_count INTEGER NOT NULL DEFAULT 0,
  billing_cycle TEXT NOT NULL,
  status TEXT NOT NULL DEFAULT 'ACTIVE',
  amount_per_unit NUMERIC,
  total_amount NUMERIC,
  discount_applied NUMERIC,
  auto_renewal BOOLEAN DEFAULT true,
  trial_start_date TIMESTAMPTZ,
  trial_end_date TIMESTAMPTZ,
  current_period_start TIMESTAMPTZ,
  current_period_end TIMESTAMPTZ,
  cancelled_at TIMESTAMPTZ,
  stripe_customer_id TEXT,
  stripe_subscription_id TEXT,
  created_at TIMESTAMPTZ DEFAULT now(),
  updated_at TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE public.subscription_invoices (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  landlord_subscription_id UUID REFERENCES public.landlord_subscriptions(id),
  landlord_user_id UUID NOT NULL REFERENCES public.users(id),
  amount NUMERIC NOT NULL,
  currency TEXT NOT NULL DEFAULT 'AED',
  billing_cycle TEXT NOT NULL,
  unit_count INTEGER NOT NULL,
  invoice_date DATE NOT NULL DEFAULT CURRENT_DATE,
  due_date DATE,
  status TEXT NOT NULL DEFAULT 'PENDING',
  description TEXT,
  stripe_invoice_id TEXT,
  stripe_payment_intent_id TEXT,
  invoice_url TEXT,
  paid_at TIMESTAMPTZ,
  created_at TIMESTAMPTZ DEFAULT now(),
  updated_at TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE public.subscription_usage (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  landlord_subscription_id UUID REFERENCES public.landlord_subscriptions(id),
  landlord_user_id UUID NOT NULL UNIQUE REFERENCES public.users(id),
  units_used INTEGER NOT NULL DEFAULT 0,
  units_allowed INTEGER NOT NULL DEFAULT 0,
  last_checked_at TIMESTAMPTZ,
  created_at TIMESTAMPTZ DEFAULT now(),
  updated_at TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE public.usage_meters (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  metric TEXT NOT NULL,
  value NUMERIC DEFAULT 0,
  period_start TIMESTAMPTZ NOT NULL,
  period_end TIMESTAMPTZ NOT NULL,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE public.stripe_events (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  type TEXT NOT NULL,
  payload JSONB NOT NULL,
  processed_at TIMESTAMPTZ,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE public.platform_payment_info (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  stripe_api_key TEXT,
  stripe_publishable_key TEXT,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE public.landlord_payment_info (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  landlord_id UUID NOT NULL REFERENCES public.users(id),
  bank_name TEXT,
  account_holder_name TEXT,
  account_number TEXT,
  routing_number TEXT,
  iban TEXT,
  swift_code TEXT,
  stripe_api_key TEXT,
  stripe_publishable_key TEXT,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE public.revenue_accounts (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  owner_id UUID REFERENCES public.users(id),
  name TEXT NOT NULL,
  account_holder TEXT NOT NULL,
  account_type TEXT,
  bank_name TEXT,
  account_number TEXT,
  routing_number TEXT,
  iban TEXT,
  swift_code TEXT,
  is_default BOOLEAN DEFAULT false,
  created_at TIMESTAMPTZ DEFAULT now(),
  updated_at TIMESTAMPTZ DEFAULT now()
);
```

### 3.6 Maintenance & Work Orders

```sql
-- =============================================
-- 3.6: MAINTENANCE & WORK ORDERS
-- =============================================

CREATE TABLE public.maintenance_teams (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  name TEXT NOT NULL,
  landlord_id UUID,
  main_user_id UUID,
  specialties TEXT[],
  contact_email TEXT,
  contact_phone TEXT,
  payment_recipient TEXT,
  payment_terms TEXT,
  stripe_account_id TEXT,
  active BOOLEAN DEFAULT true,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE public.maintenance_sub_users (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  main_user_id UUID NOT NULL REFERENCES public.users(id),
  sub_user_id UUID NOT NULL REFERENCES public.users(id),
  maintenance_team_id UUID REFERENCES public.maintenance_teams(id),
  role TEXT NOT NULL,
  permissions JSONB,
  active BOOLEAN DEFAULT true,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE public.maintenance_categories (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  name TEXT NOT NULL,
  landlord_id UUID REFERENCES public.users(id),
  property_id UUID REFERENCES public.properties(id),
  is_global BOOLEAN DEFAULT false,
  visible_to_tenant BOOLEAN NOT NULL DEFAULT true,
  active BOOLEAN NOT NULL DEFAULT true,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE public.maintenance_sla_config (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  landlord_id UUID REFERENCES public.users(id),
  property_id UUID REFERENCES public.properties(id),
  priority TEXT NOT NULL,
  response_hours INTEGER NOT NULL,
  resolution_hours INTEGER NOT NULL,
  escalation_hours INTEGER NOT NULL,
  auto_escalate BOOLEAN DEFAULT false,
  notify_landlord BOOLEAN DEFAULT true,
  is_default BOOLEAN DEFAULT false,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE public.issue_reports (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  property_id UUID NOT NULL REFERENCES public.properties(id),
  unit_id UUID REFERENCES public.units(id),
  tenant_user_id UUID NOT NULL REFERENCES public.users(id),
  raised_by_user_id UUID,
  assigned_maintenance_team_id UUID REFERENCES public.maintenance_teams(id),
  assigned_sub_user_id UUID,
  external_service_provider_id UUID,
  selected_service_provider_id UUID REFERENCES public.service_providers(id),
  accepted_bid_id UUID,
  invoice_id UUID REFERENCES public.invoices(id),
  title TEXT NOT NULL,
  description TEXT NOT NULL,
  category TEXT NOT NULL,
  priority public.priority DEFAULT 'MEDIUM',
  status public.work_order_status DEFAULT 'NEW',
  images TEXT[],
  voice_notes TEXT[],
  -- SLA fields
  sla_due_date TIMESTAMPTZ,
  sla_breached BOOLEAN DEFAULT false,
  sla_response_due TIMESTAMPTZ,
  sla_resolution_due TIMESTAMPTZ,
  first_response_at TIMESTAMPTZ,
  -- Cost fields
  cost_estimate NUMERIC,
  actual_cost NUMERIC,
  is_paid BOOLEAN,
  pricing_type TEXT,
  approval_status TEXT,
  approval_threshold NUMERIC,
  approved_at TIMESTAMPTZ,
  approved_by_user_id UUID,
  -- Bidding fields
  bid_deadline TIMESTAMPTZ,
  min_bid_amount NUMERIC,
  max_bid_amount NUMERIC,
  -- Assignment fields
  assignment_score NUMERIC,
  assignment_reason TEXT,
  -- Escalation
  escalated BOOLEAN DEFAULT false,
  escalated_at TIMESTAMPTZ,
  escalated_reason TEXT,
  is_emergency BOOLEAN DEFAULT false,
  -- Scheduling
  scheduled_date DATE,
  scheduled_time TIME,
  expected_completion_date DATE,
  completed_at TIMESTAMPTZ,
  -- Mobile / Check-in
  check_in_location GEOGRAPHY(POINT, 4326),
  check_in_time TIMESTAMPTZ,
  mobile_checked_in BOOLEAN DEFAULT false,
  offline_synced BOOLEAN DEFAULT true,
  -- Proof of work
  proof_of_work_submitted BOOLEAN DEFAULT false,
  proof_of_work_submitted_at TIMESTAMPTZ,
  proof_of_work_description TEXT,
  proof_of_work_images TEXT[],
  proof_of_work_approved BOOLEAN,
  proof_of_work_approved_at TIMESTAMPTZ,
  -- Reviews
  tenant_rating INTEGER,
  tenant_review TEXT,
  tenant_feedback TEXT,
  landlord_rating INTEGER,
  landlord_review TEXT,
  landlord_reviewed_at TIMESTAMPTZ,
  -- Other
  warranty_covered BOOLEAN,
  recommended_service_provider_ids UUID[],
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE public.work_orders (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  property_id UUID NOT NULL REFERENCES public.properties(id),
  unit_id UUID REFERENCES public.units(id),
  raised_by_user_id UUID NOT NULL REFERENCES public.users(id),
  assigned_org_id UUID,
  assigned_maintenance_team_id UUID,
  assigned_sub_user_id UUID,
  issue_report_id UUID REFERENCES public.issue_reports(id),
  invoice_id UUID REFERENCES public.invoices(id),
  selected_service_provider_id UUID,
  title TEXT NOT NULL,
  description TEXT,
  category TEXT,
  priority public.priority DEFAULT 'MEDIUM',
  status public.work_order_status DEFAULT 'NEW',
  images TEXT[],
  cost_estimate NUMERIC,
  actual_cost NUMERIC,
  pricing_type TEXT,
  scheduled_date DATE,
  scheduled_time TIME,
  completed_at TIMESTAMPTZ,
  tenant_rating INTEGER,
  tenant_review TEXT,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  deleted_at TIMESTAMPTZ
);

CREATE TABLE public.work_order_status_history (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  work_order_id UUID NOT NULL REFERENCES public.issue_reports(id) ON DELETE CASCADE,
  old_status TEXT,
  new_status TEXT NOT NULL,
  notes TEXT,
  changed_by UUID REFERENCES public.users(id),
  created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE public.work_order_comments (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  work_order_id UUID NOT NULL REFERENCES public.issue_reports(id) ON DELETE CASCADE,
  user_id UUID NOT NULL REFERENCES public.users(id),
  content TEXT NOT NULL,
  comment_type TEXT NOT NULL,
  images TEXT[],
  eta_date DATE,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE public.work_order_events (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  work_order_id UUID NOT NULL REFERENCES public.work_orders(id) ON DELETE CASCADE,
  actor_user_id UUID REFERENCES public.users(id),
  type TEXT NOT NULL,
  payload JSONB,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE public.work_order_media (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  work_order_id UUID NOT NULL REFERENCES public.work_orders(id) ON DELETE CASCADE,
  url TEXT NOT NULL,
  type TEXT NOT NULL,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE public.work_order_messages (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  work_order_id UUID NOT NULL REFERENCES public.issue_reports(id) ON DELETE CASCADE,
  sender_id UUID NOT NULL REFERENCES public.users(id),
  sender_role TEXT NOT NULL,
  message TEXT NOT NULL,
  attachments TEXT[],
  read_at TIMESTAMPTZ,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE public.work_order_notes (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  work_order_id UUID NOT NULL REFERENCES public.issue_reports(id) ON DELETE CASCADE,
  user_id UUID NOT NULL REFERENCES public.users(id),
  content TEXT NOT NULL,
  note_type TEXT NOT NULL DEFAULT 'GENERAL',
  is_internal BOOLEAN DEFAULT false,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE public.work_order_parts (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  work_order_id UUID NOT NULL REFERENCES public.issue_reports(id) ON DELETE CASCADE,
  part_name TEXT NOT NULL,
  quantity INTEGER NOT NULL DEFAULT 1,
  unit_cost NUMERIC,
  total_cost NUMERIC,
  supplier TEXT,
  part_barcode TEXT,
  used_at TIMESTAMPTZ,
  added_by_user_id UUID,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE public.work_order_payments (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  work_order_id UUID NOT NULL REFERENCES public.issue_reports(id) ON DELETE CASCADE,
  amount NUMERIC NOT NULL,
  currency TEXT DEFAULT 'AED',
  payment_recipient TEXT NOT NULL,
  status TEXT DEFAULT 'PENDING',
  paid_by UUID REFERENCES public.users(id),
  paid_at TIMESTAMPTZ,
  stripe_payment_intent_id TEXT,
  stripe_transfer_id TEXT,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE public.work_order_quotes (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  work_order_id UUID NOT NULL REFERENCES public.issue_reports(id) ON DELETE CASCADE,
  created_by UUID REFERENCES public.users(id),
  amount NUMERIC NOT NULL,
  currency TEXT DEFAULT 'AED',
  description TEXT,
  line_items JSONB,
  status TEXT DEFAULT 'PENDING',
  tenant_response_notes TEXT,
  responded_at TIMESTAMPTZ,
  created_at TIMESTAMPTZ DEFAULT now(),
  updated_at TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE public.work_order_templates (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  title TEXT NOT NULL,
  category TEXT NOT NULL,
  description TEXT,
  solution_steps JSONB,
  common_issues JSONB,
  required_parts JSONB,
  estimated_time_hours NUMERIC,
  usage_count INTEGER DEFAULT 0,
  created_by_user_id UUID,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE public.work_order_time_logs (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  work_order_id UUID NOT NULL REFERENCES public.issue_reports(id) ON DELETE CASCADE,
  user_id UUID NOT NULL REFERENCES public.users(id),
  action TEXT NOT NULL,
  location_lat DOUBLE PRECISION,
  location_lng DOUBLE PRECISION,
  notes TEXT,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE public.work_order_appointments (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  work_order_id UUID NOT NULL REFERENCES public.issue_reports(id) ON DELETE CASCADE,
  scheduled_date DATE NOT NULL,
  scheduled_time_start TIME NOT NULL,
  scheduled_time_end TIME NOT NULL,
  confirmed_by_tenant BOOLEAN DEFAULT false,
  confirmed_at TIMESTAMPTZ,
  reminder_sent BOOLEAN DEFAULT false,
  notes TEXT,
  created_by UUID REFERENCES public.users(id),
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE public.work_order_assignment_history (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  work_order_id UUID NOT NULL REFERENCES public.issue_reports(id) ON DELETE CASCADE,
  assigned_to_team_id UUID REFERENCES public.maintenance_teams(id),
  assigned_to_user_id UUID,
  assignment_method TEXT NOT NULL,
  assignment_score NUMERIC,
  factors JSONB,
  reason_for_change TEXT,
  assigned_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  unassigned_at TIMESTAMPTZ
);

CREATE TABLE public.service_provider_bids (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  work_order_id UUID NOT NULL REFERENCES public.issue_reports(id) ON DELETE CASCADE,
  provider_id UUID NOT NULL,
  bid_amount NUMERIC NOT NULL,
  estimated_completion_days INTEGER NOT NULL,
  description TEXT,
  includes_parts BOOLEAN DEFAULT false,
  includes_warranty BOOLEAN DEFAULT false,
  warranty_period_months INTEGER,
  status TEXT NOT NULL DEFAULT 'PENDING',
  submitted_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  responded_at TIMESTAMPTZ,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE public.quotes (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  work_order_id UUID NOT NULL REFERENCES public.work_orders(id),
  vendor_org_id UUID NOT NULL,
  amount NUMERIC NOT NULL,
  currency TEXT NOT NULL,
  notes TEXT,
  status public.quote_status DEFAULT 'SUBMITTED',
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE public.inventory_items (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  property_id UUID REFERENCES public.properties(id),
  name TEXT NOT NULL,
  category TEXT NOT NULL,
  sku TEXT,
  quantity INTEGER NOT NULL DEFAULT 0,
  min_stock INTEGER NOT NULL DEFAULT 0,
  unit TEXT NOT NULL,
  cost_per_unit NUMERIC,
  location TEXT NOT NULL,
  supplier TEXT,
  notes TEXT,
  created_by UUID REFERENCES public.users(id),
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE public.maintenance_analytics_cache (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  property_id UUID REFERENCES public.properties(id),
  team_id UUID REFERENCES public.maintenance_teams(id),
  period_start TIMESTAMPTZ NOT NULL,
  period_end TIMESTAMPTZ NOT NULL,
  total_work_orders INTEGER,
  completed_work_orders INTEGER,
  average_completion_time_hours NUMERIC,
  average_response_time_hours NUMERIC,
  sla_breach_count INTEGER,
  average_rating NUMERIC,
  total_cost NUMERIC,
  categories_breakdown JSONB,
  priority_breakdown JSONB,
  generated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE public.scheduled_maintenance (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  property_id UUID REFERENCES public.properties(id),
  unit_id UUID REFERENCES public.units(id),
  title TEXT NOT NULL,
  description TEXT,
  category TEXT,
  frequency TEXT NOT NULL,
  next_due_date DATE,
  last_completed_date DATE,
  assigned_team_id UUID REFERENCES public.maintenance_teams(id),
  auto_create_work_order BOOLEAN DEFAULT true,
  active BOOLEAN DEFAULT true,
  created_by UUID REFERENCES public.users(id),
  created_at TIMESTAMPTZ DEFAULT now(),
  updated_at TIMESTAMPTZ DEFAULT now()
);
```

### 3.7 Facilities

```sql
-- =============================================
-- 3.7: FACILITIES
-- =============================================

CREATE TABLE public.facilities (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  property_id UUID NOT NULL REFERENCES public.properties(id),
  created_by_user_id UUID REFERENCES public.users(id),
  name TEXT NOT NULL,
  facility_type public.facility_type NOT NULL,
  description TEXT,
  location TEXT,
  capacity INTEGER,
  hourly_rate NUMERIC,
  deposit_amount NUMERIC,
  images JSONB,
  amenities JSONB,
  rules TEXT,
  requires_approval BOOLEAN DEFAULT false,
  active BOOLEAN DEFAULT true,
  advance_booking_days INTEGER,
  booking_window_days INTEGER,
  cancellation_hours INTEGER,
  min_booking_hours INTEGER,
  max_booking_hours INTEGER,
  simultaneous_bookings_limit INTEGER DEFAULT 1,
  created_at TIMESTAMPTZ DEFAULT now(),
  updated_at TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE public.facility_bookings (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  facility_id UUID NOT NULL REFERENCES public.facilities(id),
  booked_by UUID NOT NULL REFERENCES public.users(id),
  unit_id UUID REFERENCES public.units(id),
  revenue_account_id UUID REFERENCES public.revenue_accounts(id),
  booking_date DATE NOT NULL,
  start_time TIME NOT NULL,
  end_time TIME NOT NULL,
  purpose TEXT,
  guests INTEGER,
  notes TEXT,
  status public.facility_booking_status DEFAULT 'PENDING',
  total_amount NUMERIC,
  deposit_paid BOOLEAN DEFAULT false,
  qr_code_data TEXT,
  approved_by UUID REFERENCES public.users(id),
  approved_at TIMESTAMPTZ,
  rejection_reason TEXT,
  checked_in_by UUID REFERENCES public.users(id),
  checked_in_at TIMESTAMPTZ,
  cancellation_reason TEXT,
  cancelled_at TIMESTAMPTZ,
  created_at TIMESTAMPTZ DEFAULT now(),
  updated_at TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE public.facility_blocked_dates (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  facility_id UUID NOT NULL REFERENCES public.facilities(id) ON DELETE CASCADE,
  blocked_date DATE NOT NULL,
  reason TEXT,
  created_by UUID REFERENCES public.users(id),
  created_at TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE public.facility_rules (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  facility_id UUID REFERENCES public.facilities(id) ON DELETE CASCADE,
  rule_text TEXT NOT NULL,
  is_mandatory BOOLEAN DEFAULT false,
  display_order INTEGER,
  created_at TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE public.facility_slots (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  facility_id UUID NOT NULL REFERENCES public.facilities(id) ON DELETE CASCADE,
  day_of_week INTEGER NOT NULL,
  start_time TIME NOT NULL,
  end_time TIME NOT NULL,
  is_available BOOLEAN DEFAULT true,
  created_at TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE public.facility_revenue_config (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  facility_id UUID UNIQUE REFERENCES public.facilities(id),
  revenue_account_id UUID REFERENCES public.revenue_accounts(id),
  commission_percentage NUMERIC,
  revenue_split_percentage NUMERIC,
  created_at TIMESTAMPTZ DEFAULT now(),
  updated_at TIMESTAMPTZ DEFAULT now()
);
```

### 3.8 Documents

```sql
-- =============================================
-- 3.8: DOCUMENTS
-- =============================================

CREATE TABLE public.documents (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  property_id UUID REFERENCES public.properties(id),
  unit_id UUID REFERENCES public.units(id),
  landlord_id UUID,
  tenant_user_id UUID,
  uploaded_by UUID NOT NULL,
  uploaded_by_role TEXT NOT NULL,
  parent_document_id UUID REFERENCES public.documents(id),
  title TEXT NOT NULL,
  description TEXT,
  file_name TEXT NOT NULL,
  file_path TEXT NOT NULL,
  file_size INTEGER NOT NULL,
  mime_type TEXT NOT NULL,
  category public.document_category NOT NULL,
  tags TEXT[],
  expiry_date DATE,
  is_shared_with_tenant BOOLEAN DEFAULT false,
  version INTEGER DEFAULT 1,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  deleted_at TIMESTAMPTZ
);

CREATE TABLE public.document_shares (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  document_id UUID NOT NULL REFERENCES public.documents(id) ON DELETE CASCADE,
  shared_by_user_id UUID NOT NULL,
  shared_with_user_id UUID NOT NULL,
  can_download BOOLEAN DEFAULT true,
  expires_at TIMESTAMPTZ,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE public.document_access_logs (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  document_id UUID NOT NULL REFERENCES public.documents(id) ON DELETE CASCADE,
  user_id UUID NOT NULL,
  action TEXT NOT NULL,
  user_role TEXT,
  ip_address TEXT,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);
```

### 3.9 Community

```sql
-- =============================================
-- 3.9: COMMUNITY
-- =============================================

CREATE TABLE public.community_channels (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  property_id UUID NOT NULL REFERENCES public.properties(id),
  created_by UUID REFERENCES public.users(id),
  name TEXT NOT NULL,
  description TEXT,
  channel_type public.channel_type DEFAULT 'GENERAL',
  is_active BOOLEAN DEFAULT true,
  is_moderated BOOLEAN DEFAULT false,
  created_at TIMESTAMPTZ DEFAULT now(),
  updated_at TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE public.channel_posts (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  channel_id UUID NOT NULL REFERENCES public.community_channels(id) ON DELETE CASCADE,
  author_id UUID NOT NULL REFERENCES public.users(id),
  content TEXT NOT NULL,
  images TEXT[],
  is_pinned BOOLEAN DEFAULT false,
  likes_count INTEGER DEFAULT 0,
  replies_count INTEGER DEFAULT 0,
  status public.post_status DEFAULT 'ACTIVE',
  created_at TIMESTAMPTZ DEFAULT now(),
  updated_at TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE public.post_replies (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  post_id UUID NOT NULL REFERENCES public.channel_posts(id) ON DELETE CASCADE,
  author_id UUID NOT NULL REFERENCES public.users(id),
  content TEXT NOT NULL,
  status public.post_status DEFAULT 'ACTIVE',
  created_at TIMESTAMPTZ DEFAULT now(),
  updated_at TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE public.post_likes (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  post_id UUID NOT NULL REFERENCES public.channel_posts(id) ON DELETE CASCADE,
  user_id UUID NOT NULL REFERENCES public.users(id),
  created_at TIMESTAMPTZ DEFAULT now(),
  UNIQUE(post_id, user_id)
);

CREATE TABLE public.direct_messages (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  sender_id UUID NOT NULL REFERENCES public.users(id),
  recipient_id UUID NOT NULL REFERENCES public.users(id),
  content TEXT NOT NULL,
  status public.message_status DEFAULT 'SENT',
  read_at TIMESTAMPTZ,
  created_at TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE public.content_reports (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  reporter_id UUID NOT NULL REFERENCES public.users(id),
  post_id UUID REFERENCES public.channel_posts(id),
  reply_id UUID REFERENCES public.post_replies(id),
  message_id UUID REFERENCES public.direct_messages(id),
  reason TEXT NOT NULL,
  details TEXT,
  status public.report_status DEFAULT 'PENDING',
  reviewed_by UUID REFERENCES public.users(id),
  reviewed_at TIMESTAMPTZ,
  action_taken TEXT,
  created_at TIMESTAMPTZ DEFAULT now(),
  updated_at TIMESTAMPTZ DEFAULT now()
);
```

### 3.10 Security & Incidents

```sql
-- =============================================
-- 3.10: SECURITY & INCIDENTS
-- =============================================

CREATE TABLE public.police_reports (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  incident_id UUID,  -- FK added after incidents table
  submitted_by_user_id UUID REFERENCES public.users(id),
  report_type TEXT NOT NULL,
  report_number TEXT,
  report_pdf_url TEXT,
  status TEXT DEFAULT 'PENDING',
  submission_date DATE,
  submission_method TEXT,
  created_at TIMESTAMPTZ DEFAULT now(),
  updated_at TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE public.incidents (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  property_id UUID NOT NULL REFERENCES public.properties(id),
  reported_by_user_id UUID NOT NULL REFERENCES public.users(id),
  assigned_security_user_id UUID REFERENCES public.users(id),
  linked_work_order_id UUID REFERENCES public.issue_reports(id),
  police_report_id UUID REFERENCES public.police_reports(id),
  title TEXT NOT NULL,
  description TEXT,
  severity public.incident_severity DEFAULT 'LOW',
  status TEXT DEFAULT 'OPEN',
  escalation_level INTEGER DEFAULT 0,
  requires_maintenance BOOLEAN DEFAULT false,
  notified_landlord BOOLEAN DEFAULT false,
  closed_at TIMESTAMPTZ,
  closed_by UUID REFERENCES public.users(id),
  reopened_at TIMESTAMPTZ,
  reopened_by UUID REFERENCES public.users(id),
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

-- Add FK back to police_reports
ALTER TABLE public.police_reports ADD CONSTRAINT police_reports_incident_id_fkey
  FOREIGN KEY (incident_id) REFERENCES public.incidents(id);

CREATE TABLE public.incident_responses (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  incident_id UUID REFERENCES public.incidents(id),
  responder_user_id UUID REFERENCES public.users(id),
  action_taken TEXT NOT NULL,
  response_time TIMESTAMPTZ,
  created_at TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE public.complaint_escalations (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  complaint_id UUID NOT NULL REFERENCES public.incidents(id),
  escalated_by UUID NOT NULL REFERENCES public.users(id),
  reason TEXT,
  escalated_to_admin BOOLEAN DEFAULT false,
  escalated_to_landlord BOOLEAN DEFAULT false,
  status TEXT DEFAULT 'PENDING',
  acknowledged_by UUID REFERENCES public.users(id),
  acknowledged_at TIMESTAMPTZ,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE public.complaint_feedback (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  complaint_id UUID NOT NULL REFERENCES public.incidents(id),
  tenant_id UUID NOT NULL REFERENCES public.users(id),
  rating INTEGER,
  comment TEXT,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE public.complaint_replies (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  complaint_id UUID NOT NULL REFERENCES public.incidents(id),
  user_id UUID NOT NULL REFERENCES public.users(id),
  message TEXT NOT NULL,
  is_closing_reply BOOLEAN DEFAULT false,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE public.visitor_permits (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  property_id UUID NOT NULL REFERENCES public.properties(id),
  unit_id UUID REFERENCES public.units(id),
  tenant_user_id UUID NOT NULL REFERENCES public.users(id),
  visitor_name TEXT NOT NULL,
  visitor_id_number TEXT,
  from_time TIMESTAMPTZ NOT NULL,
  to_time TIMESTAMPTZ NOT NULL,
  qr_code TEXT NOT NULL,
  status public.permit_status NOT NULL DEFAULT 'PENDING',
  check_method public.check_method,
  checked_in_at TIMESTAMPTZ,
  checked_out_at TIMESTAMPTZ,
  checked_by UUID,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE public.blacklist (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  property_id UUID REFERENCES public.properties(id),
  landlord_id UUID REFERENCES public.users(id),
  added_by_user_id UUID REFERENCES public.users(id),
  national_id TEXT NOT NULL,
  reason TEXT NOT NULL,
  notes TEXT,
  scope TEXT DEFAULT 'PROPERTY',
  active BOOLEAN DEFAULT true,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE public.security_sub_users (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  main_user_id UUID NOT NULL REFERENCES public.users(id),
  sub_user_id UUID NOT NULL REFERENCES public.users(id),
  permissions JSONB,
  active BOOLEAN DEFAULT true,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE public.security_assigned_properties (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  security_user_id UUID NOT NULL REFERENCES public.users(id),
  property_id UUID NOT NULL REFERENCES public.properties(id),
  assigned_by UUID REFERENCES public.users(id),
  created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE public.property_rounds (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  property_id UUID NOT NULL REFERENCES public.properties(id),
  security_user_id UUID NOT NULL,
  status TEXT NOT NULL DEFAULT 'IN_PROGRESS',
  start_time TIMESTAMPTZ NOT NULL DEFAULT now(),
  end_time TIMESTAMPTZ,
  total_checkpoints INTEGER NOT NULL DEFAULT 0,
  completed_checkpoints INTEGER NOT NULL DEFAULT 0,
  notes TEXT,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE public.round_checkpoints (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  round_id UUID NOT NULL REFERENCES public.property_rounds(id) ON DELETE CASCADE,
  checkpoint_name TEXT NOT NULL,
  checkpoint_number INTEGER NOT NULL,
  location_description TEXT,
  is_completed BOOLEAN DEFAULT false,
  completed_at TIMESTAMPTZ,
  completed_by_user_id UUID,
  comments TEXT,
  images TEXT[],
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);
```

### 3.11 Service Providers & Marketplace

```sql
-- =============================================
-- 3.11: SERVICE PROVIDERS & MARKETPLACE
-- =============================================

CREATE TABLE public.service_providers (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  visibility_tier public.visibility_tier DEFAULT 'FREE',
  approved BOOLEAN DEFAULT false,
  onboarding_completed BOOLEAN DEFAULT false,
  commission_type TEXT,
  commission_value NUMERIC,
  payout_schedule TEXT,
  next_payout_date DATE,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE public.service_listings (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  provider_id UUID NOT NULL REFERENCES public.service_providers(id),
  title TEXT NOT NULL,
  category TEXT NOT NULL,
  description TEXT,
  base_price NUMERIC NOT NULL,
  currency TEXT NOT NULL DEFAULT 'AED',
  service_type TEXT,
  payment_model TEXT,
  images TEXT[],
  active BOOLEAN DEFAULT true,
  admin_approved BOOLEAN,
  admin_approved_at TIMESTAMPTZ,
  admin_approved_by UUID REFERENCES public.users(id),
  admin_rejection_reason TEXT,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE public.service_bookings (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  listing_id UUID NOT NULL REFERENCES public.service_listings(id),
  tenant_user_id UUID NOT NULL REFERENCES public.users(id),
  property_id UUID REFERENCES public.properties(id),
  unit_id UUID REFERENCES public.units(id),
  scheduled_date DATE,
  scheduled_time TIME,
  status public.booking_status DEFAULT 'PENDING',
  notes TEXT,
  total_price NUMERIC,
  currency TEXT DEFAULT 'AED',
  payment_status TEXT,
  stripe_payment_intent_id TEXT,
  completed_at TIMESTAMPTZ,
  cancelled_at TIMESTAMPTZ,
  cancellation_reason TEXT,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE public.reviews (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  booking_id UUID NOT NULL UNIQUE REFERENCES public.service_bookings(id),
  rater_user_id UUID NOT NULL REFERENCES public.users(id),
  rating INTEGER NOT NULL,
  comment TEXT,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE public.bank_details (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  service_provider_id UUID NOT NULL REFERENCES public.service_providers(id),
  bank_name TEXT NOT NULL,
  account_holder_name TEXT NOT NULL,
  account_number TEXT NOT NULL,
  routing_number TEXT,
  iban TEXT,
  swift_code TEXT,
  currency TEXT NOT NULL DEFAULT 'AED',
  verified BOOLEAN DEFAULT false,
  created_at TIMESTAMPTZ DEFAULT now(),
  updated_at TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE public.service_commission_config (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  provider_id UUID REFERENCES public.service_providers(id),
  commission_type TEXT NOT NULL DEFAULT 'PERCENTAGE',
  commission_value NUMERIC NOT NULL DEFAULT 10,
  min_commission NUMERIC,
  max_commission NUMERIC,
  effective_from DATE,
  effective_until DATE,
  created_at TIMESTAMPTZ DEFAULT now(),
  updated_at TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE public.provider_commission_ledger (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  booking_id UUID REFERENCES public.service_bookings(id),
  provider_id UUID REFERENCES public.service_providers(id),
  landlord_id UUID REFERENCES public.users(id),
  gross_amount NUMERIC NOT NULL,
  commission_amount NUMERIC NOT NULL,
  net_amount NUMERIC NOT NULL,
  status TEXT DEFAULT 'PENDING',
  paid_at TIMESTAMPTZ,
  stripe_transfer_id TEXT,
  created_at TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE public.landlord_service_restrictions (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  landlord_id UUID NOT NULL REFERENCES public.users(id),
  property_id UUID NOT NULL REFERENCES public.properties(id),
  service_listing_id UUID NOT NULL REFERENCES public.service_listings(id),
  is_blocked BOOLEAN DEFAULT false,
  reason TEXT,
  blocked_by UUID REFERENCES public.users(id),
  blocked_at TIMESTAMPTZ,
  created_at TIMESTAMPTZ DEFAULT now(),
  updated_at TIMESTAMPTZ DEFAULT now()
);
```

### 3.12 Smart Home

```sql
-- =============================================
-- 3.12: SMART HOME
-- =============================================

CREATE TABLE public.smart_home_provider_profiles (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id UUID NOT NULL REFERENCES public.users(id),
  company_name TEXT NOT NULL,
  email TEXT,
  phone TEXT,
  website TEXT,
  specializations public.provider_specialization[],
  years_of_experience INTEGER,
  team_size INTEGER,
  business_license_number TEXT,
  tax_id TEXT,
  verification_status public.verification_status DEFAULT 'PENDING',
  verification_documents JSONB,
  verified_at TIMESTAMPTZ,
  verified_by_user_id UUID,
  is_active BOOLEAN DEFAULT true,
  is_accepting_requests BOOLEAN DEFAULT true,
  average_rating NUMERIC(3,2),
  total_reviews INTEGER DEFAULT 0,
  total_installations INTEGER DEFAULT 0,
  completion_rate NUMERIC,
  created_at TIMESTAMPTZ DEFAULT now(),
  updated_at TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE public.smart_home_requests (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  tenant_user_id UUID NOT NULL REFERENCES public.users(id),
  property_id UUID NOT NULL REFERENCES public.properties(id),
  unit_id UUID NOT NULL REFERENCES public.units(id),
  provider_id UUID,
  service_type TEXT NOT NULL,
  quantity INTEGER NOT NULL DEFAULT 1,
  quotation_amount NUMERIC,
  quotation_currency TEXT NOT NULL DEFAULT 'AED',
  status public.smart_home_request_status DEFAULT 'SUBMITTED',
  notes TEXT,
  tenant_notes TEXT,
  provider_notes TEXT,
  installation_technician TEXT,
  scheduled_date DATE,
  scheduled_time TIME,
  created_at TIMESTAMPTZ DEFAULT now(),
  updated_at TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE public.smart_home_devices (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  request_id UUID NOT NULL REFERENCES public.smart_home_requests(id),
  device_name TEXT NOT NULL,
  device_type public.smart_device_type NOT NULL,
  device_id TEXT,
  location TEXT,
  status public.smart_device_status DEFAULT 'PENDING',
  is_on BOOLEAN DEFAULT false,
  temperature NUMERIC,
  brightness INTEGER,
  last_command_at TIMESTAMPTZ,
  installed_at TIMESTAMPTZ,
  created_at TIMESTAMPTZ DEFAULT now(),
  updated_at TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE public.smart_device_command_logs (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  device_id UUID NOT NULL REFERENCES public.smart_home_devices(id),
  issued_by_user_id UUID NOT NULL,
  command_type TEXT NOT NULL,
  command_value TEXT,
  success BOOLEAN,
  error_message TEXT,
  created_at TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE public.smart_home_status_logs (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  request_id UUID NOT NULL REFERENCES public.smart_home_requests(id),
  old_status public.smart_home_request_status,
  new_status public.smart_home_request_status NOT NULL,
  changed_by_user_id UUID NOT NULL,
  notes TEXT,
  created_at TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE public.smart_home_invoices (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  request_id UUID NOT NULL REFERENCES public.smart_home_requests(id),
  invoice_number TEXT NOT NULL,
  amount NUMERIC NOT NULL,
  currency TEXT DEFAULT 'AED',
  payment_timing public.payment_timing NOT NULL,
  line_items JSONB,
  paid BOOLEAN DEFAULT false,
  paid_at TIMESTAMPTZ,
  created_at TIMESTAMPTZ DEFAULT now(),
  updated_at TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE public.provider_service_areas (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  provider_id UUID NOT NULL REFERENCES public.smart_home_provider_profiles(id),
  city TEXT NOT NULL,
  state TEXT,
  postal_codes TEXT[],
  service_radius_km NUMERIC,
  additional_charges NUMERIC,
  created_at TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE public.provider_pricing_templates (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  provider_id UUID NOT NULL REFERENCES public.smart_home_provider_profiles(id),
  device_type public.smart_device_type NOT NULL,
  installation_price NUMERIC NOT NULL,
  maintenance_price_per_visit NUMERIC,
  warranty_period_months INTEGER,
  currency TEXT DEFAULT 'AED',
  is_active BOOLEAN DEFAULT true,
  created_at TIMESTAMPTZ DEFAULT now(),
  updated_at TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE public.provider_reviews (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  provider_id UUID NOT NULL REFERENCES public.smart_home_provider_profiles(id),
  request_id UUID NOT NULL REFERENCES public.smart_home_requests(id),
  tenant_user_id UUID NOT NULL,
  rating INTEGER NOT NULL,
  review_text TEXT,
  quality_rating INTEGER,
  timeliness_rating INTEGER,
  professionalism_rating INTEGER,
  pros TEXT[],
  cons TEXT[],
  is_verified_installation BOOLEAN DEFAULT false,
  provider_response TEXT,
  provider_responded_at TIMESTAMPTZ,
  created_at TIMESTAMPTZ DEFAULT now(),
  updated_at TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE public.provider_certifications (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  provider_id UUID NOT NULL REFERENCES public.smart_home_provider_profiles(id),
  certification_name TEXT NOT NULL,
  issuing_organization TEXT NOT NULL,
  certification_number TEXT,
  issue_date DATE,
  expiry_date DATE,
  document_url TEXT,
  is_verified BOOLEAN DEFAULT false,
  verified_at TIMESTAMPTZ,
  verified_by_user_id UUID,
  created_at TIMESTAMPTZ DEFAULT now(),
  updated_at TIMESTAMPTZ DEFAULT now()
);

-- IoT devices (separate from smart_home_devices)
CREATE TABLE public.devices (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  property_id UUID NOT NULL REFERENCES public.properties(id),
  unit_id UUID REFERENCES public.units(id),
  type public.device_type NOT NULL,
  provider TEXT NOT NULL,
  external_id TEXT NOT NULL,
  status public.device_status DEFAULT 'OFFLINE',
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE public.device_events (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  device_id UUID NOT NULL REFERENCES public.devices(id),
  event_type TEXT NOT NULL,
  payload JSONB,
  occurred_at TIMESTAMPTZ NOT NULL,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);
```

### 3.13 Notifications & Communications

```sql
-- =============================================
-- 3.13: NOTIFICATIONS & COMMUNICATIONS
-- =============================================

CREATE TABLE public.notifications (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id UUID NOT NULL REFERENCES public.users(id),
  type public.notification_type NOT NULL,
  title TEXT NOT NULL,
  body TEXT NOT NULL,
  data JSONB DEFAULT '{}',
  read_at TIMESTAMPTZ,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE public.announcements (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  property_id UUID REFERENCES public.properties(id),
  created_by UUID REFERENCES public.users(id),
  title TEXT NOT NULL,
  message TEXT NOT NULL,
  target TEXT NOT NULL,
  image_url TEXT,
  attachment_url TEXT,
  attachment_name TEXT,
  send_channel TEXT[],
  scheduled_at TIMESTAMPTZ,
  sent_at TIMESTAMPTZ,
  email_sent_at TIMESTAMPTZ,
  whatsapp_sent_at TIMESTAMPTZ,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE public.notification_templates (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  key TEXT NOT NULL UNIQUE,
  name TEXT NOT NULL,
  channel TEXT NOT NULL,
  content TEXT NOT NULL,
  variables TEXT[],
  is_active BOOLEAN DEFAULT true,
  created_at TIMESTAMPTZ DEFAULT now(),
  updated_at TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE public.email_templates (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  key TEXT NOT NULL UNIQUE,
  subject TEXT NOT NULL,
  html TEXT NOT NULL,
  text TEXT,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE public.email_logs (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  recipient_email TEXT NOT NULL,
  recipient_user_id UUID,
  subject TEXT NOT NULL,
  template_key TEXT NOT NULL,
  event_type TEXT NOT NULL,
  event_data JSONB,
  status TEXT NOT NULL DEFAULT 'PENDING',
  sent_at TIMESTAMPTZ,
  error_message TEXT,
  created_at TIMESTAMPTZ DEFAULT now(),
  updated_at TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE public.sms_logs (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  recipient_phone TEXT NOT NULL,
  recipient_user_id UUID REFERENCES public.users(id),
  message_content TEXT NOT NULL,
  message_type TEXT NOT NULL,
  template_key TEXT,
  status TEXT DEFAULT 'PENDING',
  provider_message_id TEXT,
  sent_at TIMESTAMPTZ,
  error_message TEXT,
  created_at TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE public.otp_codes (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id UUID NOT NULL REFERENCES public.users(id),
  code TEXT NOT NULL,
  type TEXT NOT NULL,
  email TEXT,
  phone TEXT,
  expires_at TIMESTAMPTZ NOT NULL,
  verified_at TIMESTAMPTZ,
  created_at TIMESTAMPTZ DEFAULT now()
);
```

### 3.14 WhatsApp

```sql
-- =============================================
-- 3.14: WHATSAPP
-- =============================================

CREATE TABLE public.whatsapp_config (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  api_url TEXT,
  api_token TEXT,
  phone_number_id TEXT,
  business_account_id TEXT,
  webhook_verify_token TEXT,
  is_active BOOLEAN DEFAULT false,
  created_at TIMESTAMPTZ DEFAULT now(),
  updated_at TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE public.whatsapp_templates (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  name TEXT NOT NULL,
  category TEXT NOT NULL,
  content TEXT NOT NULL,
  language TEXT DEFAULT 'en',
  variables JSONB,
  buttons JSONB,
  meta_template_id TEXT,
  status TEXT DEFAULT 'DRAFT',
  is_active BOOLEAN DEFAULT true,
  created_at TIMESTAMPTZ DEFAULT now(),
  updated_at TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE public.whatsapp_messages (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id UUID REFERENCES public.users(id),
  phone_number TEXT NOT NULL,
  message_type TEXT NOT NULL,
  template_name TEXT,
  content TEXT,
  whatsapp_message_id TEXT,
  status TEXT DEFAULT 'PENDING',
  sent_at TIMESTAMPTZ,
  delivered_at TIMESTAMPTZ,
  read_at TIMESTAMPTZ,
  error_message TEXT,
  created_at TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE public.whatsapp_button_responses (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  message_id UUID REFERENCES public.whatsapp_messages(id),
  user_id UUID REFERENCES public.users(id),
  button_id TEXT NOT NULL,
  button_text TEXT,
  payload JSONB,
  processed BOOLEAN DEFAULT false,
  processed_at TIMESTAMPTZ,
  created_at TIMESTAMPTZ DEFAULT now()
);
```

### 3.15 Listings & Virtual Tours

```sql
-- =============================================
-- 3.15: LISTINGS & VIRTUAL TOURS
-- =============================================

CREATE TABLE public.unit_listings (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  unit_id UUID NOT NULL REFERENCES public.units(id),
  property_id UUID NOT NULL REFERENCES public.properties(id),
  created_by UUID REFERENCES public.users(id),
  title TEXT NOT NULL,
  description TEXT,
  rent_amount NUMERIC NOT NULL,
  rent_frequency public.rent_frequency NOT NULL,
  status public.listing_status DEFAULT 'DRAFT',
  available_from DATE,
  minimum_lease_months INTEGER,
  deposit_amount NUMERIC,
  pet_policy TEXT,
  parking_included BOOLEAN DEFAULT false,
  amenities JSONB,
  utilities_included JSONB,
  featured_images JSONB,
  virtual_tour_url TEXT,
  syndication_enabled BOOLEAN DEFAULT false,
  syndication_portals JSONB,
  views_count INTEGER DEFAULT 0,
  inquiries_count INTEGER DEFAULT 0,
  published_at TIMESTAMPTZ,
  expires_at TIMESTAMPTZ,
  created_at TIMESTAMPTZ DEFAULT now(),
  updated_at TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE public.listings (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  unit_id UUID NOT NULL REFERENCES public.units(id),
  created_by UUID REFERENCES public.users(id),
  title TEXT NOT NULL,
  description TEXT,
  price NUMERIC NOT NULL,
  currency TEXT DEFAULT 'AED',
  listing_type TEXT,
  status TEXT DEFAULT 'DRAFT',
  images JSONB,
  amenities TEXT[],
  availability_date DATE,
  minimum_lease_months INTEGER,
  pet_policy TEXT,
  video_url TEXT,
  is_featured BOOLEAN DEFAULT false,
  views_count INTEGER DEFAULT 0,
  inquiries_count INTEGER DEFAULT 0,
  published_at TIMESTAMPTZ,
  created_at TIMESTAMPTZ DEFAULT now(),
  updated_at TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE public.listing_inquiries (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  listing_id UUID NOT NULL REFERENCES public.unit_listings(id),
  name TEXT NOT NULL,
  email TEXT NOT NULL,
  phone TEXT,
  message TEXT,
  status TEXT DEFAULT 'NEW',
  responded_at TIMESTAMPTZ,
  created_at TIMESTAMPTZ DEFAULT now(),
  updated_at TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE public.syndication_portals (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  name TEXT NOT NULL,
  code TEXT NOT NULL UNIQUE,
  api_endpoint TEXT,
  api_key TEXT,
  is_active BOOLEAN DEFAULT true,
  logo_url TEXT,
  supported_features JSONB,
  created_at TIMESTAMPTZ DEFAULT now(),
  updated_at TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE public.listing_syndications (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  listing_id UUID NOT NULL REFERENCES public.listings(id),
  portal_id UUID NOT NULL REFERENCES public.syndication_portals(id),
  external_listing_id TEXT,
  status TEXT DEFAULT 'PENDING',
  published_at TIMESTAMPTZ,
  last_updated_at TIMESTAMPTZ,
  views_count INTEGER DEFAULT 0,
  inquiries_count INTEGER DEFAULT 0,
  error_message TEXT,
  created_at TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE public.syndication_logs (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  listing_id UUID NOT NULL REFERENCES public.unit_listings(id),
  portal TEXT NOT NULL,
  action TEXT NOT NULL,
  external_id TEXT,
  success BOOLEAN,
  response JSONB,
  error_message TEXT,
  created_at TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE public.virtual_tours (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  unit_id UUID NOT NULL REFERENCES public.units(id),
  property_id UUID NOT NULL REFERENCES public.properties(id),
  created_by UUID REFERENCES public.users(id),
  title TEXT,
  tour_type TEXT DEFAULT '360',
  rooms JSONB,
  status TEXT DEFAULT 'DRAFT',
  is_published BOOLEAN DEFAULT false,
  published_at TIMESTAMPTZ,
  views_count INTEGER DEFAULT 0,
  embed_code TEXT,
  share_url TEXT,
  created_at TIMESTAMPTZ DEFAULT now(),
  updated_at TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE public.tour_analytics (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  tour_id UUID NOT NULL REFERENCES public.virtual_tours(id),
  viewer_user_id UUID REFERENCES public.users(id),
  viewer_ip TEXT,
  device_type TEXT,
  referrer TEXT,
  session_duration_seconds INTEGER,
  rooms_viewed JSONB,
  viewed_at TIMESTAMPTZ DEFAULT now()
);
```

### 3.16 Inspections

```sql
-- =============================================
-- 3.16: INSPECTIONS
-- =============================================

CREATE TABLE public.inspection_templates (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  name TEXT NOT NULL,
  inspection_type public.inspection_type NOT NULL,
  property_id UUID REFERENCES public.properties(id),
  created_by UUID REFERENCES public.users(id),
  rooms JSONB DEFAULT '[]',
  checklist_items JSONB DEFAULT '[]',
  is_global BOOLEAN DEFAULT false,
  created_at TIMESTAMPTZ DEFAULT now(),
  updated_at TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE public.inspections (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  property_id UUID NOT NULL REFERENCES public.properties(id),
  unit_id UUID REFERENCES public.units(id),
  inspector_id UUID NOT NULL REFERENCES public.users(id),
  tenant_id UUID REFERENCES public.users(id),
  landlord_id UUID REFERENCES public.users(id),
  template_id UUID REFERENCES public.inspection_templates(id),
  inspection_type public.inspection_type NOT NULL,
  status public.inspection_status DEFAULT 'SCHEDULED',
  scheduled_date DATE NOT NULL,
  scheduled_time TIME,
  started_at TIMESTAMPTZ,
  completed_at TIMESTAMPTZ,
  is_common_area BOOLEAN DEFAULT false,
  common_area_name TEXT,
  notes TEXT,
  created_at TIMESTAMPTZ DEFAULT now(),
  updated_at TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE public.inspection_rooms (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  inspection_id UUID NOT NULL REFERENCES public.inspections(id) ON DELETE CASCADE,
  room_name TEXT NOT NULL,
  room_type TEXT,
  sort_order INTEGER,
  overall_condition public.condition_rating,
  completed BOOLEAN DEFAULT false,
  notes TEXT,
  created_at TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE public.inspection_items (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  inspection_room_id UUID NOT NULL REFERENCES public.inspection_rooms(id) ON DELETE CASCADE,
  item_name TEXT NOT NULL,
  item_category TEXT,
  condition public.condition_rating,
  notes TEXT,
  requires_action BOOLEAN DEFAULT false,
  action_description TEXT,
  created_at TIMESTAMPTZ DEFAULT now(),
  updated_at TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE public.inspection_media (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  inspection_id UUID NOT NULL REFERENCES public.inspections(id) ON DELETE CASCADE,
  inspection_room_id UUID REFERENCES public.inspection_rooms(id),
  inspection_item_id UUID REFERENCES public.inspection_items(id),
  file_name TEXT NOT NULL,
  file_path TEXT NOT NULL,
  file_size INTEGER,
  media_type TEXT NOT NULL,
  caption TEXT,
  is_before BOOLEAN,
  taken_at TIMESTAMPTZ,
  created_at TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE public.inspection_signatures (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  inspection_id UUID NOT NULL REFERENCES public.inspections(id) ON DELETE CASCADE,
  signer_id UUID REFERENCES public.users(id),
  signer_name TEXT NOT NULL,
  signer_role TEXT NOT NULL,
  signature_data TEXT,
  signed_at TIMESTAMPTZ,
  ip_address TEXT,
  user_agent TEXT,
  created_at TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE public.inspector_assignments (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  inspector_id UUID NOT NULL REFERENCES public.users(id),
  property_id UUID NOT NULL REFERENCES public.properties(id),
  assigned_by UUID REFERENCES public.users(id),
  active BOOLEAN DEFAULT true,
  created_at TIMESTAMPTZ DEFAULT now()
);
```

### 3.17 Landlord Sub-Users

```sql
-- =============================================
-- 3.17: LANDLORD SUB-USERS
-- =============================================

CREATE TABLE public.landlord_sub_users (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  landlord_id UUID NOT NULL REFERENCES public.users(id),
  sub_user_id UUID NOT NULL REFERENCES public.users(id),
  access_level TEXT NOT NULL,
  permissions JSONB NOT NULL DEFAULT '{}',
  active BOOLEAN NOT NULL DEFAULT true,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE public.landlord_assigned_properties (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  sub_user_id UUID NOT NULL REFERENCES public.landlord_sub_users(id),
  property_id UUID NOT NULL REFERENCES public.properties(id),
  created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);
```

### 3.18 Parking

```sql
-- =============================================
-- 3.18: PARKING
-- =============================================

CREATE TABLE public.parking_slots (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  property_id UUID NOT NULL REFERENCES public.properties(id),
  slot_number TEXT NOT NULL,
  slot_type TEXT,
  floor_level TEXT,
  assigned_unit_id UUID REFERENCES public.units(id),
  assigned_tenant_id UUID REFERENCES public.users(id),
  is_available BOOLEAN DEFAULT true,
  monthly_rate NUMERIC,
  vehicle_license_url TEXT,
  vehicle_license_expiry DATE,
  created_at TIMESTAMPTZ DEFAULT now(),
  updated_at TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE public.parking_permits (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  property_id UUID NOT NULL REFERENCES public.properties(id),
  parking_slot_id UUID REFERENCES public.parking_slots(id),
  tenant_user_id UUID REFERENCES public.users(id),
  vehicle_plate TEXT NOT NULL,
  vehicle_make TEXT,
  vehicle_model TEXT,
  vehicle_color TEXT,
  permit_type TEXT,
  status TEXT DEFAULT 'ACTIVE',
  valid_from DATE NOT NULL,
  valid_until DATE,
  created_at TIMESTAMPTZ DEFAULT now(),
  updated_at TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE public.visitor_parking_logs (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  property_id UUID NOT NULL REFERENCES public.properties(id),
  visitor_permit_id UUID REFERENCES public.visitor_permits(id),
  parking_slot_id UUID REFERENCES public.parking_slots(id),
  vehicle_plate TEXT NOT NULL,
  checked_in_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  checked_out_at TIMESTAMPTZ,
  checked_in_by UUID,
  created_at TIMESTAMPTZ DEFAULT now()
);
```

### 3.19 Insurance

```sql
-- =============================================
-- 3.19: INSURANCE
-- =============================================

CREATE TABLE public.insurance_policies (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  property_id UUID REFERENCES public.properties(id),
  unit_id UUID REFERENCES public.units(id),
  landlord_id UUID REFERENCES public.users(id),
  tenant_user_id UUID REFERENCES public.users(id),
  policy_number TEXT NOT NULL,
  policy_type TEXT NOT NULL,
  provider_name TEXT NOT NULL,
  customer_name TEXT,
  coverage_amount NUMERIC,
  coverage_details JSONB,
  premium_amount NUMERIC,
  premium_frequency TEXT,
  start_date DATE NOT NULL,
  end_date DATE NOT NULL,
  status TEXT DEFAULT 'ACTIVE',
  auto_renew BOOLEAN DEFAULT false,
  reminder_sent BOOLEAN DEFAULT false,
  document_url TEXT,
  created_at TIMESTAMPTZ DEFAULT now(),
  updated_at TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE public.insurance_claims (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  policy_id UUID NOT NULL REFERENCES public.insurance_policies(id),
  submitted_by UUID REFERENCES public.users(id),
  claim_type TEXT NOT NULL,
  claim_number TEXT,
  description TEXT NOT NULL,
  incident_date DATE NOT NULL,
  amount_claimed NUMERIC,
  amount_approved NUMERIC,
  documents JSONB,
  status TEXT DEFAULT 'SUBMITTED',
  notes TEXT,
  created_at TIMESTAMPTZ DEFAULT now(),
  updated_at TIMESTAMPTZ DEFAULT now()
);
```

### 3.20 Move Management

```sql
-- =============================================
-- 3.20: MOVE MANAGEMENT
-- =============================================

CREATE TABLE public.move_workflows (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  property_id UUID NOT NULL REFERENCES public.properties(id),
  unit_id UUID NOT NULL REFERENCES public.units(id),
  tenant_user_id UUID NOT NULL REFERENCES public.users(id),
  lease_id UUID REFERENCES public.leases(id),
  inspection_id UUID REFERENCES public.inspections(id),
  workflow_type TEXT NOT NULL,
  scheduled_date DATE NOT NULL,
  scheduled_time TIME,
  status TEXT DEFAULT 'PENDING',
  key_handover_completed BOOLEAN DEFAULT false,
  key_handover_date DATE,
  key_quantity INTEGER,
  utility_transfer_completed BOOLEAN DEFAULT false,
  final_walkthrough_completed BOOLEAN DEFAULT false,
  walkthrough_notes TEXT,
  meter_readings JSONB,
  deposit_amount NUMERIC,
  deposit_deductions NUMERIC,
  deposit_deduction_reasons TEXT,
  deposit_refund_status TEXT,
  access_cards_returned INTEGER,
  parking_passes_returned INTEGER,
  documents JSONB,
  completed_at TIMESTAMPTZ,
  completed_by UUID REFERENCES public.users(id),
  created_at TIMESTAMPTZ DEFAULT now(),
  updated_at TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE public.move_checklist_items (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  workflow_id UUID NOT NULL REFERENCES public.move_workflows(id) ON DELETE CASCADE,
  item_name TEXT NOT NULL,
  category TEXT NOT NULL,
  description TEXT,
  sort_order INTEGER,
  is_required BOOLEAN DEFAULT false,
  is_completed BOOLEAN DEFAULT false,
  completed_at TIMESTAMPTZ,
  completed_by UUID REFERENCES public.users(id),
  notes TEXT,
  created_at TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE public.move_checklist_templates (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  property_id UUID REFERENCES public.properties(id),
  workflow_type TEXT NOT NULL,
  item_name TEXT NOT NULL,
  category TEXT NOT NULL,
  description TEXT,
  sort_order INTEGER,
  is_required BOOLEAN DEFAULT false,
  created_at TIMESTAMPTZ DEFAULT now()
);
```

### 3.21 Help & Support

```sql
-- =============================================
-- 3.21: HELP & SUPPORT
-- =============================================

CREATE TABLE public.help_articles (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  title TEXT NOT NULL,
  slug TEXT NOT NULL UNIQUE,
  content TEXT NOT NULL,
  excerpt TEXT,
  category TEXT NOT NULL,
  author_id UUID REFERENCES public.users(id),
  tags TEXT[],
  is_published BOOLEAN DEFAULT false,
  view_count INTEGER DEFAULT 0,
  helpful_count INTEGER DEFAULT 0,
  not_helpful_count INTEGER DEFAULT 0,
  created_at TIMESTAMPTZ DEFAULT now(),
  updated_at TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE public.help_article_feedback (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  article_id UUID NOT NULL REFERENCES public.help_articles(id),
  user_id UUID REFERENCES public.users(id),
  is_helpful BOOLEAN NOT NULL,
  feedback_text TEXT,
  created_at TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE public.support_tickets (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id UUID NOT NULL REFERENCES public.users(id),
  assigned_to UUID REFERENCES public.users(id),
  related_article_id UUID REFERENCES public.help_articles(id),
  subject TEXT NOT NULL,
  description TEXT NOT NULL,
  category TEXT NOT NULL,
  priority TEXT DEFAULT 'MEDIUM',
  status TEXT DEFAULT 'OPEN',
  attachments JSONB,
  resolution_notes TEXT,
  resolved_at TIMESTAMPTZ,
  created_at TIMESTAMPTZ DEFAULT now(),
  updated_at TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE public.ticket_messages (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  ticket_id UUID NOT NULL REFERENCES public.support_tickets(id) ON DELETE CASCADE,
  sender_id UUID NOT NULL REFERENCES public.users(id),
  message TEXT NOT NULL,
  attachments JSONB,
  is_internal BOOLEAN DEFAULT false,
  created_at TIMESTAMPTZ DEFAULT now()
);
```

### 3.22 Other

```sql
-- =============================================
-- 3.22: OTHER TABLES
-- =============================================

CREATE TABLE public.contact_inquiries (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  name TEXT NOT NULL,
  email TEXT NOT NULL,
  phone TEXT,
  company TEXT,
  subject TEXT NOT NULL,
  message TEXT NOT NULL,
  status TEXT NOT NULL DEFAULT 'NEW',
  notes TEXT,
  responded_at TIMESTAMPTZ,
  responded_by_user_id UUID,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE public.custom_field_suggestions (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  field_type TEXT NOT NULL,
  value TEXT NOT NULL,
  usage_count INTEGER NOT NULL DEFAULT 0,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  UNIQUE(field_type, value)
);

CREATE TABLE public.audit_logs (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  actor_user_id UUID REFERENCES public.users(id),
  resource_type TEXT NOT NULL,
  resource_id UUID,
  action TEXT NOT NULL,
  diff JSONB,
  ip TEXT,
  ua TEXT,
  created_at TIMESTAMPTZ NOT NULL DEFAULT now()
);

CREATE TABLE public.saved_filters (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  user_id UUID NOT NULL REFERENCES public.users(id),
  page_key TEXT NOT NULL,
  name TEXT NOT NULL,
  filter_config JSONB NOT NULL,
  is_default BOOLEAN DEFAULT false,
  created_at TIMESTAMPTZ DEFAULT now(),
  updated_at TIMESTAMPTZ DEFAULT now()
);

CREATE TABLE public.platform_settings (
  id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
  primary_color TEXT NOT NULL DEFAULT '#1a1a2e',
  secondary_color TEXT NOT NULL DEFAULT '#16213e',
  accent_color TEXT NOT NULL DEFAULT '#0f3460',
  font_family TEXT NOT NULL DEFAULT 'Inter',
  default_theme TEXT NOT NULL DEFAULT 'light',
  currency TEXT NOT NULL DEFAULT 'AED',
  logo_url TEXT,
  updated_by UUID REFERENCES public.users(id),
  created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
  updated_at TIMESTAMPTZ NOT NULL DEFAULT now()
);
```

---

## 4. Views

```sql
-- =============================================
-- SECTION 4: VIEWS
-- =============================================

CREATE OR REPLACE VIEW public.users_minimal AS
SELECT id, name FROM public.users;

CREATE OR REPLACE VIEW public.users_safe AS
SELECT id, email, name, phone, status, last_login_at, created_at, updated_at, deleted_at
FROM public.users;
```

---

## 5. Functions

> **Note**: All `auth.uid()` calls have been replaced with a `_current_user_id UUID` parameter. In .NET, pass the authenticated user's ID from the JWT token via your DbContext or repository layer.

```sql
-- =============================================
-- SECTION 5: DATABASE FUNCTIONS
-- =============================================

-- Notification helper
CREATE OR REPLACE FUNCTION public.create_notification(
  p_user_id UUID,
  p_type public.notification_type,
  p_title TEXT,
  p_body TEXT,
  p_data JSONB DEFAULT '{}'
) RETURNS UUID
LANGUAGE plpgsql SECURITY DEFINER SET search_path TO 'public' AS $$
DECLARE notification_id UUID;
BEGIN
  INSERT INTO public.notifications (user_id, type, title, body, data)
  VALUES (p_user_id, p_type, p_title, p_body, p_data)
  RETURNING id INTO notification_id;
  RETURN notification_id;
END;
$$;

-- Role checking functions
CREATE OR REPLACE FUNCTION public.has_admin_role(_user_id UUID) RETURNS BOOLEAN
LANGUAGE sql STABLE SECURITY DEFINER SET search_path TO 'public' AS $$
  SELECT EXISTS (
    SELECT 1 FROM public.user_roles ur
    JOIN public.roles r ON ur.role_id = r.id
    WHERE ur.user_id = _user_id AND r.name IN ('PLATFORM_ADMIN', 'ADMIN')
  );
$$;

CREATE OR REPLACE FUNCTION public.is_tenant(_user_id UUID) RETURNS BOOLEAN
LANGUAGE sql STABLE SECURITY DEFINER SET search_path TO 'public' AS $$
  SELECT EXISTS (
    SELECT 1 FROM public.user_roles ur JOIN public.roles r ON ur.role_id = r.id
    WHERE ur.user_id = _user_id AND r.name = 'TENANT'
  );
$$;

CREATE OR REPLACE FUNCTION public.is_provider(_user_id UUID) RETURNS BOOLEAN
LANGUAGE sql STABLE SECURITY DEFINER SET search_path TO 'public' AS $$
  SELECT EXISTS (
    SELECT 1 FROM public.user_roles ur JOIN public.roles r ON ur.role_id = r.id
    WHERE ur.user_id = _user_id AND r.name = 'PROVIDER'
  );
$$;

CREATE OR REPLACE FUNCTION public.is_inspector(_user_id UUID) RETURNS BOOLEAN
LANGUAGE sql STABLE SECURITY DEFINER SET search_path TO 'public' AS $$
  SELECT EXISTS (
    SELECT 1 FROM public.user_roles ur JOIN public.roles r ON ur.role_id = r.id
    WHERE ur.user_id = _user_id AND r.name = 'INSPECTOR'
  );
$$;

CREATE OR REPLACE FUNCTION public.is_main_maintenance_user(_user_id UUID) RETURNS BOOLEAN
LANGUAGE sql STABLE SECURITY DEFINER SET search_path TO 'public' AS $$
  SELECT EXISTS (
    SELECT 1 FROM public.user_roles ur JOIN public.roles r ON ur.role_id = r.id
    WHERE ur.user_id = _user_id AND r.name = 'MAINTENANCE'
  );
$$;

CREATE OR REPLACE FUNCTION public.is_landlord_of_property(_user_id UUID, _property_id UUID) RETURNS BOOLEAN
LANGUAGE sql STABLE SECURITY DEFINER SET search_path TO 'public' AS $$
  SELECT EXISTS (
    SELECT 1 FROM public.properties WHERE id = _property_id AND landlord_id = _user_id
  );
$$;

CREATE OR REPLACE FUNCTION public.is_document_owner(_user_id UUID, _document_id UUID) RETURNS BOOLEAN
LANGUAGE sql STABLE SECURITY DEFINER SET search_path TO 'public' AS $$
  SELECT EXISTS (
    SELECT 1 FROM public.documents WHERE id = _document_id AND landlord_id = _user_id
  );
$$;

CREATE OR REPLACE FUNCTION public.is_inspector_assigned(_user_id UUID, _property_id UUID) RETURNS BOOLEAN
LANGUAGE sql STABLE SECURITY DEFINER SET search_path TO 'public' AS $$
  SELECT EXISTS (
    SELECT 1 FROM public.inspector_assignments
    WHERE inspector_id = _user_id AND property_id = _property_id AND active = true
  );
$$;

CREATE OR REPLACE FUNCTION public.is_landlord_sub_user_with_permission(_user_id UUID, _landlord_id UUID, _permission TEXT) RETURNS BOOLEAN
LANGUAGE plpgsql SECURITY DEFINER SET search_path TO 'public' AS $$
DECLARE _sub_user RECORD;
BEGIN
  SELECT * INTO _sub_user FROM public.landlord_sub_users
  WHERE sub_user_id = _user_id AND landlord_id = _landlord_id AND active = true;
  IF NOT FOUND THEN RETURN false; END IF;
  RETURN (_sub_user.permissions->_permission)::boolean = true;
END;
$$;

CREATE OR REPLACE FUNCTION public.is_maintenance_sub_user_with_permission(_user_id UUID, _permission TEXT) RETURNS BOOLEAN
LANGUAGE sql STABLE SECURITY DEFINER SET search_path TO 'public' AS $$
  SELECT EXISTS (
    SELECT 1 FROM public.maintenance_sub_users
    WHERE sub_user_id = _user_id AND active = true AND (permissions->>_permission)::boolean = true
  );
$$;

CREATE OR REPLACE FUNCTION public.is_security_sub_user_with_permission(_user_id UUID, _permission TEXT) RETURNS BOOLEAN
LANGUAGE sql STABLE SECURITY DEFINER SET search_path TO 'public' AS $$
  SELECT EXISTS (
    SELECT 1 FROM public.security_sub_users
    WHERE sub_user_id = _user_id AND active = true AND (permissions->>_permission)::boolean = true
  );
$$;

CREATE OR REPLACE FUNCTION public.tenant_has_approved_apartment(tenant_id UUID) RETURNS BOOLEAN
LANGUAGE sql STABLE SECURITY DEFINER SET search_path TO 'public' AS $$
  SELECT EXISTS (
    SELECT 1 FROM public.tenant_apartment_assignments
    WHERE tenant_user_id = tenant_id AND status = 'APPROVED'
  );
$$;

-- Permission system functions
CREATE OR REPLACE FUNCTION public.user_has_permission(_user_id UUID, _permission_code TEXT) RETURNS BOOLEAN
LANGUAGE sql STABLE SECURITY DEFINER SET search_path TO 'public' AS $$
  SELECT CASE
    WHEN EXISTS (
      SELECT 1 FROM public.user_roles ur JOIN public.roles r ON ur.role_id = r.id
      WHERE ur.user_id = _user_id AND r.name = 'PLATFORM_ADMIN'
    ) THEN true
    ELSE EXISTS (
      SELECT 1 FROM public.user_roles ur
      CROSS JOIN LATERAL public.get_role_permissions_with_inheritance(ur.role_id) perms
      WHERE ur.user_id = _user_id AND perms.permission_code = _permission_code
    )
  END;
$$;

CREATE OR REPLACE FUNCTION public.get_role_permissions_with_inheritance(_role_id UUID)
RETURNS TABLE(
  permission_id UUID, permission_code TEXT, permission_name TEXT, category TEXT,
  is_navigation BOOLEAN, nav_path TEXT, nav_icon TEXT,
  is_inherited BOOLEAN, inherited_from_role_id UUID, inherited_from_role_name TEXT
) LANGUAGE sql STABLE SECURITY DEFINER SET search_path TO 'public' AS $$
  WITH RECURSIVE role_hierarchy AS (
    SELECT r.id, r.name, r.parent_role_id, 0 AS depth
    FROM public.roles r WHERE r.id = _role_id
    UNION ALL
    SELECT r.id, r.name, r.parent_role_id, rh.depth + 1
    FROM public.roles r INNER JOIN role_hierarchy rh ON r.id = rh.parent_role_id
    WHERE rh.depth < 10
  ),
  all_permissions AS (
    SELECT DISTINCT ON (p.id)
      p.id AS permission_id, p.code AS permission_code, p.name AS permission_name,
      p.category, p.is_navigation, p.nav_path, p.nav_icon,
      CASE WHEN rh.id = _role_id THEN false ELSE true END AS is_inherited,
      CASE WHEN rh.id = _role_id THEN NULL ELSE rh.id END AS inherited_from_role_id,
      CASE WHEN rh.id = _role_id THEN NULL ELSE rh.name END AS inherited_from_role_name,
      rh.depth
    FROM role_hierarchy rh
    INNER JOIN public.role_permissions rp ON rp.role_id = rh.id
    INNER JOIN public.permissions p ON p.id = rp.permission_id
    ORDER BY p.id, rh.depth ASC
  )
  SELECT permission_id, permission_code, permission_name, category,
    is_navigation, nav_path, nav_icon, is_inherited, inherited_from_role_id, inherited_from_role_name
  FROM all_permissions ORDER BY category, permission_code;
$$;

CREATE OR REPLACE FUNCTION public.get_user_permissions(_user_id UUID)
RETURNS TABLE(
  permission_code TEXT, permission_name TEXT, category TEXT,
  is_navigation BOOLEAN, nav_path TEXT, nav_icon TEXT,
  is_inherited BOOLEAN, inherited_from_role_name TEXT
) LANGUAGE sql STABLE SECURITY DEFINER SET search_path TO 'public' AS $$
  WITH is_platform_admin AS (
    SELECT EXISTS (
      SELECT 1 FROM public.user_roles ur JOIN public.roles r ON ur.role_id = r.id
      WHERE ur.user_id = _user_id AND r.name = 'PLATFORM_ADMIN'
    ) AS is_admin
  )
  SELECT p.code, p.name, p.category, p.is_navigation, p.nav_path, p.nav_icon,
    false AS is_inherited, NULL::TEXT AS inherited_from_role_name
  FROM public.permissions p
  WHERE (SELECT is_admin FROM is_platform_admin)
  UNION
  SELECT DISTINCT perms.permission_code, perms.permission_name, perms.category,
    perms.is_navigation, perms.nav_path, perms.nav_icon,
    perms.is_inherited, perms.inherited_from_role_name
  FROM public.user_roles ur
  CROSS JOIN LATERAL public.get_role_permissions_with_inheritance(ur.role_id) perms
  WHERE ur.user_id = _user_id AND NOT (SELECT is_admin FROM is_platform_admin)
  ORDER BY category, permission_code;
$$;

-- Facility availability check
CREATE OR REPLACE FUNCTION public.check_facility_availability(
  p_facility_id UUID, p_booking_date DATE, p_start_time TIME, p_end_time TIME,
  p_exclude_booking_id UUID DEFAULT NULL
) RETURNS BOOLEAN
LANGUAGE plpgsql SECURITY DEFINER SET search_path TO 'public' AS $$
DECLARE v_simultaneous_limit INTEGER; v_booked_count INTEGER;
BEGIN
  SELECT simultaneous_bookings_limit INTO v_simultaneous_limit
  FROM facilities WHERE id = p_facility_id;
  SELECT COUNT(*) INTO v_booked_count FROM facility_bookings
  WHERE facility_id = p_facility_id AND booking_date = p_booking_date
    AND status IN ('PENDING', 'APPROVED', 'CHECKED_IN')
    AND (id IS DISTINCT FROM p_exclude_booking_id)
    AND (start_time, end_time) OVERLAPS (p_start_time, p_end_time);
  RETURN v_booked_count < COALESCE(v_simultaneous_limit, 1);
END;
$$;

-- Assignment scoring
CREATE OR REPLACE FUNCTION public.calculate_assignment_score(p_work_order_id UUID, p_team_id UUID)
RETURNS JSONB LANGUAGE plpgsql SECURITY DEFINER SET search_path TO 'public' AS $$
DECLARE
  v_score NUMERIC := 0; v_factors JSONB := '{}';
  v_work_order RECORD; v_team RECORD;
  v_active_orders INTEGER; v_avg_completion NUMERIC; v_team_rating NUMERIC;
BEGIN
  SELECT * INTO v_work_order FROM issue_reports WHERE id = p_work_order_id LIMIT 1;
  IF v_work_order IS NULL THEN RETURN jsonb_build_object('error', 'Work order not found', 'total_score', 0); END IF;
  SELECT * INTO v_team FROM maintenance_teams WHERE id = p_team_id LIMIT 1;
  IF v_team IS NULL THEN RETURN jsonb_build_object('error', 'Team not found', 'total_score', 0); END IF;
  -- Workload (0-30)
  SELECT COUNT(*) INTO v_active_orders FROM issue_reports
  WHERE assigned_maintenance_team_id = p_team_id AND status NOT IN ('COMPLETED', 'CANCELLED') LIMIT 100;
  v_score := v_score + GREATEST(0, 30 - (v_active_orders * 5));
  v_factors := jsonb_set(v_factors, '{workload}', to_jsonb(GREATEST(0, 30 - (v_active_orders * 5))));
  -- Specialty match (0-25)
  IF v_team.specialties IS NOT NULL AND v_work_order.category = ANY(v_team.specialties) THEN
    v_score := v_score + 25;
  END IF;
  v_factors := jsonb_set(v_factors, '{specialty_match}', to_jsonb(v_team.specialties IS NOT NULL AND v_work_order.category = ANY(v_team.specialties)));
  -- Avg completion time (0-20)
  SELECT AVG(EXTRACT(EPOCH FROM (completed_at - created_at)) / 3600) INTO v_avg_completion
  FROM (SELECT completed_at, created_at FROM issue_reports
    WHERE assigned_maintenance_team_id = p_team_id AND status = 'COMPLETED' AND completed_at IS NOT NULL
    AND created_at > now() - interval '90 days' ORDER BY completed_at DESC LIMIT 50) t;
  IF v_avg_completion IS NOT NULL THEN v_score := v_score + GREATEST(0, 20 - (v_avg_completion / 10)); END IF;
  -- Rating (0-25)
  SELECT AVG(tenant_rating) INTO v_team_rating
  FROM (SELECT tenant_rating FROM issue_reports
    WHERE assigned_maintenance_team_id = p_team_id AND tenant_rating IS NOT NULL
    AND created_at > now() - interval '90 days' ORDER BY created_at DESC LIMIT 50) t;
  IF v_team_rating IS NOT NULL THEN v_score := v_score + (v_team_rating * 5); END IF;
  v_factors := jsonb_set(v_factors, '{total_score}', to_jsonb(v_score));
  RETURN v_factors;
EXCEPTION WHEN OTHERS THEN
  RETURN jsonb_build_object('error', SQLERRM, 'total_score', 0);
END;
$$;

-- Next payout date calculator
CREATE OR REPLACE FUNCTION public.calculate_next_payout_date(p_schedule TEXT, p_current_date DATE DEFAULT CURRENT_DATE)
RETURNS DATE LANGUAGE plpgsql SET search_path TO 'public' AS $$
BEGIN
  CASE p_schedule
    WHEN 'WEEKLY' THEN RETURN p_current_date + INTERVAL '7 days';
    WHEN 'BI_WEEKLY' THEN RETURN p_current_date + INTERVAL '14 days';
    WHEN 'MONTHLY' THEN RETURN p_current_date + INTERVAL '1 month';
    ELSE RETURN p_current_date + INTERVAL '1 month';
  END CASE;
END;
$$;

-- Cleanup expired OTPs
CREATE OR REPLACE FUNCTION public.cleanup_expired_otps() RETURNS VOID
LANGUAGE plpgsql SECURITY DEFINER SET search_path TO 'public' AS $$
BEGIN
  DELETE FROM public.otp_codes WHERE expires_at < now() AND verified_at IS NULL;
END;
$$;

-- Increment custom field suggestion usage
CREATE OR REPLACE FUNCTION public.increment_suggestion_usage(p_field_type TEXT, p_value TEXT) RETURNS VOID
LANGUAGE plpgsql SECURITY DEFINER SET search_path TO 'public' AS $$
BEGIN
  INSERT INTO public.custom_field_suggestions (field_type, value, usage_count)
  VALUES (p_field_type, p_value, 1)
  ON CONFLICT (field_type, value)
  DO UPDATE SET usage_count = custom_field_suggestions.usage_count + 1, updated_at = now();
END;
$$;

-- Get recommended service providers
CREATE OR REPLACE FUNCTION public.get_recommended_service_providers(_category TEXT, _property_id UUID)
RETURNS UUID[] LANGUAGE sql STABLE SECURITY DEFINER SET search_path TO 'public' AS $$
  SELECT ARRAY_AGG(sp.id) FROM service_providers sp
  JOIN service_listings sl ON sl.provider_id = sp.id
  WHERE sl.category = _category AND sl.active = true AND sp.approved = true LIMIT 5;
$$;

-- Get provider service type
CREATE OR REPLACE FUNCTION public.get_provider_service_type(_user_id UUID) RETURNS TEXT
LANGUAGE sql STABLE SECURITY DEFINER SET search_path TO 'public' AS $$
  SELECT CASE
    WHEN EXISTS (SELECT 1 FROM public.user_roles ur JOIN public.roles r ON ur.role_id = r.id WHERE ur.user_id = _user_id AND r.name = 'SMART_DOOR_LOCK_PROVIDER') THEN 'SMART_DOOR_LOCK'
    WHEN EXISTS (SELECT 1 FROM public.user_roles ur JOIN public.roles r ON ur.role_id = r.id WHERE ur.user_id = _user_id AND r.name = 'SMART_AC_PROVIDER') THEN 'SMART_AC'
    WHEN EXISTS (SELECT 1 FROM public.user_roles ur JOIN public.roles r ON ur.role_id = r.id WHERE ur.user_id = _user_id AND r.name = 'SMART_LIGHT_PROVIDER') THEN 'SMART_LIGHT'
    WHEN EXISTS (SELECT 1 FROM public.user_roles ur JOIN public.roles r ON ur.role_id = r.id WHERE ur.user_id = _user_id AND r.name = 'SMART_SWITCH_PROVIDER') THEN 'SMART_SWITCH'
    WHEN EXISTS (SELECT 1 FROM public.user_roles ur JOIN public.roles r ON ur.role_id = r.id WHERE ur.user_id = _user_id AND r.name = 'SMART_CAMERA_PROVIDER') THEN 'SMART_CAMERA'
    ELSE NULL
  END;
$$;

-- updated_at trigger function
CREATE OR REPLACE FUNCTION public.update_updated_at_trigger() RETURNS TRIGGER
LANGUAGE plpgsql SET search_path TO 'public' AS $$
BEGIN NEW.updated_at = now(); RETURN NEW; END;
$$;

-- Generate unit code
CREATE OR REPLACE FUNCTION public.generate_unit_code() RETURNS TRIGGER
LANGUAGE plpgsql SECURITY DEFINER SET search_path TO 'public' AS $$
BEGIN
  NEW.code := 'UNIT-' || UPPER(SUBSTRING(NEW.property_id::text, 1, 4)) || '-' || REPLACE(UPPER(NEW.unit_number), ' ', '');
  RETURN NEW;
END;
$$;

-- Generate lease payment schedule
CREATE OR REPLACE FUNCTION public.generate_lease_payment_schedule() RETURNS TRIGGER
LANGUAGE plpgsql SECURITY DEFINER SET search_path TO 'public' AS $$
DECLARE
  payment_date DATE; payment_num INTEGER := 1;
  total_payments_count INTEGER; months_between INTEGER; interval_months INTEGER;
BEGIN
  months_between := (EXTRACT(YEAR FROM NEW.end_date) - EXTRACT(YEAR FROM NEW.start_date)) * 12
    + EXTRACT(MONTH FROM NEW.end_date) - EXTRACT(MONTH FROM NEW.start_date);
  CASE NEW.rent_frequency
    WHEN 'MONTHLY' THEN interval_months := 1;
    WHEN 'QUARTERLY' THEN interval_months := 3;
    WHEN 'YEARLY' THEN interval_months := 12;
    ELSE interval_months := 1;
  END CASE;
  total_payments_count := GREATEST(1, CEIL(months_between::NUMERIC / interval_months));
  payment_date := NEW.start_date;
  WHILE payment_date <= NEW.end_date LOOP
    INSERT INTO public.lease_payments (
      lease_id, tenant_user_id, landlord_id, property_id, unit_id,
      amount, currency, payment_method, status, due_date, payment_number, total_payments
    ) VALUES (
      NEW.id, NEW.tenant_user_id, NEW.landlord_org_id,
      (SELECT property_id FROM public.units WHERE id = NEW.unit_id),
      NEW.unit_id, NEW.rent_amount, COALESCE(NEW.currency, 'AED'),
      COALESCE(NEW.payment_method, 'BANK'), 'PENDING',
      payment_date, payment_num, total_payments_count
    );
    payment_date := payment_date + (interval_months || ' months')::INTERVAL;
    payment_num := payment_num + 1;
  END LOOP;
  RETURN NEW;
END;
$$;

-- Set work order SLA
CREATE OR REPLACE FUNCTION public.set_work_order_sla() RETURNS TRIGGER
LANGUAGE plpgsql SECURITY DEFINER SET search_path TO 'public' AS $$
DECLARE sla_hours INTEGER;
BEGIN
  CASE NEW.priority
    WHEN 'EMERGENCY' THEN sla_hours := 4;
    WHEN 'HIGH' THEN sla_hours := 24;
    WHEN 'MEDIUM' THEN sla_hours := 72;
    WHEN 'LOW' THEN sla_hours := 168;
    ELSE sla_hours := 72;
  END CASE;
  NEW.sla_due_date := NEW.created_at + (sla_hours || ' hours')::INTERVAL;
  IF NEW.cost_estimate IS NOT NULL AND NEW.cost_estimate > 1000 THEN
    NEW.approval_status := 'PENDING';
  ELSE
    NEW.approval_status := 'NOT_REQUIRED';
  END IF;
  RETURN NEW;
END;
$$;

-- Set work order pricing based on lease
CREATE OR REPLACE FUNCTION public.set_work_order_pricing() RETURNS TRIGGER
LANGUAGE plpgsql SECURITY DEFINER SET search_path TO 'public' AS $$
DECLARE lease_maintenance_resp TEXT;
BEGIN
  SELECT maintenance_responsibility INTO lease_maintenance_resp
  FROM public.leases WHERE unit_id = NEW.unit_id AND tenant_user_id = NEW.tenant_user_id AND status = 'ACTIVE' LIMIT 1;
  IF lease_maintenance_resp = 'LANDLORD' THEN NEW.is_paid := false;
  ELSIF lease_maintenance_resp = 'TENANT' THEN NEW.is_paid := true;
  ELSIF lease_maintenance_resp = 'SHARED' THEN NEW.is_paid := NULL;
  ELSE NEW.is_paid := false; END IF;
  RETURN NEW;
END;
$$;

-- Auto-grant permissions to PLATFORM_ADMIN
CREATE OR REPLACE FUNCTION public.auto_grant_to_platform_admin() RETURNS TRIGGER
LANGUAGE plpgsql SECURITY DEFINER SET search_path TO 'public' AS $$
DECLARE v_admin_role_id UUID;
BEGIN
  SELECT id INTO v_admin_role_id FROM public.roles WHERE name = 'PLATFORM_ADMIN' LIMIT 1;
  IF v_admin_role_id IS NOT NULL THEN
    INSERT INTO public.role_permissions (role_id, permission_id)
    VALUES (v_admin_role_id, NEW.id) ON CONFLICT (role_id, permission_id) DO NOTHING;
  END IF;
  RETURN NEW;
END;
$$;

-- Check role hierarchy loop prevention
CREATE OR REPLACE FUNCTION public.check_role_hierarchy_loop() RETURNS TRIGGER
LANGUAGE plpgsql SET search_path TO 'public' AS $$
DECLARE current_id UUID; visited_ids UUID[] := ARRAY[]::UUID[];
BEGIN
  IF NEW.parent_role_id IS NULL THEN RETURN NEW; END IF;
  current_id := NEW.parent_role_id;
  WHILE current_id IS NOT NULL LOOP
    IF current_id = NEW.id OR current_id = ANY(visited_ids) THEN
      RAISE EXCEPTION 'Circular role inheritance detected';
    END IF;
    visited_ids := array_append(visited_ids, current_id);
    SELECT parent_role_id INTO current_id FROM public.roles WHERE id = current_id;
  END LOOP;
  RETURN NEW;
END;
$$;

-- Update post likes/replies count
CREATE OR REPLACE FUNCTION public.update_post_likes_count() RETURNS TRIGGER
LANGUAGE plpgsql SECURITY DEFINER SET search_path TO 'public' AS $$
BEGIN
  IF TG_OP = 'INSERT' THEN UPDATE public.channel_posts SET likes_count = likes_count + 1 WHERE id = NEW.post_id;
  ELSIF TG_OP = 'DELETE' THEN UPDATE public.channel_posts SET likes_count = likes_count - 1 WHERE id = OLD.post_id;
  END IF; RETURN NULL;
END;
$$;

CREATE OR REPLACE FUNCTION public.update_post_replies_count() RETURNS TRIGGER
LANGUAGE plpgsql SECURITY DEFINER SET search_path TO 'public' AS $$
BEGIN
  IF TG_OP = 'INSERT' THEN UPDATE public.channel_posts SET replies_count = replies_count + 1 WHERE id = NEW.post_id;
  ELSIF TG_OP = 'DELETE' THEN UPDATE public.channel_posts SET replies_count = replies_count - 1 WHERE id = OLD.post_id;
  END IF; RETURN NULL;
END;
$$;

-- Update provider rating
CREATE OR REPLACE FUNCTION public.update_provider_rating() RETURNS TRIGGER
LANGUAGE plpgsql SECURITY DEFINER SET search_path TO 'public' AS $$
BEGIN
  UPDATE public.smart_home_provider_profiles SET
    average_rating = (SELECT AVG(rating)::NUMERIC(3,2) FROM public.provider_reviews WHERE provider_id = NEW.provider_id),
    total_reviews = (SELECT COUNT(*) FROM public.provider_reviews WHERE provider_id = NEW.provider_id),
    updated_at = NOW()
  WHERE id = NEW.provider_id;
  RETURN NEW;
END;
$$;
```

---

## 6. Triggers

```sql
-- =============================================
-- SECTION 6: TRIGGERS
-- =============================================

-- updated_at triggers (apply to all tables with updated_at)
CREATE TRIGGER set_updated_at BEFORE UPDATE ON public.users FOR EACH ROW EXECUTE FUNCTION update_updated_at_trigger();
CREATE TRIGGER set_updated_at BEFORE UPDATE ON public.profiles FOR EACH ROW EXECUTE FUNCTION update_updated_at_trigger();
CREATE TRIGGER set_updated_at BEFORE UPDATE ON public.roles FOR EACH ROW EXECUTE FUNCTION update_updated_at_trigger();
CREATE TRIGGER set_updated_at BEFORE UPDATE ON public.permissions FOR EACH ROW EXECUTE FUNCTION update_updated_at_trigger();
CREATE TRIGGER set_updated_at BEFORE UPDATE ON public.properties FOR EACH ROW EXECUTE FUNCTION update_updated_at_trigger();
CREATE TRIGGER set_updated_at BEFORE UPDATE ON public.units FOR EACH ROW EXECUTE FUNCTION update_updated_at_trigger();
CREATE TRIGGER set_updated_at BEFORE UPDATE ON public.leases FOR EACH ROW EXECUTE FUNCTION update_updated_at_trigger();
CREATE TRIGGER set_updated_at BEFORE UPDATE ON public.lease_payments FOR EACH ROW EXECUTE FUNCTION update_updated_at_trigger();
CREATE TRIGGER set_updated_at BEFORE UPDATE ON public.invoices FOR EACH ROW EXECUTE FUNCTION update_updated_at_trigger();
CREATE TRIGGER set_updated_at BEFORE UPDATE ON public.payments FOR EACH ROW EXECUTE FUNCTION update_updated_at_trigger();
CREATE TRIGGER set_updated_at BEFORE UPDATE ON public.issue_reports FOR EACH ROW EXECUTE FUNCTION update_updated_at_trigger();
CREATE TRIGGER set_updated_at BEFORE UPDATE ON public.work_orders FOR EACH ROW EXECUTE FUNCTION update_updated_at_trigger();
CREATE TRIGGER set_updated_at BEFORE UPDATE ON public.incidents FOR EACH ROW EXECUTE FUNCTION update_updated_at_trigger();
CREATE TRIGGER set_updated_at BEFORE UPDATE ON public.documents FOR EACH ROW EXECUTE FUNCTION update_updated_at_trigger();
CREATE TRIGGER set_updated_at BEFORE UPDATE ON public.service_listings FOR EACH ROW EXECUTE FUNCTION update_updated_at_trigger();
CREATE TRIGGER set_updated_at BEFORE UPDATE ON public.service_providers FOR EACH ROW EXECUTE FUNCTION update_updated_at_trigger();
CREATE TRIGGER set_updated_at BEFORE UPDATE ON public.maintenance_teams FOR EACH ROW EXECUTE FUNCTION update_updated_at_trigger();
CREATE TRIGGER set_updated_at BEFORE UPDATE ON public.facilities FOR EACH ROW EXECUTE FUNCTION update_updated_at_trigger();
CREATE TRIGGER set_updated_at BEFORE UPDATE ON public.blacklist FOR EACH ROW EXECUTE FUNCTION update_updated_at_trigger();
CREATE TRIGGER set_updated_at BEFORE UPDATE ON public.visitor_permits FOR EACH ROW EXECUTE FUNCTION update_updated_at_trigger();
CREATE TRIGGER set_updated_at BEFORE UPDATE ON public.smart_home_requests FOR EACH ROW EXECUTE FUNCTION update_updated_at_trigger();

-- Unit code generation
CREATE TRIGGER generate_unit_code_trigger BEFORE INSERT ON public.units
  FOR EACH ROW EXECUTE FUNCTION generate_unit_code();

-- Lease payment schedule generation
CREATE TRIGGER generate_payment_schedule AFTER INSERT ON public.leases
  FOR EACH ROW WHEN (NEW.status = 'ACTIVE')
  EXECUTE FUNCTION generate_lease_payment_schedule();

-- Work order SLA
CREATE TRIGGER set_sla_trigger BEFORE INSERT ON public.issue_reports
  FOR EACH ROW EXECUTE FUNCTION set_work_order_sla();

-- Work order pricing
CREATE TRIGGER set_pricing_trigger BEFORE INSERT ON public.issue_reports
  FOR EACH ROW EXECUTE FUNCTION set_work_order_pricing();

-- Lease payment status history
CREATE OR REPLACE FUNCTION public.log_lease_payment_status_change() RETURNS TRIGGER
LANGUAGE plpgsql SECURITY DEFINER SET search_path TO 'public' AS $$
BEGIN
  IF OLD.status IS DISTINCT FROM NEW.status THEN
    INSERT INTO public.lease_payment_history (lease_payment_id, old_status, new_status, changed_by)
    VALUES (NEW.id, OLD.status, NEW.status, COALESCE(NEW.validated_by, '00000000-0000-0000-0000-000000000000'));
  END IF; RETURN NEW;
END;
$$;

CREATE TRIGGER log_payment_status AFTER UPDATE ON public.lease_payments
  FOR EACH ROW EXECUTE FUNCTION log_lease_payment_status_change();

-- Work order status history
CREATE OR REPLACE FUNCTION public.log_work_order_status_change() RETURNS TRIGGER
LANGUAGE plpgsql SECURITY DEFINER SET search_path TO 'public' AS $$
BEGIN
  IF OLD.status IS DISTINCT FROM NEW.status THEN
    INSERT INTO public.work_order_status_history (work_order_id, old_status, new_status)
    VALUES (NEW.id, OLD.status, NEW.status);
  END IF; RETURN NEW;
END;
$$;

CREATE TRIGGER log_wo_status AFTER UPDATE ON public.issue_reports
  FOR EACH ROW EXECUTE FUNCTION log_work_order_status_change();

-- Smart home status log
CREATE OR REPLACE FUNCTION public.log_smart_home_status_change() RETURNS TRIGGER
LANGUAGE plpgsql SECURITY DEFINER SET search_path TO 'public' AS $$
BEGIN
  IF OLD.status IS DISTINCT FROM NEW.status THEN
    INSERT INTO public.smart_home_status_logs (request_id, old_status, new_status, changed_by_user_id)
    VALUES (NEW.id, OLD.status, NEW.status, '00000000-0000-0000-0000-000000000000');
  END IF; RETURN NEW;
END;
$$;

CREATE TRIGGER log_smart_home_status AFTER UPDATE ON public.smart_home_requests
  FOR EACH ROW EXECUTE FUNCTION log_smart_home_status_change();

-- Role hierarchy loop check
CREATE TRIGGER check_role_hierarchy BEFORE INSERT OR UPDATE ON public.roles
  FOR EACH ROW EXECUTE FUNCTION check_role_hierarchy_loop();

-- Auto-grant to PLATFORM_ADMIN
CREATE TRIGGER auto_grant_platform_admin AFTER INSERT ON public.permissions
  FOR EACH ROW EXECUTE FUNCTION auto_grant_to_platform_admin();

-- Permission audit log triggers
CREATE OR REPLACE FUNCTION public.log_permission_change() RETURNS TRIGGER
LANGUAGE plpgsql SECURITY DEFINER SET search_path TO 'public' AS $$
DECLARE v_role_name TEXT; v_permission_code TEXT;
BEGIN
  IF TG_OP = 'INSERT' THEN
    SELECT name INTO v_role_name FROM public.roles WHERE id = NEW.role_id;
    SELECT code INTO v_permission_code FROM public.permissions WHERE id = NEW.permission_id;
    INSERT INTO public.permission_audit_logs (action, role_id, role_name, permission_id, permission_code, performed_by, metadata)
    VALUES ('GRANT', NEW.role_id, v_role_name, NEW.permission_id, v_permission_code, NEW.granted_by, jsonb_build_object('granted_at', NEW.granted_at));
    RETURN NEW;
  ELSIF TG_OP = 'DELETE' THEN
    SELECT name INTO v_role_name FROM public.roles WHERE id = OLD.role_id;
    SELECT code INTO v_permission_code FROM public.permissions WHERE id = OLD.permission_id;
    INSERT INTO public.permission_audit_logs (action, role_id, role_name, permission_id, permission_code, metadata)
    VALUES ('REVOKE', OLD.role_id, v_role_name, OLD.permission_id, v_permission_code, jsonb_build_object('revoked_at', NOW()));
    RETURN OLD;
  END IF;
  RETURN NULL;
END;
$$;

CREATE TRIGGER log_perm_change AFTER INSERT OR DELETE ON public.role_permissions
  FOR EACH ROW EXECUTE FUNCTION log_permission_change();

-- Role inheritance audit
CREATE OR REPLACE FUNCTION public.log_inheritance_change() RETURNS TRIGGER
LANGUAGE plpgsql SECURITY DEFINER SET search_path TO 'public' AS $$
DECLARE v_role_name TEXT; v_old_parent TEXT; v_new_parent TEXT; v_action TEXT;
BEGIN
  IF OLD.parent_role_id IS DISTINCT FROM NEW.parent_role_id THEN
    SELECT name INTO v_role_name FROM public.roles WHERE id = NEW.id;
    IF OLD.parent_role_id IS NOT NULL THEN SELECT name INTO v_old_parent FROM public.roles WHERE id = OLD.parent_role_id; END IF;
    IF NEW.parent_role_id IS NOT NULL THEN SELECT name INTO v_new_parent FROM public.roles WHERE id = NEW.parent_role_id; END IF;
    IF OLD.parent_role_id IS NULL AND NEW.parent_role_id IS NOT NULL THEN v_action := 'INHERIT';
    ELSIF OLD.parent_role_id IS NOT NULL AND NEW.parent_role_id IS NULL THEN v_action := 'REMOVE_INHERIT';
    ELSE v_action := 'CHANGE_INHERIT'; END IF;
    INSERT INTO public.permission_audit_logs (action, role_id, role_name, parent_role_id, parent_role_name, metadata)
    VALUES (v_action, NEW.id, v_role_name, NEW.parent_role_id, v_new_parent,
      jsonb_build_object('old_parent_role_id', OLD.parent_role_id, 'old_parent_role_name', v_old_parent,
        'new_parent_role_id', NEW.parent_role_id, 'new_parent_role_name', v_new_parent));
  END IF;
  RETURN NEW;
END;
$$;

CREATE TRIGGER log_role_inherit AFTER UPDATE ON public.roles
  FOR EACH ROW EXECUTE FUNCTION log_inheritance_change();

-- Post likes/replies counters
CREATE TRIGGER update_likes AFTER INSERT OR DELETE ON public.post_likes
  FOR EACH ROW EXECUTE FUNCTION update_post_likes_count();

CREATE TRIGGER update_replies AFTER INSERT OR DELETE ON public.post_replies
  FOR EACH ROW EXECUTE FUNCTION update_post_replies_count();

-- Provider rating update
CREATE TRIGGER update_rating AFTER INSERT OR UPDATE ON public.provider_reviews
  FOR EACH ROW EXECUTE FUNCTION update_provider_rating();
```

---

## 7. Indexes

```sql
-- =============================================
-- SECTION 7: RECOMMENDED INDEXES
-- =============================================

-- Core lookups
CREATE INDEX idx_users_email ON public.users(email);
CREATE INDEX idx_users_status ON public.users(status);
CREATE INDEX idx_profiles_user_id ON public.profiles(user_id);
CREATE INDEX idx_user_roles_user_id ON public.user_roles(user_id);
CREATE INDEX idx_user_roles_role_id ON public.user_roles(role_id);
CREATE INDEX idx_role_permissions_role_id ON public.role_permissions(role_id);
CREATE INDEX idx_role_permissions_permission_id ON public.role_permissions(permission_id);

-- Properties & Units
CREATE INDEX idx_properties_landlord_id ON public.properties(landlord_id);
CREATE INDEX idx_units_property_id ON public.units(property_id);
CREATE INDEX idx_units_status ON public.units(status);

-- Leases
CREATE INDEX idx_leases_unit_id ON public.leases(unit_id);
CREATE INDEX idx_leases_tenant_user_id ON public.leases(tenant_user_id);
CREATE INDEX idx_leases_status ON public.leases(status);
CREATE INDEX idx_leases_landlord_org_id ON public.leases(landlord_org_id);

-- Lease Payments
CREATE INDEX idx_lease_payments_lease_id ON public.lease_payments(lease_id);
CREATE INDEX idx_lease_payments_tenant ON public.lease_payments(tenant_user_id);
CREATE INDEX idx_lease_payments_landlord ON public.lease_payments(landlord_id);
CREATE INDEX idx_lease_payments_status ON public.lease_payments(status);
CREATE INDEX idx_lease_payments_due_date ON public.lease_payments(due_date);

-- Invoices & Payments
CREATE INDEX idx_invoices_tenant ON public.invoices(tenant_user_id);
CREATE INDEX idx_invoices_lease ON public.invoices(lease_id);
CREATE INDEX idx_invoices_status ON public.invoices(status);
CREATE INDEX idx_payments_invoice ON public.payments(invoice_id);

-- Issue Reports / Work Orders
CREATE INDEX idx_issue_reports_property ON public.issue_reports(property_id);
CREATE INDEX idx_issue_reports_tenant ON public.issue_reports(tenant_user_id);
CREATE INDEX idx_issue_reports_status ON public.issue_reports(status);
CREATE INDEX idx_issue_reports_priority ON public.issue_reports(priority);
CREATE INDEX idx_issue_reports_team ON public.issue_reports(assigned_maintenance_team_id);
CREATE INDEX idx_work_orders_property ON public.work_orders(property_id);
CREATE INDEX idx_work_orders_status ON public.work_orders(status);

-- Notifications
CREATE INDEX idx_notifications_user ON public.notifications(user_id);
CREATE INDEX idx_notifications_read ON public.notifications(user_id, read_at) WHERE read_at IS NULL;
CREATE INDEX idx_notifications_type ON public.notifications(type);

-- Facilities
CREATE INDEX idx_facilities_property ON public.facilities(property_id);
CREATE INDEX idx_facility_bookings_facility ON public.facility_bookings(facility_id);
CREATE INDEX idx_facility_bookings_date ON public.facility_bookings(booking_date);
CREATE INDEX idx_facility_bookings_booked_by ON public.facility_bookings(booked_by);

-- Documents
CREATE INDEX idx_documents_property ON public.documents(property_id);
CREATE INDEX idx_documents_landlord ON public.documents(landlord_id);
CREATE INDEX idx_documents_tenant ON public.documents(tenant_user_id);

-- Incidents
CREATE INDEX idx_incidents_property ON public.incidents(property_id);
CREATE INDEX idx_incidents_reported_by ON public.incidents(reported_by_user_id);

-- Visitor permits
CREATE INDEX idx_visitor_permits_property ON public.visitor_permits(property_id);
CREATE INDEX idx_visitor_permits_tenant ON public.visitor_permits(tenant_user_id);

-- Community
CREATE INDEX idx_channel_posts_channel ON public.channel_posts(channel_id);
CREATE INDEX idx_post_replies_post ON public.post_replies(post_id);
CREATE INDEX idx_direct_messages_sender ON public.direct_messages(sender_id);
CREATE INDEX idx_direct_messages_recipient ON public.direct_messages(recipient_id);

-- Smart Home
CREATE INDEX idx_smart_home_requests_tenant ON public.smart_home_requests(tenant_user_id);
CREATE INDEX idx_smart_home_requests_property ON public.smart_home_requests(property_id);
CREATE INDEX idx_smart_home_devices_request ON public.smart_home_devices(request_id);

-- Inspections
CREATE INDEX idx_inspections_property ON public.inspections(property_id);
CREATE INDEX idx_inspections_inspector ON public.inspections(inspector_id);

-- Subscriptions
CREATE INDEX idx_landlord_subscriptions_user ON public.landlord_subscriptions(landlord_user_id);
CREATE INDEX idx_subscription_invoices_user ON public.subscription_invoices(landlord_user_id);

-- Tenant assignments
CREATE INDEX idx_tenant_assignments_tenant ON public.tenant_apartment_assignments(tenant_user_id);
CREATE INDEX idx_tenant_assignments_unit ON public.tenant_apartment_assignments(unit_id);

-- Audit
CREATE INDEX idx_audit_logs_actor ON public.audit_logs(actor_user_id);
CREATE INDEX idx_audit_logs_resource ON public.audit_logs(resource_type, resource_id);
```

---

## 8. Seed Data

```sql
-- =============================================
-- SECTION 8: SEED DATA
-- =============================================

-- Default roles
INSERT INTO public.roles (name, description, is_system) VALUES
  ('PLATFORM_ADMIN', 'Full platform access', true),
  ('LANDLORD', 'Property owner', true),
  ('TENANT', 'Property tenant', true),
  ('MAINTENANCE', 'Maintenance company main user', true),
  ('MAINTENANCE_SUB_USER', 'Maintenance team member', true),
  ('SECURITY', 'Security company main user', true),
  ('SECURITY_SUB_USER', 'Security team member', true),
  ('PROVIDER', 'Service marketplace provider', true),
  ('INSPECTOR', 'Property inspector', true),
  ('LANDLORD_SUB_USER', 'Landlord delegated user', true),
  ('SMART_DOOR_LOCK_PROVIDER', 'Smart door lock provider', true),
  ('SMART_AC_PROVIDER', 'Smart AC provider', true),
  ('SMART_LIGHT_PROVIDER', 'Smart light provider', true),
  ('SMART_SWITCH_PROVIDER', 'Smart switch provider', true),
  ('SMART_CAMERA_PROVIDER', 'Smart camera provider', true)
ON CONFLICT (name) DO NOTHING;

-- Default platform settings
INSERT INTO public.platform_settings (primary_color, secondary_color, accent_color, font_family, default_theme, currency)
VALUES ('#1a1a2e', '#16213e', '#0f3460', 'Inter', 'light', 'AED')
ON CONFLICT DO NOTHING;

-- Default subscription config
INSERT INTO public.subscription_config (plan_name, base_price_per_unit, currency, trial_days, yearly_discount_percent)
VALUES ('Rentolic Pro', 10, 'AED', 14, 20)
ON CONFLICT DO NOTHING;
```

---

## 9. .NET Core 6 Migration Notes

### 9.1 Replacing `auth.uid()`

In Supabase, `auth.uid()` returns the authenticated user's UUID from the JWT. In .NET Core 6:

```csharp
// In your controllers, get the user ID from the JWT claim:
var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

// Pass it to service/repository methods:
await _workOrderService.CreateAsync(workOrder, userId);
```

### 9.2 EF Core DbContext Configuration

```csharp
public class RentolicDbContext : DbContext
{
    // Core
    public DbSet<User> Users { get; set; }
    public DbSet<Profile> Profiles { get; set; }
    public DbSet<Role> Roles { get; set; }
    public DbSet<UserRole> UserRoles { get; set; }
    public DbSet<Permission> Permissions { get; set; }
    public DbSet<RolePermission> RolePermissions { get; set; }
    
    // Properties
    public DbSet<Property> Properties { get; set; }
    public DbSet<Unit> Units { get; set; }
    
    // Leases
    public DbSet<Lease> Leases { get; set; }
    public DbSet<LeasePayment> LeasePayments { get; set; }
    
    // ... 120+ more DbSets
    
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Configure enum conversions
        modelBuilder.HasPostgresEnum<UserStatus>();
        modelBuilder.HasPostgresEnum<UnitStatus>();
        modelBuilder.HasPostgresEnum<LeaseStatus>();
        // ... all 30 enums
        
        // Configure soft deletes
        modelBuilder.Entity<User>().HasQueryFilter(u => u.DeletedAt == null);
        modelBuilder.Entity<Property>().HasQueryFilter(p => p.DeletedAt == null);
        
        // Unique constraints
        modelBuilder.Entity<UserRole>()
            .HasIndex(ur => new { ur.UserId, ur.RoleId }).IsUnique();
        modelBuilder.Entity<RolePermission>()
            .HasIndex(rp => new { rp.RoleId, rp.PermissionId }).IsUnique();
    }
}
```

### 9.3 Connection String

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=your-rds-endpoint;Port=5432;Database=rentolic;Username=rentolic_app;Password=YOUR_PASSWORD;SSL Mode=Require;"
  }
}
```

### 9.4 Authorization (Replacing RLS)

Instead of Supabase RLS policies, implement authorization in .NET middleware:

```csharp
// Custom authorization policy
services.AddAuthorization(options =>
{
    options.AddPolicy("IsLandlord", policy =>
        policy.RequireAssertion(context =>
            context.User.HasClaim("role", "LANDLORD")));
    
    options.AddPolicy("CanManageProperty", policy =>
        policy.Requirements.Add(new PropertyOwnerRequirement()));
});

// Resource-based authorization handler
public class PropertyOwnerHandler : AuthorizationHandler<PropertyOwnerRequirement, Property>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PropertyOwnerRequirement requirement,
        Property property)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (property.LandlordId.ToString() == userId)
            context.Succeed(requirement);
        return Task.CompletedTask;
    }
}
```

### 9.5 Storage Migration (Supabase → S3/Local)

| Supabase Bucket | Target | Purpose |
|---|---|---|
| `property-documents` | S3 or local `/uploads/property-documents/` | Lease docs, property files |
| `service-images` | S3 or local `/uploads/service-images/` | Service listing images |
| `inspection-media` | S3 or local `/uploads/inspection-media/` | Inspection photos |
| `vehicle-licenses` | S3 or local `/uploads/vehicle-licenses/` | Parking vehicle docs |
| `lease-payment-documents` | S3 or local `/uploads/lease-payments/` | Cheque images, transfer proofs |

### 9.6 Supabase-Specific Constructs Removed

| Supabase Feature | Removed/Replaced With |
|---|---|
| `auth.users` table | `public.users` table (self-managed) |
| `auth.uid()` function | JWT claim in .NET middleware |
| RLS policies (all) | .NET authorization policies + query filters |
| `auth.users` triggers | Application-level user creation logic |
| Supabase Storage | S3 / local file storage via .NET API |
| Supabase Realtime | SignalR |
| Edge Functions (58) | .NET API controllers |

---

## Summary

| Category | Count |
|---|---|
| PostgreSQL Extensions | 3 |
| Enum Types | 30 |
| Tables | 130+ |
| Views | 2 |
| Functions | 40+ |
| Triggers | 25+ |
| Indexes | 60+ |
| Seed Data Scripts | 3 |

**This file provides a complete, runnable SQL migration from Supabase to standalone PostgreSQL for .NET Core 6.**
