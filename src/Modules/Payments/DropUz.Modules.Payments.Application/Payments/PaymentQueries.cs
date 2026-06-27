using DropUz.Common.Application.Messaging;
using DropUz.Common.Application.Pagination;

namespace DropUz.Modules.Payments.Application.Payments;

public sealed record GetMyPaymentsQuery(PageRequest Page) : IQuery<PagedResponse<PaymentResponse>>;

public sealed record GetAdminPaymentsQuery(PageRequest Page) : IQuery<PagedResponse<PaymentResponse>>;
