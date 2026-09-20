# E-commerce Database Schema (PostgreSQL)

## Overview
This project defines a relational database schema for a global e-commerce application.
It models customers, products, orders, and order line items while supporting
multi-currency pricing and geographic reporting.

The schema is designed to:
- Support customers with zero or more orders
- Support orders containing multiple products
- Preserve historical pricing accuracy
- Enable efficient analytical queries

## Schema Design

### Core Tables
- countries 
  Reference table using ISO country codes for consistent geographic data.

- customers  
  Stores customer identity and country of residence.

- customer_addresses  
  Supports multiple shipping/billing addresses per customer.

- products  
  Stores product catalog data and current pricing.

- orders  
  Represents a customer purchase and captures shipping country and order status.

- order_items  
  Join table between orders and products. Stores snapshot pricing to preserve
  historical order totals.

---

## Key Design Decisions

- **Normalization**  
  Schema is normalized to reduce duplication and maintain data integrity.

- **Snapshot Pricing**  
  Product prices are copied into `order_items` at order time to prevent
  historical totals from changing if product prices are updated later.

- **Geographic Accuracy**  
  Orders store `shipping_country` explicitly to support accurate regional reporting
  even if customer profile data changes.

- **Data Integrity**
  - Foreign keys enforce relationships
  - CHECK constraints validate statuses and quantities
  - Unique constraints prevent duplicate identifiers

## Future improvements

If the database grows too large to manage efficiently, I would: 

- Add and tune indexes based on real query patterns (`EXPLAIN ANALYZE`)
- Partition large tables like `orders` and `order_items` by date
- Use read replicas and caching for read-heavy workloads
- Pre-aggregate analytics data (e.g., sales by country)
- Archive older, infrequently accessed data

---

## Setup Instructions

### Requirements
- PostgreSQL 15+ (used 18 locally)
- `psql` CLI

### Create Database
```sql
DROP DATABASE IF EXISTS ecommerce_db;
CREATE DATABASE ecommerce;
\q

Run the SQL scripts using psql in the following order:
psql -d ecommerce -f schema.sql
psql -d ecommerce -f indexes.sql
psql -d ecommerce -f seed.sql

Included tests.sql to validate key business rules and data integrity.