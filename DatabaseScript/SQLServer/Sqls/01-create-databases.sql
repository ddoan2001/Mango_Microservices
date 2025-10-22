-- Create all Mango microservice databases
SET NOCOUNT ON;
USE master;
GO

-- Enable advanced options for size calculations
PRINT '';
EXEC sp_configure 'show advanced options', 1;
RECONFIGURE;
GO

-- Create AuthAPI database
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'Mango_Auth')
BEGIN
    CREATE DATABASE Mango_Auth;
    PRINT CHAR(10) + 'NOTICE:  Created database: Mango_Auth';
END
ELSE
BEGIN
    PRINT CHAR(10) + 'NOTICE:  Database already exists: Mango_Auth';
END
GO

-- Create ShoppingCartAPI database
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'Mango_ShoppingCart')
BEGIN
    CREATE DATABASE Mango_ShoppingCart;
    PRINT CHAR(10) + 'NOTICE:  Created database: Mango_ShoppingCart';
END
ELSE
BEGIN
    PRINT CHAR(10) + 'NOTICE:  Database already exists: Mango_ShoppingCart';
END
GO

-- Create EmailAPI database
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'Mango_Email')
BEGIN
    CREATE DATABASE Mango_Email;
    PRINT CHAR(10) + 'NOTICE:  Created database: Mango_Email';
END
ELSE
BEGIN
    PRINT CHAR(10) + 'NOTICE:  Database already exists: Mango_Email';
END
GO

-- Create OrderAPI database
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'Mango_Order')
BEGIN
    CREATE DATABASE Mango_Order;
    PRINT CHAR(10) + 'NOTICE:  Created database: Mango_Order';
END
ELSE
BEGIN
    PRINT CHAR(10) + 'NOTICE:  Database already exists: Mango_Order';
END
GO

-- Create RewardAPI database
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'Mango_Reward')
BEGIN
    CREATE DATABASE Mango_Reward;
    PRINT CHAR(10) + 'NOTICE:  Created database: Mango_Reward';
END
ELSE
BEGIN
    PRINT CHAR(10) + 'NOTICE:  Database already exists: Mango_Reward';
END
GO

-- Create ProductAPI database
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'Mango_Product')
BEGIN
    CREATE DATABASE Mango_Product;
    PRINT CHAR(10) + 'NOTICE:  Created database: Mango_Product';
END
ELSE
BEGIN
    PRINT CHAR(10) + 'NOTICE:  Database already exists: Mango_Product';
END
GO

-- Create CouponAPI database
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'Mango_Coupon')
BEGIN
    CREATE DATABASE Mango_Coupon;
    PRINT CHAR(10) + 'NOTICE:  Created database: Mango_Coupon';
END
ELSE
BEGIN
    PRINT CHAR(10) + 'NOTICE:  Database already exists: Mango_Coupon';
END
GO

-- Verify created databases and their sizes
PRINT '';
PRINT 'Verifying created databases...';
PRINT '';

SELECT 
    CAST(d.name as CHAR(30)) as 'Database Name',
    CAST(CAST(SUM(CAST(f.size AS BIGINT) * 8.0 / 1024) AS DECIMAL(10,0)) AS VARCHAR(10)) + ' kB' as 'Size'
FROM sys.databases d
LEFT JOIN sys.master_files f ON d.database_id = f.database_id
WHERE d.name LIKE 'Mango_%'
GROUP BY d.name
ORDER BY d.name;

PRINT '';
PRINT 'NOTICE:  Database setup completed successfully!';