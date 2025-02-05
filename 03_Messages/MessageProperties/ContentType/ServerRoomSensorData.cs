namespace Messages.ContentType;

public class ServerRoomSensorData
{
    public int RoomId { get; set; }
    public double Temperature { get; set; }
    public double Humidity { get; set; }
    public bool IsAirConditioningOn { get; set; }
    public int PowerUsageKW { get; set; }

    override public string ToString()
    {
        return $"RoomId: {RoomId}, Temperature: {Temperature}, Humidity: {Humidity}, IsAirConditioningOn: {IsAirConditioningOn}, PowerUsageKW: {PowerUsageKW}";
    }

    public static ServerRoomSensorData CreateRandom()
    {
        var random = new Random();
        return new ServerRoomSensorData
        {
            RoomId = random.Next(1, 100),
            Temperature = random.Next(15, 30),
            Humidity = random.Next(30, 70),
            IsAirConditioningOn = random.Next(0, 2) == 1,
            PowerUsageKW = random.Next(1, 10)
        };
    }
}

