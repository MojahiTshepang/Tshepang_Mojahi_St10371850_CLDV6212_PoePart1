using Microsoft.AspNetCore.Mvc;
using ABCRetailers.Models;
using ABCRetailers.Models.ViewModels;
using ABCRetailers.Services;
using System.Text.Json;

namespace ABCRetailers.Controllers
{
    public class OrderController : Controller
    {
        private readonly IAzureStorageService _storageService;

        public OrderController(IAzureStorageService storageService)
        {
            _storageService = storageService;
        }

        // GET: /Order/
        public async Task<IActionResult> Index()
        {
            var orders = await _storageService.GetAllEntitiesAsync<Order>();
            return View(orders);
        }

        // GET: /Order/Create
        public async Task<IActionResult> Create()
        {
            var viewModel = new OrderCreateViewModel
            {
                Customers = await _storageService.GetAllEntitiesAsync<Customer>(),
                Products = await _storageService.GetAllEntitiesAsync<Product>()
            };
            return View(viewModel);
        }

        // POST: /Order/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(OrderCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(model);
                return View(model);
            }

            try
            {
                var customer = await _storageService.GetEntityAsync<Customer>("Customer", model.CustomerId);
                var product = await _storageService.GetEntityAsync<Product>("Product", model.ProductId);

                if (customer == null || product == null)
                {
                    ModelState.AddModelError("", "Invalid customer or product selected.");
                    await PopulateDropdowns(model);
                    return View(model);
                }

                if (product.StockAvailable < model.Quantity)
                {
                    ModelState.AddModelError("Quantity", $"Insufficient stock. Available: {product.StockAvailable}");
                    await PopulateDropdowns(model);
                    return View(model);
                }

                // Capture stock before update
                var previousStock = product.StockAvailable;

                var order = new Order
                {
                    CustomerId = model.CustomerId,
                    Username = $"{customer.Name} {customer.Surname}", // Full name for display
                    ProductId = model.ProductId,
                    ProductName = product.ProductName,
                    OrderDate = model.OrderDate.DateTime,
                    Quantity = model.Quantity,
                    UnitPrice = product.Price,
                    TotalPrice = product.Price * model.Quantity,
                    Status = model.Status // 👈 use the value from ViewModel
                };

                await _storageService.AddEntityAsync(order);

                // Update stock
                product.StockAvailable -= model.Quantity;
                await _storageService.UpdateEntityAsync(product);

                // Send order notification
                var orderMessage = new
                {
                    order.OrderId,
                    order.CustomerId,
                    CustomerName = $"{customer.Name} {customer.Surname}",
                    order.ProductName,
                    order.Quantity,
                    order.TotalPrice,
                    order.OrderDate,
                    order.Status
                };
                await _storageService.SendMessageAsync("order-notifications", JsonSerializer.Serialize(orderMessage));

                // Send stock update notification
                var stockMessage = new
                {
                    product.ProductId,
                    product.ProductName,
                    PreviousStock = previousStock,
                    NewStock = product.StockAvailable,
                    UpdatedBy = "Order System",
                    UpdateDate = DateTime.UtcNow
                };
                await _storageService.SendMessageAsync("stock-updates", JsonSerializer.Serialize(stockMessage));

                TempData["Success"] = "Order created successfully!";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Error creating order: {ex.Message}");
            }

            await PopulateDropdowns(model);
            return View(model);
        }

        // GET: /Order/Details/{id}
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();
            var order = await _storageService.GetEntityAsync<Order>("Order", id);
            if (order == null) return NotFound();
            return View(order);
        }

        // GET: /Order/Edit/{id}
        public async Task<IActionResult> Edit(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();
            var order = await _storageService.GetEntityAsync<Order>("Order", id);
            if (order == null) return NotFound();
            return View(order);
        }

        // POST: /Order/Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Order order)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    await _storageService.UpdateEntityAsync(order);
                    TempData["Success"] = "Order updated successfully!";
                    return RedirectToAction(nameof(Index));
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"Error updating order: {ex.Message}");
                }
            }
            return View(order);
        }

        // POST: /Order/Delete/{id}
        [HttpPost]
        public async Task<IActionResult> Delete(string id)
        {
            try
            {
                await _storageService.DeleteEntityAsync<Order>("Order", id);
                TempData["Success"] = "Order deleted successfully!";
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Error deleting order: {ex.Message}";
            }
            return RedirectToAction(nameof(Index));
        }

        // GET: /Order/GetProductPrice?productId=xxx
        [HttpGet]
        public async Task<JsonResult> GetProductPrice(string productId)
        {
            try
            {
                var product = await _storageService.GetEntityAsync<Product>("Product", productId);
                if (product != null)
                {
                    return Json(new
                    {
                        success = true,
                        price = product.Price,
                        stock = product.StockAvailable,
                        productName = product.ProductName
                    });
                }
                return Json(new { success = false, message = "Product not found" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // POST: /Order/UpdateOrderStatus
        [HttpPost]
        public async Task<IActionResult> UpdateOrderStatus(string id, string newStatus)
        {
            try
            {
                var order = await _storageService.GetEntityAsync<Order>("Order", id);
                if (order == null)
                    return Json(new { success = false, message = "Order not found" });

                var previousStatus = order.Status;
                order.Status = newStatus;
                await _storageService.UpdateEntityAsync(order);

                var statusMessage = new
                {
                    order.OrderId,
                    order.CustomerId,
                    CustomerName = order.Username,
                    order.ProductName,
                    PreviousStatus = previousStatus,
                    NewStatus = newStatus,
                    UpdatedDate = DateTime.UtcNow,
                    UpdatedBy = "System"
                };

                await _storageService.SendMessageAsync("order-notifications", JsonSerializer.Serialize(statusMessage));

                return Json(new { success = true, message = $"Order status updated to {newStatus}" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // Helper to populate dropdowns
        private async Task PopulateDropdowns(OrderCreateViewModel model)
        {
            model.Customers = await _storageService.GetAllEntitiesAsync<Customer>();
            model.Products = await _storageService.GetAllEntitiesAsync<Product>();
        }
    }
}
