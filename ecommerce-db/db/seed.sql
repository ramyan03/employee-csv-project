-- =====================================================
-- Seed Data
-- -----------------------------------------------------
-- Purpose:
--   - Populate the database with realistic test data
--   - Validate schema constraints
--   - Enable verification of business queries:
--       1) Customers outside Canada
--       2) Revenue per shipping country
--       3) Customers with no orders
-- =====================================================
/*
Seed Data

Why seed exists:
- Demonstrates realistic scenarios for testing queries and constraints
- Ensures all business questions can be verified:
  1) customers outside Canada (non-CA rows exist)
  2) revenue per shipping country (orders + items exist across countries)
  3) customers with no orders (at least one customer without orders exists)
*/

BEGIN;

-- Countries
INSERT INTO countries (country_code, country_name) VALUES
  ('CA', 'Canada'),
  ('US', 'United States'),
  ('GB', 'United Kingdom'),
  ('FR', 'France'),
  ('IN', 'India'),
  ('JP', 'Japan')
ON CONFLICT (country_code) DO NOTHING;

INSERT INTO customers (email, first_name, last_name, phone, country_code, status) VALUES
  ('alice.ca@example.com', 'Alice', 'Martin',  '416-555-0101', 'CA', 'active'),
  ('bob.us@example.com',   'Bob',   'Turner',  '212-555-0102', 'US', 'active'),
  ('chloe.gb@example.com', 'Chloe', 'Singh',   NULL,          'GB', 'active'),
  ('dan.fr@example.com',   'Dan',   'Dupont',  NULL,          'FR', 'active'),
  ('eric.in@example.com',  'Eric',  'Kumar',   NULL,          'IN', 'active'),
  ('noorders.jp@example.com','Nina','Sato',    NULL,          'JP', 'active');

-- Customer Addresses
INSERT INTO customer_addresses
  (customer_id, address_type, line1, city, region, postal_code, country_code, is_default)
SELECT customer_id, 'shipping', '123 Maple St', 'Toronto', 'ON', 'M5V 2T6', 'CA', TRUE
FROM customers WHERE email = 'alice.ca@example.com';

INSERT INTO customer_addresses
  (customer_id, address_type, line1, city, region, postal_code, country_code, is_default)
SELECT customer_id, 'shipping', '55 Broadway', 'New York', 'NY', '10006', 'US', TRUE
FROM customers WHERE email = 'bob.us@example.com';

INSERT INTO customer_addresses
  (customer_id, address_type, line1, city, region, postal_code, country_code, is_default)
SELECT customer_id, 'shipping', '10 Downing St', 'London', NULL, 'SW1A 2AA', 'GB', TRUE
FROM customers WHERE email = 'chloe.gb@example.com';

-- Products
INSERT INTO products (sku, name, description, currency_code, unit_price, is_active) VALUES
  ('SKU-TSHIRT-001', 'T-Shirt', 'Cotton t-shirt', 'CAD', 25.00, TRUE),
  ('SKU-MUG-001',    'Mug',    'Ceramic mug',    'CAD', 12.50, TRUE),
  ('SKU-HOODIE-001', 'Hoodie', 'Pullover hoodie','USD', 45.00, TRUE),
  ('SKU-CAP-001',    'Cap',    'Baseball cap',   'USD', 18.00, TRUE),
  ('SKU-BAG-001',    'Bag',    'Canvas tote',    'USD', 22.00, TRUE);

-- Sample Orders

-- Order 1 (Alice, shipped to CA, CAD)
INSERT INTO orders (customer_id, order_status, order_currency, shipping_country, created_at, paid_at)
SELECT customer_id, 'paid', 'CAD', 'CA', NOW() - INTERVAL '10 days', NOW() - INTERVAL '10 days'
FROM customers WHERE email = 'alice.ca@example.com';

-- Order 2 (Bob, shipped to US, USD)
INSERT INTO orders (customer_id, order_status, order_currency, shipping_country, created_at, paid_at)
SELECT customer_id, 'shipped', 'USD', 'US', NOW() - INTERVAL '7 days', NOW() - INTERVAL '7 days'
FROM customers WHERE email = 'bob.us@example.com';

-- Order 3 (Chloe, shipped to GB, USD)
INSERT INTO orders (customer_id, order_status, order_currency, shipping_country, created_at, paid_at)
SELECT customer_id, 'paid', 'USD', 'GB', NOW() - INTERVAL '3 days', NOW() - INTERVAL '3 days'
FROM customers WHERE email = 'chloe.gb@example.com';

-- Order 4 (Dan, shipped to FR, USD)
INSERT INTO orders (customer_id, order_status, order_currency, shipping_country, created_at, paid_at)
SELECT customer_id, 'pending', 'USD', 'FR', NOW() - INTERVAL '1 days', NULL
FROM customers WHERE email = 'dan.fr@example.com';


INSERT INTO order_items (order_id, product_id, quantity, unit_price, currency_code)
SELECT o.order_id, p.product_id, 2, 25.00, 'CAD'
FROM orders o
JOIN customers c ON c.customer_id = o.customer_id
JOIN products p  ON p.sku = 'SKU-TSHIRT-001'
WHERE c.email = 'alice.ca@example.com'
  AND o.shipping_country = 'CA'
  AND o.order_currency = 'CAD'
  AND o.order_status = 'paid'
LIMIT 1;

INSERT INTO order_items (order_id, product_id, quantity, unit_price, currency_code)
SELECT o.order_id, p.product_id, 1, 12.50, 'CAD'
FROM orders o
JOIN customers c ON c.customer_id = o.customer_id
JOIN products p  ON p.sku = 'SKU-MUG-001'
WHERE c.email = 'alice.ca@example.com'
  AND o.shipping_country = 'CA'
  AND o.order_currency = 'CAD'
  AND o.order_status = 'paid'
LIMIT 1;

INSERT INTO order_items (order_id, product_id, quantity, unit_price, currency_code)
SELECT o.order_id, p.product_id, 1, 45.00, 'USD'
FROM orders o
JOIN customers c ON c.customer_id = o.customer_id
JOIN products p  ON p.sku = 'SKU-HOODIE-001'
WHERE c.email = 'bob.us@example.com'
  AND o.shipping_country = 'US'
  AND o.order_currency = 'USD'
  AND o.order_status = 'shipped'
LIMIT 1;

INSERT INTO order_items (order_id, product_id, quantity, unit_price, currency_code)
SELECT o.order_id, p.product_id, 2, 18.00, 'USD'
FROM orders o
JOIN customers c ON c.customer_id = o.customer_id
JOIN products p  ON p.sku = 'SKU-CAP-001'
WHERE c.email = 'bob.us@example.com'
  AND o.shipping_country = 'US'
  AND o.order_currency = 'USD'
  AND o.order_status = 'shipped'
LIMIT 1;

INSERT INTO order_items (order_id, product_id, quantity, unit_price, currency_code)
SELECT o.order_id, p.product_id, 3, 22.00, 'USD'
FROM orders o
JOIN customers c ON c.customer_id = o.customer_id
JOIN products p  ON p.sku = 'SKU-BAG-001'
WHERE c.email = 'chloe.gb@example.com'
  AND o.shipping_country = 'GB'
  AND o.order_currency = 'USD'
  AND o.order_status = 'paid'
LIMIT 1;


INSERT INTO order_items (order_id, product_id, quantity, unit_price, currency_code)
SELECT o.order_id, p.product_id, 1, 18.00, 'USD'
FROM orders o
JOIN customers c ON c.customer_id = o.customer_id
JOIN products p  ON p.sku = 'SKU-CAP-001'
WHERE c.email = 'dan.fr@example.com'
  AND o.shipping_country = 'FR'
  AND o.order_currency = 'USD'
  AND o.order_status = 'pending'
LIMIT 1;

COMMIT;
