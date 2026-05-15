using FluentValidation;

namespace AuctionSystem.Application.Admin.Reports.GenerateAdminReport;

public sealed class GenerateAdminReportCommandValidator : AbstractValidator<GenerateAdminReportCommand>
{
    public GenerateAdminReportCommandValidator()
    {
        RuleFor(x => x.RequesterUserId)
            .NotEmpty();

        RuleFor(x => x.ReportType)
            .NotEmpty()
            .MaximumLength(64);

        RuleFor(x => x.RangeStartUtc)
            .LessThanOrEqualTo(x => x.RangeEndUtc)
            .WithMessage("Range start must be earlier than or equal to range end.");

        RuleFor(x => x)
            .Must(x => (x.RangeEndUtc - x.RangeStartUtc).TotalDays <= 366)
            .WithMessage("Report date range cannot exceed 366 days.");
    }
}
