BEGIN;

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
