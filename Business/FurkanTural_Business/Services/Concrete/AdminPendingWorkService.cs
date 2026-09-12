using FurkanTural_Application.DTOs.Common;
using FurkanTural_Application.Repositories.Abstract;
using FurkanTural_Application.Services.Abstract;
using FurkanTural_Domain.Constants;

namespace FurkanTural_Business.Services.Concrete;

public class AdminPendingWorkService(IUnitOfWork unitOfWork) : IAdminPendingWorkService
{
    private readonly IUnitOfWork _unitOfWork = unitOfWork;

    public async Task<AdminPendingWorkDto> GetAsync(string kind, CancellationToken cancellationToken = default)
    {
        var comments = await _unitOfWork.Comments.CountForAdminAsync(
            x => !x.IsDeleted && x.Status == CommentStatuses.Pending, cancellationToken);

        var contacts = await _unitOfWork.Contacts.CountForAdminAsync(
            x => !x.IsDeleted && !x.IsRead, cancellationToken);

        var reports = await _unitOfWork.Reports.CountForAdminAsync(
            x => !x.IsDeleted && x.Status == ReportDefinitions.Statuses.Pending, cancellationToken);

        return new AdminPendingWorkDto(kind, comments, contacts, reports);
    }
}
