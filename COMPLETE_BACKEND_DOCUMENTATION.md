# Rentolic - Complete Backend Documentation
## For .NET Core Microservices Migration

**Version:** 2.0  
**Date:** March 2026  
**Current Stack:** Supabase (PostgreSQL + Auth + Storage + Edge Functions) + AWS Lambda Functions  
**Target Stack:** .NET Core Microservices  
**Edge Functions Count:** 61 (Supabase) + 8 (AWS Lambda)  
**Database Tables:** 100+  
**Database Functions:** 45+  

---

## Table of Contents

1. [Architecture Overview](#architecture-overview)
2. [Database Schema](#database-schema)
3. [Row Level Security (RLS) Policies](#row-level-security-rls-policies)
4. [Database Functions](#database-functions)
5. [Edge Functions (API Endpoints)](#edge-functions-api-endpoints)
6. [Authentication & Authorization](#authentication--authorization)
7. [Storage Buckets](#storage-buckets)
8. [Business Logic & Workflows](#business-logic--workflows)
9. [Migration Considerations](#migration-considerations)

---

## Architecture Overview

### Current Architecture (Supabase)

```
┌─────────────────┐
│   React SPA     │
│   (Frontend)    │
└────────┬────────┘
         │
    ┌────┴─────────────────────────────┐
    │                                   │
┌───▼────────────┐           ┌─────────▼──────┐
│  Supabase Auth │           │  Edge Functions │
│  (PostgREST)   │           │  (Deno Runtime) │
└───┬────────────┘           └─────────┬──────┘
    │                                   │
    └────────┬──────────────────────────┘
             │
    ┌────────▼────────┐
    │   PostgreSQL    │
    │   Database      │
    │   + RLS         │
    └─────────────────┘
```

### Target Architecture (.NET Core Microservices)

```
┌─────────────────┐
│   React SPA     │
│   (Frontend)    │
└────────┬────────┘
         │
    ┌────┴──────────────┐
    │   API Gateway     │
    │   (Ocelot/YARP)   │
    └────┬──────────────┘
         │
    ┌────┴──────────────────────────────────┐
    │                                        │
┌───▼───────────┐  ┌──────────┐  ┌─────────▼──────┐
│ Auth Service  │  │ User Svc │  │ Property Svc   │
│ (Identity)    │  │          │  │                │
└───┬───────────┘  └────┬─────┘  └─────────┬──────┘
    │                   │                   │
    └───────────────────┴───────────────────┘
                        │
              ┌─────────▼──────────┐
              │   PostgreSQL/      │
              │   SQL Server       │
              └────────────────────┘
```

---

## Database Schema

### Core Tables

#### 1. users
**Purpose:** Main user accounts table (synced with auth.users)

```sql
CREATE TABLE public.users (
    id UUID PRIMARY KEY REFERENCES auth.users(id) ON DELETE CASCADE,
    email TEXT UNIQUE NOT NULL,
    name TEXT,
    status user_status DEFAULT 'ACTIVE',
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TYPE user_status AS ENUM ('ACTIVE', 'INACTIVE', 'SUSPENDED', 'DELETED');
```

**Indexes:**
```sql
CREATE INDEX idx_users_email ON users(email);
CREATE INDEX idx_users_status ON users(status);
```

---

#### 2. profiles
**Purpose:** Extended user profile information

```sql
CREATE TABLE public.profiles (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID UNIQUE NOT NULL REFERENCES auth.users(id) ON DELETE CASCADE,
    name TEXT,
    phone TEXT,
    avatar_url TEXT,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);
```

**Indexes:**
```sql
CREATE INDEX idx_profiles_user_id ON profiles(user_id);
```

---

#### 3. roles
**Purpose:** Define system roles with configurable RBAC and hierarchy

```sql
CREATE TABLE public.roles (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name TEXT UNIQUE NOT NULL,
    description TEXT,
    is_system BOOLEAN DEFAULT FALSE,
    parent_role_id UUID REFERENCES roles(id),
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

-- Default roles
INSERT INTO roles (name, description, is_system) VALUES
('PLATFORM_ADMIN', 'Platform administrator with full access', true),
('LANDLORD', 'Property owner/manager', true),
('TENANT', 'Property tenant', true),
('MAINTENANCE', 'Maintenance team main user', true),
('MAINTENANCE_SUB_USER', 'Maintenance team sub-user', true),
('SECURITY', 'Security team main user', true),
('SECURITY_SUB_USER', 'Security guard/sub-user', true),
('PROVIDER', 'Service provider', true),
('SERVICE_PROVIDER_SUB_USER', 'Service provider sub-user', true),
('LANDLORD_SUB_USER', 'Landlord team member', true),
('INSPECTOR', 'Property inspector', true),
('SMART_DOOR_LOCK_PROVIDER', 'Smart door lock provider', true),
('SMART_AC_PROVIDER', 'Smart AC provider', true),
('SMART_LIGHT_PROVIDER', 'Smart light provider', true),
('SMART_SWITCH_PROVIDER', 'Smart switch provider', true),
('SMART_CAMERA_PROVIDER', 'Smart camera provider', true);
```

---

#### 3a. permissions
**Purpose:** Configurable RBAC permissions (navigation + feature)

```sql
CREATE TABLE public.permissions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    code TEXT UNIQUE NOT NULL,
    name TEXT NOT NULL,
    description TEXT,
    category TEXT NOT NULL,
    is_navigation BOOLEAN DEFAULT FALSE,
    nav_path TEXT,
    nav_icon TEXT,
    is_system BOOLEAN DEFAULT FALSE,
    sort_order INTEGER,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);
```

#### 3b. role_permissions
**Purpose:** Many-to-many relationship between roles and permissions

```sql
CREATE TABLE public.role_permissions (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    role_id UUID NOT NULL REFERENCES roles(id) ON DELETE CASCADE,
    permission_id UUID NOT NULL REFERENCES permissions(id) ON DELETE CASCADE,
    granted_by UUID,
    granted_at TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(role_id, permission_id)
);
```

#### 3c. permission_audit_logs
**Purpose:** Track all permission changes (GRANT, REVOKE, INHERIT, REMOVE_INHERIT)

```sql
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
    performed_at TIMESTAMPTZ DEFAULT NOW(),
    metadata JSONB
);
```

**Key RBAC Functions:**
- `get_user_permissions(_user_id)` — Returns all permissions for a user with inheritance
- `get_role_permissions_with_inheritance(_role_id)` — Returns permissions with parent role inheritance
- `user_has_permission(_user_id, _permission_code)` — Boolean check for specific permission
- `has_admin_role(_user_id)` — PLATFORM_ADMIN always gets all permissions
- `auto_grant_to_platform_admin()` — Trigger: auto-grants new permissions to PLATFORM_ADMIN
- `check_role_hierarchy_loop()` — Trigger: prevents circular role inheritance
- `log_permission_change()` — Trigger: logs GRANT/REVOKE to audit table
- `log_inheritance_change()` — Trigger: logs inheritance changes to audit table

---

#### 4. user_roles
**Purpose:** Many-to-many relationship between users and roles

```sql
CREATE TABLE public.user_roles (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES auth.users(id) ON DELETE CASCADE,
    role_id UUID NOT NULL REFERENCES roles(id) ON DELETE CASCADE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(user_id, role_id)
);
```

**Indexes:**
```sql
CREATE INDEX idx_user_roles_user_id ON user_roles(user_id);
CREATE INDEX idx_user_roles_role_id ON user_roles(role_id);
```

---

#### 5. properties
**Purpose:** Store property information

```sql
CREATE TABLE public.properties (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    landlord_id UUID REFERENCES auth.users(id),
    name TEXT NOT NULL,
    type TEXT NOT NULL,
    address TEXT,
    city TEXT,
    state TEXT,
    country TEXT,
    lat DECIMAL(10, 8),
    lng DECIMAL(11, 8),
    google_map_link TEXT,
    total_units INTEGER,
    amenities JSONB,
    utilities_included JSONB,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    deleted_at TIMESTAMPTZ
);
```

**Indexes:**
```sql
CREATE INDEX idx_properties_landlord_id ON properties(landlord_id);
CREATE INDEX idx_properties_deleted_at ON properties(deleted_at);
CREATE INDEX idx_properties_city ON properties(city);
CREATE INDEX idx_properties_type ON properties(type);
```

---

#### 6. units
**Purpose:** Individual units within properties

```sql
CREATE TABLE public.units (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    property_id UUID NOT NULL REFERENCES properties(id) ON DELETE CASCADE,
    unit_number TEXT NOT NULL,
    code TEXT UNIQUE,
    floor_number INTEGER,
    bedrooms INTEGER,
    bathrooms DECIMAL(3,1),
    area_sqft DECIMAL(10,2),
    rent_amount DECIMAL(10,2),
    status unit_status DEFAULT 'VACANT',
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    deleted_at TIMESTAMPTZ,
    UNIQUE(property_id, unit_number)
);

CREATE TYPE unit_status AS ENUM ('VACANT', 'OCCUPIED', 'MAINTENANCE', 'RESERVED');
```

**Indexes:**
```sql
CREATE INDEX idx_units_property_id ON units(property_id);
CREATE INDEX idx_units_status ON units(status);
CREATE INDEX idx_units_code ON units(code);
```

---

#### 7. leases
**Purpose:** Rental agreements between landlords and tenants

```sql
CREATE TABLE public.leases (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    unit_id UUID NOT NULL REFERENCES units(id) ON DELETE CASCADE,
    tenant_user_id UUID NOT NULL REFERENCES auth.users(id),
    landlord_org_id UUID NOT NULL REFERENCES auth.users(id),
    start_date DATE NOT NULL,
    end_date DATE NOT NULL,
    rent_amount DECIMAL(10,2) NOT NULL,
    rent_frequency rent_frequency NOT NULL,
    payment_frequency_text TEXT DEFAULT 'MONTHLY',
    security_deposit DECIMAL(10,2),
    payment_method TEXT,
    auto_payment BOOLEAN DEFAULT FALSE,
    maintenance_responsibility TEXT DEFAULT 'LANDLORD',
    status lease_status DEFAULT 'DRAFT',
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    deleted_at TIMESTAMPTZ
);

CREATE TYPE lease_status AS ENUM ('DRAFT', 'ACTIVE', 'EXPIRED', 'TERMINATED', 'PENDING');
CREATE TYPE rent_frequency AS ENUM ('MONTHLY', 'QUARTERLY', 'YEARLY');
```

**Indexes:**
```sql
CREATE INDEX idx_leases_unit_id ON leases(unit_id);
CREATE INDEX idx_leases_tenant_user_id ON leases(tenant_user_id);
CREATE INDEX idx_leases_landlord_org_id ON leases(landlord_org_id);
CREATE INDEX idx_leases_status ON leases(status);
CREATE INDEX idx_leases_end_date ON leases(end_date);
```

---

#### 8. invoices
**Purpose:** Billing invoices for tenants

```sql
CREATE TABLE public.invoices (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    lease_id UUID REFERENCES leases(id),
    tenant_user_id UUID REFERENCES auth.users(id),
    number TEXT UNIQUE NOT NULL,
    amount DECIMAL(10,2) NOT NULL,
    currency TEXT NOT NULL,
    due_date DATE NOT NULL,
    status invoice_status DEFAULT 'OPEN',
    meta JSONB,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    deleted_at TIMESTAMPTZ
);

CREATE TYPE invoice_status AS ENUM ('OPEN', 'PAID', 'OVERDUE', 'CANCELLED', 'PARTIAL');
```

**Indexes:**
```sql
CREATE INDEX idx_invoices_lease_id ON invoices(lease_id);
CREATE INDEX idx_invoices_tenant_user_id ON invoices(tenant_user_id);
CREATE INDEX idx_invoices_status ON invoices(status);
CREATE INDEX idx_invoices_due_date ON invoices(due_date);
CREATE INDEX idx_invoices_number ON invoices(number);
```

---

#### 9. payments
**Purpose:** Payment transactions

```sql
CREATE TABLE public.payments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    invoice_id UUID NOT NULL REFERENCES invoices(id),
    amount DECIMAL(10,2) NOT NULL,
    currency TEXT NOT NULL,
    method payment_method NOT NULL,
    provider payment_provider NOT NULL,
    provider_payment_id TEXT,
    transaction_reference TEXT,
    payment_date DATE,
    payment_proof_url TEXT,
    status payment_status DEFAULT 'PENDING',
    verified_by_user_id UUID REFERENCES auth.users(id),
    verified_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TYPE payment_method AS ENUM ('BANK_TRANSFER', 'CREDIT_CARD', 'CASH', 'CHEQUE', 'ONLINE');
CREATE TYPE payment_provider AS ENUM ('STRIPE', 'BANK', 'CASH', 'OTHER');
CREATE TYPE payment_status AS ENUM ('PENDING', 'COMPLETED', 'FAILED', 'REFUNDED', 'CANCELLED');
```

**Indexes:**
```sql
CREATE INDEX idx_payments_invoice_id ON payments(invoice_id);
CREATE INDEX idx_payments_status ON payments(status);
CREATE INDEX idx_payments_provider_payment_id ON payments(provider_payment_id);
```

---

#### 10. issue_reports (Work Orders)
**Purpose:** Maintenance requests and work orders

```sql
CREATE TABLE public.issue_reports (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    property_id UUID NOT NULL REFERENCES properties(id),
    unit_id UUID REFERENCES units(id),
    tenant_user_id UUID NOT NULL REFERENCES auth.users(id),
    raised_by_user_id UUID REFERENCES auth.users(id),
    title TEXT NOT NULL,
    description TEXT NOT NULL,
    category TEXT NOT NULL,
    priority priority DEFAULT 'MEDIUM',
    status work_order_status DEFAULT 'NEW',
    images TEXT[],
    voice_notes TEXT[],
    assigned_maintenance_team_id UUID REFERENCES maintenance_teams(id),
    assigned_sub_user_id UUID,
    selected_service_provider_id UUID REFERENCES service_providers(id),
    cost_estimate DECIMAL(10,2),
    actual_cost DECIMAL(10,2),
    is_paid BOOLEAN DEFAULT FALSE,
    is_emergency BOOLEAN DEFAULT FALSE,
    pricing_type TEXT,
    approval_status TEXT DEFAULT 'NOT_REQUIRED',
    approval_threshold DECIMAL(10,2) DEFAULT 500.00,
    approved_by_user_id UUID REFERENCES auth.users(id),
    approved_at TIMESTAMPTZ,
    scheduled_date DATE,
    scheduled_time TIME,
    expected_completion_date DATE,
    completed_at TIMESTAMPTZ,
    tenant_rating INTEGER CHECK (tenant_rating >= 1 AND tenant_rating <= 5),
    tenant_review TEXT,
    landlord_rating INTEGER CHECK (landlord_rating >= 1 AND landlord_rating <= 5),
    landlord_review TEXT,
    landlord_reviewed_at TIMESTAMPTZ,
    sla_due_date TIMESTAMPTZ,
    sla_breached BOOLEAN DEFAULT FALSE,
    escalated BOOLEAN DEFAULT FALSE,
    escalated_at TIMESTAMPTZ,
    assignment_reason TEXT,
    assignment_score DECIMAL(5,2),
    mobile_checked_in BOOLEAN DEFAULT FALSE,
    check_in_location POINT,
    check_in_time TIMESTAMPTZ,
    offline_synced BOOLEAN DEFAULT TRUE,
    warranty_covered BOOLEAN DEFAULT FALSE,
    bid_deadline TIMESTAMPTZ,
    min_bid_amount DECIMAL(10,2),
    max_bid_amount DECIMAL(10,2),
    accepted_bid_id UUID,
    recommended_service_provider_ids UUID[],
    invoice_id UUID REFERENCES invoices(id),
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TYPE priority AS ENUM ('LOW', 'MEDIUM', 'HIGH', 'EMERGENCY');
CREATE TYPE work_order_status AS ENUM (
    'NEW', 'ASSIGNED', 'IN_PROGRESS', 'PENDING_APPROVAL', 
    'APPROVED', 'REJECTED', 'COMPLETED', 'CANCELLED', 
    'ON_HOLD', 'BIDDING'
);
```

**Indexes:**
```sql
CREATE INDEX idx_issue_reports_property_id ON issue_reports(property_id);
CREATE INDEX idx_issue_reports_tenant_user_id ON issue_reports(tenant_user_id);
CREATE INDEX idx_issue_reports_status ON issue_reports(status);
CREATE INDEX idx_issue_reports_assigned_team ON issue_reports(assigned_maintenance_team_id);
CREATE INDEX idx_issue_reports_priority ON issue_reports(priority);
CREATE INDEX idx_issue_reports_sla_due ON issue_reports(sla_due_date);
```

---

#### 11. maintenance_teams
**Purpose:** Maintenance service teams

```sql
CREATE TABLE public.maintenance_teams (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    landlord_id UUID REFERENCES auth.users(id),
    main_user_id UUID REFERENCES auth.users(id),
    name TEXT NOT NULL,
    contact_email TEXT,
    contact_phone TEXT,
    specialties TEXT[],
    active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);
```

**Indexes:**
```sql
CREATE INDEX idx_maintenance_teams_landlord_id ON maintenance_teams(landlord_id);
CREATE INDEX idx_maintenance_teams_main_user_id ON maintenance_teams(main_user_id);
CREATE INDEX idx_maintenance_teams_active ON maintenance_teams(active);
```

---

#### 12. maintenance_sub_users
**Purpose:** Sub-users for maintenance teams with granular permissions

```sql
CREATE TABLE public.maintenance_sub_users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    main_user_id UUID NOT NULL REFERENCES auth.users(id),
    sub_user_id UUID NOT NULL REFERENCES auth.users(id),
    maintenance_team_id UUID REFERENCES maintenance_teams(id),
    role TEXT NOT NULL,
    permissions JSONB DEFAULT '{"view_issues": true, "assign_tools": false, "view_inventory": false}',
    active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(sub_user_id)
);
```

**Indexes:**
```sql
CREATE INDEX idx_maintenance_sub_users_main_user ON maintenance_sub_users(main_user_id);
CREATE INDEX idx_maintenance_sub_users_sub_user ON maintenance_sub_users(sub_user_id);
CREATE INDEX idx_maintenance_sub_users_team ON maintenance_sub_users(maintenance_team_id);
```

---

#### 13. security_main_users
**Purpose:** Security teams assigned to properties

```sql
CREATE TABLE public.security_main_users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    landlord_id UUID NOT NULL REFERENCES auth.users(id),
    main_user_id UUID NOT NULL REFERENCES auth.users(id),
    team_name TEXT NOT NULL,
    contact_email TEXT,
    contact_phone TEXT,
    active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);
```

---

#### 14. security_sub_users
**Purpose:** Security guards with permissions

```sql
CREATE TABLE public.security_sub_users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    main_user_id UUID NOT NULL REFERENCES auth.users(id),
    sub_user_id UUID NOT NULL REFERENCES auth.users(id),
    role TEXT NOT NULL,
    permissions JSONB DEFAULT '{
        "check_in_visitors": true,
        "view_permits": true,
        "manage_blacklist": false,
        "manage_rounds": false,
        "view_incidents": true,
        "create_incidents": true,
        "access_control": false,
        "emergency_response": false
    }',
    active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(sub_user_id)
);
```

---

#### 15. security_assigned_properties
**Purpose:** Link security users to properties

```sql
CREATE TABLE public.security_assigned_properties (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    security_user_id UUID NOT NULL REFERENCES auth.users(id),
    property_id UUID NOT NULL REFERENCES properties(id),
    created_at TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(security_user_id, property_id)
);
```

---

#### 16. visitor_permits
**Purpose:** Visitor access permissions

```sql
CREATE TABLE public.visitor_permits (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    property_id UUID NOT NULL REFERENCES properties(id),
    unit_id UUID REFERENCES units(id),
    tenant_user_id UUID NOT NULL REFERENCES auth.users(id),
    visitor_name TEXT NOT NULL,
    visitor_national_id TEXT,
    visitor_phone TEXT,
    visitor_vehicle TEXT,
    purpose TEXT,
    expected_date DATE NOT NULL,
    expected_time TIME,
    valid_from TIMESTAMPTZ NOT NULL,
    valid_until TIMESTAMPTZ NOT NULL,
    status TEXT DEFAULT 'PENDING',
    qr_code TEXT,
    checked_in_at TIMESTAMPTZ,
    checked_out_at TIMESTAMPTZ,
    checked_in_by_user_id UUID REFERENCES auth.users(id),
    notes TEXT,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);
```

**Indexes:**
```sql
CREATE INDEX idx_visitor_permits_property_id ON visitor_permits(property_id);
CREATE INDEX idx_visitor_permits_tenant_user_id ON visitor_permits(tenant_user_id);
CREATE INDEX idx_visitor_permits_status ON visitor_permits(status);
CREATE INDEX idx_visitor_permits_expected_date ON visitor_permits(expected_date);
```

---

#### 17. incidents
**Purpose:** Security incidents and complaints

```sql
CREATE TABLE public.incidents (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    property_id UUID NOT NULL REFERENCES properties(id),
    reported_by_user_id UUID NOT NULL REFERENCES auth.users(id),
    title TEXT NOT NULL,
    description TEXT,
    severity incident_severity DEFAULT 'LOW',
    status TEXT DEFAULT 'OPEN',
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TYPE incident_severity AS ENUM ('LOW', 'MEDIUM', 'HIGH', 'CRITICAL');
```

**Indexes:**
```sql
CREATE INDEX idx_incidents_property_id ON incidents(property_id);
CREATE INDEX idx_incidents_severity ON incidents(severity);
CREATE INDEX idx_incidents_status ON incidents(status);
```

---

#### 18. service_providers
**Purpose:** External service providers

```sql
CREATE TABLE public.service_providers (
    id UUID PRIMARY KEY REFERENCES auth.users(id) ON DELETE CASCADE,
    business_name TEXT,
    contact_person TEXT,
    phone TEXT,
    email TEXT,
    address TEXT,
    services_offered TEXT[],
    rating DECIMAL(3,2),
    total_jobs INTEGER DEFAULT 0,
    approved BOOLEAN DEFAULT FALSE,
    visibility_tier TEXT DEFAULT 'FREE',
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);
```

---

#### 19. service_provider_sub_users
**Purpose:** Sub-users for service providers

```sql
CREATE TABLE public.service_provider_sub_users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    main_provider_id UUID NOT NULL REFERENCES auth.users(id),
    sub_user_id UUID NOT NULL REFERENCES auth.users(id),
    role TEXT NOT NULL,
    permissions JSONB DEFAULT '{
        "view_bookings": true,
        "manage_bookings": false,
        "view_services": true,
        "manage_services": false,
        "view_analytics": false
    }',
    active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(sub_user_id)
);
```

---

#### 20. service_listings
**Purpose:** Services offered by providers

```sql
CREATE TABLE public.service_listings (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    provider_id UUID NOT NULL REFERENCES service_providers(id),
    title TEXT NOT NULL,
    description TEXT,
    category TEXT NOT NULL,
    base_price DECIMAL(10,2),
    pricing_type TEXT DEFAULT 'FIXED',
    duration_minutes INTEGER,
    availability TEXT[],
    images TEXT[],
    active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);
```

---

#### 21. service_bookings
**Purpose:** Tenant bookings for services

```sql
CREATE TABLE public.service_bookings (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    service_listing_id UUID NOT NULL REFERENCES service_listings(id),
    tenant_user_id UUID NOT NULL REFERENCES auth.users(id),
    property_id UUID NOT NULL REFERENCES properties(id),
    unit_id UUID REFERENCES units(id),
    provider_id UUID NOT NULL REFERENCES service_providers(id),
    booking_type TEXT NOT NULL,
    scheduled_date DATE NOT NULL,
    scheduled_time TIME,
    recurrence_pattern TEXT,
    status TEXT DEFAULT 'PENDING',
    total_amount DECIMAL(10,2),
    notes TEXT,
    rating INTEGER CHECK (rating >= 1 AND rating <= 5),
    review TEXT,
    completed_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);
```

---

#### 22. landlord_sub_users
**Purpose:** Sub-users for landlord accounts

```sql
CREATE TABLE public.landlord_sub_users (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    landlord_id UUID NOT NULL REFERENCES auth.users(id),
    sub_user_id UUID NOT NULL REFERENCES auth.users(id),
    access_level TEXT NOT NULL,
    permissions JSONB DEFAULT '{}',
    active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    UNIQUE(sub_user_id)
);
```

---

#### 23. documents
**Purpose:** Document storage metadata

```sql
CREATE TABLE public.documents (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    landlord_id UUID REFERENCES auth.users(id),
    property_id UUID REFERENCES properties(id),
    unit_id UUID REFERENCES units(id),
    tenant_user_id UUID REFERENCES auth.users(id),
    uploaded_by UUID NOT NULL REFERENCES auth.users(id),
    uploaded_by_role TEXT NOT NULL,
    title TEXT NOT NULL,
    description TEXT,
    category document_category NOT NULL,
    file_name TEXT NOT NULL,
    file_path TEXT NOT NULL,
    file_size BIGINT NOT NULL,
    mime_type TEXT NOT NULL,
    tags TEXT[],
    version INTEGER DEFAULT 1,
    parent_document_id UUID REFERENCES documents(id),
    expiry_date DATE,
    is_shared_with_tenant BOOLEAN DEFAULT FALSE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW(),
    deleted_at TIMESTAMPTZ
);

CREATE TYPE document_category AS ENUM (
    'LEASE_AGREEMENT', 'INVOICE', 'PAYMENT_PROOF', 
    'MAINTENANCE_REPORT', 'PROPERTY_DOCUMENT', 
    'INSURANCE', 'INSPECTION_REPORT', 'OTHER'
);
```

---

#### 24. notifications
**Purpose:** User notifications

```sql
CREATE TABLE public.notifications (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES auth.users(id),
    type notification_type NOT NULL,
    title TEXT NOT NULL,
    body TEXT NOT NULL,
    data JSONB,
    read_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TYPE notification_type AS ENUM (
    'ANNOUNCEMENT', 'PAYMENT_REMINDER', 'LEASE_RENEWAL',
    'WORK_ORDER', 'INVOICE', 'MESSAGE'
);
```

---

#### 25. devices (Smart Home)
**Purpose:** IoT devices

```sql
CREATE TABLE public.devices (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    property_id UUID NOT NULL REFERENCES properties(id),
    unit_id UUID REFERENCES units(id),
    type device_type NOT NULL,
    provider TEXT NOT NULL,
    external_id TEXT NOT NULL,
    status device_status DEFAULT 'OFFLINE',
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TYPE device_type AS ENUM (
    'SMART_LOCK', 'THERMOSTAT', 'LIGHT', 
    'CAMERA', 'SWITCH', 'SENSOR'
);

CREATE TYPE device_status AS ENUM ('ONLINE', 'OFFLINE', 'ERROR');
```

---

### Supporting Tables

#### blacklist
```sql
CREATE TABLE public.blacklist (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    national_id TEXT UNIQUE NOT NULL,
    reason TEXT NOT NULL,
    active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);
```

#### email_logs
```sql
CREATE TABLE public.email_logs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    recipient_email TEXT NOT NULL,
    recipient_user_id UUID,
    template_key TEXT NOT NULL,
    subject TEXT NOT NULL,
    event_type TEXT NOT NULL,
    status TEXT DEFAULT 'PENDING',
    sent_at TIMESTAMPTZ,
    error_message TEXT,
    event_data JSONB,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);
```

#### audit_logs
```sql
CREATE TABLE public.audit_logs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    actor_user_id UUID REFERENCES auth.users(id),
    action TEXT NOT NULL,
    resource_type TEXT NOT NULL,
    resource_id TEXT,
    diff JSONB,
    ip TEXT,
    ua TEXT,
    created_at TIMESTAMPTZ DEFAULT NOW()
);
```

---

## Row Level Security (RLS) Policies

### Security Helper Functions

```sql
-- Check if user has admin role
CREATE OR REPLACE FUNCTION public.has_admin_role(_user_id UUID)
RETURNS BOOLEAN
LANGUAGE SQL
STABLE SECURITY DEFINER
SET search_path = public
AS $$
  SELECT EXISTS (
    SELECT 1
    FROM public.user_roles ur
    JOIN public.roles r ON ur.role_id = r.id
    WHERE ur.user_id = _user_id
      AND r.name IN ('PLATFORM_ADMIN', 'ADMIN')
  );
$$;

-- Check if user is landlord of property
CREATE OR REPLACE FUNCTION public.is_landlord_of_property(_user_id UUID, _property_id UUID)
RETURNS BOOLEAN
LANGUAGE SQL
STABLE SECURITY DEFINER
SET search_path = public
AS $$
  SELECT EXISTS (
    SELECT 1
    FROM public.properties
    WHERE id = _property_id
    AND landlord_id = _user_id
  );
$$;

-- Check if user is tenant
CREATE OR REPLACE FUNCTION public.is_tenant(_user_id UUID)
RETURNS BOOLEAN
LANGUAGE SQL
STABLE SECURITY DEFINER
SET search_path = public
AS $$
  SELECT EXISTS (
    SELECT 1
    FROM public.user_roles ur
    JOIN public.roles r ON ur.role_id = r.id
    WHERE ur.user_id = _user_id
      AND r.name = 'TENANT'
  );
$$;

-- Check maintenance sub-user permission
CREATE OR REPLACE FUNCTION public.is_maintenance_sub_user_with_permission(_user_id UUID, _permission TEXT)
RETURNS BOOLEAN
LANGUAGE SQL
STABLE SECURITY DEFINER
SET search_path = public
AS $$
  SELECT EXISTS (
    SELECT 1
    FROM public.maintenance_sub_users
    WHERE sub_user_id = _user_id
      AND active = TRUE
      AND (permissions->>_permission)::BOOLEAN = TRUE
  );
$$;

-- Check security sub-user permission
CREATE OR REPLACE FUNCTION public.is_security_sub_user_with_permission(_user_id UUID, _permission TEXT)
RETURNS BOOLEAN
LANGUAGE SQL
STABLE SECURITY DEFINER
SET search_path = public
AS $$
  SELECT EXISTS (
    SELECT 1
    FROM public.security_sub_users
    WHERE sub_user_id = _user_id
      AND active = TRUE
      AND (permissions->>_permission)::BOOLEAN = TRUE
  );
$$;

-- Check landlord sub-user permission
CREATE OR REPLACE FUNCTION public.is_landlord_sub_user_with_permission(
    _user_id UUID, 
    _landlord_id UUID, 
    _permission TEXT
)
RETURNS BOOLEAN
LANGUAGE PLPGSQL
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  _sub_user RECORD;
BEGIN
  SELECT * INTO _sub_user
  FROM public.landlord_sub_users
  WHERE sub_user_id = _user_id
    AND landlord_id = _landlord_id
    AND active = TRUE;
  
  IF NOT FOUND THEN
    RETURN FALSE;
  END IF;
  
  RETURN (_sub_user.permissions->_permission)::BOOLEAN = TRUE;
END;
$$;
```

### RLS Policies by Table

#### properties
```sql
ALTER TABLE properties ENABLE ROW LEVEL SECURITY;

-- Admins can manage all properties
CREATE POLICY "Admins can manage all properties"
ON properties FOR ALL
TO authenticated
USING (has_admin_role(auth.uid()));

-- Landlords can manage their own properties
CREATE POLICY "Landlords can manage their properties"
ON properties FOR ALL
TO authenticated
USING (landlord_id = auth.uid());

-- Tenants can view properties they have leases for
CREATE POLICY "Tenants can view their properties"
ON properties FOR SELECT
TO authenticated
USING (
    EXISTS (
        SELECT 1 FROM leases l
        JOIN units u ON l.unit_id = u.id
        WHERE u.property_id = properties.id
        AND l.tenant_user_id = auth.uid()
        AND l.status = 'ACTIVE'
    )
);
```

#### units
```sql
ALTER TABLE units ENABLE ROW LEVEL SECURITY;

-- Landlords can manage units in their properties
CREATE POLICY "Landlords can manage their units"
ON units FOR ALL
TO authenticated
USING (
    EXISTS (
        SELECT 1 FROM properties p
        WHERE p.id = units.property_id
        AND p.landlord_id = auth.uid()
    )
);

-- Tenants can view their units
CREATE POLICY "Tenants can view their units"
ON units FOR SELECT
TO authenticated
USING (
    EXISTS (
        SELECT 1 FROM leases l
        WHERE l.unit_id = units.id
        AND l.tenant_user_id = auth.uid()
    )
);
```

#### leases
```sql
ALTER TABLE leases ENABLE ROW LEVEL SECURITY;

-- Landlords can manage leases for their properties
CREATE POLICY "Landlords can manage their leases"
ON leases FOR ALL
TO authenticated
USING (
    landlord_org_id = auth.uid() OR
    EXISTS (
        SELECT 1 FROM units u
        JOIN properties p ON u.property_id = p.id
        WHERE u.id = leases.unit_id
        AND p.landlord_id = auth.uid()
    )
);

-- Tenants can view their own leases
CREATE POLICY "Tenants can view their leases"
ON leases FOR SELECT
TO authenticated
USING (tenant_user_id = auth.uid());
```

#### invoices
```sql
ALTER TABLE invoices ENABLE ROW LEVEL SECURITY;

-- Landlords can manage invoices for their leases
CREATE POLICY "Landlords can manage invoices"
ON invoices FOR ALL
TO authenticated
USING (
    EXISTS (
        SELECT 1 FROM leases l
        WHERE l.id = invoices.lease_id
        AND l.landlord_org_id = auth.uid()
    ) OR has_admin_role(auth.uid())
);

-- Tenants can view their invoices
CREATE POLICY "Tenants can view their invoices"
ON invoices FOR SELECT
TO authenticated
USING (tenant_user_id = auth.uid());
```

#### payments
```sql
ALTER TABLE payments ENABLE ROW LEVEL SECURITY;

-- Tenants can create payments for their invoices
CREATE POLICY "Tenants can create payments"
ON payments FOR INSERT
TO authenticated
WITH CHECK (
    EXISTS (
        SELECT 1 FROM invoices i
        WHERE i.id = payments.invoice_id
        AND i.tenant_user_id = auth.uid()
    )
);

-- Tenants and landlords can view payments
CREATE POLICY "Users can view relevant payments"
ON payments FOR SELECT
TO authenticated
USING (
    EXISTS (
        SELECT 1 FROM invoices i
        WHERE i.id = payments.invoice_id
        AND (
            i.tenant_user_id = auth.uid() OR
            EXISTS (
                SELECT 1 FROM leases l
                WHERE l.id = i.lease_id
                AND l.landlord_org_id = auth.uid()
            )
        )
    )
);
```

#### issue_reports
```sql
ALTER TABLE issue_reports ENABLE ROW LEVEL SECURITY;

-- Tenants can create and view their issue reports
CREATE POLICY "Tenants can manage their issues"
ON issue_reports
FOR ALL
TO authenticated
USING (tenant_user_id = auth.uid());

-- Landlords can view and manage issues for their properties
CREATE POLICY "Landlords can manage property issues"
ON issue_reports
FOR ALL
TO authenticated
USING (
    EXISTS (
        SELECT 1 FROM properties p
        WHERE p.id = issue_reports.property_id
        AND p.landlord_id = auth.uid()
    )
);

-- Maintenance teams can view assigned work orders
CREATE POLICY "Maintenance teams can view assigned work"
ON issue_reports
FOR SELECT
TO authenticated
USING (
    assigned_maintenance_team_id IN (
        SELECT id FROM maintenance_teams
        WHERE main_user_id = auth.uid() OR landlord_id = auth.uid()
    ) OR is_maintenance_sub_user_with_permission(auth.uid(), 'view_issues')
);

-- Maintenance teams can update assigned work orders
CREATE POLICY "Maintenance teams can update work"
ON issue_reports
FOR UPDATE
TO authenticated
USING (
    assigned_maintenance_team_id IN (
        SELECT id FROM maintenance_teams
        WHERE main_user_id = auth.uid() OR landlord_id = auth.uid()
    ) OR is_maintenance_sub_user_with_permission(auth.uid(), 'view_issues')
);
```

#### maintenance_teams
```sql
ALTER TABLE maintenance_teams ENABLE ROW LEVEL SECURITY;

-- Landlords can create and manage their teams
CREATE POLICY "Landlords can manage their teams"
ON maintenance_teams
FOR ALL
TO authenticated
USING (
    landlord_id = auth.uid() OR 
    main_user_id = auth.uid()
);
```

#### maintenance_sub_users
```sql
ALTER TABLE maintenance_sub_users ENABLE ROW LEVEL SECURITY;

-- Main users can manage their sub-users
CREATE POLICY "Main users can manage sub-users"
ON maintenance_sub_users
FOR ALL
TO authenticated
USING (main_user_id = auth.uid());

-- Sub-users can view their own record
CREATE POLICY "Sub-users can view their record"
ON maintenance_sub_users
FOR SELECT
TO authenticated
USING (sub_user_id = auth.uid());
```

#### security_assigned_properties
```sql
ALTER TABLE security_assigned_properties ENABLE ROW LEVEL SECURITY;

-- Security users can view their assignments
CREATE POLICY "Security users can view assignments"
ON security_assigned_properties
FOR SELECT
TO authenticated
USING (security_user_id = auth.uid());

-- Landlords can manage assignments for their properties
CREATE POLICY "Landlords can manage assignments"
ON security_assigned_properties
FOR ALL
TO authenticated
USING (
    EXISTS (
        SELECT 1 FROM properties p
        WHERE p.id = security_assigned_properties.property_id
        AND p.landlord_id = auth.uid()
    )
);
```

#### visitor_permits
```sql
ALTER TABLE visitor_permits ENABLE ROW LEVEL SECURITY;

-- Tenants can create and manage their permits
CREATE POLICY "Tenants can manage permits"
ON visitor_permits
FOR ALL
TO authenticated
USING (tenant_user_id = auth.uid());

-- Security can view and update permits for assigned properties
CREATE POLICY "Security can manage permits"
ON visitor_permits
FOR ALL
TO authenticated
USING (
    EXISTS (
        SELECT 1 FROM security_assigned_properties
        WHERE security_user_id = auth.uid()
        AND property_id = visitor_permits.property_id
    ) OR is_security_sub_user_with_permission(auth.uid(), 'view_permits')
);
```

#### incidents
```sql
ALTER TABLE incidents ENABLE ROW LEVEL SECURITY;

-- Security users can create incidents
CREATE POLICY "Security can create incidents"
ON incidents
FOR INSERT
TO authenticated
WITH CHECK (
    EXISTS (
        SELECT 1 FROM security_assigned_properties
        WHERE security_user_id = auth.uid()
        AND property_id = incidents.property_id
    ) OR reported_by_user_id = auth.uid()
);

-- Security users can view incidents for assigned properties
CREATE POLICY "Security can view incidents"
ON incidents
FOR SELECT
TO authenticated
USING (
    EXISTS (
        SELECT 1 FROM security_assigned_properties
        WHERE security_user_id = auth.uid()
        AND property_id = incidents.property_id
    ) OR 
    is_security_sub_user_with_permission(auth.uid(), 'view_incidents') OR
    reported_by_user_id = auth.uid()
);
```

#### service_bookings
```sql
ALTER TABLE service_bookings ENABLE ROW LEVEL SECURITY;

-- Tenants can create and view their bookings
CREATE POLICY "Tenants can manage bookings"
ON service_bookings
FOR ALL
TO authenticated
USING (tenant_user_id = auth.uid());

-- Providers can view and manage their bookings
CREATE POLICY "Providers can manage their bookings"
ON service_bookings
FOR ALL
TO authenticated
USING (provider_id = auth.uid());
```

#### documents
```sql
ALTER TABLE documents ENABLE ROW LEVEL SECURITY;

-- Landlords can manage documents for their properties
CREATE POLICY "Landlords can manage documents"
ON documents
FOR ALL
TO authenticated
USING (
    landlord_id = auth.uid() OR
    is_landlord_of_property(auth.uid(), property_id)
);

-- Tenants can view shared documents
CREATE POLICY "Tenants can view shared documents"
ON documents
FOR SELECT
TO authenticated
USING (
    tenant_user_id = auth.uid() AND is_shared_with_tenant = TRUE
);
```

#### notifications
```sql
ALTER TABLE notifications ENABLE ROW LEVEL SECURITY;

-- Users can view their own notifications
CREATE POLICY "Users can view their notifications"
ON notifications
FOR SELECT
TO authenticated
USING (user_id = auth.uid());

-- Users can update their own notifications (mark as read)
CREATE POLICY "Users can update their notifications"
ON notifications
FOR UPDATE
TO authenticated
USING (user_id = auth.uid());
```

---

## Database Functions

### 1. Notification Creation
```sql
CREATE OR REPLACE FUNCTION public.create_notification(
    p_user_id UUID,
    p_type notification_type,
    p_title TEXT,
    p_body TEXT,
    p_data JSONB DEFAULT '{}'
)
RETURNS UUID
LANGUAGE PLPGSQL
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  notification_id UUID;
BEGIN
  INSERT INTO public.notifications (user_id, type, title, body, data)
  VALUES (p_user_id, p_type, p_title, p_body, p_data)
  RETURNING id INTO notification_id;
  
  RETURN notification_id;
END;
$$;
```

### 2. Work Order Pricing Trigger
```sql
CREATE OR REPLACE FUNCTION public.set_work_order_pricing()
RETURNS TRIGGER
LANGUAGE PLPGSQL
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  lease_maintenance_resp TEXT;
BEGIN
  -- Get maintenance responsibility from active lease
  SELECT maintenance_responsibility INTO lease_maintenance_resp
  FROM public.leases
  WHERE unit_id = NEW.unit_id
    AND tenant_user_id = NEW.tenant_user_id
    AND status = 'ACTIVE'
  LIMIT 1;

  -- Set is_paid based on responsibility
  IF lease_maintenance_resp = 'LANDLORD' THEN
    NEW.is_paid := FALSE;  -- Free for tenant
  ELSIF lease_maintenance_resp = 'TENANT' THEN
    NEW.is_paid := TRUE;   -- Tenant pays
  ELSIF lease_maintenance_resp = 'SHARED' THEN
    NEW.is_paid := NULL;   -- To be decided
  ELSE
    NEW.is_paid := FALSE;  -- Default to landlord responsibility
  END IF;

  RETURN NEW;
END;
$$;

CREATE TRIGGER set_work_order_pricing_trigger
BEFORE INSERT ON issue_reports
FOR EACH ROW
EXECUTE FUNCTION set_work_order_pricing();
```

### 3. SLA Management
```sql
CREATE OR REPLACE FUNCTION public.set_work_order_sla()
RETURNS TRIGGER
LANGUAGE PLPGSQL
SECURITY DEFINER
AS $$
DECLARE
  sla_hours INTEGER;
BEGIN
  -- Set SLA based on priority
  CASE NEW.priority
    WHEN 'EMERGENCY' THEN sla_hours := 4;
    WHEN 'HIGH' THEN sla_hours := 24;
    WHEN 'MEDIUM' THEN sla_hours := 72;
    WHEN 'LOW' THEN sla_hours := 168;
    ELSE sla_hours := 72;
  END CASE;
  
  NEW.sla_due_date := NEW.created_at + (sla_hours || ' hours')::INTERVAL;
  
  -- Check if approval required
  IF NEW.cost_estimate IS NOT NULL AND NEW.cost_estimate > 1000 THEN
    NEW.approval_status := 'PENDING';
  ELSE
    NEW.approval_status := 'NOT_REQUIRED';
  END IF;
  
  RETURN NEW;
END;
$$;

CREATE TRIGGER set_work_order_sla_trigger
BEFORE INSERT ON issue_reports
FOR EACH ROW
EXECUTE FUNCTION set_work_order_sla();
```

### 4. Smart Assignment Algorithm
```sql
CREATE OR REPLACE FUNCTION public.calculate_assignment_score(
    p_work_order_id UUID,
    p_team_id UUID
)
RETURNS JSONB
LANGUAGE PLPGSQL
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  v_score NUMERIC := 0;
  v_factors JSONB := '{}';
  v_work_order RECORD;
  v_team RECORD;
  v_active_orders INTEGER;
  v_avg_completion NUMERIC;
  v_specialty_match BOOLEAN := FALSE;
  v_team_rating NUMERIC;
BEGIN
  -- Get work order and team details
  SELECT * INTO v_work_order FROM issue_reports WHERE id = p_work_order_id LIMIT 1;
  SELECT * INTO v_team FROM maintenance_teams WHERE id = p_team_id LIMIT 1;
  
  IF v_work_order IS NULL OR v_team IS NULL THEN
    RETURN jsonb_build_object('error', 'Not found', 'total_score', 0);
  END IF;
  
  -- Factor 1: Current workload (0-30 points)
  SELECT COUNT(*) INTO v_active_orders
  FROM issue_reports
  WHERE assigned_maintenance_team_id = p_team_id
  AND status NOT IN ('COMPLETED', 'CANCELLED')
  LIMIT 100;
  
  v_score := v_score + GREATEST(0, 30 - (v_active_orders * 5));
  v_factors := jsonb_set(v_factors, '{workload}', to_jsonb(GREATEST(0, 30 - (v_active_orders * 5))));
  
  -- Factor 2: Specialty match (0-25 points)
  IF v_team.specialties IS NOT NULL AND v_work_order.category = ANY(v_team.specialties) THEN
    v_score := v_score + 25;
    v_specialty_match := TRUE;
  END IF;
  v_factors := jsonb_set(v_factors, '{specialty_match}', to_jsonb(v_specialty_match));
  
  -- Factor 3: Average completion time (0-20 points)
  SELECT AVG(EXTRACT(EPOCH FROM (completed_at - created_at)) / 3600) INTO v_avg_completion
  FROM (
    SELECT completed_at, created_at
    FROM issue_reports
    WHERE assigned_maintenance_team_id = p_team_id
    AND status = 'COMPLETED'
    AND completed_at IS NOT NULL
    ORDER BY completed_at DESC
    LIMIT 50
  ) recent_orders;
  
  IF v_avg_completion IS NOT NULL THEN
    v_score := v_score + GREATEST(0, 20 - (v_avg_completion / 10));
  END IF;
  
  -- Factor 4: Team rating (0-25 points)
  SELECT AVG(tenant_rating) INTO v_team_rating
  FROM (
    SELECT tenant_rating
    FROM issue_reports
    WHERE assigned_maintenance_team_id = p_team_id
    AND tenant_rating IS NOT NULL
    ORDER BY created_at DESC
    LIMIT 50
  ) recent_ratings;
  
  IF v_team_rating IS NOT NULL THEN
    v_score := v_score + (v_team_rating * 5);
  END IF;
  
  v_factors := jsonb_set(v_factors, '{total_score}', to_jsonb(v_score));
  
  RETURN v_factors;
END;
$$;
```

### 5. Get Recommended Service Providers
```sql
CREATE OR REPLACE FUNCTION public.get_recommended_service_providers(
    _category TEXT,
    _property_id UUID
)
RETURNS UUID[]
LANGUAGE SQL
STABLE SECURITY DEFINER
SET search_path = public
AS $$
  SELECT ARRAY_AGG(sp.id)
  FROM service_providers sp
  JOIN service_listings sl ON sl.provider_id = sp.id
  WHERE sl.category = _category
    AND sl.active = TRUE
    AND sp.approved = TRUE
  LIMIT 5;
$$;
```

### 6. Work Order Notification Trigger
```sql
CREATE OR REPLACE FUNCTION public.notify_work_order_assignment()
RETURNS TRIGGER
LANGUAGE PLPGSQL
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  property_landlord_id UUID;
  recommended_providers UUID[];
BEGIN
  SELECT p.landlord_id INTO property_landlord_id
  FROM properties p
  WHERE p.id = NEW.property_id;

  -- Notify tenant on creation
  IF TG_OP = 'INSERT' THEN
    PERFORM create_notification(
      NEW.tenant_user_id,
      'WORK_ORDER',
      'Issue Report Submitted',
      'Your issue report "' || NEW.title || '" has been submitted.',
      jsonb_build_object('issue_id', NEW.id, 'status', NEW.status)
    );
    
    -- Notify landlord
    IF property_landlord_id IS NOT NULL THEN
      PERFORM create_notification(
        property_landlord_id,
        'WORK_ORDER',
        'New Issue Reported',
        'A new issue "' || NEW.title || '" has been reported.',
        jsonb_build_object('issue_id', NEW.id)
      );
    END IF;
  END IF;

  -- Notify on status changes
  IF TG_OP = 'UPDATE' AND OLD.status != NEW.status THEN
    PERFORM create_notification(
      NEW.tenant_user_id,
      'WORK_ORDER',
      'Issue Status Updated',
      'Your issue "' || NEW.title || '" status changed to ' || NEW.status,
      jsonb_build_object('issue_id', NEW.id, 'status', NEW.status)
    );
    
    -- If rejected, get recommended providers
    IF NEW.status = 'REJECTED' THEN
      recommended_providers := public.get_recommended_service_providers(NEW.category, NEW.property_id);
      NEW.recommended_service_provider_ids := recommended_providers;
    END IF;
  END IF;

  RETURN NEW;
END;
$$;

CREATE TRIGGER notify_work_order_trigger
AFTER INSERT OR UPDATE ON issue_reports
FOR EACH ROW
EXECUTE FUNCTION notify_work_order_assignment();
```

### 7. New User Handler
```sql
CREATE OR REPLACE FUNCTION public.handle_new_user()
RETURNS TRIGGER
LANGUAGE PLPGSQL
SECURITY DEFINER
SET search_path = public
AS $$
DECLARE
  role_name TEXT;
  role_id_var UUID;
BEGIN
  -- Get role from metadata or default to TENANT
  role_name := COALESCE(NEW.raw_user_meta_data->>'role', 'TENANT');
  
  -- Get role ID
  SELECT id INTO role_id_var FROM public.roles WHERE name = role_name LIMIT 1;

  -- Insert into users table
  INSERT INTO public.users (id, email, name, status)
  VALUES (NEW.id, NEW.email, 
    COALESCE(NEW.raw_user_meta_data->>'name', split_part(NEW.email, '@', 1)),
    'ACTIVE'::user_status
  );

  -- Insert into profiles
  INSERT INTO public.profiles (user_id, name)
  VALUES (NEW.id, 
    COALESCE(NEW.raw_user_meta_data->>'name', split_part(NEW.email, '@', 1))
  );

  -- Assign role
  IF role_id_var IS NOT NULL THEN
    INSERT INTO public.user_roles (user_id, role_id)
    VALUES (NEW.id, role_id_var);
  END IF;

  -- Create service_providers entry for PROVIDER role
  IF role_name = 'PROVIDER' THEN
    INSERT INTO public.service_providers (id, approved, visibility_tier)
    VALUES (NEW.id, TRUE, 'FREE');
  END IF;

  RETURN NEW;
EXCEPTION
  WHEN OTHERS THEN
    RAISE WARNING 'Error in handle_new_user: %', SQLERRM;
    RETURN NEW;
END;
$$;

-- Trigger on auth.users
CREATE TRIGGER on_auth_user_created
AFTER INSERT ON auth.users
FOR EACH ROW
EXECUTE FUNCTION handle_new_user();
```

---

## Edge Functions (API Endpoints)

### Authentication Required
All edge functions require JWT verification unless explicitly marked as `verify_jwt = false`.

### 1. create-user
**Path:** `/functions/v1/create-user`  
**Method:** POST  
**Auth:** Required (Admin only)  
**Purpose:** Create new users with specific roles

**Request Body:**
```json
{
  "email": "user@example.com",
  "password": "securepassword",
  "name": "John Doe",
  "role": "TENANT"
}
```

**Response:**
```json
{
  "success": true,
  "user": {
    "id": "uuid",
    "email": "user@example.com"
  }
}
```

**Implementation (.NET Core):**
```csharp
[HttpPost("api/users")]
[Authorize(Roles = "PLATFORM_ADMIN")]
public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
{
    // Validate request
    // Create user in Identity
    // Assign role
    // Create profile
    // Return response
}
```

---

### 2. create-maintenance-user
**Path:** `/functions/v1/create-maintenance-user`  
**Method:** POST  
**Auth:** Required (Landlord only)  
**Purpose:** Create maintenance team and main user

**Request Body:**
```json
{
  "email": "maintenance@example.com",
  "password": "securepassword",
  "team_name": "ProMaintenance",
  "contact_email": "contact@promaintenance.com",
  "contact_phone": "+1234567890",
  "specialties": ["Plumbing", "Electrical"]
}
```

---

### 3. create-sub-user
**Path:** `/functions/v1/create-sub-user`  
**Method:** POST  
**Auth:** Required (Main user only)  
**Purpose:** Create sub-users for maintenance teams

**Request Body:**
```json
{
  "email": "subuser@example.com",
  "password": "securepassword",
  "name": "Sub User",
  "role": "technician",
  "permissions": {
    "view_issues": true,
    "assign_tools": false,
    "view_inventory": true
  },
  "maintenance_team_id": "uuid"
}
```

---

### 4. create-security-user
**Path:** `/functions/v1/create-security-user`  
**Method:** POST  
**Auth:** Required (Landlord/Security main user)  
**Purpose:** Create security team or sub-user

**Request Body:**
```json
{
  "email": "guard@example.com",
  "password": "securepassword",
  "name": "Security Guard",
  "is_sub_user": true,
  "team_name": "Security Pro",
  "role": "gate_guard",
  "permissions": {
    "check_in_visitors": true,
    "view_permits": true,
    "manage_blacklist": false
  }
}
```

---

### 5. create-service-provider-user
**Path:** `/functions/v1/create-service-provider-user`  
**Method:** POST  
**Auth:** Required  
**Purpose:** Create service provider account

**Request Body:**
```json
{
  "email": "provider@example.com",
  "password": "securepassword",
  "business_name": "CleanCorp",
  "contact_person": "John Doe",
  "phone": "+1234567890",
  "services_offered": ["Cleaning", "Pest Control"]
}
```

---

### 6. create-landlord-sub-user
**Path:** `/functions/v1/create-landlord-sub-user`  
**Method:** POST  
**Auth:** Required (Landlord only)  
**Purpose:** Create sub-users for landlord teams

**Request Body:**
```json
{
  "email": "assistant@example.com",
  "password": "securepassword",
  "name": "Property Manager",
  "access_level": "manager",
  "permissions": {
    "manage_properties": true,
    "manage_tenants": true,
    "view_financials": false
  }
}
```

---

### 7. create-payment-intent
**Path:** `/functions/v1/create-payment-intent`  
**Method:** POST  
**Auth:** Required (Tenant only)  
**Purpose:** Create Stripe payment intent for invoice

**Request Body:**
```json
{
  "invoice_id": "uuid",
  "payment_method_id": "pm_xxx"
}
```

**Response:**
```json
{
  "client_secret": "pi_xxx_secret_xxx",
  "payment_intent_id": "pi_xxx"
}
```

---

### 8. verify-bank-payment
**Path:** `/functions/v1/verify-bank-payment`  
**Method:** POST  
**Auth:** Required (Landlord only)  
**Purpose:** Verify manual bank transfer payments

**Request Body:**
```json
{
  "payment_id": "uuid",
  "verified": true,
  "notes": "Payment confirmed"
}
```

---

### 9. generate-invoice
**Path:** `/functions/v1/generate-invoice`  
**Method:** POST  
**Auth:** Required (Landlord/Admin)  
**Purpose:** Generate invoice for a lease

**Request Body:**
```json
{
  "lease_id": "uuid",
  "amount": 1500.00,
  "due_date": "2025-02-01"
}
```

---

### 10. auto-generate-invoices
**Path:** `/functions/v1/auto-generate-invoices`  
**Method:** POST  
**Auth:** Service role (scheduled job)  
**Purpose:** Automatically generate monthly invoices

**Implementation:** Cron job / Background service

---

### 11. auto-process-payments
**Path:** `/functions/v1/auto-process-payments`  
**Method:** POST  
**Auth:** Service role (scheduled job)  
**Purpose:** Process pending auto-payments

---

### 12. process-stripe-webhook
**Path:** `/functions/v1/process-stripe-webhook`  
**Method:** POST  
**Auth:** None (webhook signature verification)  
**Purpose:** Handle Stripe webhook events

**Events Handled:**
- `payment_intent.succeeded`
- `payment_intent.failed`
- `charge.refunded`

---

### 13. send-email
**Path:** `/functions/v1/send-email`  
**Method:** POST  
**Auth:** Service role  
**Purpose:** Send transactional emails

**Request Body:**
```json
{
  "to": "user@example.com",
  "template_key": "lease_renewal",
  "data": {
    "tenant_name": "John Doe",
    "lease_end_date": "2025-12-31"
  }
}
```

---

### 14. send-verification-email
**Path:** `/functions/v1/send-verification-email`  
**Method:** POST  
**Auth:** Required  
**Purpose:** Send email verification link

---

### 15. verify-email
**Path:** `/functions/v1/verify-email`  
**Method:** GET  
**Auth:** None  
**Purpose:** Verify email with token

**Query Params:**
- `token`: Verification token

---

### 16. process-provider-payouts
**Path:** `/functions/v1/process-provider-payouts`  
**Method:** POST  
**Auth:** Service role  
**Purpose:** Process payouts to service providers

---

### 17. recurring-service-scheduler
**Path:** `/functions/v1/recurring-service-scheduler`  
**Method:** POST  
**Auth:** Service role  
**Purpose:** Schedule recurring service bookings

---

### 18. smart-home-voice
**Path:** `/functions/v1/smart-home-voice`  
**Method:** POST  
**Auth:** Required  
**Purpose:** Process voice commands for smart devices

**Request Body:**
```json
{
  "command": "turn on living room lights"
}
```

---

### 19. smart-home-maintenance
**Path:** `/functions/v1/smart-home-maintenance`  
**Method:** POST  
**Auth:** Required  
**Purpose:** Handle smart device maintenance alerts

---

### 20. delete-user
**Path:** `/functions/v1/delete-user`  
**Method:** POST  
**Auth:** Required (Admin only)  
**Purpose:** Soft delete user account

**Request Body:**
```json
{
  "user_id": "uuid"
}
```

---

### 21. signup-user *(NEW)*
**Path:** `/functions/v1/signup-user`  
**Method:** POST  
**Auth:** None (verify_jwt = false)  
**Purpose:** Self-registration with email verification required

**Request Body:**
```json
{
  "email": "user@example.com",
  "password": "securepassword",
  "name": "John Doe",
  "role": "TENANT"
}
```

**Response:**
```json
{
  "success": true,
  "user_id": "uuid",
  "message": "Account created successfully. Please verify your email."
}
```

**Business Logic:**
1. Normalize email, check for existing user
2. Validate password (min 8 chars)
3. Create auth user with `email_confirm: false`
4. `handle_new_user` trigger creates users/profiles/user_roles entries
5. Return user_id for client to initiate OTP verification

---

### 22. send-password-reset-otp *(NEW)*
**Path:** `/functions/v1/send-password-reset-otp`  
**Method:** POST  
**Auth:** None (verify_jwt = false)  
**Purpose:** Send password reset OTP via email

**Request Body:**
```json
{
  "email": "user@example.com"
}
```

**Business Logic:**
1. Check if user exists (return success even if not found — security)
2. Delete existing unexpired `password_reset` OTPs for this email
3. Generate 6-digit OTP with 5-minute expiry
4. Store in `otp_codes` table with `type = 'password_reset'`
5. Send branded HTML email via SMTP
6. Log to `email_logs`

---

### 23. validate-password-reset-otp *(NEW)*
**Path:** `/functions/v1/validate-password-reset-otp`  
**Method:** POST  
**Auth:** None (verify_jwt = false)  
**Purpose:** Validate password reset OTP (rate-limited)

**Request Body:**
```json
{
  "email": "user@example.com",
  "code": "123456"
}
```

**Business Logic:**
1. Rate limit: 5 attempts per 15-minute window per email
2. Find OTP in `otp_codes` where `type = 'password_reset'` and not verified
3. Check expiry
4. Mark as verified (`verified_at = NOW()`) but don't delete (needed for reset step)
5. Clear rate limit on success

---

### 24. reset-password-with-otp *(NEW)*
**Path:** `/functions/v1/reset-password-with-otp`  
**Method:** POST  
**Auth:** None (verify_jwt = false)  
**Purpose:** Reset password using verified OTP

**Request Body:**
```json
{
  "email": "user@example.com",
  "newPassword": "newSecurePassword"
}
```

**Business Logic:**
1. Find verified OTP (`verified_at IS NOT NULL`) for this email
2. Check `verified_at` is within 10 minutes (prevent stale tokens)
3. Update user password via `auth.admin.updateUserById`
4. Delete all `password_reset` OTPs for this email

---

### 25. schedule-work *(NEW)*
**Path:** `/functions/v1/schedule-work`  
**Method:** POST  
**Auth:** Required (verify_jwt = true)  
**Purpose:** Schedule maintenance visit for a work order

**Request Body:**
```json
{
  "issueReportId": "uuid",
  "scheduledDate": "2026-03-15T10:00:00"
}
```

**Business Logic:**
1. Fetch issue report with property/unit details
2. Update `issue_reports.scheduled_date` and set `status = 'IN_PROGRESS'`
3. Find landlord from active lease for the unit
4. Create notification for tenant ("Work Scheduled")
5. Create notification for landlord ("Maintenance work scheduled")

---

### 26. create-work-order-payment *(NEW)*
**Path:** `/functions/v1/create-work-order-payment`  
**Method:** POST  
**Auth:** Required (verify_jwt = true)  
**Purpose:** Create Stripe checkout session for work order payment

**Request Body:**
```json
{
  "workOrderId": "uuid"
}
```

**Response:**
```json
{
  "url": "https://checkout.stripe.com/...",
  "sessionId": "cs_xxx"
}
```

**Business Logic:**
1. Authenticate user, get work order details
2. Check not already paid
3. Amount = `cost_estimate` or `actual_cost`
4. Create Stripe customer if needed
5. Determine payment recipient (LANDLORD or MAINTENANCE_TEAM)
6. If maintenance team has `stripe_account_id`, use Stripe Connect transfer
7. Create checkout session and insert `work_order_payments` record

---

### 27. verify-work-order-payment *(NEW)*
**Path:** `/functions/v1/verify-work-order-payment`  
**Method:** POST  
**Auth:** None (verify_jwt = false)  
**Purpose:** Verify Stripe payment for work order (callback)

**Request Body:**
```json
{
  "workOrderId": "uuid",
  "paymentStatus": "success" | "cancelled"
}
```

**Business Logic:**
1. Get latest payment record for work order
2. If success: verify with Stripe, update payment status to COMPLETED, set `is_paid = true` on issue_reports
3. If cancelled: mark payment as CANCELLED

---

### 28. create-service-booking-payment *(NEW)*
**Path:** `/functions/v1/create-service-booking-payment`  
**Method:** POST  
**Auth:** Required (verify_jwt = true)  
**Purpose:** Create Stripe checkout for service booking

---

### 29. verify-service-payment *(NEW)*
**Path:** `/functions/v1/verify-service-payment`  
**Method:** POST  
**Auth:** None (verify_jwt = false)  
**Purpose:** Verify service booking payment

---

### 30. create-lease-payment-checkout *(NEW)*
**Path:** `/functions/v1/create-lease-payment-checkout`  
**Method:** POST  
**Auth:** Required  
**Purpose:** Create Stripe checkout session for lease/rent payment

**Request Body:**
```json
{
  "paymentId": "uuid"
}
```

**Business Logic:**
1. Get `lease_payments` where `id = paymentId`, `tenant_user_id = caller`, `status IN ('PENDING','OVERDUE')`
2. Calculate total = `amount + late_fee_amount`
3. Create Stripe Checkout Session
4. Update `lease_payments.stripe_session_id` and `payment_method = 'CARD'`
5. Return checkout URL

---

### 31. create-landlord-subscription *(NEW)*
**Path:** `/functions/v1/create-landlord-subscription`  
**Method:** POST  
**Auth:** Required (Landlord only)  
**Purpose:** Create Stripe subscription for landlord

---

### 32. manage-landlord-subscription *(NEW)*
**Path:** `/functions/v1/manage-landlord-subscription`  
**Method:** POST  
**Auth:** Required (Landlord only)  
**Purpose:** Manage (cancel, upgrade, portal) landlord subscription

---

### 33. send-announcement *(NEW)*
**Path:** `/functions/v1/send-announcement`  
**Method:** POST  
**Auth:** Required (verify_jwt = true)  
**Purpose:** Send announcement via multiple channels (in-app, email, WhatsApp)

**Request Body:**
```json
{
  "announcement_id": "uuid",
  "channels": ["in_app", "email", "whatsapp"]
}
```

---

### 34. send-bulk-sms *(NEW)*
**Path:** `/functions/v1/send-bulk-sms`  
**Method:** POST  
**Auth:** Required (verify_jwt = true)  
**Purpose:** Send SMS to multiple phone numbers via AWS SNS

---

### 35. send-login-otp *(NEW)*
**Path:** `/functions/v1/send-login-otp`  
**Method:** POST  
**Auth:** None (verify_jwt = false)  
**Purpose:** Send login OTP via SMS (AWS SNS)

---

### 36. verify-login-otp *(NEW)*
**Path:** `/functions/v1/verify-login-otp`  
**Method:** POST  
**Auth:** None (verify_jwt = false)  
**Purpose:** Verify login OTP

---

### 37. extract-mrz-data *(NEW)*
**Path:** `/functions/v1/extract-mrz-data`  
**Method:** POST  
**Auth:** Required  
**Purpose:** Extract visitor information from Emirates ID / Passport MRZ using AI vision

**Request Body:**
```json
{
  "image_base64": "data:image/jpeg;base64,...",
  "document_type": "EMIRATES_ID" | "PASSPORT"
}
```

**Response:**
```json
{
  "success": true,
  "data": {
    "full_name": "JOHN DOE",
    "id_number": "784-xxxx-xxxxxxx-x",
    "nationality": "USA",
    "date_of_birth": "1990-01-15",
    "gender": "M",
    "expiry_date": "2028-12-31",
    "document_type": "EMIRATES_ID"
  }
}
```

**Business Logic:** Uses Lovable AI Gateway (Gemini 2.5 Flash) to extract MRZ data from ID document images.

---

### 38. notify-lease-document *(NEW)*
**Path:** `/functions/v1/notify-lease-document`  
**Method:** POST  
**Auth:** Required  
**Purpose:** Send notifications for lease document lifecycle events

**Request Body:**
```json
{
  "document_id": "uuid",
  "action": "created" | "sent_for_signature" | "signed" | "reminder",
  "created_by_role": "admin" | "landlord"
}
```

**Business Logic:**
1. Fetch lease document with unit/property details
2. Determine recipients based on action and creator role
3. Create in-app notifications
4. Schedule reminder for pending signatures (3-day followup)

---

### 39. generate-payment-schedule *(NEW)*
**Path:** `/functions/v1/generate-payment-schedule`  
**Method:** POST  
**Auth:** Required  
**Purpose:** Generate payment schedule for a lease (auto-create lease_payments)

---

### 40-46. Additional Edge Functions

| # | Function | Auth | Purpose |
|---|----------|------|---------|
| 40 | `send-payment-receipt` | JWT | Email payment receipt PDF |
| 41 | `send-payment-reminder` | JWT | Email payment reminder |
| 42 | `send-unit-code-email` | JWT | Email unit access code + QR |
| 43 | `send-lease-expiry-notification` | None | Notify expiring leases |
| 44 | `send-test-email` | JWT | Send test email for SMTP verification |
| 45 | `generate-visitor-qr` | JWT | Generate QR code for visitor permit |
| 46 | `validate-visitor-qr` | None | Validate visitor QR at gate |

### 47-54. Scheduled/Background Edge Functions

| # | Function | Schedule | Purpose |
|---|----------|----------|---------|
| 47 | `auto-generate-invoices` | Daily | Generate invoices for active leases |
| 48 | `auto-process-payments` | Daily | Process auto-pay payments |
| 49 | `calculate-late-fees` | Daily | Calculate and apply late fees |
| 50 | `calculate-provider-commission` | Daily | Calculate provider commissions |
| 51 | `lease-payment-reminders` | Daily | Send cheque payment reminders at 30/15/2 days |
| 52 | `recurring-service-scheduler` | Daily | Generate recurring service bookings |
| 53 | `process-provider-payouts` | Weekly | Process payouts to service providers |
| 54 | `reset-demo-passwords` | Manual | Reset demo account passwords |

### 55-61. Additional Functions

| # | Function | Auth | Purpose |
|---|----------|------|---------|
| 55 | `incident-dispatcher` | JWT | Auto-dispatch incidents to security/landlords |
| 56 | `generate-facility-qr` | JWT | Generate QR for facility booking |
| 57 | `send-whatsapp` | JWT | Send WhatsApp messages |
| 58 | `whatsapp-webhook` | None | Handle WhatsApp incoming messages |
| 59 | `smart-home-maintenance` | JWT | Smart home device maintenance alerts |
| 60 | `smart-home-voice` | JWT | Voice command processing for smart devices |
| 61 | `reset-demo-password` | None | Single demo password reset |

### AWS Lambda Functions (8)

| # | Function | Purpose |
|---|----------|---------|
| 1 | `verify-service-payment` | Verify service booking payment (RDS) |
| 2 | `verify-bank-payment` | Verify bank transfer payment (RDS) |
| 3 | `verify-login-otp` | Verify login OTP (RDS + Cognito) |
| 4 | `send-login-otp` | Send login OTP via AWS SNS |
| 5 | `reset-demo-passwords` | Reset demo passwords (RDS + Cognito) |
| 6 | `delete-user` | Delete user (RDS + Cognito) |
| 7 | `generate-invoice` | Generate invoice data (RDS) |
| 8 | `send-bulk-sms` | Send bulk SMS via AWS SNS |

---

## Authentication & Authorization

### User Registration Flow

```
1. User submits registration form
   ↓
2. Frontend calls Supabase Auth signup
   ↓
3. Auth trigger: handle_new_user() executes
   ↓
4. Creates entries in:
   - users table
   - profiles table
   - user_roles table
   ↓
5. Email verification sent
   ↓
6. User verifies email
   ↓
7. User can log in
```

### Login Flow

```
1. User submits credentials
   ↓
2. Supabase Auth validates
   ↓
3. JWT token issued with:
   - user_id
   - email
   - role (from user_metadata)
   ↓
4. Frontend stores token
   ↓
5. Subsequent requests include token
   ↓
6. RLS policies validate access
```

### JWT Token Structure

```json
{
  "sub": "user-uuid",
  "email": "user@example.com",
  "role": "authenticated",
  "user_metadata": {
    "role": "TENANT",
    "name": "John Doe"
  },
  "aud": "authenticated",
  "exp": 1234567890
}
```

### Role Hierarchy

```
PLATFORM_ADMIN (highest)
  ├── LANDLORD
  │   ├── LANDLORD_SUB_USER
  │   ├── MAINTENANCE (hired by landlord)
  │   │   └── MAINTENANCE_SUB_USER
  │   └── SECURITY (hired by landlord)
  │       └── SECURITY_SUB_USER
  ├── TENANT
  └── PROVIDER
      └── SERVICE_PROVIDER_SUB_USER
```

### Permission System

#### Maintenance Sub-User Permissions
```json
{
  "view_issues": true,
  "assign_tools": false,
  "view_inventory": true,
  "update_work_orders": true,
  "complete_work_orders": false
}
```

#### Security Sub-User Permissions
```json
{
  "check_in_visitors": true,
  "view_permits": true,
  "manage_blacklist": false,
  "manage_rounds": false,
  "view_incidents": true,
  "create_incidents": true,
  "access_control": false,
  "emergency_response": false
}
```

#### Landlord Sub-User Permissions
```json
{
  "manage_properties": true,
  "manage_tenants": true,
  "view_financials": false,
  "approve_expenses": false,
  "manage_leases": true,
  "manage_maintenance": true
}
```

#### Service Provider Sub-User Permissions
```json
{
  "view_bookings": true,
  "manage_bookings": false,
  "view_services": true,
  "manage_services": false,
  "view_analytics": false
}
```

---

## Storage Buckets

### 1. property-documents
**Public:** No  
**Purpose:** Store confidential property documents

**RLS Policies:**
```sql
-- Landlords can upload to their properties
CREATE POLICY "Landlords can upload"
ON storage.objects FOR INSERT
TO authenticated
WITH CHECK (
  bucket_id = 'property-documents' AND
  (storage.foldername(name))[1] IN (
    SELECT id::text FROM properties WHERE landlord_id = auth.uid()
  )
);

-- Landlords can view their documents
CREATE POLICY "Landlords can view"
ON storage.objects FOR SELECT
TO authenticated
USING (
  bucket_id = 'property-documents' AND
  (storage.foldername(name))[1] IN (
    SELECT id::text FROM properties WHERE landlord_id = auth.uid()
  )
);

-- Tenants can view shared documents
CREATE POLICY "Tenants can view shared"
ON storage.objects FOR SELECT
TO authenticated
USING (
  bucket_id = 'property-documents' AND
  EXISTS (
    SELECT 1 FROM documents d
    WHERE d.file_path = name
    AND d.tenant_user_id = auth.uid()
    AND d.is_shared_with_tenant = TRUE
  )
);
```

### 2. service-images
**Public:** Yes  
**Purpose:** Store service listing images

**File Structure:**
```
service-images/
  ├── {provider_id}/
  │   ├── {service_id}/
  │   │   ├── image1.jpg
  │   │   ├── image2.jpg
```

---

## Business Logic & Workflows

### 1. Lease Creation Workflow

```
1. Landlord selects tenant and unit
   ↓
2. System auto-fills:
   - Tenant info (from tenant_profiles)
   - Unit info (from units)
   - Default rent amount
   ↓
3. Landlord sets:
   - Start/end dates
   - Rent frequency
   - Security deposit
   - Maintenance responsibility
   ↓
4. Lease created with status: DRAFT
   ↓
5. Landlord activates lease
   ↓
6. Status changes to: ACTIVE
   ↓
7. Trigger: Generate first invoice
   ↓
8. Notification sent to tenant
```

### 2. Work Order Lifecycle

```
1. Tenant creates issue report
   ↓
2. Trigger: set_work_order_pricing()
   - Checks lease.maintenance_responsibility
   - Sets is_paid flag
   ↓
3. Trigger: set_work_order_sla()
   - Calculates SLA based on priority
   - Sets approval_status if cost > threshold
   ↓
4. Status: NEW
   ↓
5. Landlord/System assigns to maintenance team
   - Smart assignment based on:
     * Current workload
     * Specialty match
     * Rating
     * Completion time
   ↓
6. Status: ASSIGNED
   ↓
7. Maintenance team accepts
   ↓
8. Status: IN_PROGRESS
   ↓
9. Maintenance team completes
   ↓
10. If approval required:
    Status: PENDING_APPROVAL
    Landlord reviews and approves
    ↓
11. Status: COMPLETED
    ↓
12. Tenant rates and reviews
```

### 3. Payment Processing Flow

```
1. Invoice generated (auto or manual)
   ↓
2. Status: OPEN
   ↓
3. Tenant initiates payment
   ↓
4. If Stripe:
   - create-payment-intent called
   - Payment processed
   - Webhook updates status
   ↓
5. If Bank Transfer:
   - Tenant uploads proof
   - Status: PENDING
   - Landlord verifies
   - Status: PAID
   ↓
6. Payment record created
   ↓
7. Invoice status: PAID
   ↓
8. Notification sent
```

### 4. Service Booking Flow

```
1. Tenant browses service listings
   ↓
2. Tenant selects service
   ↓
3. Chooses booking type:
   - One-time
   - Preset recurring
   - Custom recurring
   ↓
4. Booking created with status: PENDING
   ↓
5. Provider receives notification
   ↓
6. Provider accepts
   ↓
7. Status: CONFIRMED
   ↓
8. Service provided
   ↓
9. Provider marks as completed
   ↓
10. Status: COMPLETED
    ↓
11. Payment processed to provider
    ↓
12. Tenant can rate and review
```

### 5. Visitor Management Flow

```
1. Tenant creates visitor permit
   ↓
2. QR code generated
   ↓
3. Status: PENDING
   ↓
4. Visitor arrives at property
   ↓
5. Security scans QR code
   ↓
6. System validates:
   - Permit is active
   - Within valid date/time
   - Visitor not on blacklist
   ↓
7. Security checks in visitor
   ↓
8. Status: CHECKED_IN
   ↓
9. Visitor leaves
   ↓
10. Security checks out visitor
    ↓
11. Status: CHECKED_OUT
```

### 6. Invoice Generation (Automated)

```
Cron Job (Daily at 1 AM)
   ↓
1. auto-generate-invoices function runs
   ↓
2. Query active leases where:
   - Next invoice date <= today
   - Status = ACTIVE
   ↓
3. For each lease:
   - Calculate amount based on rent_frequency
   - Generate invoice number
   - Set due_date
   - Create invoice record
   ↓
4. Trigger: notify_invoice_created()
   - Send notification to tenant
   - Send notification to landlord
   ↓
5. Update lease.next_invoice_date
```

---

## Additional Database Tables (Added Since v1.0)

### Lease Payment System

#### lease_payments
```sql
CREATE TABLE public.lease_payments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    lease_id UUID REFERENCES leases(id),
    tenant_user_id UUID REFERENCES auth.users(id),
    landlord_id UUID REFERENCES auth.users(id),
    property_id UUID REFERENCES properties(id),
    unit_id UUID REFERENCES units(id),
    amount DECIMAL(10,2) NOT NULL,
    currency TEXT DEFAULT 'AED',
    payment_method payment_method NOT NULL,
    status lease_payment_status DEFAULT 'PENDING',
    due_date DATE NOT NULL,
    payment_number INTEGER,
    total_payments INTEGER,
    cheque_number TEXT,
    cheque_date DATE,
    cheque_image_url TEXT,
    bank_name TEXT,
    transfer_reference TEXT,
    transfer_proof_url TEXT,
    late_fee_amount DECIMAL(10,2) DEFAULT 0,
    grace_period_days INTEGER DEFAULT 5,
    validated_by UUID,
    validated_at TIMESTAMPTZ,
    rejection_reason TEXT,
    stripe_session_id TEXT,
    stripe_payment_intent_id TEXT,
    paid_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TYPE lease_payment_status AS ENUM ('PENDING', 'SUBMITTED', 'VALIDATED', 'PRESENTED', 'CLEARED', 'BOUNCED', 'REPLACED', 'OVERDUE', 'COMPLETED');
CREATE TYPE payment_method AS ENUM ('CHECK', 'BANK_TRANSFER', 'CASH', 'CREDIT_CARD', 'ONLINE', 'CARD', 'BANK');
```

#### payment_receipts
```sql
CREATE TABLE public.payment_receipts (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    lease_payment_id UUID REFERENCES lease_payments(id),
    receipt_number TEXT UNIQUE,
    generated_at TIMESTAMPTZ DEFAULT NOW(),
    pdf_url TEXT,
    created_at TIMESTAMPTZ DEFAULT NOW()
);
```

#### lease_payment_history
```sql
CREATE TABLE public.lease_payment_history (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    lease_payment_id UUID REFERENCES lease_payments(id),
    old_status TEXT,
    new_status TEXT,
    changed_by UUID,
    created_at TIMESTAMPTZ DEFAULT NOW()
);
```

### Work Order Payments & Quotes

#### work_order_payments
```sql
CREATE TABLE public.work_order_payments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    work_order_id UUID REFERENCES issue_reports(id),
    tenant_id UUID REFERENCES auth.users(id),
    amount DECIMAL(10,2),
    currency TEXT DEFAULT 'AED',
    payment_method TEXT,
    status TEXT DEFAULT 'PENDING',
    stripe_session_id TEXT,
    stripe_payment_intent_id TEXT,
    payment_recipient TEXT DEFAULT 'LANDLORD',
    maintenance_team_id UUID,
    paid_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ DEFAULT NOW()
);
```

#### work_order_quotes
```sql
CREATE TABLE public.work_order_quotes (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    work_order_id UUID REFERENCES issue_reports(id),
    provider_id UUID,
    amount DECIMAL(10,2),
    description TEXT,
    valid_until DATE,
    status TEXT DEFAULT 'PENDING',
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);
```

### Facility Management

#### facilities
```sql
CREATE TABLE public.facilities (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    property_id UUID REFERENCES properties(id),
    name TEXT NOT NULL,
    facility_type facility_type NOT NULL,
    description TEXT,
    capacity INTEGER,
    hourly_rate DECIMAL(10,2),
    deposit_amount DECIMAL(10,2),
    requires_approval BOOLEAN DEFAULT FALSE,
    simultaneous_bookings_limit INTEGER DEFAULT 1,
    min_booking_hours INTEGER DEFAULT 1,
    max_booking_hours INTEGER DEFAULT 4,
    advance_booking_days INTEGER DEFAULT 30,
    cancellation_hours INTEGER DEFAULT 24,
    rules TEXT,
    images JSONB,
    amenities JSONB,
    active BOOLEAN DEFAULT TRUE,
    created_at TIMESTAMPTZ DEFAULT NOW(),
    updated_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TYPE facility_type AS ENUM (
    'GYM', 'SWIMMING_POOL', 'MEETING_ROOM', 'PARTY_HALL',
    'BBQ_AREA', 'TENNIS_COURT', 'BASKETBALL_COURT', 'PLAYGROUND',
    'PARKING', 'STORAGE', 'ROOFTOP', 'GARDEN', 'SAUNA',
    'LIBRARY', 'CO_WORKING', 'CINEMA', 'GAMES_ROOM', 'OTHER'
);
```

#### facility_bookings
```sql
CREATE TABLE public.facility_bookings (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    facility_id UUID REFERENCES facilities(id),
    booked_by UUID REFERENCES auth.users(id),
    booking_date DATE NOT NULL,
    start_time TIME NOT NULL,
    end_time TIME NOT NULL,
    status facility_booking_status DEFAULT 'PENDING',
    guests INTEGER,
    purpose TEXT,
    total_amount DECIMAL(10,2),
    qr_code_data TEXT,
    checked_in_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ DEFAULT NOW()
);

CREATE TYPE facility_booking_status AS ENUM ('PENDING', 'APPROVED', 'REJECTED', 'CHECKED_IN', 'COMPLETED', 'CANCELLED', 'NO_SHOW');
```

### Community & Messaging

#### community_channels
```sql
CREATE TABLE public.community_channels (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    property_id UUID REFERENCES properties(id),
    name TEXT NOT NULL,
    description TEXT,
    channel_type channel_type DEFAULT 'GENERAL',
    is_moderated BOOLEAN DEFAULT FALSE,
    is_active BOOLEAN DEFAULT TRUE,
    created_by UUID REFERENCES auth.users(id),
    created_at TIMESTAMPTZ DEFAULT NOW()
);
CREATE TYPE channel_type AS ENUM ('GENERAL', 'ANNOUNCEMENTS', 'MARKETPLACE', 'EVENTS', 'SUPPORT');
```

#### channel_posts, post_replies, post_likes, direct_messages, content_reports
*(Full schemas in Supabase types file)*

### Tenant Management

#### tenant_profiles
```sql
CREATE TABLE public.tenant_profiles (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID UNIQUE REFERENCES auth.users(id),
    national_id TEXT,
    emirates_id TEXT,
    dob DATE,
    nationality TEXT,
    emergency_contact_name TEXT,
    emergency_contact_phone TEXT,
    employer_name TEXT,
    monthly_income DECIMAL(10,2),
    created_at TIMESTAMPTZ DEFAULT NOW()
);
```

#### tenant_apartment_assignments
```sql
CREATE TABLE public.tenant_apartment_assignments (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    tenant_user_id UUID REFERENCES auth.users(id),
    property_id UUID REFERENCES properties(id),
    unit_id UUID REFERENCES units(id),
    status TEXT DEFAULT 'PENDING',
    created_at TIMESTAMPTZ DEFAULT NOW()
);
```

### OTP & Authentication

#### otp_codes
```sql
CREATE TABLE public.otp_codes (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID REFERENCES auth.users(id),
    email TEXT,
    code TEXT NOT NULL,
    type TEXT NOT NULL, -- 'login_verification', 'email_verification', 'password_reset'
    expires_at TIMESTAMPTZ NOT NULL,
    verified_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ DEFAULT NOW()
);
```

#### login_otps (for AWS Lambda SMS-based OTP)
```sql
CREATE TABLE public.login_otps (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    identifier TEXT UNIQUE NOT NULL,
    otp TEXT NOT NULL,
    expires_at TIMESTAMPTZ NOT NULL,
    attempts INTEGER DEFAULT 0,
    created_at TIMESTAMPTZ DEFAULT NOW()
);
```

### Lease Documents & Ejari

#### lease_documents
```sql
CREATE TABLE public.lease_documents (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    lease_id UUID REFERENCES leases(id),
    tenant_id UUID REFERENCES auth.users(id),
    landlord_id UUID REFERENCES auth.users(id),
    document_type TEXT,
    status TEXT DEFAULT 'DRAFT',
    html_content TEXT,
    tenant_signature TEXT,
    landlord_signature TEXT,
    tenant_signed_at TIMESTAMPTZ,
    landlord_signed_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ DEFAULT NOW()
);
```

### Inspections

#### inspections, inspection_items, inspection_media, inspection_rooms, inspection_signatures, inspection_templates, inspector_assignments
*(Full schemas available in Supabase types file — 7 related tables for property inspection lifecycle)*

### Subscriptions

#### landlord_subscriptions, subscription_configs, tenant_subscriptions, tenant_subscription_plans, plans
*(5 tables managing Stripe subscription lifecycle for landlords and tenants)*

### Insurance & Utilities

#### insurance_policies, insurance_claims, utility_meters, utility_readings, utility_bills
*(Supporting tables for property insurance and utility tracking)*

### Move Workflows

#### move_workflows, move_checklist_items, move_checklist_templates
*(Move-in/move-out workflow tracking with configurable checklists)*

### Occupancy History & Termination

#### unit_occupancy_history
```sql
CREATE TABLE public.unit_occupancy_history (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    unit_id UUID REFERENCES units(id),
    tenant_user_id UUID REFERENCES auth.users(id),
    lease_id UUID REFERENCES leases(id),
    move_in_date DATE,
    move_out_date DATE,
    rent_amount DECIMAL(10,2),
    termination_reason TEXT,
    tenant_rating INTEGER,
    landlord_rating INTEGER,
    created_at TIMESTAMPTZ DEFAULT NOW()
);
```

#### termination_requests
```sql
CREATE TABLE public.termination_requests (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    lease_id UUID REFERENCES leases(id),
    requested_by UUID REFERENCES auth.users(id),
    requested_by_role TEXT,
    reason TEXT,
    requested_end_date DATE,
    early_termination_fee DECIMAL(10,2),
    deposit_deduction DECIMAL(10,2),
    status TEXT DEFAULT 'PENDING',
    approved_by UUID,
    approved_at TIMESTAMPTZ,
    created_at TIMESTAMPTZ DEFAULT NOW()
);
```

---

### Password Reset Flow *(NEW)*

```
1. User clicks "Forgot Password"
   ↓
2. Frontend calls send-password-reset-otp with email
   ↓
3. 6-digit OTP stored in otp_codes (type: password_reset, 5min expiry)
   ↓
4. Branded HTML email sent via SMTP
   ↓
5. User enters OTP → validate-password-reset-otp
   - Rate limited (5 attempts / 15 min)
   - Marks OTP as verified
   ↓
6. User enters new password → reset-password-with-otp
   - Checks verified_at within 10 min
   - Updates password via auth.admin.updateUserById
   - Cleans up all password_reset OTPs
```

### User Signup Flow *(NEW)*

```
1. User submits signup form (name, email, password)
   ↓
2. Frontend calls signup-user edge function
   ↓
3. Auth user created with email_confirm: false
   ↓
4. handle_new_user trigger creates:
   - users table entry
   - profiles entry
   - user_roles entry (default: TENANT)
   ↓
5. Frontend sends verification email (send-verification-email)
   ↓
6. User verifies OTP → verify-email
   ↓
7. User can now login
```

### Configurable RBAC Flow *(NEW)*

```
1. Admin navigates to Roles & Permissions page
   ↓
2. System loads permissions via get_user_permissions(user_id)
   ↓
3. Navigation sidebar dynamically filtered by is_navigation permissions
   ↓
4. Admin can:
   - Create/edit roles with parent_role_id for inheritance
   - Grant/revoke permissions per role
   - All changes logged to permission_audit_logs
   ↓
5. PLATFORM_ADMIN auto-receives all permissions via trigger
   ↓
6. Frontend PermissionContext caches permissions in Set<string> for O(1) lookups
   ↓
7. ProtectedRoute uses requiredPermission instead of allowedRoles
```

---

### .NET Core Microservices Architecture

#### Recommended Services

1. **Identity Service**
   - ASP.NET Core Identity
   - JWT token generation
   - Role management
   - Email verification

2. **Property Service**
   - Properties CRUD
   - Units management
   - Property assignments

3. **Tenant Service**
   - Tenant profiles
   - Lease management
   - Unit assignments

4. **Finance Service**
   - Invoice generation
   - Payment processing
   - Stripe integration
   - Payment verification

5. **Maintenance Service**
   - Work orders
   - Maintenance teams
   - Assignment algorithm
   - SLA management

6. **Security Service**
   - Visitor management
   - Incidents
   - Access control
   - Blacklist

7. **Provider Service**
   - Service listings
   - Bookings
   - Provider profiles
   - Payouts

8. **Notification Service**
   - Email sending
   - Push notifications
   - SMS (optional)
   - Notification preferences

9. **Document Service**
   - File upload/download
   - Document metadata
   - Access control
   - Versioning

10. **Smart Home Service**
    - Device management
    - Voice commands
    - Automation
    - Analytics

#### Database Strategy

**Option 1: Single Database with Service-Specific Schemas**
```sql
-- Each service has its own schema
CREATE SCHEMA identity;
CREATE SCHEMA property;
CREATE SCHEMA finance;
-- etc.
```

**Option 2: Database Per Service**
- Separate database for each microservice
- Use distributed transactions (Saga pattern)
- Event-driven communication

#### API Gateway

Use Ocelot or YARP for:
- Request routing
- Authentication
- Rate limiting
- Load balancing

#### Authentication

**Implementation:**
```csharp
// Startup.cs
services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = Configuration["Jwt:Issuer"],
            ValidAudience = Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(Configuration["Jwt:Key"]))
        };
    });

services.AddAuthorization(options =>
{
    options.AddPolicy("AdminOnly", policy => 
        policy.RequireRole("PLATFORM_ADMIN"));
    options.AddPolicy("LandlordOnly", policy => 
        policy.RequireRole("LANDLORD"));
    // etc.
});
```

#### Authorization with RLS-like Behavior

```csharp
// Custom authorization handler
public class PropertyOwnerHandler : AuthorizationHandler<PropertyOwnerRequirement>
{
    private readonly IPropertyRepository _propertyRepo;

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PropertyOwnerRequirement requirement)
    {
        var userId = context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var propertyId = context.Resource as string;

        if (await _propertyRepo.IsOwner(userId, propertyId))
        {
            context.Succeed(requirement);
        }
    }
}

// Usage in controller
[HttpGet("{id}")]
[Authorize(Policy = "PropertyOwner")]
public async Task<IActionResult> GetProperty(string id)
{
    // Implementation
}
```

#### Event-Driven Communication

**Using MassTransit/RabbitMQ:**

```csharp
// Event definition
public record InvoiceCreatedEvent
{
    public Guid InvoiceId { get; init; }
    public Guid TenantId { get; init; }
    public decimal Amount { get; init; }
    public DateTime DueDate { get; init; }
}

// Publisher (Finance Service)
public class InvoiceService
{
    private readonly IPublishEndpoint _publishEndpoint;

    public async Task CreateInvoice(Invoice invoice)
    {
        // Save to database
        await _repository.SaveAsync(invoice);

        // Publish event
        await _publishEndpoint.Publish(new InvoiceCreatedEvent
        {
            InvoiceId = invoice.Id,
            TenantId = invoice.TenantId,
            Amount = invoice.Amount,
            DueDate = invoice.DueDate
        });
    }
}

// Consumer (Notification Service)
public class InvoiceCreatedConsumer : IConsumer<InvoiceCreatedEvent>
{
    private readonly IEmailService _emailService;

    public async Task Consume(ConsumeContext<InvoiceCreatedEvent> context)
    {
        var tenant = await _tenantRepo.GetAsync(context.Message.TenantId);
        await _emailService.SendInvoiceEmail(tenant.Email, context.Message);
    }
}
```

#### Background Jobs

**Using Hangfire:**

```csharp
// Startup.cs
services.AddHangfire(config =>
    config.UseSqlServerStorage(Configuration.GetConnectionString("Default")));
services.AddHangfireServer();

// Schedule recurring job
RecurringJob.AddOrUpdate<InvoiceGenerator>(
    "generate-monthly-invoices",
    x => x.GenerateInvoices(),
    Cron.Daily(1)); // 1 AM daily

// Job implementation
public class InvoiceGenerator
{
    private readonly ILeaseRepository _leaseRepo;
    private readonly IInvoiceRepository _invoiceRepo;

    public async Task GenerateInvoices()
    {
        var leases = await _leaseRepo.GetDueForInvoicing();
        
        foreach (var lease in leases)
        {
            var invoice = new Invoice
            {
                LeaseId = lease.Id,
                Amount = lease.RentAmount,
                DueDate = CalculateDueDate(lease)
            };
            
            await _invoiceRepo.CreateAsync(invoice);
        }
    }
}
```

#### Data Migration

**Steps:**
1. Export existing Supabase data to SQL scripts
2. Transform data to match new schema (if needed)
3. Import to PostgreSQL/SQL Server
4. Verify data integrity
5. Run migration tests

**Tool Recommendation:** Entity Framework Core Migrations

```bash
dotnet ef migrations add InitialCreate
dotnet ef database update
```

---

## API Documentation Format

For each microservice, create OpenAPI/Swagger documentation:

```csharp
// Startup.cs
services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Property Service API",
        Version = "v1",
        Description = "Manages properties and units"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });
});
```

---

## Testing Strategy

### Unit Tests
```csharp
[Fact]
public async Task CreateInvoice_ValidData_ReturnsSuccess()
{
    // Arrange
    var service = new InvoiceService(_mockRepo.Object);
    var invoice = new Invoice { /* ... */ };

    // Act
    var result = await service.CreateAsync(invoice);

    // Assert
    Assert.True(result.IsSuccess);
}
```

### Integration Tests
```csharp
public class InvoiceApiTests : IClassFixture<WebApplicationFactory<Startup>>
{
    [Fact]
    public async Task POST_Invoice_ReturnsCreated()
    {
        var response = await _client.PostAsJsonAsync("/api/invoices", invoice);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}
```

---

## Security Checklist

- [ ] Implement JWT authentication
- [ ] Add role-based authorization
- [ ] Validate all inputs
- [ ] Use parameterized queries
- [ ] Implement rate limiting
- [ ] Add CORS policies
- [ ] Enable HTTPS only
- [ ] Implement audit logging
- [ ] Use secrets management (Azure Key Vault/AWS Secrets Manager)
- [ ] Add request validation middleware
- [ ] Implement SQL injection protection
- [ ] Add XSS protection headers
- [ ] Enable CSRF protection

---

## Monitoring & Logging

**Recommended Tools:**
- **Application Insights** (Azure)
- **Seq** (structured logging)
- **Serilog** (logging framework)

```csharp
// Program.cs
builder.Host.UseSerilog((context, config) =>
{
    config
        .WriteTo.Console()
        .WriteTo.Seq("http://localhost:5341")
        .WriteTo.ApplicationInsights(
            context.Configuration["ApplicationInsights:InstrumentationKey"],
            TelemetryConverter.Traces);
});
```

---

## Performance Considerations

1. **Caching Strategy**
   - Redis for distributed caching
   - Cache frequently accessed data (properties, roles, etc.)

2. **Database Optimization**
   - Add indexes on foreign keys
   - Use query optimization
   - Implement pagination

3. **Load Balancing**
   - Use Azure Load Balancer / AWS ELB
   - Horizontal scaling

4. **CDN**
   - Use CloudFlare / Azure CDN for static assets

---

## Deployment Strategy

**Containerization with Docker:**

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["PropertyService.csproj", "./"]
RUN dotnet restore
COPY . .
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PropertyService.dll"]
```

**Kubernetes Deployment:**

```yaml
apiVersion: apps/v1
kind: Deployment
metadata:
  name: property-service
spec:
  replicas: 3
  selector:
    matchLabels:
      app: property-service
  template:
    metadata:
      labels:
        app: property-service
    spec:
      containers:
      - name: property-service
        image: rentolic/property-service:latest
        ports:
        - containerPort: 80
        env:
        - name: ConnectionStrings__Default
          valueFrom:
            secretKeyRef:
              name: db-secret
              key: connection-string
```

---

## Conclusion

This document provides a complete overview of the Rentolic backend architecture. Use this as a reference for implementing the .NET Core microservices version while maintaining feature parity and security standards.

**Key Migration Steps:**
1. Set up microservices architecture
2. Migrate database schema
3. Implement authentication & authorization
4. Port business logic to services
5. Set up event-driven communication
6. Implement background jobs
7. Add monitoring & logging
8. Deploy to cloud infrastructure
9. Run comprehensive testing
10. Gradual rollout with feature flags

**Contact:** For questions or clarifications, refer to the original Supabase implementation or consult with the frontend team.

---

**Document Version:** 1.0  
**Last Updated:** 2025  
**Maintained By:** Rentolic Development Team
