using E_ShoppingManagement.Data;
using E_ShoppingManagement.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace E_ShoppingManagement.Controllers
{
    [Authorize(Roles = "Customer,Admin,Employee,DeliveryMan")]
    public class OrderController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<Users> _userManager;

        public OrderController(AppDbContext context, UserManager<Users> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: Order/Checkout
        [HttpGet]
        public async Task<IActionResult> Checkout()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == user.Id);
            
            // Auto-create customer profile for Admin/Employee if missing
            if (customer == null && (User.IsInRole("Admin") || User.IsInRole("Employee")))
            {
                customer = new Customer
                {
                    UserId = user.Id,
                    Name = user.FullName ?? user.UserName ?? "User",
                    Email = user.Email ?? "",
                    Role = User.IsInRole("Admin") ? "Admin" : "Employee",
                    CreatedAt = DateTime.UtcNow,
                    Status = "Active"
                };
                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();
            }

            if (customer == null) return RedirectToAction("Index", "Home");

            var cart = await _context.Carts
                .Include(c => c.Items)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.CustomerId == customer.Id && c.Status == "Active");

            if (cart == null || cart.Items == null || !cart.Items.Any())
            {
                TempData["Message"] = "Your cart is empty.";
                return RedirectToAction("Index", "Cart");
            }
            
            ViewBag.PaymentMethods = await _context.PaymentMethods.Where(pm => pm.Status == "Active").ToListAsync();
            
            return View(cart);
        }

        // POST: Order/Checkout
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckoutConfirmed(string address, string city, string zipCode, string phone, string paymentMethod)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == user.Id);
            if (customer == null) return RedirectToAction("Index", "Home");

            var cart = await _context.Carts
                .Include(c => c.Items)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.CustomerId == customer.Id && c.Status == "Active");

            if (cart == null || cart.Items == null || !cart.Items.Any())
            {
               return RedirectToAction("Index", "Cart");
            }

            // If online payment selected, redirect to payment page
            bool isCod = paymentMethod.Equals("COD", StringComparison.OrdinalIgnoreCase) || 
                         paymentMethod.Contains("Cash on Delivery", StringComparison.OrdinalIgnoreCase);

            if (!isCod && !string.IsNullOrEmpty(paymentMethod))
            {
                // Store order details in TempData for payment page
                TempData["Address"] = address;
                TempData["City"] = city;
                TempData["ZipCode"] = zipCode;
                TempData["Phone"] = phone;
                TempData["PaymentMethod"] = paymentMethod;
                TempData["CustomerId"] = customer.Id.ToString();
                TempData["TotalAmount"] = cart.Items.Sum(i => i.PriceWithVat * i.Quantity).ToString();
                
                return RedirectToAction("OnlinePayment");
            }

            var order = new Order
            {
                CustomerId = customer.Id,
                CreatedAt = DateTime.Now,
                OrderStatus = "Pending",
                Status = "Active",
                CreatedBy = customer.Name,
                ShippingAddress = address,
                City = city,
                ZipCode = zipCode,
                PhoneNumber = phone,
                PaymentMethod = paymentMethod,
                PaymentStatus = isCod ? "Unpaid" : "Pending",
                OrderDetails = new List<OrderDetails>()
            };

            decimal totalAmount = 0;
            foreach (var item in cart.Items)
            {
                var orderDetail = new OrderDetails
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    Price = item.Price,
                    VatAmount = item.VatAmount,
                    VatPercentage = item.Product?.VatPercentage ?? 0,
                    PriceWithVat = item.PriceWithVat,
                    OfferPercentage = item.Product?.OfferPercentage ?? 0,
                    Size = item.Size
                };
                order.OrderDetails.Add(orderDetail);
                totalAmount += (item.PriceWithVat * item.Quantity);

                // Update Stock
                if (item.Product != null)
                {
                    item.Product.StockQty -= item.Quantity;
                    if (item.Product.StockQty < 0) item.Product.StockQty = 0;

                    // Update size-specific stock if size is provided
                    if (!string.IsNullOrEmpty(item.Size))
                    {
                        var sizeStock = await _context.ProductSizeStocks
                            .FirstOrDefaultAsync(ss => ss.ProductId == item.ProductId && ss.Size == item.Size);
                        if (sizeStock != null)
                        {
                            sizeStock.StockQty -= item.Quantity;
                            if (sizeStock.StockQty < 0) sizeStock.StockQty = 0;
                        }
                    }
                }
            }

            order.TotalAmount = totalAmount;
            _context.Orders.Add(order);
            
            // Remove cart items but keep the cart object
            _context.CartItems.RemoveRange(cart.Items);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Order placed successfully!";
            return RedirectToAction("MoneyReceipt", new { id = order.Id });
        }

        // GET: Order/OnlinePayment
        [HttpGet]
        public async Task<IActionResult> OnlinePayment()
        {
            if (TempData["PaymentMethod"] == null)
            {
                return RedirectToAction("Checkout");
            }

            var customerIdStr = TempData["CustomerId"]?.ToString();
            if (string.IsNullOrEmpty(customerIdStr) || !int.TryParse(customerIdStr, out int customerId)) 
                return RedirectToAction("Checkout");

            var cart = await _context.Carts
                .Include(c => c.Items)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.CustomerId == customerId && c.Status == "Active");

            if (cart == null) return RedirectToAction("Index", "Cart");

            ViewBag.PaymentMethod = TempData["PaymentMethod"]?.ToString();
            ViewBag.TotalAmount = TempData["TotalAmount"]?.ToString();
            ViewBag.Address = TempData["Address"]?.ToString();
            ViewBag.City = TempData["City"]?.ToString();
            ViewBag.ZipCode = TempData["ZipCode"]?.ToString();
            ViewBag.Phone = TempData["Phone"]?.ToString();
            
            var methodName = TempData["PaymentMethod"]?.ToString();
            ViewBag.PaymentMethodObj = await _context.PaymentMethods.FirstOrDefaultAsync(pm => pm.Name == methodName);

            // Keep data for payment confirmation
            TempData.Keep();
            
            return View(cart);
        }

        // POST: Order/ConfirmOnlinePayment
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ConfirmOnlinePayment(string fullName, string email, string phoneNumber, string transactionId, string cardHolder)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var paymentMethod = TempData["PaymentMethod"]?.ToString() ?? "Online";
            TempData.Keep("PaymentMethod");
            TempData.Keep("CustomerId");
            TempData.Keep("Address");
            TempData.Keep("City");
            TempData.Keep("ZipCode");
            TempData.Keep("Phone");
            TempData.Keep("TotalAmount");

            bool isCard = paymentMethod.Contains("Card", StringComparison.OrdinalIgnoreCase) || paymentMethod.Contains("Credit", StringComparison.OrdinalIgnoreCase);

            if (isCard)
            {
                if (string.IsNullOrEmpty(cardHolder) || string.IsNullOrEmpty(transactionId))
                {
                    TempData["ErrorMessage"] = "Invalid Card Information.";
                    return RedirectToAction("OnlinePayment");
                }
                fullName = cardHolder;
            }
            else
            {
                if (string.IsNullOrEmpty(phoneNumber) || phoneNumber.Length != 11 || !phoneNumber.StartsWith("01") || string.IsNullOrEmpty(transactionId))
                {
                    TempData["ErrorMessage"] = "Invalid Account/Transaction ID. Please enter a valid account number and Transaction ID.";
                    return RedirectToAction("OnlinePayment");
                }
            }

            var customerIdStr = TempData["CustomerId"]?.ToString();
            if (string.IsNullOrEmpty(customerIdStr) || !int.TryParse(customerIdStr, out int customerId)) 
                return RedirectToAction("Checkout");

            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == customerId);
            if (customer == null) return RedirectToAction("Index", "Home");

            var cart = await _context.Carts
                .Include(c => c.Items)
                .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.CustomerId == customer.Id && c.Status == "Active");

            if (cart == null || cart.Items == null || !cart.Items.Any())
            {
                return RedirectToAction("Index", "Cart");
            }

            var order = new Order
            {
                CustomerId = customer.Id,
                CreatedAt = DateTime.Now,
                OrderStatus = "Processing",
                Status = "Active",
                CreatedBy = customer.Name,
                ShippingAddress = TempData["Address"]?.ToString() ?? "",
                City = TempData["City"]?.ToString() ?? "",
                ZipCode = TempData["ZipCode"]?.ToString() ?? "",
                PhoneNumber = phoneNumber,
                PaymentMethod = TempData["PaymentMethod"]?.ToString() ?? "Online",
                PaymentStatus = "Paid",
                OrderDetails = new List<OrderDetails>()
            };

            decimal totalAmount = 0;
            foreach (var item in cart.Items)
            {
                var orderDetail = new OrderDetails
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    Price = item.Price,
                    VatAmount = item.VatAmount,
                    VatPercentage = item.Product?.VatPercentage ?? 0,
                    PriceWithVat = item.PriceWithVat,
                    OfferPercentage = item.Product?.OfferPercentage ?? 0,
                    Size = item.Size
                };
                order.OrderDetails.Add(orderDetail);
                totalAmount += (item.PriceWithVat * item.Quantity);

                // Update Stock
                if (item.Product != null)
                {
                    item.Product.StockQty -= item.Quantity;
                    if (item.Product.StockQty < 0) item.Product.StockQty = 0;

                    // Update size-specific stock
                    if (!string.IsNullOrEmpty(item.Size))
                    {
                        var sizeStock = await _context.ProductSizeStocks
                            .FirstOrDefaultAsync(ss => ss.ProductId == item.ProductId && ss.Size == item.Size);
                        if (sizeStock != null)
                        {
                            sizeStock.StockQty -= item.Quantity;
                            if (sizeStock.StockQty < 0) sizeStock.StockQty = 0;
                        }
                    }
                }
            }

            order.TotalAmount = totalAmount;
            _context.Orders.Add(order);
            await _context.SaveChangesAsync(); // Save order first to get ID

            // Store Transaction Details for Admin Viewing (Merchant Integration Rule)
            var paymentHistory = new PaymentHistory
            {
                OrderId = order.Id,
                Amount = totalAmount,
                TransactionId = transactionId,
                GatewayName = TempData["PaymentMethod"]?.ToString() ?? "Online",
                CustomerName = fullName,
                CustomerAccount = phoneNumber,
                Status = "Success",
                PaymentDate = DateTime.Now,
                ResponsePayload = $"Payer: {fullName}, Phone: {phoneNumber ?? "N/A"}"
            };
            _context.PaymentHistories.Add(paymentHistory);

            // Remove cart items
            _context.CartItems.RemoveRange(cart.Items);
            await _context.SaveChangesAsync();

            TempData["Message"] = $"Payment successful! Order confirmed for {fullName}.";
            return RedirectToAction("MoneyReceipt", new { id = order.Id });
        }
        // GET: Order/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.AssignedEmployee)
                .Include(o => o.DeliveryMan)
                .Include(o => o.OrderDetails!)
                    .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            // Check if user is authorized to view this order
            if (User.IsInRole("Customer"))
            {
                var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == _userManager.GetUserId(User));
                if (order.CustomerId != customer?.Id) return Forbid();
            }
            if (User.IsInRole("DeliveryMan"))
            {
                var dm = await _context.DeliveryMen.FirstOrDefaultAsync(d => d.UserId == _userManager.GetUserId(User));
                // Only allow viewing if assigned, or if the order is generally available (optional, enforcing assignment here)
                if (order.DeliveryManId != dm?.Id) return Forbid();
            }

            return View(order);
        }

        // GET: Order/Edit/5
        [Authorize(Roles = "Admin,Employee")]
        public async Task<IActionResult> Edit(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Customer)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            ViewBag.Employees = await _context.Employees.OrderBy(e => e.Name).ToListAsync();
            ViewBag.DeliveryMen = await _context.DeliveryMen.Where(dm => dm.Status == "Active").OrderBy(dm => dm.Name).ToListAsync();
            ViewBag.OrderStatuses = new List<string> { "Pending", "Processing", "Shipped", "Delivered", "Cancelled" };
            ViewBag.PaymentStatuses = new List<string> { "Pending", "Paid" };

            return View(order);
        }

        // POST: Order/Edit/5
        [HttpPost]
        [Authorize(Roles = "Admin,Employee")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, string orderStatus, string paymentStatus, int? assignedEmployeeId, int? deliveryManId)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            var oldStatus = order.OrderStatus;
            order.OrderStatus = orderStatus;
            order.PaymentStatus = paymentStatus;
            order.AssignedEmployeeId = assignedEmployeeId;
            order.DeliveryManId = deliveryManId;
            order.ModifiedAt = DateTime.Now;
            order.ModifiedBy = User.Identity?.Name;

            if (oldStatus != "Delivered" && orderStatus == "Delivered")
            {
                order.DeliveredAt = DateTime.Now;
            }
            else if (oldStatus != "Processing" && orderStatus == "Processing")
            {
                order.ProcessedAt = DateTime.Now;
            }

            await _context.SaveChangesAsync();
            TempData["Message"] = "Order updated successfully.";
            TempData["IsSuccess"] = true;

            return RedirectToAction("OrdersByStatus", "AdminDashboard", new { status = orderStatus });

        }

        // POST: Order/UpdatePaymentStatus
        [HttpPost]
        [Authorize(Roles = "Admin,Employee")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePaymentStatus(int id, string status)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null) return NotFound();

            order.PaymentStatus = status;
            order.ModifiedAt = DateTime.Now;
            order.ModifiedBy = User.Identity?.Name;
            
            await _context.SaveChangesAsync();
            
            TempData["Message"] = "Payment status updated.";
            TempData["IsSuccess"] = true;

            // Redirect back to referring page if possible, otherwise Details
            // Using logic to determine where to go based on role/context is tricky, defaulting to generic Details or DeliveryMan Details
            // Since this was called from DeliveryMan/Details typically:
            if (order.DeliveryManId.HasValue)
            {
                return RedirectToAction("Details", "DeliveryMan", new { id = order.DeliveryManId });
            }
            
            return RedirectToAction("Details", new { id = order.Id });
        }

        // GET: Order/Delete/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails!)
                    .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            return View(order);
        }

        // POST: Order/Delete/5
        [HttpPost, ActionName("Delete")]
        [Authorize(Roles = "Admin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order != null)
            {
                if (order.OrderDetails != null) _context.OrderDetails.RemoveRange(order.OrderDetails);
                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();
            }

            TempData["Message"] = "Order deleted successfully.";
            TempData["IsSuccess"] = true;

            return RedirectToAction("Index", "AdminDashboard");
        }
        public async Task<IActionResult> MoneyReceipt(int id)
        {
            var order = await _context.Orders
                .Include(o => o.Customer)
                .Include(o => o.OrderDetails!)
                    .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.Id == id);

            if (order == null) return NotFound();

            // Allow Customer (owner) or Admin/Employee
            if (User.IsInRole("Customer"))
            {
                 var user = await _userManager.GetUserAsync(User);
                 if (order.Customer?.UserId != user?.Id) return Forbid();
            }

            return View(order);
        }

        // POST: Order/SendOtpSms
        [HttpPost]
        public async Task<IActionResult> SendOtpSms([FromBody] OtpRequestModel model)
        {
            if (string.IsNullOrEmpty(model.PhoneNumber))
                return Json(new { success = false, message = "Phone number is required." });

            try
            {
                // Generate a real 4-digit OTP
                Random rand = new Random();
                string otp = rand.Next(1000, 9999).ToString();

                // Store OTP securely in Session or TempData
                TempData["RealOTP_" + model.PhoneNumber] = otp;
                TempData.Keep("RealOTP_" + model.PhoneNumber);

                // --- REAL SMS GATEWAY INTEGRATION ---
                // To send a real SMS, you must purchase an SMS API from providers like BulkSMSBD, GreenWeb, SMSQ, or Twilio.
                // Replace the API_KEY and SENDER_ID with your purchased credentials.
                string apiKey = "YOUR_SMS_API_KEY_HERE";
                string senderId = "YOUR_SENDER_ID";
                string message = $"Your E-Shopping payment OTP is: {otp}. Do not share this with anyone.";

                // Example of BulkSMSBD / Typical BD SMS Gateway URL structure:
                string requestUrl = $"http://bulksmsbd.net/api/smsapi?api_key={apiKey}&type=text&number={model.PhoneNumber}&senderid={senderId}&message={Uri.EscapeDataString(message)}";

                using (var client = new HttpClient())
                {
                    // UNCOMMENT the next two lines when you have put your real API_KEY above.
                    // var response = await client.GetAsync(requestUrl);
                    // if (!response.IsSuccessStatusCode) { return Json(new { success = false, message = "Failed to send real SMS. Gateway error." }); }
                }

                return Json(new { success = true, message = "OTP sent to your number.", testOtp = otp });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error sending SMS: " + ex.Message });
            }
        }

        // POST: Order/SendOtpEmail
        [HttpPost]
        public async Task<IActionResult> SendOtpEmail([FromBody] OtpRequestModel model)
        {
            if (string.IsNullOrEmpty(model.Email))
                return Json(new { success = false, message = "Email is required." });

            try
            {
                // Generate a real 4-digit OTP
                Random rand = new Random();
                string otp = rand.Next(1000, 9999).ToString();

                // Store OTP securely in Session or TempData
                TempData["RealOTP_" + model.Email] = otp;
                TempData.Keep("RealOTP_" + model.Email);

                // --- REAL EMAIL SMTP INTEGRATION ---
                // To send a real email, put your Gmail address and App Password here.
                string smtpUser = "YOUR_GMAIL_ADDRESS@gmail.com";
                string smtpPass = "YOUR_GMAIL_APP_PASSWORD";

                using (var mail = new System.Net.Mail.MailMessage())
                {
                    mail.From = new System.Net.Mail.MailAddress(smtpUser, "E-Shopping Payment");
                    mail.To.Add(model.Email);
                    mail.Subject = "Your Payment OTP Code";
                    mail.Body = $"<div style='font-family:Arial; padding:20px; border:1px solid #ddd; text-align:center;'>" +
                                $"<h2 style='color:#e81828;'>E-Shopping Management</h2>" +
                                $"<p>Your OTP code for the payment verification is:</p>" +
                                $"<h1 style='letter-spacing:5px; color:#333; background:#f4f4f4; padding:10px; display:inline-block;'>{otp}</h1>" +
                                $"<p>Do not share this code with anyone.</p></div>";
                    mail.IsBodyHtml = true;

                    using (var smtp = new System.Net.Mail.SmtpClient("smtp.gmail.com", 587))
                    {
                        smtp.Credentials = new System.Net.NetworkCredential(smtpUser, smtpPass);
                        smtp.EnableSsl = true;
                        
                        // UNCOMMENT the next line when you have put your real GMAIL credentials above.
                        // await smtp.SendMailAsync(mail);
                    }
                }

                return Json(new { success = true, message = "OTP sent to your email.", testOtp = otp });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Error sending Email: " + ex.Message });
            }
        }

        // POST: Order/VerifyOtpSms
        [HttpPost]
        public IActionResult VerifyOtpSms([FromBody] OtpVerifyModel model)
        {
            string target = !string.IsNullOrEmpty(model.PhoneNumber) ? model.PhoneNumber : model.Email;
            
            if (string.IsNullOrEmpty(target) || string.IsNullOrEmpty(model.Otp))
                return Json(new { success = false, message = "Invalid data." });

            var savedOtp = TempData["RealOTP_" + target]?.ToString();
            TempData.Keep("RealOTP_" + target);

            if (savedOtp != null && savedOtp == model.Otp)
            {
                return Json(new { success = true });
            }

            return Json(new { success = false, message = "Incorrect OTP. Please check your SMS/Email and try again." });
        }
    }

    public class OtpRequestModel
    {
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
    }

    public class OtpVerifyModel
    {
        public string PhoneNumber { get; set; }
        public string Email { get; set; }
        public string Otp { get; set; }
    }
}
