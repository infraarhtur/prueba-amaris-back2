using TechnicalTest.Domain.Exceptions;

namespace TechnicalTest.Domain.Entities;

public class Schedule
{
    private Schedule()
    {
        AppointmentDate = DateTime.UtcNow;
    }

    public Schedule(int bankBranchId, Guid clientId, DateTime appointmentDate)
    {
        UpdateBankBranch(bankBranchId);
        UpdateClient(clientId);
        UpdateAppointmentDate(appointmentDate);
    }

    public int Id { get; private set; }
    public int BankBranchId { get; private set; }
    public Guid ClientId { get; private set; }
    public DateTime AppointmentDate { get; private set; }

    public void Update(int bankBranchId, Guid clientId, DateTime appointmentDate)
    {
        UpdateBankBranch(bankBranchId);
        UpdateClient(clientId);
        UpdateAppointmentDate(appointmentDate);
    }

    private void UpdateBankBranch(int bankBranchId)
    {
        if (bankBranchId <= 0)
        {
            throw new DomainException("El identificador de la sucursal bancaria es inválido.");
        }

        BankBranchId = bankBranchId;
    }

    private void UpdateClient(Guid clientId)
    {
        if (clientId == Guid.Empty)
        {
            throw new DomainException("El identificador del cliente es inválido.");
        }

        ClientId = clientId;
    }

    private void UpdateAppointmentDate(DateTime appointmentDate)
    {
        if (appointmentDate == default)
        {
            throw new DomainException("La fecha de la cita es obligatoria.");
        }

        AppointmentDate = DateTime.SpecifyKind(appointmentDate, DateTimeKind.Utc);
    }
}


