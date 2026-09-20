-- =====================================================
-- E-commerce DB Tests (PASS/FAIL output)
-- Run: psql -d ecommerce_db -f db/tests.sql
-- =====================================================

\echo ''
\echo '======================'
\echo 'Running DB Tests...'
\echo '======================'
\echo ''

-- -----------------------------------------------------
-- Test 1: Customers table seeded correctly (expect 6)
-- -----------------------------------------------------
SELECT
  'T01_customers_count_is_6' AS test_name,
  CASE WHEN COUNT(*) = 6 THEN 'PASS' ELSE 'FAIL' END AS result,
  COUNT(*) AS actual_count,
  6 AS expected_count
FROM customers;

-- -----------------------------------------------------
-- Test 2: At least one customer has no orders
-- -----------------------------------------------------
SELECT
  'T02_has_customer_with_no_orders' AS test_name,
  CASE WHEN EXISTS (
    SELECT 1
    FROM customers c
    LEFT JOIN orders o ON o.customer_id = c.customer_id
    WHERE o.order_id IS NULL
  ) THEN 'PASS' ELSE 'FAIL' END AS result;

-- -----------------------------------------------------
-- Test 3: Revenue logic excludes pending orders
-- (i.e., no "pending" order should appear in the set of
--  orders that would be included for paid/shipped revenue)
-- -----------------------------------------------------
SELECT
  'T03_pending_orders_not_in_revenue_set' AS test_name,
  CASE WHEN COUNT(*) = 0 THEN 'PASS' ELSE 'FAIL' END AS result,
  COUNT(*) AS offending_pending_orders
FROM orders o_pending
WHERE o_pending.order_status = 'pending'
  AND o_pending.order_id IN (
    SELECT DISTINCT oi.order_id
    FROM order_items oi
    JOIN orders o ON o.order_id = oi.order_id
    WHERE o.order_status IN ('paid', 'shipped')
  );

SELECT
  p.product_id,
  p.sku,
  p.name,
FROM products p
LEFT JOIN order_items oi ON oi.product_id = p.product_id
WHERE oi.product_id is NULL;

SELECT
  c.customer_id,
  c.first_name,
  c.last_name,
  c.email
  SUM(oi.quantity * oi.unit_price) AS lifetime
FROM customers c
INNER JOIN orders o ON o.customer_id = c.customer_id
INNER JOIN order_items oi ON oi.order_id = o.order_id
GROUP BY c.customer_id, c.first_name, c.last_name, c.email
GROUP BY lifetime DESC;

-- -----------------------------------------------------
-- Test 4: No invalid order items (negative price or non-positive quantity)
-- -----------------------------------------------------
SELECT
  'T04_no_invalid_order_items' AS test_name,
  CASE WHEN COUNT(*) = 0 THEN 'PASS' ELSE 'FAIL' END AS result,
  COUNT(*) AS invalid_rows
FROM order_items
WHERE unit_price < 0 OR quantity <= 0;

-- -----------------------------------------------------
-- Test 5: Query outside Canada
-- -----------------------------------------------------

SELECT * customers
WHERE country_code != 'CA';

-- -----------------------------------------------------
-- Test 6: Calculate revenue per shipping country
-- -----------------------------------------------------

SELECT
  o.shipping_country
  SUM(oi.quantity * oi.unit_price) AS total_revenue
FROM orders o
JOIN order_items oi ON oi.order_id = o.order_id
WHERE o.order_status IN ('paid', 'shipped')
GROUP BY o.shipping_country
GROUP BY total_revenue DESC;

-- -----------------------------------------------------
-- Test 7: Test customers with no orders
-- -----------------------------------------------------

SELECT c.*
FROM customers c
LEFT JOIN orders o ON o.customer_id = c.customer_id
WHERE o.order_id IS NULL;

SELECT * FROM customers c
WHERE NOT EXISTS (
  SELECT 1 FROM orders WHERE customer_id = c.customer_id
);

-- -----------------------------------------------------
-- Test 8: Find specific users orders
-- -----------------------------------------------------

SELECT
  o.order_id,
  o.order_status,
  o.created_at,
  o.shipping_country,
  SUM(oi.quantity * oi.unit_price) AS order_total
FROM orders o
JOIN order_items oi ON oi.order_id = o.order_id
WHERE(
  SELECT customer_id
  FROM customers
  WHERE email = 'alice.ca@example.com'
)
GROUP BY o.order_id, o.order_status, o.created_at, o.shipping_country
ORDER BY o.created_at DESC;

\echo ''
\echo '======================'
\echo 'Done.'
\echo '======================'
\echo ''
