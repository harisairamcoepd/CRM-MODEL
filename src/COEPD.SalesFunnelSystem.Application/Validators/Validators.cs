using COEPD.SalesFunnelSystem.Application.DTOs;
using COEPD.SalesFunnelSystem.Domain.Entities;
using FluentValidation;

namespace COEPD.SalesFunnelSystem.Application.Validators;

public class CreateLeadRequestValidator : AbstractValidator<CreateLeadRequest>
{
    public CreateLeadRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Phone).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(150);
        RuleFor(x => x.Location).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Domain).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Source)
            .MaximumLength(50)
            .Must(x =>
                string.IsNullOrWhiteSpace(x) ||
                string.Equals(x, LeadSources.Website, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(x, LeadSources.Chatbot, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(x, LeadSources.Ads, StringComparison.OrdinalIgnoreCase))
            .WithMessage("Source must be Website, Chatbot, or Ads.");
        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(x =>
                string.Equals(x, LeadStatuses.New, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(x, LeadStatuses.Contacted, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(x, LeadStatuses.DemoBooked, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(x, LeadStatuses.Converted, StringComparison.OrdinalIgnoreCase))
            .WithMessage("Status must be New, Contacted, DemoBooked, or Converted.");
        RuleFor(x => x.Score)
            .NotEmpty()
            .Must(x =>
                string.Equals(x, LeadScores.Hot, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(x, LeadScores.Warm, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(x, LeadScores.Cold, StringComparison.OrdinalIgnoreCase));
        RuleFor(x => x.Notes).MaximumLength(2000);
    }
}

public class CreateDemoBookingRequestValidator : AbstractValidator<CreateDemoBookingRequest>
{
    public CreateDemoBookingRequestValidator()
    {
        RuleFor(x => x.LeadId).NotNull().GreaterThan(0);
        RuleFor(x => x.Date).NotEmpty().MaximumLength(40);
        RuleFor(x => x.TimeSlot).NotEmpty().MaximumLength(40);
        RuleFor(x => x.Status)
            .Must(x =>
                string.IsNullOrWhiteSpace(x) ||
                string.Equals(x, DemoBookingStatuses.Pending, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(x, DemoBookingStatuses.Confirmed, StringComparison.OrdinalIgnoreCase));
    }
}

public class UpdateLeadStatusRequestValidator : AbstractValidator<UpdateLeadStatusRequest>
{
    public UpdateLeadStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(x =>
                string.Equals(x, LeadStatuses.New, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(x, LeadStatuses.Contacted, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(x, LeadStatuses.DemoBooked, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(x, LeadStatuses.Converted, StringComparison.OrdinalIgnoreCase))
            .WithMessage("Status must be New, Contacted, DemoBooked, or Converted.");
    }
}

public class UpdateDemoBookingStatusRequestValidator : AbstractValidator<UpdateDemoBookingStatusRequest>
{
    public UpdateDemoBookingStatusRequestValidator()
    {
        RuleFor(x => x.Status)
            .NotEmpty()
            .Must(x =>
                string.Equals(x, DemoBookingStatuses.Pending, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(x, DemoBookingStatuses.Confirmed, StringComparison.OrdinalIgnoreCase))
            .WithMessage("Status must be Pending or Confirmed.");
    }
}

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(150);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(128);
    }
}

public class CreateUserRequestValidator : AbstractValidator<CreateUserRequest>
{
    public CreateUserRequestValidator()
    {
        RuleFor(x => x.FullName).NotEmpty().MaximumLength(120);
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(150);
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8).MaximumLength(128);
        RuleFor(x => x.Role)
            .NotEmpty()
            .Must(x => x.Equals("Admin", StringComparison.OrdinalIgnoreCase) || x.Equals("Staff", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Role must be Admin or Staff.");
    }
}

public class UpdateUserRoleRequestValidator : AbstractValidator<UpdateUserRoleRequest>
{
    public UpdateUserRoleRequestValidator()
    {
        RuleFor(x => x.Role)
            .NotEmpty()
            .Must(x => x.Equals("Admin", StringComparison.OrdinalIgnoreCase) || x.Equals("Staff", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Role must be Admin or Staff.");
    }
}
