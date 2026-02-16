BEGIN;
/*
INTERVIEW NOTES — Index Strategy

Why these indexes:
- orders(customer_id): customer order history / joins
- orders(shipping_country): country-level reporting (SUM by country)
- orders(order_status, created_at): common filtering + sorting for admin/reporting screens
- order_items(order_id / product_id): fast joins both directions
- customers(country_code): filter customers outside Canada quickly
- unique default shipping per customer: enforces business rule at DB level

- Prevents race conditions:
    Occurs when multiple database transactions or operations attempt to access and modify the same data concurrently, 
    leading to unpredictable and potentially incorrect results or data corruption.

Tradeoffs:
- Indexes speed reads but slow writes; choose them based on query patterns and measure with EXPLAIN.

Questions:
- Started with business queries we need to support Customer order history, Country level reporting, Admin/Reporting screens, Order details, Customer filtering
- uq_default_shipping_per_customer with a WHERE clause enforces business rule at DB level -> Each customer can have only one default shipping address. Partial unique index
    DB level constraint -> Prevents race conditions where two concurrent API requests try to set different addresses as defaults
*/

-- Optimizes customer lookup for order history queries
CREATE INDEX IF NOT EXISTS idx_orders_customer_id ON orders(customer_id);
CREATE INDEX IF NOT EXISTS idx_orders_shipping_country ON orders(shipping_country);
CREATE INDEX IF NOT EXISTS idx_orders_status_created_at ON orders(order_status, created_at);

-- Order items are frequently joined by order_id
CREATE INDEX IF NOT EXISTS idx_order_items_order_id ON order_items(order_id);
CREATE INDEX IF NOT EXISTS idx_order_items_product_id ON order_items(product_id);

-- Customers: frequent lookups by country
CREATE INDEX IF NOT EXISTS idx_customers_country_code ON customers(country_code);

-- Addresses: common lookups by customer + type
CREATE INDEX IF NOT EXISTS idx_addresses_customer_type ON customer_addresses(customer_id, address_type);

CREATE UNIQUE INDEX IF NOT EXISTS uq_default_shipping_per_customer
ON customer_addresses(customer_id)
WHERE (address_type = 'shipping' AND is_default = TRUE);

COMMIT;
