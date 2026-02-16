-- =====================================================
-- E-commerce Database Schema (PostgreSQL)
-- -----------------------------------------------------
-- Purpose:
--   - Store customer, product, and order data
--   - Support multi-item orders
--   - Enable analytics by country and customer behavior
--
-- Design notes:
--   - Normalized schema (3NF)
--   - Snapshot pricing stored at order time
--   - ISO country codes for consistency
-- =====================================================
/*
INTERVIEW NOTES — Why this schema

Relationships:
- customers -> orders is 1-to-many (customer can have 0..N orders)
- orders -> order_items is 1-to-many
- products linked via order_items => supports orders with multiple products (M:N)

Design choices:
- countries reference table ensures consistent country codes for analytics
- orders.shipping_country stored separately from customers.country_code
  (preserves historical accuracy if customer moves later)
- order_items stores unit_price snapshot at purchase time
  (price changes later won't rewrite history)

Follow-up answers:
- Indexing: focus on join keys + common filters (customer_id, shipping_country, order_id)
- Migrations: in production use versioned migrations (expand/contract for breaking changes)
- Scaling: partition large fact tables (orders/order_items), caching/reporting tables for aggregates

Questions:
- Tables with foreign key (references x)
- Store unit_price in order_items rather than just reference product price
- orders.shipping_country is intentionally separate because of snapshot pricing
- CHECK constraints ensure no bad inputs
- 3NF -> Normalized to eliminate data redundancy and ensure data integrity. For example,
  countries is a reference table so we don't have 'USA', 'US', 'United States' inconsistencies
- Tradeoff is normalized schemas require more JOINs for queries but I mitigated this with proper indexing on foreign keys
  I'd consider denormilization for specific reporting tables
- Store unit_price snapshot in order_items to preserve historical accuracy, so if prices change, order placed will reflect what they paid
  I could reference products.unit_price but that would rewrite history when prices change
- shipping_country separate from customers.country_code in case someone buys a gift from another country.
  Also if they move, we preserve historical orders that were actually shipped. Plus we need to know cost for shipping/tax purposes
- CHAR instead of VARCHAR in country codes to keep them consistent at 2 characters
*/

BEGIN; -- Start of transaction

DROP TABLE IF EXISTS order_items;
DROP TABLE IF EXISTS orders;
DROP TABLE IF EXISTS customer_addresses;
DROP TABLE IF EXISTS products;
DROP TABLE IF EXISTS customers;
DROP TABLE IF EXISTS countries;

-- Reference table for country data to avoid inconsistent country names
-- and enable reliable geographic reporting
CREATE TABLE countries (
  country_code CHAR(2) PRIMARY KEY,                 
  country_name VARCHAR(100) NOT NULL UNIQUE
);

-- Customers can exist without orders (0..N relationship)
-- Country stored as FK for fast filtering and reporting
CREATE TABLE customers (
  customer_id      BIGSERIAL PRIMARY KEY, -- 8 byte auto-incrementing integer. Designed for automatic, unique primary keys
  email            VARCHAR(320) NOT NULL UNIQUE,
  first_name       VARCHAR(100) NOT NULL,
  last_name        VARCHAR(100) NOT NULL,
  phone            VARCHAR(30),
  country_code     CHAR(2) NOT NULL REFERENCES countries(country_code),
  created_at       TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  status           VARCHAR(20) NOT NULL DEFAULT 'active'
                  CHECK (status IN ('active', 'inactive', 'banned'))
);

CREATE TABLE customer_addresses (
  address_id     BIGSERIAL PRIMARY KEY,
  customer_id    BIGINT NOT NULL REFERENCES customers(customer_id) ON DELETE CASCADE, -- Auto deletes when corresponding parent row is deleted
  address_type   VARCHAR(20) NOT NULL CHECK (address_type IN ('shipping', 'billing')),
  line1          VARCHAR(200) NOT NULL,
  line2          VARCHAR(200),
  city           VARCHAR(100) NOT NULL,
  region         VARCHAR(100),
  postal_code    VARCHAR(30),
  country_code   CHAR(2) NOT NULL REFERENCES countries(country_code),
  is_default     BOOLEAN NOT NULL DEFAULT FALSE, -- Is primary address
  created_at     TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

CREATE TABLE products (
  product_id     BIGSERIAL PRIMARY KEY,
  sku            VARCHAR(64) NOT NULL UNIQUE,
  name           VARCHAR(200) NOT NULL,
  description    TEXT,
  currency_code  CHAR(3) NOT NULL,                   
  unit_price     NUMERIC(12,2) NOT NULL CHECK (unit_price >= 0),
  is_active      BOOLEAN NOT NULL DEFAULT TRUE,
  created_at     TIMESTAMPTZ NOT NULL DEFAULT NOW()
);

-- Orders store shipping_country separately from customer country
-- to preserve historical accuracy if a customer later moves
CREATE TABLE orders (
  order_id          BIGSERIAL PRIMARY KEY,
  customer_id       BIGINT NOT NULL REFERENCES customers(customer_id),
  order_status      VARCHAR(20) NOT NULL
                    CHECK (order_status IN ('pending', 'paid', 'shipped', 'cancelled', 'refunded')),
  order_currency    CHAR(3) NOT NULL,                -- currency used for this order
  shipping_country  CHAR(2) NOT NULL REFERENCES countries(country_code),
  created_at        TIMESTAMPTZ NOT NULL DEFAULT NOW(),
  paid_at           TIMESTAMPTZ
);

CREATE TABLE order_items (
  order_id      BIGINT NOT NULL REFERENCES orders(order_id) ON DELETE CASCADE,
  product_id    BIGINT NOT NULL REFERENCES products(product_id),
  quantity      INTEGER NOT NULL CHECK (quantity > 0),
  unit_price    NUMERIC(12,2) NOT NULL CHECK (unit_price >= 0),
  currency_code CHAR(3) NOT NULL,
  PRIMARY KEY (order_id, product_id),
  CONSTRAINT fk_line_currency_matches_order
    CHECK (currency_code IN ('CAD', 'USD', 'EUR', 'GBP', 'JPY')) -- constraint that enforces a whitelist of supported currency codes.
);

COMMIT;
