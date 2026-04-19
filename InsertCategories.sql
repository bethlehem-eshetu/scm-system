-- Hierarchical Category Seeding Script

-- Clear existing data if necessary (CAUTION: backup if needed)
-- DELETE FROM ProductCategories;

-- Insert Level 1 Categories
INSERT INTO ProductCategories (CategoryName, Description, [Level], IsActive, CreatedAt)
VALUES 
('Clothing', 'Apparel and fashion items', 1, 1, GETDATE()),
('Electronics', 'Electronic devices and gadgets', 1, 1, GETDATE()),
('Food & Beverage', 'Food products and drinks', 1, 1, GETDATE()),
('Furniture', 'Home and office furniture', 1, 1, GETDATE()),
('Raw Materials', 'Industrial raw materials', 1, 1, GETDATE());

-- Insert Level 2 Categories (Subcategories)
-- Clothing
INSERT INTO ProductCategories (CategoryName, Description, ParentCategoryId, [Level], IsActive, CreatedAt)
SELECT 'Men''s Clothing', 'Clothing for men', Id, 2, 1, GETDATE() FROM ProductCategories WHERE CategoryName = 'Clothing' AND [Level] = 1;
INSERT INTO ProductCategories (CategoryName, Description, ParentCategoryId, [Level], IsActive, CreatedAt)
SELECT 'Women''s Clothing', 'Clothing for women', Id, 2, 1, GETDATE() FROM ProductCategories WHERE CategoryName = 'Clothing' AND [Level] = 1;
INSERT INTO ProductCategories (CategoryName, Description, ParentCategoryId, [Level], IsActive, CreatedAt)
SELECT 'Children''s Clothing', 'Clothing for children', Id, 2, 1, GETDATE() FROM ProductCategories WHERE CategoryName = 'Clothing' AND [Level] = 1;

-- Electronics
INSERT INTO ProductCategories (CategoryName, Description, ParentCategoryId, [Level], IsActive, CreatedAt)
SELECT 'Mobile Phones', 'Smartphones and mobile devices', Id, 2, 1, GETDATE() FROM ProductCategories WHERE CategoryName = 'Electronics' AND [Level] = 1;
INSERT INTO ProductCategories (CategoryName, Description, ParentCategoryId, [Level], IsActive, CreatedAt)
SELECT 'Computers & Laptops', 'Laptops, desktops, and accessories', Id, 2, 1, GETDATE() FROM ProductCategories WHERE CategoryName = 'Electronics' AND [Level] = 1;
INSERT INTO ProductCategories (CategoryName, Description, ParentCategoryId, [Level], IsActive, CreatedAt)
SELECT 'Home Appliances', 'Kitchen and household appliances', Id, 2, 1, GETDATE() FROM ProductCategories WHERE CategoryName = 'Electronics' AND [Level] = 1;

-- Food & Beverage
INSERT INTO ProductCategories (CategoryName, Description, ParentCategoryId, [Level], IsActive, CreatedAt)
SELECT 'Fresh Produce', 'Fruits and vegetables', Id, 2, 1, GETDATE() FROM ProductCategories WHERE CategoryName = 'Food & Beverage' AND [Level] = 1;
INSERT INTO ProductCategories (CategoryName, Description, ParentCategoryId, [Level], IsActive, CreatedAt)
SELECT 'Beverages', 'Soft drinks, water, and juices', Id, 2, 1, GETDATE() FROM ProductCategories WHERE CategoryName = 'Food & Beverage' AND [Level] = 1;
INSERT INTO ProductCategories (CategoryName, Description, ParentCategoryId, [Level], IsActive, CreatedAt)
SELECT 'Packaged Food', 'Canned and processed food', Id, 2, 1, GETDATE() FROM ProductCategories WHERE CategoryName = 'Food & Beverage' AND [Level] = 1;

-- Furniture
INSERT INTO ProductCategories (CategoryName, Description, ParentCategoryId, [Level], IsActive, CreatedAt)
SELECT 'Office Furniture', 'Desks, chairs, and office setups', Id, 2, 1, GETDATE() FROM ProductCategories WHERE CategoryName = 'Furniture' AND [Level] = 1;
INSERT INTO ProductCategories (CategoryName, Description, ParentCategoryId, [Level], IsActive, CreatedAt)
SELECT 'Living Room Furniture', 'Sofas, coffee tables, etc.', Id, 2, 1, GETDATE() FROM ProductCategories WHERE CategoryName = 'Furniture' AND [Level] = 1;
INSERT INTO ProductCategories (CategoryName, Description, ParentCategoryId, [Level], IsActive, CreatedAt)
SELECT 'Bedroom Furniture', 'Beds, wardrobes, etc.', Id, 2, 1, GETDATE() FROM ProductCategories WHERE CategoryName = 'Furniture' AND [Level] = 1;

-- Raw Materials
INSERT INTO ProductCategories (CategoryName, Description, ParentCategoryId, [Level], IsActive, CreatedAt)
SELECT 'Metal', 'Steel, aluminum, etc.', Id, 2, 1, GETDATE() FROM ProductCategories WHERE CategoryName = 'Raw Materials' AND [Level] = 1;
INSERT INTO ProductCategories (CategoryName, Description, ParentCategoryId, [Level], IsActive, CreatedAt)
SELECT 'Plastic', 'Raw plastic materials', Id, 2, 1, GETDATE() FROM ProductCategories WHERE CategoryName = 'Raw Materials' AND [Level] = 1;
INSERT INTO ProductCategories (CategoryName, Description, ParentCategoryId, [Level], IsActive, CreatedAt)
SELECT 'Wood', 'Lumber and timber', Id, 2, 1, GETDATE() FROM ProductCategories WHERE CategoryName = 'Raw Materials' AND [Level] = 1;
