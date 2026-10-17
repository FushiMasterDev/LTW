using HomestayBooking.Web.Data;
using HomestayBooking.Web.Models.Entities;
using HomestayBooking.Web.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace HomestayBooking.Tests.Services;

internal sealed class RecordingMessageSender : IMessageSender
{
    public List<(MessageChannel Channel, string Phone, string Message)> Calls { get; } = [];
    public Func<MessageChannel, bool> Fails { get; set; } = _ => false;
    public Func<MessageChannel, Task>? BeforeSend { get; set; }
    public async Task SendAsync(MessageChannel channel, string recipientPhone, string message, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        Calls.Add((channel, recipientPhone, message));
        if (BeforeSend is not null) await BeforeSend(channel);
        if (Fails(channel)) throw new InvalidOperationException("Lỗi giả lập " + message);
    }
}

internal sealed class GuestServicesTestHost : IDisposable
{
    public BackofficeTestHost B { get; }
    public BookingTestHost H => B.H;
    public PartnerTestHost P => B.P;
    public ApplicationDbContext Db => B.Db;
    public DateOnly Today => B.Today;
    public Actor Customer => new(ActorType.Customer, Db.Users.Single(u => u.PersonId == H.Customer.PersonId).Id);
    public ReminderOptions Options { get; } = new();
    public RecordingMessageSender Sender { get; } = new();
    private GuestServicesTestHost(BackofficeTestHost b) => B = b;
    public static async Task<GuestServicesTestHost> CreateAsync() => new(await BackofficeTestHost.CreateAsync());
    public ReviewService Reviews(ApplicationDbContext? db = null) => new(db ?? Db, new(db ?? Db), H.Clock);
    public MessageService Messages(ApplicationDbContext? db = null) => new(db ?? Db, Sender, H.Clock, NullLogger<MessageService>.Instance);
    public ReminderService Reminders(ApplicationDbContext? db = null) => new(db ?? Db, Messages(db), new(db ?? Db), H.Clock,
        Microsoft.Extensions.Options.Options.Create(Options), NullLogger<ReminderService>.Instance);
    public OwnerScheduleService Schedule => new(Db, H.Clock);

    public async Task<Booking> BookAsync(int days = 1, BookingStatus status = BookingStatus.Confirmed)
    {
        var b = await H.Bookings().CreateAsync(H.Request(Today.AddDays(days), Today.AddDays(days + 2)));
        if (status == BookingStatus.AwaitingPayment) return b;
        await H.Bookings().ChangeStatusAsync(b, BookingStatus.Deposited, Actor.System);
        if (status == BookingStatus.Deposited) return b;
        await H.Bookings().ChangeStatusAsync(b, BookingStatus.Confirmed, Actor.System);
        if (status == BookingStatus.Confirmed) return b;
        if (days > 0) H.Clock.Advance(TimeSpan.FromDays(days));
        await H.Bookings().ChangeStatusAsync(b, BookingStatus.CheckedIn, Actor.System);
        if (status == BookingStatus.Completed) await H.Bookings().ChangeStatusAsync(b, BookingStatus.Completed, Actor.System);
        return b;
    }
    public void Dispose() => B.Dispose();
}
