using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;
using Shoes.Models;

namespace Shoes.Services;

public class DatabaseService
{
    private readonly string _connectionString;
    
    public DatabaseService()
    {
        _connectionString = "Host=localhost;Username=postgres;Password=123456;Database=Shoes";
    }
    
    private NpgsqlConnection GetConnection()
    {
        return new NpgsqlConnection(_connectionString);
    }
    
    // User methods
    public async Task<User?> AuthenticateUser(string login, string password)
    {
        using var conn = GetConnection();
        await conn.OpenAsync();
        
        var cmd = new NpgsqlCommand(@"
            SELECT u.id, u.role_id, u.last_name, u.first_name, u.middle_name, 
                   u.login, u.password, ur.role
            FROM users u
            JOIN user_role ur ON u.role_id = ur.id
            WHERE u.login = @login AND u.password = @password", conn);
        
        cmd.Parameters.AddWithValue("login", login);
        cmd.Parameters.AddWithValue("password", password);
        
        using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            return new User
            {
                Id = reader.GetInt32(0),
                RoleId = reader.GetInt32(1),
                LastName = reader.GetString(2),
                FirstName = reader.GetString(3),
                MiddleName = reader.IsDBNull(4) ? null : reader.GetString(4),
                Login = reader.GetString(5),
                Password = reader.GetString(6),
                RoleName = reader.GetString(7)
            };
        }
        
        return null;
    }
    
    // Product methods
    public async Task<List<Product>> GetAllProducts()
    {
        var products = new List<Product>();
        
        using var conn = GetConnection();
        await conn.OpenAsync();
        
        var cmd = new NpgsqlCommand(@"
            SELECT p.article, p.product_name_id, p.unit_id, p.price, p.supplier_id,
                   p.manufacturer_id, p.category_id, p.current_discount, p.stock_quantity,
                   p.description, p.photo,
                   pn.product_name, u.unit_name, s.supplier_name, 
                   m.manufacturer_name, c.category_name
            FROM products p
            JOIN product_name pn ON p.product_name_id = pn.id
            JOIN unit_of_measure u ON p.unit_id = u.id
            JOIN supplier s ON p.supplier_id = s.id
            JOIN manufacturer m ON p.manufacturer_id = m.id
            JOIN category c ON p.category_id = c.id
            ORDER BY pn.product_name", conn);
        
        using var reader = await cmd.ExecuteReaderAsync();
        int index = 0;
        while (await reader.ReadAsync())
        {
            var rawPhoto = reader.IsDBNull(10) ? null : reader.GetString(10);
            if (string.IsNullOrWhiteSpace(rawPhoto))
            {
                rawPhoto = "picture.png";
            }
            
            var product = new Product
            {
                Article = reader.GetString(0),
                ProductNameId = reader.GetInt32(1),
                UnitId = reader.GetInt32(2),
                Price = reader.GetDecimal(3),
                SupplierId = reader.GetInt32(4),
                ManufacturerId = reader.GetInt32(5),
                CategoryId = reader.GetInt32(6),
                CurrentDiscount = reader.IsDBNull(7) ? null : reader.GetDecimal(7),
                StockQuantity = reader.GetInt32(8),
                Description = reader.IsDBNull(9) ? null : reader.GetString(9),
                Photo = rawPhoto,
                ProductName = reader.GetString(11),
                UnitName = reader.GetString(12),
                SupplierName = reader.GetString(13),
                ManufacturerName = reader.GetString(14),
                CategoryName = reader.GetString(15)
            };
            
            products.Add(product);
            index++;
            var logPath = product.ImagePath;
            if (logPath.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    logPath = new Uri(logPath).LocalPath;
                }
                catch (UriFormatException)
                {
                    logPath = Uri.UnescapeDataString(product.ImagePath);
                }
            }
            var log = $"[Products] #{index}: {product.ProductName} ({product.Article}) -> {logPath}";
            System.Diagnostics.Debug.WriteLine(log);
            Console.WriteLine(log);
        }
        
        return products;
    }
    
    public async Task<bool> AddProduct(Product product)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            // Проверка существования значений перед вставкой
            var checkCmd = new NpgsqlCommand(@"
                SELECT 
                    (SELECT COUNT(*) FROM product_name WHERE id = @product_name_id) as pn_exists,
                    (SELECT COUNT(*) FROM unit_of_measure WHERE id = @unit_id) as unit_exists,
                    (SELECT COUNT(*) FROM supplier WHERE id = @supplier_id) as supplier_exists,
                    (SELECT COUNT(*) FROM manufacturer WHERE id = @manufacturer_id) as manufacturer_exists,
                    (SELECT COUNT(*) FROM category WHERE id = @category_id) as category_exists", conn);
            
            checkCmd.Parameters.AddWithValue("product_name_id", product.ProductNameId);
            checkCmd.Parameters.AddWithValue("unit_id", product.UnitId);
            checkCmd.Parameters.AddWithValue("supplier_id", product.SupplierId);
            checkCmd.Parameters.AddWithValue("manufacturer_id", product.ManufacturerId);
            checkCmd.Parameters.AddWithValue("category_id", product.CategoryId);
            
            using var checkReader = await checkCmd.ExecuteReaderAsync();
            if (await checkReader.ReadAsync())
            {
                if (checkReader.GetInt64(0) == 0) 
                {
                    checkReader.Close();
                    throw new Exception("Наименование товара не существует в базе данных. Выберите другое значение.");
                }
                if (checkReader.GetInt64(1) == 0) 
                {
                    checkReader.Close();
                    throw new Exception("Единица измерения не существует в базе данных. Выберите другое значение.");
                }
                if (checkReader.GetInt64(2) == 0) 
                {
                    checkReader.Close();
                    throw new Exception("Поставщик не существует в базе данных. Выберите другое значение.");
                }
                if (checkReader.GetInt64(3) == 0) 
                {
                    checkReader.Close();
                    throw new Exception("Производитель не существует в базе данных. Выберите другое значение.");
                }
                if (checkReader.GetInt64(4) == 0) 
                {
                    checkReader.Close();
                    throw new Exception("Категория не существует в базе данных. Выберите другое значение.");
                }
            }
            checkReader.Close();
            
            var cmd = new NpgsqlCommand(@"
                INSERT INTO products (article, product_name_id, unit_id, price, supplier_id,
                                      manufacturer_id, category_id, current_discount, 
                                      stock_quantity, description, photo)
                VALUES (@article, @product_name_id, @unit_id, @price, @supplier_id,
                        @manufacturer_id, @category_id, @current_discount, 
                        @stock_quantity, @description, @photo)", conn);
            
            cmd.Parameters.AddWithValue("article", product.Article);
            cmd.Parameters.AddWithValue("product_name_id", product.ProductNameId);
            cmd.Parameters.AddWithValue("unit_id", product.UnitId);
            cmd.Parameters.AddWithValue("price", product.Price);
            cmd.Parameters.AddWithValue("supplier_id", product.SupplierId);
            cmd.Parameters.AddWithValue("manufacturer_id", product.ManufacturerId);
            cmd.Parameters.AddWithValue("category_id", product.CategoryId);
            cmd.Parameters.AddWithValue("current_discount", (object?)product.CurrentDiscount ?? DBNull.Value);
            cmd.Parameters.AddWithValue("stock_quantity", product.StockQuantity);
            cmd.Parameters.AddWithValue("description", (object?)product.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("photo", (object?)product.Photo ?? DBNull.Value);
            
            return await cmd.ExecuteNonQueryAsync() > 0;
        }
        catch (Npgsql.PostgresException ex) when (ex.SqlState == "23503")
        {
            throw new Exception("Ошибка: выбранное значение не существует в базе данных. Выберите другое значение.");
        }
        catch (Exception ex)
        {
            throw new Exception($"Ошибка при добавлении товара: {ex.Message}");
        }
    }
    
    public async Task<bool> UpdateProduct(Product product)
    {
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            // Проверка существования значений перед обновлением
            var checkCmd = new NpgsqlCommand(@"
                SELECT 
                    (SELECT COUNT(*) FROM product_name WHERE id = @product_name_id) as pn_exists,
                    (SELECT COUNT(*) FROM unit_of_measure WHERE id = @unit_id) as unit_exists,
                    (SELECT COUNT(*) FROM supplier WHERE id = @supplier_id) as supplier_exists,
                    (SELECT COUNT(*) FROM manufacturer WHERE id = @manufacturer_id) as manufacturer_exists,
                    (SELECT COUNT(*) FROM category WHERE id = @category_id) as category_exists", conn);
            
            checkCmd.Parameters.AddWithValue("product_name_id", product.ProductNameId);
            checkCmd.Parameters.AddWithValue("unit_id", product.UnitId);
            checkCmd.Parameters.AddWithValue("supplier_id", product.SupplierId);
            checkCmd.Parameters.AddWithValue("manufacturer_id", product.ManufacturerId);
            checkCmd.Parameters.AddWithValue("category_id", product.CategoryId);
            
            using var checkReader = await checkCmd.ExecuteReaderAsync();
            if (await checkReader.ReadAsync())
            {
                if (checkReader.GetInt64(0) == 0) 
                {
                    checkReader.Close();
                    throw new Exception("Наименование товара не существует в базе данных. Выберите другое значение.");
                }
                if (checkReader.GetInt64(1) == 0) 
                {
                    checkReader.Close();
                    throw new Exception("Единица измерения не существует в базе данных. Выберите другое значение.");
                }
                if (checkReader.GetInt64(2) == 0) 
                {
                    checkReader.Close();
                    throw new Exception("Поставщик не существует в базе данных. Выберите другое значение.");
                }
                if (checkReader.GetInt64(3) == 0) 
                {
                    checkReader.Close();
                    throw new Exception("Производитель не существует в базе данных. Выберите другое значение.");
                }
                if (checkReader.GetInt64(4) == 0) 
                {
                    checkReader.Close();
                    throw new Exception("Категория не существует в базе данных. Выберите другое значение.");
                }
            }
            checkReader.Close();
            
            var cmd = new NpgsqlCommand(@"
                UPDATE products 
                SET product_name_id = @product_name_id, unit_id = @unit_id, 
                    price = @price, supplier_id = @supplier_id,
                    manufacturer_id = @manufacturer_id, category_id = @category_id, 
                    current_discount = @current_discount, stock_quantity = @stock_quantity,
                    description = @description, photo = @photo
                WHERE article = @article", conn);
            
            cmd.Parameters.AddWithValue("article", product.Article);
            cmd.Parameters.AddWithValue("product_name_id", product.ProductNameId);
            cmd.Parameters.AddWithValue("unit_id", product.UnitId);
            cmd.Parameters.AddWithValue("price", product.Price);
            cmd.Parameters.AddWithValue("supplier_id", product.SupplierId);
            cmd.Parameters.AddWithValue("manufacturer_id", product.ManufacturerId);
            cmd.Parameters.AddWithValue("category_id", product.CategoryId);
            cmd.Parameters.AddWithValue("current_discount", (object?)product.CurrentDiscount ?? DBNull.Value);
            cmd.Parameters.AddWithValue("stock_quantity", product.StockQuantity);
            cmd.Parameters.AddWithValue("description", (object?)product.Description ?? DBNull.Value);
            cmd.Parameters.AddWithValue("photo", (object?)product.Photo ?? DBNull.Value);
            
            return await cmd.ExecuteNonQueryAsync() > 0;
        }
        catch (Npgsql.PostgresException ex) when (ex.SqlState == "23503")
        {
            throw new Exception("Ошибка: выбранное значение не существует в базе данных. Выберите другое значение.");
        }
        catch (Exception ex)
        {
            throw new Exception($"Ошибка при обновлении товара: {ex.Message}");
        }
    }
    
    public async Task<bool> DeleteProduct(string article)
    {
        using var conn = GetConnection();
        await conn.OpenAsync();
        
        var cmd = new NpgsqlCommand("DELETE FROM products WHERE article = @article", conn);
        cmd.Parameters.AddWithValue("article", article);
        
        return await cmd.ExecuteNonQueryAsync() > 0;
    }
    
    // Order methods
    public async Task<List<Order>> GetAllOrders()
    {
        var orders = new List<Order>();
        
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            System.Diagnostics.Debug.WriteLine("=== GetAllOrders: Начало загрузки ===");
            
            // Используем LEFT JOIN, чтобы получить заказы даже если нет связанных данных
            var cmd = new NpgsqlCommand(@"
                SELECT o.order_number, o.order_date, o.delivery_date, o.pickup_point_id,
                       o.client_id, o.receipt_code, o.order_status, o.total_amount,
                       COALESCE(pp.city || ', ' || pp.street || ', ' || pp.house_number, 'Не указан') as pickup_point_address,
                       COALESCE(u.last_name || ' ' || u.first_name, 'Неизвестный клиент') as client_name
                FROM orders o
                LEFT JOIN pickup_points pp ON o.pickup_point_id = pp.id
                LEFT JOIN users u ON o.client_id = u.id
                ORDER BY o.order_date DESC", conn);
            
            using var reader = await cmd.ExecuteReaderAsync();
            int count = 0;
            while (await reader.ReadAsync())
            {
                var order = new Order
                {
                    OrderNumber = reader.GetInt32(0),
                    OrderDate = reader.GetDateTime(1),
                    DeliveryDate = reader.GetDateTime(2),
                    PickupPointId = reader.GetInt32(3),
                    ClientId = reader.GetInt32(4),
                    ReceiptCode = reader.GetString(5),
                    OrderStatus = reader.GetString(6),
                    TotalAmount = reader.GetDecimal(7),
                    PickupPointAddress = reader.IsDBNull(8) ? "Не указан" : reader.GetString(8),
                    ClientName = reader.IsDBNull(9) ? "Неизвестный клиент" : reader.GetString(9)
                };
                orders.Add(order);
                count++;
                System.Diagnostics.Debug.WriteLine($"  Заказ #{order.OrderNumber}: {order.ClientName}, {order.TotalAmount} ₽");
            }
            
            System.Diagnostics.Debug.WriteLine($"=== GetAllOrders: Загружено {count} заказов ===");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ОШИБКА GetAllOrders: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
        
        return orders;
    }
    
    public async Task<List<Order>> GetClientOrders(int clientId)
    {
        var orders = new List<Order>();
        
        try
        {
            using var conn = GetConnection();
            await conn.OpenAsync();
            
            System.Diagnostics.Debug.WriteLine($"=== GetClientOrders: Начало загрузки для клиента ID={clientId} ===");
            
            // Используем LEFT JOIN, чтобы получить заказы даже если нет связанных данных
            var cmd = new NpgsqlCommand(@"
                SELECT o.order_number, o.order_date, o.delivery_date, o.pickup_point_id,
                       o.client_id, o.receipt_code, o.order_status, o.total_amount,
                       COALESCE(pp.city || ', ' || pp.street || ', ' || pp.house_number, 'Не указан') as pickup_point_address,
                       COALESCE(u.last_name || ' ' || u.first_name, 'Неизвестный клиент') as client_name
                FROM orders o
                LEFT JOIN pickup_points pp ON o.pickup_point_id = pp.id
                LEFT JOIN users u ON o.client_id = u.id
                WHERE o.client_id = @client_id
                ORDER BY o.order_date DESC", conn);
            
            cmd.Parameters.AddWithValue("client_id", clientId);
            
            using var reader = await cmd.ExecuteReaderAsync();
            int count = 0;
            while (await reader.ReadAsync())
            {
                var order = new Order
                {
                    OrderNumber = reader.GetInt32(0),
                    OrderDate = reader.GetDateTime(1),
                    DeliveryDate = reader.GetDateTime(2),
                    PickupPointId = reader.GetInt32(3),
                    ClientId = reader.GetInt32(4),
                    ReceiptCode = reader.GetString(5),
                    OrderStatus = reader.GetString(6),
                    TotalAmount = reader.GetDecimal(7),
                    PickupPointAddress = reader.IsDBNull(8) ? "Не указан" : reader.GetString(8),
                    ClientName = reader.IsDBNull(9) ? "Неизвестный клиент" : reader.GetString(9)
                };
                orders.Add(order);
                count++;
                System.Diagnostics.Debug.WriteLine($"  Заказ клиента #{order.OrderNumber}: {order.ClientName}, {order.TotalAmount} ₽");
            }
            
            System.Diagnostics.Debug.WriteLine($"=== GetClientOrders: Загружено {count} заказов ===");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"ОШИБКА GetClientOrders: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            throw;
        }
        
        return orders;
    }
    
    public async Task<List<OrderDetail>> GetOrderDetails(int orderNumber)
    {
        var details = new List<OrderDetail>();
        
        using var conn = GetConnection();
        await conn.OpenAsync();
        
        var cmd = new NpgsqlCommand(@"
            SELECT od.id, od.order_number, od.article, od.quantity, od.unit_price,
                   od.discount, od.total_price, pn.product_name
            FROM order_details od
            JOIN products p ON od.article = p.article
            JOIN product_name pn ON p.product_name_id = pn.id
            WHERE od.order_number = @order_number", conn);
        
        cmd.Parameters.AddWithValue("order_number", orderNumber);
        
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            details.Add(new OrderDetail
            {
                Id = reader.GetInt32(0),
                OrderNumber = reader.GetInt32(1),
                Article = reader.GetString(2),
                Quantity = reader.GetInt32(3),
                UnitPrice = reader.GetDecimal(4),
                Discount = reader.GetDecimal(5),
                TotalPrice = reader.GetDecimal(6),
                ProductName = reader.GetString(7)
            });
        }
        
        return details;
    }
    
    public async Task<bool> DeleteOrder(int orderNumber)
    {
        using var conn = GetConnection();
        await conn.OpenAsync();
        
        var cmd = new NpgsqlCommand("DELETE FROM orders WHERE order_number = @order_number", conn);
        cmd.Parameters.AddWithValue("order_number", orderNumber);
        
        return await cmd.ExecuteNonQueryAsync() > 0;
    }
    
    public async Task<bool> UpdateOrderStatus(int orderNumber, string newStatus)
    {
        using var conn = GetConnection();
        await conn.OpenAsync();
        
        var cmd = new NpgsqlCommand("UPDATE orders SET order_status = @status WHERE order_number = @order_number", conn);
        cmd.Parameters.AddWithValue("status", newStatus);
        cmd.Parameters.AddWithValue("order_number", orderNumber);
        
        return await cmd.ExecuteNonQueryAsync() > 0;
    }
    
    public async Task<bool> UpdateOrder(Order order, List<OrderDetail> details)
    {
        using var conn = GetConnection();
        await conn.OpenAsync();
        
        using var transaction = await conn.BeginTransactionAsync();
        
        try
        {
            // Обновляем заказ
            var orderCmd = new NpgsqlCommand(@"
                UPDATE orders 
                SET order_date = @order_date, 
                    delivery_date = @delivery_date, 
                    pickup_point_id = @pickup_point_id, 
                    client_id = @client_id, 
                    receipt_code = @receipt_code, 
                    order_status = @order_status, 
                    total_amount = @total_amount
                WHERE order_number = @order_number", conn, transaction);
            
            orderCmd.Parameters.AddWithValue("order_number", order.OrderNumber);
            orderCmd.Parameters.AddWithValue("order_date", order.OrderDate);
            orderCmd.Parameters.AddWithValue("delivery_date", order.DeliveryDate);
            orderCmd.Parameters.AddWithValue("pickup_point_id", order.PickupPointId);
            orderCmd.Parameters.AddWithValue("client_id", order.ClientId);
            orderCmd.Parameters.AddWithValue("receipt_code", order.ReceiptCode);
            orderCmd.Parameters.AddWithValue("order_status", order.OrderStatus);
            orderCmd.Parameters.AddWithValue("total_amount", order.TotalAmount);
            
            await orderCmd.ExecuteNonQueryAsync();
            
            // Удаляем старые детали заказа
            var deleteCmd = new NpgsqlCommand("DELETE FROM order_details WHERE order_number = @order_number", conn, transaction);
            deleteCmd.Parameters.AddWithValue("order_number", order.OrderNumber);
            await deleteCmd.ExecuteNonQueryAsync();
            
            // Добавляем новые детали заказа
            foreach (var detail in details)
            {
                var detailCmd = new NpgsqlCommand(@"
                    INSERT INTO order_details (order_number, article, quantity, unit_price, discount, total_price)
                    VALUES (@order_number, @article, @quantity, @unit_price, @discount, @total_price)", conn, transaction);
                
                detailCmd.Parameters.AddWithValue("order_number", order.OrderNumber);
                detailCmd.Parameters.AddWithValue("article", detail.Article);
                detailCmd.Parameters.AddWithValue("quantity", detail.Quantity);
                detailCmd.Parameters.AddWithValue("unit_price", detail.UnitPrice);
                detailCmd.Parameters.AddWithValue("discount", detail.Discount);
                detailCmd.Parameters.AddWithValue("total_price", detail.TotalPrice);
                
                await detailCmd.ExecuteNonQueryAsync();
            }
            
            await transaction.CommitAsync();
            return true;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
    
    public async Task<List<User>> GetAllUsers()
    {
        var users = new List<User>();
        
        using var conn = GetConnection();
        await conn.OpenAsync();
        
        var cmd = new NpgsqlCommand(@"
            SELECT u.id, u.role_id, u.last_name, u.first_name, u.middle_name, 
                   u.login, u.password, ur.role
            FROM users u
            JOIN user_role ur ON u.role_id = ur.id
            ORDER BY u.last_name, u.first_name", conn);
        
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            users.Add(new User
            {
                Id = reader.GetInt32(0),
                RoleId = reader.GetInt32(1),
                LastName = reader.GetString(2),
                FirstName = reader.GetString(3),
                MiddleName = reader.IsDBNull(4) ? null : reader.GetString(4),
                Login = reader.GetString(5),
                Password = reader.GetString(6),
                RoleName = reader.GetString(7)
            });
        }
        
        return users;
    }
    
    public async Task<int> CreateOrder(Order order, List<OrderDetail> details)
    {
        using var conn = GetConnection();
        await conn.OpenAsync();
        
        using var transaction = await conn.BeginTransactionAsync();
        
        try
        {
            // Создаем заказ
            var orderCmd = new NpgsqlCommand(@"
                INSERT INTO orders (order_date, delivery_date, pickup_point_id, client_id, receipt_code, order_status, total_amount)
                VALUES (@order_date, @delivery_date, @pickup_point_id, @client_id, @receipt_code, @order_status, @total_amount)
                RETURNING order_number", conn, transaction);
            
            orderCmd.Parameters.AddWithValue("order_date", order.OrderDate);
            orderCmd.Parameters.AddWithValue("delivery_date", order.DeliveryDate);
            orderCmd.Parameters.AddWithValue("pickup_point_id", order.PickupPointId);
            orderCmd.Parameters.AddWithValue("client_id", order.ClientId);
            orderCmd.Parameters.AddWithValue("receipt_code", order.ReceiptCode);
            orderCmd.Parameters.AddWithValue("order_status", order.OrderStatus);
            orderCmd.Parameters.AddWithValue("total_amount", order.TotalAmount);
            
            var orderNumber = (int)(await orderCmd.ExecuteScalarAsync())!;
            
            // Создаем детали заказа
            foreach (var detail in details)
            {
                var detailCmd = new NpgsqlCommand(@"
                    INSERT INTO order_details (order_number, article, quantity, unit_price, discount, total_price)
                    VALUES (@order_number, @article, @quantity, @unit_price, @discount, @total_price)", conn, transaction);
                
                detailCmd.Parameters.AddWithValue("order_number", orderNumber);
                detailCmd.Parameters.AddWithValue("article", detail.Article);
                detailCmd.Parameters.AddWithValue("quantity", detail.Quantity);
                detailCmd.Parameters.AddWithValue("unit_price", detail.UnitPrice);
                detailCmd.Parameters.AddWithValue("discount", detail.Discount);
                detailCmd.Parameters.AddWithValue("total_price", detail.TotalPrice);
                
                await detailCmd.ExecuteNonQueryAsync();
            }
            
            await transaction.CommitAsync();
            return orderNumber;
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }
    
    public async Task<List<ProductName>> GetAllProductNames()
    {
        var names = new List<ProductName>();
        
        using var conn = GetConnection();
        await conn.OpenAsync();
        
        var cmd = new NpgsqlCommand("SELECT id, product_name FROM product_name ORDER BY product_name", conn);
        
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            names.Add(new ProductName
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1)
            });
        }
        
        return names;
    }
    
    public async Task<int> CreateProductName(string name)
    {
        using var conn = GetConnection();
        await conn.OpenAsync();
        
        var cmd = new NpgsqlCommand("INSERT INTO product_name (product_name) VALUES (@name) RETURNING id", conn);
        cmd.Parameters.AddWithValue("name", name);
        
        return (int)(await cmd.ExecuteScalarAsync())!;
    }
    
    // Lookup methods
    public async Task<List<Category>> GetAllCategories()
    {
        var categories = new List<Category>();
        
        using var conn = GetConnection();
        await conn.OpenAsync();
        
        var cmd = new NpgsqlCommand("SELECT id, category_name FROM category ORDER BY category_name", conn);
        
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            categories.Add(new Category
            {
                Id = reader.GetInt32(0),
                CategoryName = reader.GetString(1)
            });
        }
        
        return categories;
    }
    
    public async Task<List<Manufacturer>> GetAllManufacturers()
    {
        var manufacturers = new List<Manufacturer>();
        
        using var conn = GetConnection();
        await conn.OpenAsync();
        
        var cmd = new NpgsqlCommand("SELECT id, manufacturer_name FROM manufacturer ORDER BY manufacturer_name", conn);
        
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            manufacturers.Add(new Manufacturer
            {
                Id = reader.GetInt32(0),
                ManufacturerName = reader.GetString(1)
            });
        }
        
        return manufacturers;
    }
    
    public async Task<List<Supplier>> GetAllSuppliers()
    {
        var suppliers = new List<Supplier>();
        
        using var conn = GetConnection();
        await conn.OpenAsync();
        
        var cmd = new NpgsqlCommand("SELECT id, supplier_name FROM supplier ORDER BY supplier_name", conn);
        
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            suppliers.Add(new Supplier
            {
                Id = reader.GetInt32(0),
                SupplierName = reader.GetString(1)
            });
        }
        
        return suppliers;
    }
    
    public async Task<List<PickupPoint>> GetAllPickupPoints()
    {
        var points = new List<PickupPoint>();
        
        using var conn = GetConnection();
        await conn.OpenAsync();
        
        var cmd = new NpgsqlCommand("SELECT id, point_code, city, street, house_number FROM pickup_points ORDER BY city, street", conn);
        
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            points.Add(new PickupPoint
            {
                Id = reader.GetInt32(0),
                PointCode = reader.GetString(1),
                City = reader.GetString(2),
                Street = reader.GetString(3),
                HouseNumber = reader.GetString(4)
            });
        }
        
        return points;
    }
    
    public async Task<List<UnitOfMeasure>> GetAllUnitOfMeasure()
    {
        var units = new List<UnitOfMeasure>();
        
        using var conn = GetConnection();
        await conn.OpenAsync();
        
        var cmd = new NpgsqlCommand("SELECT id, unit_name FROM unit_of_measure ORDER BY unit_name", conn);
        
        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            units.Add(new UnitOfMeasure
            {
                Id = reader.GetInt32(0),
                UnitName = reader.GetString(1)
            });
        }
        
        return units;
    }
}

