using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SCM_System.Data;
using SCM_System.Models.Entities;
using SCM_System.Models.ViewModels;
using SCM_System.Services;

namespace SCM_System.Controllers
{
    public class MessageController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IContactDetectionService _contactDetectionService;
        private readonly IPenaltyService _penaltyService;
        private readonly INotificationService _notificationService;

        public MessageController(
            INotificationService notificationService,
            ApplicationDbContext context,
            IContactDetectionService contactDetectionService,
            IPenaltyService penaltyService)
        {
            _notificationService = notificationService;
            _context = context;
            _contactDetectionService = contactDetectionService;
            _penaltyService = penaltyService;
        }

        // Helper methods
        private int GetCurrentUserId()
        {
            return HttpContext.Session.GetInt32("UserId") ?? 0;
        }

        private string GetCurrentUserRole()
        {
            return HttpContext.Session.GetString("UserRole") ?? "";
        }

        // GET: /Message/Inbox
        public async Task<IActionResult> Inbox()
        {
            int currentUserId = GetCurrentUserId();
            if (currentUserId == 0)
                return RedirectToAction("Login", "Account");

            string currentUserRole = GetCurrentUserRole();
            var conversations = await GetUserConversations(currentUserId, currentUserRole);

            // ✅ ADD THIS: Get active penalty count for warning banner
            ViewBag.ActivePenalties = await _context.Penalties
                .CountAsync(p => p.UserId == currentUserId &&
                                p.IsActive &&
                                (p.ExpiresAt == null || p.ExpiresAt > DateTime.Now));

            return View(conversations.ToList());
        }

        // GET: /Message/Conversation/{id}
        public async Task<IActionResult> Conversation(int id) // id = other user's ID
        {
            int currentUserId = GetCurrentUserId();
            if (currentUserId == 0)
                return RedirectToAction("Login", "Account");

            string currentUserRole = GetCurrentUserRole();

            // Check if user can send messages
            ViewBag.CanSendMessage = await _penaltyService.CanSendMessage(currentUserId);
            ViewBag.PenaltyLevel = await _penaltyService.GetViolationCount(currentUserId) >= 3 ? "Restricted" : null;

            // ✅ ADD THIS: Get active penalty details for display
            var activePenalty = await _context.Penalties
                .Where(p => p.UserId == currentUserId &&
                           p.IsActive &&
                           (p.ExpiresAt == null || p.ExpiresAt > DateTime.Now))
                .OrderByDescending(p => p.PenaltyType)
                .FirstOrDefaultAsync();

            if (activePenalty != null)
            {
                if (activePenalty.PenaltyType == "Message Restriction")
                {
                    ViewBag.PenaltyLevel = "Restricted";
                    ViewBag.PenaltyExpiry = activePenalty.ExpiresAt?.ToString("MMM dd, yyyy");
                    ViewBag.PenaltyMessage = $"You can only send 1 message per day until {ViewBag.PenaltyExpiry}";
                }
                else if (activePenalty.PenaltyType == "Account Suspension")
                {
                    ViewBag.PenaltyLevel = "Suspended";
                    ViewBag.PenaltyExpiry = activePenalty.ExpiresAt?.ToString("MMM dd, yyyy");
                    ViewBag.PenaltyMessage = $"Your account is suspended until {ViewBag.PenaltyExpiry}";
                }
            }

            // Find or create conversation
            var conversation = await GetOrCreateConversation(currentUserId, currentUserRole, id);

            if (conversation == null)
            {
                TempData["ErrorMessage"] = "Cannot start conversation: Missing Retailer or Supplier profile for the involved users.";
                return RedirectToAction("Inbox");
            }

            // Get messages for this conversation
            var messages = await _context.Messages
                .Include(m => m.Sender)
                .Where(m => m.ConversationId == conversation.Id)
                .OrderByDescending(m => m.CreatedAt)
                .Select(m => new MessageViewModel
                {
                    Id = m.Id,
                    SenderId = m.SenderId,
                    SenderName = m.Sender.FullName,
                    SenderRole = m.Sender.Role,
                    Content = m.MessageText,
                    CreatedAt = m.CreatedAt,
                    IsRead = m.IsRead,
                    IsBlocked = m.IsBlocked,
                    BlockedReason = m.BlockedReason,
                    TriggeredPenalty = m.TriggeredPenalty,
                    IsFromCurrentUser = m.SenderId == currentUserId
                })
                .ToListAsync();

            // Mark messages as read
            await MarkMessagesAsRead(conversation.Id, currentUserId);

            // Get other user info
            var otherUser = await _context.Users.FindAsync(id);
            ViewBag.OtherUserId = id;
            ViewBag.OtherUserName = otherUser?.FullName ?? $"User {id}";
            ViewBag.OtherUserRole = otherUser?.Role ?? "User";
            ViewBag.ConversationId = conversation.Id;

            return View(messages.OrderBy(m => m.CreatedAt).ToList());
        }

        // POST: /Message/Send
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(int receiverId, string content, int conversationId)
        {
            int currentUserId = GetCurrentUserId();
            if (currentUserId == 0)
            {
                TempData["ErrorMessage"] = "Please log in to send messages.";
                return RedirectToAction("Login", "Account");
            }

            string currentUserRole = GetCurrentUserRole();

            // Check if user can send messages (penalty system)
            if (!await _penaltyService.CanSendMessage(currentUserId))
            {
                TempData["ErrorMessage"] = "Your account is restricted from sending messages due to previous violations.";
                return RedirectToAction("Conversation", new { id = receiverId });
            }

            // Validate input
            if (string.IsNullOrWhiteSpace(content))
            {
                TempData["ErrorMessage"] = "Message cannot be empty.";
                return RedirectToAction("Conversation", new { id = receiverId });
            }

            // Verify the conversation exists and the user is part of it
            var conversation = await _context.Conversations
                .FirstOrDefaultAsync(c => c.Id == conversationId);

            if (conversation == null)
            {
                TempData["ErrorMessage"] = "Conversation not found.";
                return RedirectToAction("Conversation", new { id = receiverId });
            }

            // Verify the current user is part of this conversation
            if (currentUserRole == "Supplier")
            {
                var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.UserId == currentUserId);
                if (supplier == null || conversation.SupplierId != supplier.Id)
                {
                    TempData["ErrorMessage"] = "You are not authorized to send messages in this conversation.";
                    return RedirectToAction("Conversation", new { id = receiverId });
                }
            }
            else // Retailer
            {
                var retailer = await _context.Retailers.FirstOrDefaultAsync(r => r.UserId == currentUserId);
                if (retailer == null || conversation.RetailerId != retailer.Id)
                {
                    TempData["ErrorMessage"] = "You are not authorized to send messages in this conversation.";
                    return RedirectToAction("Conversation", new { id = receiverId });
                }
            }

            // Detect contact information
            var detectionResult = _contactDetectionService.DetectContactInfo(content);

            // Create the message WITH blocking fields
            var message = new Message
            {
                SenderId = currentUserId,
                ConversationId = conversationId,
                MessageText = content,
                CreatedAt = DateTime.Now,
                IsRead = false,
                IsBlocked = detectionResult.HasContactInfo,
                BlockedReason = detectionResult.HasContactInfo ? detectionResult.BlockedReason : null,
                BlockedAt = detectionResult.HasContactInfo ? DateTime.Now : null,
                TriggeredPenalty = detectionResult.HasContactInfo
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            // If contact info detected, record violation
            if (detectionResult.HasContactInfo)
            {
                // Record violation in penalty system
                var penalty = await _penaltyService.RecordViolation(
                    currentUserId,
                    currentUserRole,
                    detectionResult.DetectedPatterns.FirstOrDefault() ?? "Contact Info",
                    $"Attempted to share: {detectionResult.BlockedReason}",
                    message.Id
                );

                // ✅ Send notification to the user about the penalty
                await _notificationService.SendPenaltyNotificationAsync(
                    currentUserId,
                    penalty.PenaltyType,
                    detectionResult.BlockedReason,
                    penalty.Id
                );

                // Create violation record
                var violation = new MessageViolation
                {
                    MessageId = message.Id,
                    ViolationType = string.Join(", ", detectionResult.DetectedPatterns),
                    CreatedAt = DateTime.Now,
                    IsResolved = false
                };
                _context.MessageViolations.Add(violation);
                await _context.SaveChangesAsync();

                TempData["WarningMessage"] = GetWarningMessage(detectionResult.DetectedPatterns);
            }
            else
            {
                // ✅ Send notification to the receiver about new message
                await _notificationService.SendMessageNotificationAsync(receiverId, currentUserId, conversationId);
                TempData["SuccessMessage"] = "Message sent successfully!";
            }

            // Update conversation's last message time
            conversation.LastMessageAt = DateTime.Now;
            await _context.SaveChangesAsync();

            return RedirectToAction("Conversation", new { id = receiverId });
        }

        // GET: /Message/Sent
        public async Task<IActionResult> Sent()
        {
            int currentUserId = GetCurrentUserId();
            if (currentUserId == 0)
                return RedirectToAction("Login", "Account");

            var sentMessages = await _context.Messages
                .Include(m => m.Sender)
                .Include(m => m.Conversation)
                .Where(m => m.SenderId == currentUserId)
                .OrderByDescending(m => m.CreatedAt)
                .Select(m => new MessageViewModel
                {
                    Id = m.Id,
                    SenderId = m.SenderId,
                    SenderName = m.Sender.FullName,
                    SenderRole = m.Sender.Role,
                    Content = m.MessageText,
                    CreatedAt = m.CreatedAt,
                    IsRead = m.IsRead,
                    IsFromCurrentUser = true,
                    ConversationId = m.ConversationId,
                    ReceiverName = m.Conversation.RetailerId == currentUserId ?
                        (m.Conversation.Supplier != null ? m.Conversation.Supplier.User.FullName : "Unknown") :
                        (m.Conversation.Retailer != null ? m.Conversation.Retailer.User.FullName : "Unknown")
                })
                .ToListAsync();

            return View(sentMessages);
        }

        // GET: /Message/SelectRecipient
        public async Task<IActionResult> SelectRecipient()
        {
            int currentUserId = GetCurrentUserId();
            if (currentUserId == 0)
                return RedirectToAction("Login", "Account");

            string currentUserRole = GetCurrentUserRole();

            if (currentUserRole == "Supplier")
            {
                // Get all approved retailers for supplier to message
                var retailers = await _context.Retailers
                    .Include(r => r.User)
                    .Where(r => r.UserId != currentUserId && r.IsVerified && r.User.IsApproved)
                    .OrderBy(r => r.BusinessName)
                    .Select(r => new SelectRecipientViewModel
                    {
                        UserId = r.UserId,
                        BusinessName = r.BusinessName,
                        ContactPerson = r.User.FullName,
                        City = r.City,
                        IsVerified = r.IsVerified
                    })
                    .ToListAsync();

                ViewBag.Role = "Retailer";
                ViewBag.Title = "Select a Retailer";
                return View(retailers);
            }
            else // Retailer
            {
                // Get all verified suppliers for retailer to message
                var suppliers = await _context.Suppliers
                    .Include(s => s.User)
                    .Where(s => s.UserId != currentUserId && s.VerificationStatus == "Verified" && s.User.IsApproved)
                    .OrderBy(s => s.CompanyName)
                    .Select(s => new SelectRecipientViewModel
                    {
                        UserId = s.UserId,
                        BusinessName = s.CompanyName,
                        ContactPerson = s.User.FullName,
                        City = s.City,
                        IsVerified = s.VerificationStatus == "Verified"
                    })
                    .ToListAsync();

                ViewBag.Role = "Supplier";
                ViewBag.Title = "Select a Supplier";
                return View(suppliers);
            }
        }

        // GET: /Message/SendMessage/{receiverId}
        public async Task<IActionResult> SendMessage(int receiverId)
        {
            int currentUserId = GetCurrentUserId();
            if (currentUserId == 0)
                return RedirectToAction("Login", "Account");

            string currentUserRole = GetCurrentUserRole();

            // Get or create conversation
            var conversation = await GetOrCreateConversation(currentUserId, currentUserRole, receiverId);

            if (conversation == null)
            {
                TempData["ErrorMessage"] = "Cannot start conversation: Missing Retailer or Supplier profile for the involved users.";
                return RedirectToAction("Inbox");
            }

            // Get receiver info
            var receiver = await _context.Users.FindAsync(receiverId);
            if (receiver == null)
                return NotFound();

            var model = new SendMessageViewModel
            {
                ReceiverId = receiverId,
                ReceiverName = receiver.FullName,
                ConversationId = conversation.Id
            };

            return View(model);
        }

        // GET: /Message/GetUnreadCount
        [HttpGet]
        public async Task<IActionResult> GetUnreadCount()
        {
            int currentUserId = GetCurrentUserId();
            if (currentUserId == 0)
                return Json(0);

            var unreadCount = await _context.Messages
                .Where(m => (m.Conversation.RetailerId == currentUserId ||
                            m.Conversation.SupplierId == currentUserId) &&
                            m.SenderId != currentUserId &&
                            !m.IsRead)
                .CountAsync();

            return Json(unreadCount);
        }

        // GET: /Message/GetRecentConversations
        [HttpGet]
        public async Task<IActionResult> GetRecentConversations()
        {
            int currentUserId = GetCurrentUserId();
            string currentUserRole = GetCurrentUserRole();

            var conversations = await GetUserConversations(currentUserId, currentUserRole);
            var recent = conversations.Take(5).ToList();

            return Json(recent);
        }

        // Helper Methods
        private string GetWarningMessage(List<string> detectedPatterns)
        {
            if (detectedPatterns.Contains("Email"))
                return "⚠️ Email addresses are not allowed. Your message has been blocked.";
            if (detectedPatterns.Contains("EthiopianPhone"))
                return "⚠️ Phone numbers are not allowed. Your message has been blocked.";
            if (detectedPatterns.Contains("Telegram"))
                return "⚠️ Telegram contacts are not allowed. Your message has been blocked.";
            if (detectedPatterns.Contains("WhatsApp"))
                return "⚠️ WhatsApp contacts are not allowed. Your message has been blocked.";
            if (detectedPatterns.Contains("SocialMedia"))
                return "⚠️ Social media contacts are not allowed. Your message has been blocked.";
            return "⚠️ Sharing contact information is not allowed. Your message has been blocked.";
        }

        private async Task<Conversation> GetOrCreateConversation(int currentUserId, string currentUserRole, int otherUserId)
        {
            // Get the IDs of the entities involved
            var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.UserId == currentUserId);
            var retailer = await _context.Retailers.FirstOrDefaultAsync(r => r.UserId == currentUserId);

            // If we can't find the current user's profile, try finding the other user's profile
            var otherSupplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.UserId == otherUserId);
            var otherRetailer = await _context.Retailers.FirstOrDefaultAsync(r => r.UserId == otherUserId);

            int supplierId = 0;
            int retailerId = 0;

            if (supplier != null && otherRetailer != null)
            {
                supplierId = supplier.Id;
                retailerId = otherRetailer.Id;
            }
            else if (retailer != null && otherSupplier != null)
            {
                retailerId = retailer.Id;
                supplierId = otherSupplier.Id;
            }
            else
            {
                // Fallback to role-based lookup if one is missing but roles suggest intent
                if (currentUserRole == "Supplier" && supplier != null)
                {
                    supplierId = supplier.Id;
                    if (otherRetailer != null) retailerId = otherRetailer.Id;
                }
                else if (currentUserRole == "Retailer" && retailer != null)
                {
                    retailerId = retailer.Id;
                    if (otherSupplier != null) supplierId = otherSupplier.Id;
                }
            }

            if (supplierId == 0 || retailerId == 0)
            {
                // Return null instead of throwing an exception to prevent application crashes
                return null;
            }

            var conversation = await _context.Conversations
                .FirstOrDefaultAsync(c => c.SupplierId == supplierId && c.RetailerId == retailerId);

            if (conversation == null)
            {
                conversation = new Conversation
                {
                    SupplierId = supplierId,
                    RetailerId = retailerId,
                    CreatedAt = DateTime.Now,
                    LastMessageAt = null
                };

                _context.Conversations.Add(conversation);
                await _context.SaveChangesAsync();
            }

            return conversation;
        }

        private async Task<List<ConversationViewModel>> GetUserConversations(int userId, string userRole)
        {
            IQueryable<Conversation> query;

            if (userRole == "Supplier")
            {
                // First get the Supplier ID from UserId
                var supplier = await _context.Suppliers.FirstOrDefaultAsync(s => s.UserId == userId);
                if (supplier == null) return new List<ConversationViewModel>();

                query = _context.Conversations
                    .Include(c => c.Retailer)
                        .ThenInclude(r => r.User)
                    .Where(c => c.SupplierId == supplier.Id);
            }
            else
            {
                // First get the Retailer ID from UserId
                var retailer = await _context.Retailers.FirstOrDefaultAsync(r => r.UserId == userId);
                if (retailer == null) return new List<ConversationViewModel>();

                query = _context.Conversations
                    .Include(c => c.Supplier)
                        .ThenInclude(s => s.User)
                    .Where(c => c.RetailerId == retailer.Id);
            }

            var conversations = await query
                .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
                .Select(c => new ConversationViewModel
                {
                    Id = c.Id,
                    OtherUserId = userRole == "Supplier" ? c.Retailer.UserId : c.Supplier.UserId,
                    OtherUserName = userRole == "Supplier" ?
                        (c.Retailer != null ? c.Retailer.User.FullName : $"Retailer {c.RetailerId}") :
                        (c.Supplier != null ? c.Supplier.User.FullName : $"Supplier {c.SupplierId}"),
                    OtherUserType = userRole == "Supplier" ? "Retailer" : "Supplier",
                    LastMessage = c.Messages
                        .OrderByDescending(m => m.CreatedAt)
                        .Select(m => m.MessageText)
                        .FirstOrDefault() ?? "No messages yet",
                    LastMessageAt = c.LastMessageAt ?? c.CreatedAt,
                    UnreadCount = c.Messages.Count(m => m.SenderId != userId && !m.IsRead),
                    HasBlockedMessages = false,
                    IsActive = true
                })
                .ToListAsync();

            return conversations;
        }

        private async Task MarkMessagesAsRead(int conversationId, int userId)
        {
            var unreadMessages = await _context.Messages
                .Where(m => m.ConversationId == conversationId &&
                           m.SenderId != userId &&
                           !m.IsRead)
                .ToListAsync();

            foreach (var message in unreadMessages)
            {
                message.IsRead = true;
            }

            await _context.SaveChangesAsync();
        }


    }
}