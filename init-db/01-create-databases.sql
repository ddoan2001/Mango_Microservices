-- Create all Mango microservice databases
USE master;
GO

-- Create AuthAPI database
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'Mango_Auth')
BEGIN
    CREATE DATABASE Mango_Auth;
    PRINT 'Database Mango_Auth created successfully';
END
ELSE
BEGIN
    PRINT 'Database Mango_Auth already exists';
END
GO

-- Create ShoppingCartAPI database
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'Mango_ShoppingCart')
BEGIN
    CREATE DATABASE Mango_ShoppingCart;
    PRINT 'Database Mango_ShoppingCart created successfully';
END
ELSE
BEGIN
    PRINT 'Database Mango_ShoppingCart already exists';
END
GO

-- Create EmailAPI database
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'Mango_Email')
BEGIN
    CREATE DATABASE Mango_Email;
    PRINT 'Database Mango_Email created successfully';
END
ELSE
BEGIN
    PRINT 'Database Mango_Email already exists';
END
GO

-- Create OrderAPI database
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'Mango_Order')
BEGIN
    CREATE DATABASE Mango_Order;
    PRINT 'Database Mango_Order created successfully';
END
ELSE
BEGIN
    PRINT 'Database Mango_Order already exists';
END
GO

-- Create RewardAPI database
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'Mango_Reward')
BEGIN
    CREATE DATABASE Mango_Reward;
    PRINT 'Database Mango_Reward created successfully';
END
ELSE
BEGIN
    PRINT 'Database Mango_Reward already exists';
END
GO

-- Create ProductAPI database
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'Mango_Product')
BEGIN
    CREATE DATABASE Mango_Product;
    PRINT 'Database Mango_Product created successfully';
END
ELSE
BEGIN
    PRINT 'Database Mango_Product already exists';
END
GO

-- Create CouponAPI database
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'Mango_Coupon')
BEGIN
    CREATE DATABASE Mango_Coupon;
    PRINT 'Database Mango_Coupon created successfully';
END
ELSE
BEGIN
    PRINT 'Database Mango_Coupon already exists';
END
GO

PRINT 'All Mango microservice databases have been created successfully!';