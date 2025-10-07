using MobyPark.Models;
using MobyPark.Services.Services;

namespace MobyPark;

public class DatabaseConnectionTest
{
    private readonly ServiceStack _services;

    public DatabaseConnectionTest(ServiceStack serviceStack)
    {
        _services = serviceStack;
    }

    public async void Testing()
    {
        Console.WriteLine("What to test?");
        Console.WriteLine("1. Create and Delete");
        Console.WriteLine("2. Get by ID");
        Console.WriteLine("3. Get all");
        Console.WriteLine("4. Count");
        Console.WriteLine("5. Update");
        Console.WriteLine("6. Specific queries");
        Console.WriteLine("7. Exit");
        Console.Write("Enter choice: ");
        var input = Console.ReadLine();
        switch (input)
        {
            case "1":
                await TestCreateDelete();
                break;
            case "2":
                await TestGetById();
                break;
            case "3":
                await TestGetAll();
                break;
            case "4":
                await TestCount();
                break;
            case "5":
                await TestUpdate();
                break;
            case "6":
                await TestQueries();
                break;
            case "7":
                Exit();
                return;
            default:
                Console.WriteLine("Invalid choice. Please try again.");
                break;
        }
    }

    async Task<int> TestGetById()
    {
        var parkingLot = await _services.ParkingLots.GetParkingLotById(1);
        Console.WriteLine($"Parking Lot: {parkingLot!.Name}, Location: {parkingLot.Location}, Capacity: {parkingLot.Capacity}");

        // session
        var session = await _services.ParkingSessions.GetParkingSessionById(1);
        Console.WriteLine($"Session ID: {session.Id}, Parking lot ID: {session.ParkingLotId}, Started: {session.Started}, Stopped: {session.Stopped}");

        // payment has no id, so no get by id

        // reservation
        var reservation = await _services.Reservations.GetReservationById(1);
        Console.WriteLine($"Reservation ID: {reservation.Id}, Vehicle ID: {reservation.VehicleId}, Parking Lot ID: {reservation.ParkingLotId}, Start: {reservation.StartTime}, End: {reservation.EndTime}");

        // user
        var user = await _services.Users.GetUserById(1);
        Console.WriteLine($"User ID: {user.Id}, Username: {user.Username}, Email: {user.Email}, Role: {user.Role}");

        // vehicle
        var vehicle = await _services.Vehicles.GetVehicleById(1);
        Console.WriteLine($"Vehicle ID: {vehicle.Id}, License Plate: {vehicle.LicensePlate}, Make: {vehicle.Make}, Model: {vehicle.Model}");

        return 0;
    }

    async Task<int> TestGetAll()
    {
        var parkingLots = await _services.ParkingLots.GetAllParkingLots();
        Console.WriteLine($"Total Parking Lots: {parkingLots.Count}");

        var sessions = await _services.ParkingSessions.GetAllParkingSessions();
        Console.WriteLine($"Total Parking Sessions: {sessions.Count}");

        var payments = await _services.Payments.GetAllPayments();
        Console.WriteLine($"Total Payments: {payments.Count}");

        var reservations = await _services.Reservations.GetAllReservations();
        Console.WriteLine($"Total Reservations: {reservations.Count}");

        var users = await _services.Users.GetAllUsers();
        Console.WriteLine($"Total Users: {users.Count}");

        var vehicles = await _services.Vehicles.GetAllVehicles();
        Console.WriteLine($"Total Vehicles: {vehicles.Count}");

        return 0;
    }

    async Task<int> TestCount()
    {
        var parkingLots = await _services.ParkingLots.CountParkingLots();
        Console.WriteLine($"Total Parking Lots: {parkingLots}");

        var sessions = await _services.ParkingSessions.CountParkingSessions();
        Console.WriteLine($"Total Parking Sessions: {sessions}");

        var payments = await _services.Payments.CountPayments();
        Console.WriteLine($"Total Payments: {payments}");

        var reservations = await _services.Reservations.CountReservations();
        Console.WriteLine($"Total Reservations: {reservations}");

        var users = await _services.Users.CountUsers();
        Console.WriteLine($"Total Users: {users}");

        var vehicles = await _services.Vehicles.CountVehicles();
        Console.WriteLine($"Total Vehicles: {vehicles}");

        return 0;
    }

    async Task<int> TestUpdate()
    {
        // For this, first get an existing entity. Ensure the old values are remembered to revert back after test.

        // parking lot
        var existingLot = await _services.ParkingLots.GetParkingLotById(1);
        if (existingLot is null)
        {
            Console.WriteLine("No parking lot with ID 1 found.");
            return -1;
        }
        var newLot = new ParkingLotModel
        {
            Id = existingLot.Id,
            Name = existingLot.Name,
            Location = existingLot.Location,
            Address = existingLot.Address,
            Capacity = existingLot.Capacity,
            Reserved = existingLot.Reserved,
            Tariff = existingLot.Tariff,
            DayTariff = existingLot.DayTariff,
            CreatedAt = existingLot.CreatedAt,
            Coordinates = new CoordinatesModel
            {
                Lat = existingLot.Coordinates.Lat,
                Lng = existingLot.Coordinates.Lng
            }
        };

        newLot.Name = "Updated Name";
        await _services.ParkingLots.UpdateParkingLot(newLot);
        var updatedLot = await _services.ParkingLots.GetParkingLotById(1);
        Console.WriteLine($"Updated Parking Lot Name: {updatedLot!.Name}");
        // Revert back
        await _services.ParkingLots.UpdateParkingLot(existingLot);
        Console.WriteLine($"Reverted Parking Lot Name back to original: {existingLot.Name}");



        // session
        var existingSession = await _services.ParkingSessions.GetParkingSessionById(1);

        var newSession = new ParkingSessionModel
        {
            Id = existingSession.Id,
            ParkingLotId = existingSession.ParkingLotId,
            LicensePlate = existingSession.LicensePlate,
            Started = existingSession.Started,
            Stopped = existingSession.Stopped,
            User = existingSession.User,
            DurationMinutes = existingSession.DurationMinutes,
            Cost = existingSession.Cost,
            PaymentStatus = existingSession.PaymentStatus
        };

        newSession.User = "updated user";
        await _services.ParkingSessions.UpdateParkingSession(newSession);
        var updatedSession = await _services.ParkingSessions.GetParkingSessionById(1);
        Console.WriteLine($"Updated Session User: {updatedSession!.User}");
        // Revert back
        await _services.ParkingSessions.UpdateParkingSession(existingSession);
        Console.WriteLine($"Reverted Session User back to original: {existingSession.User}");



        // payment
        var existingPayment = await _services.Payments.GetPaymentByTransactionId("9b3712c0752d708e2887f6ec8b1a7ebdCC");
        if (existingPayment is null)
        {
            Console.WriteLine("No payment with Transaction ID 9b3712c0752d708e2887f6ec8b1a7ebdCC found.");
            return -1;
        }

        var newPayment = new PaymentModel
        {
            TransactionId = existingPayment.TransactionId,
            Amount = existingPayment.Amount,
            Initiator = existingPayment.Initiator,
            Completed = existingPayment.Completed,
            Hash = existingPayment.Hash,
            TransactionData = new TransactionDataModel
            {
                Amount = existingPayment.TransactionData!.Amount,
                Bank = existingPayment.TransactionData.Bank,
                Date = existingPayment.TransactionData.Date,
                Issuer = existingPayment.TransactionData.Issuer,
                Method = existingPayment.TransactionData.Method
            },
            CoupledTo = existingPayment.CoupledTo,
            CreatedAt = existingPayment.CreatedAt
        };

        var newTransactionData = newPayment.TransactionData;
        newTransactionData!.Issuer = "Updated Issuer";
        await _services.Payments.ValidatePayment(newPayment.TransactionId, newPayment.Hash, newTransactionData);
        var updatedPayment = await _services.Payments.GetPaymentByTransactionId("9b3712c0752d708e2887f6ec8b1a7ebdCC");
        Console.WriteLine($"Updated Payment Issuer: {updatedPayment!.TransactionData!.Issuer}");
        // Revert back
        await _services.Payments.ValidatePayment(existingPayment.TransactionId, existingPayment.Hash, existingPayment.TransactionData!);
        Console.WriteLine($"Reverted Payment Issuer back to original: {existingPayment.TransactionData!.Issuer}");



        // reservation
        var existingReservation = await _services.Reservations.GetReservationById(1);
        if (existingReservation is null)
        {
            Console.WriteLine("No reservation with ID 1 found.");
            return -1;
        }

        var newReservation = new ReservationModel
        {
            Id = existingReservation.Id,
            UserId = existingReservation.UserId,
            ParkingLotId = existingReservation.ParkingLotId,
            VehicleId = existingReservation.VehicleId,
            StartTime = existingReservation.StartTime,
            EndTime = existingReservation.EndTime,
            Status = existingReservation.Status,
            CreatedAt = existingReservation.CreatedAt,
            Cost = existingReservation.Cost
        };

        newReservation.UserId = Int32.MaxValue;
        await _services.Reservations.UpdateReservation(newReservation);
        var updatedReservation = await _services.Reservations.GetReservationById(1);
        Console.WriteLine($"Updated Reservation User ID: {updatedReservation!.UserId}");
        // Revert back
        await _services.Reservations.UpdateReservation(existingReservation);
        Console.WriteLine($"Reverted Reservation User ID back to original: {existingReservation.UserId}");



        // user
        var existingUser = await _services.Users.GetUserById(1);
        if (existingUser is null)
        {
            Console.WriteLine("No user with ID 1 found.");
            return -1;
        }

        var newUser = new UserModel
        {
            Id = existingUser.Id,
            Username = existingUser.Username,
            Email = existingUser.Email,
            Password = existingUser.Password,
            Role = existingUser.Role,
            CreatedAt = existingUser.CreatedAt,
            Name = existingUser.Name,
            Phone = existingUser.Phone,
            BirthYear = existingUser.BirthYear
        };

        newUser.Name = "Updated Name";
        await _services.Users.UpdateUser(newUser);
        var updatedUser = await _services.Users.GetUserById(1);
        Console.WriteLine($"Updated User Name: {updatedUser!.Name}");
        // Revert back
        await _services.Users.UpdateUser(existingUser);
        Console.WriteLine($"Reverted User Name back to original: {existingUser.Name}");



        // vehicle
        var existingVehicle = await _services.Vehicles.GetVehicleById(1);
        if (existingVehicle is null)
        {
            Console.WriteLine("No vehicle with ID 1 found.");
            return -1;
        }

        var newVehicle = new VehicleModel
        {
            Id = existingVehicle.Id,
            UserId = existingVehicle.UserId,
            LicensePlate = existingVehicle.LicensePlate,
            Make = existingVehicle.Make,
            Model = existingVehicle.Model,
            Color = existingVehicle.Color,
            Year = existingVehicle.Year,
            CreatedAt = existingVehicle.CreatedAt
        };

        newVehicle.Color = "Updated Color";
        await _services.Vehicles.UpdateVehicle(newVehicle);
        var updatedVehicle = await _services.Vehicles.GetVehicleById(1);
        Console.WriteLine($"Updated Vehicle Color: {updatedVehicle!.Color}");
        // Revert back
        await _services.Vehicles.UpdateVehicle(existingVehicle);
        Console.WriteLine($"Reverted Vehicle Color back to original: {existingVehicle.Color}");


        return 0;
    }

    async Task<int> TestCreateDelete()
    {
        // For this, first create a new entity to delete.
        var parkingLot = new ParkingLotModel
        {
            Name = "Test Lot",
            Location = "Test Location",
            Address = "123 Test St",
            Capacity = 50,
            Reserved = 0,
            Tariff = 2.5m,
            DayTariff = 20.0m,
            CreatedAt = DateOnly.FromDateTime(DateTime.UtcNow),
            Coordinates = new CoordinatesModel
            {
                Lat = 0.0,
                Lng = 0.0
            }
        };
        await _services.ParkingLots.CreateParkingLot(parkingLot);
        var createdLot = await _services.ParkingLots.GetParkingLotById(parkingLot.Id);
        Console.WriteLine("Created lot");

        await _services.ParkingLots.DeleteParkingLot(parkingLot.Id);
        var deletedLot = await _services.ParkingLots.GetParkingLotById(parkingLot.Id);
        Console.WriteLine(deletedLot is null ? "Parking lot successfully deleted." : "Failed to delete parking lot.");

        var session = new ParkingSessionModel
        {
            ParkingLotId = 1,
            LicensePlate = "TEST123",
            Started = DateTime.UtcNow,
            Stopped = null,
            User = "testuser",
            DurationMinutes = 0,
            Cost = 0.0m,
            PaymentStatus = "PENDING"
        };
        await _services.ParkingSessions.CreateParkingSession(session);
        var createdSession = await _services.ParkingSessions.GetParkingSessionById(session.Id);
        Console.WriteLine("Created session");

        await _services.ParkingSessions.DeleteParkingSession(session.Id);
        var deletedSession = await _services.ParkingSessions.GetParkingSessionById(session.Id);
        Console.WriteLine("Parking session successfully deleted.");


        var payment = new PaymentModel
        {
            TransactionId = Guid.NewGuid().ToString("N"),
            Amount = 10.0m,
            Initiator = "testuser",
            Completed = null,
            Hash = Guid.NewGuid().ToString("N"),
            TransactionData = new TransactionDataModel
            {
                Amount = 10.0m,
                Bank = "Test Bank",
                Date = DateOnly.FromDateTime(DateTime.UtcNow),
                Issuer = "Test Issuer",
                Method = "CARD"
            },
            CoupledTo = null,
            CreatedAt = DateTime.UtcNow
        };
        await _services.Payments.CreatePayment(payment);
        var createdPayment = await _services.Payments.GetPaymentByTransactionId(payment.TransactionId);
        if (createdPayment is not null)
            Console.WriteLine("Created payment");
        await _services.Payments.DeletePayment(payment.TransactionId);
        var deletedPayment = await _services.Payments.GetPaymentByTransactionId(payment.TransactionId);
        Console.WriteLine(deletedPayment is null ? "Payment successfully deleted." : "Failed to delete payment.");

        var reservation = new ReservationModel
        {
            UserId = 1,
            ParkingLotId = 1,
            VehicleId = 1,
            StartTime = DateTime.UtcNow,
            EndTime = DateTime.UtcNow.AddHours(2),
            Status = "ACTIVE",
            CreatedAt = DateTime.UtcNow,
            Cost = 5.0m
        };
        await _services.Reservations.CreateReservation(reservation);
        var createdReservation = await _services.Reservations.GetReservationById(reservation.Id);
        if (createdReservation is not null)
            Console.WriteLine("Created reservation");

        await _services.Reservations.DeleteReservation(reservation.Id);
        var deletedReservation = await _services.Reservations.GetReservationById(reservation.Id);
        Console.WriteLine(deletedReservation is null ? "Reservation successfully deleted." : "Failed to delete reservation.");


        var user = new UserModel
        {
            Username = "testuser",
            Email = "test@user.com",
            Password = "Val1dP@ssword!",
            Role = "USER",
            CreatedAt = DateTime.UtcNow,
            Name = "Test User",
            Phone = "0612345678",
            BirthYear = 1990,
            Active = true
        };

        await _services.Users.CreateUser(user);
        var createdUser = await _services.Users.GetUserById(user.Id);
        if (createdUser is not null)
            Console.WriteLine("Created user");

        await _services.Users.DeleteUser(user.Id);
        var deletedUser = await _services.Users.GetUserById(user.Id);
        Console.WriteLine(deletedUser is null ? "User successfully deleted." : "Failed to delete user.");



        var vehicle = new VehicleModel
        {
            UserId = 1,
            LicensePlate = "TEST123",
            Make = "Test Make",
            Model = "Test Model",
            Color = "Red",
            Year = 2020,
            CreatedAt = DateTime.UtcNow
        };
        await _services.Vehicles.CreateVehicle(vehicle);
        var createdVehicle = await _services.Vehicles.GetVehicleById(vehicle.Id);
        if (createdVehicle is not null)
            Console.WriteLine("Created vehicle");

        await _services.Vehicles.DeleteVehicle(vehicle.Id);
        var deletedVehicle = await _services.Vehicles.GetVehicleById(vehicle.Id);
        Console.WriteLine(deletedVehicle is null ? "Vehicle successfully deleted." : "Failed to delete vehicle.");

        return 0;
    }

    async Task<int> TestQueries()
    {



        return 0;
    }

    int Exit()
    {
        return 0;
    }
}