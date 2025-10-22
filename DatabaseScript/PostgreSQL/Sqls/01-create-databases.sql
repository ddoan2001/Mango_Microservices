-- Create extension for database management
CREATE EXTENSION IF NOT EXISTS dblink;

-- Create Mango_Auth database
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_database WHERE datname = 'Mango_Auth') THEN
        PERFORM dblink_exec('', 'CREATE DATABASE "Mango_Auth"');
        RAISE NOTICE 'Created database: Mango_Auth';
    ELSE
        RAISE NOTICE 'Database already exists: Mango_Auth';
    END IF;
END $$;

-- Create Mango_ShoppingCart database
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_database WHERE datname = 'Mango_ShoppingCart') THEN
        PERFORM dblink_exec('', 'CREATE DATABASE "Mango_ShoppingCart"');
        RAISE NOTICE 'Created database: Mango_ShoppingCart';
    ELSE
        RAISE NOTICE 'Database already exists: Mango_ShoppingCart';
    END IF;
END $$;

-- Create Mango_Email database
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_database WHERE datname = 'Mango_Email') THEN
        PERFORM dblink_exec('', 'CREATE DATABASE "Mango_Email"');
        RAISE NOTICE 'Created database: Mango_Email';
    ELSE
        RAISE NOTICE 'Database already exists: Mango_Email';
    END IF;
END $$;

-- Create Mango_Order database
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_database WHERE datname = 'Mango_Order') THEN
        PERFORM dblink_exec('', 'CREATE DATABASE "Mango_Order"');
        RAISE NOTICE 'Created database: Mango_Order';
    ELSE
        RAISE NOTICE 'Database already exists: Mango_Order';
    END IF;
END $$;

-- Create Mango_Reward database
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_database WHERE datname = 'Mango_Reward') THEN
        PERFORM dblink_exec('', 'CREATE DATABASE "Mango_Reward"');
        RAISE NOTICE 'Created database: Mango_Reward';
    ELSE
        RAISE NOTICE 'Database already exists: Mango_Reward';
    END IF;
END $$;

-- Create Mango_Product database
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_database WHERE datname = 'Mango_Product') THEN
        PERFORM dblink_exec('', 'CREATE DATABASE "Mango_Product"');
        RAISE NOTICE 'Created database: Mango_Product';
    ELSE
        RAISE NOTICE 'Database already exists: Mango_Product';
    END IF;
END $$;

-- Create Mango_Coupon database
DO $$
BEGIN
    IF NOT EXISTS (SELECT 1 FROM pg_database WHERE datname = 'Mango_Coupon') THEN
        PERFORM dblink_exec('', 'CREATE DATABASE "Mango_Coupon"');
        RAISE NOTICE 'Created database: Mango_Coupon';
    ELSE
        RAISE NOTICE 'Database already exists: Mango_Coupon';
    END IF;
END $$;

-- Verify created databases
SELECT 
    datname as "Database Name",
    pg_size_pretty(pg_database_size(datname)) as "Size"
FROM pg_database 
WHERE datname LIKE 'Mango_%'
ORDER BY datname;