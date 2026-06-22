// using in_ctech_management_backend.Domain.Resources;
// using in_ctech_management_backend.Domain.Invoices.DomainEvents;
// using in_ctech_management_backend.Domain.Invoices.Exceptions;
// using in_ctech_management_backend.Domain.Invoices.Policies;

// namespace in_ctech_management_backend.Domain.Invoices
// {
//     public class Invoice : Entity
//     {
//         public InvoiceId InvoiceId { get; private set; }
//         public string Reference { get; private set; }
//         public ResourceId ResourceId { get; private set; }

//         public DateTime InvoiceDate { get; private set; }

//         private readonly List<InvoiceItem> _invoiceItems;
//         public IReadOnlyCollection<InvoiceItem> InvoiceItems => _invoiceItems.AsReadOnly();

//         public Money TotalAmount { get; internal set; } = new Money(0);
//         public Discount Discount { get; internal set; }
//         private Invoice()
//         {

//         }

//         public static Invoice Create(
//             ResourceId customerId,
//             ShippingAddress shippingAddress,
//             DateTime orderDate)
//         {
//             return new Invoice(customerId, shippingAddress, orderDate);
//         }

//         private Invoice(ResourceId customerId, ShippingAddress shippingAddress, DateTime orderDate)
//         {
//             ResourceId = customerId;
//             InvoiceId = new InvoiceId(Guid.NewGuid());
//             ShippingAddress = shippingAddress;
//             InvoiceDate = orderDate;

//             _orderItems = new List<InvoiceItem>();

//             AddDomainEvent(new InvoiceCreatedDomainEvent(this.InvoiceId.Value, this.ResourceId.Value));
//         }

//     }
// }
