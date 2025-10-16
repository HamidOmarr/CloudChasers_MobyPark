namespace MobyPark.Services.Exceptions
{
    public class ActiveSessionAlreadyExistsException : InvalidOperationException
    {
        public string LicensePlate { get; }

        public ActiveSessionAlreadyExistsException(string licensePlate)
            : base($"Cannot start a session; an active session already exists for license plate: {licensePlate}.")
        { LicensePlate = licensePlate; }
    }
}