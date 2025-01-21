
public static class SampleData
{
    public static byte[] GenerateRandomLengthMessage(int min = 10, int max = 100, string lengthUnit = "B")
    {
        int factor = lengthUnit switch
        {
            "B" => 1,
            "KB" => 1024,
            "MB" => 1024 * 1024,
            _ => 1
        };

        Random rndLength = new Random();
        int messageLength = factor * rndLength.Next(min, max);
        byte[] body = new byte[messageLength];

        //random message content generator
        Random rndContent = new Random();

        //fill the message with random characters
        for (int i = 0; i < messageLength; i++)
        {
            body[i] = (byte)rndContent.Next(65, 90);
        }

        return body;
    }

    public static IEnumerable<int> GenereateSequentialNumber(int min = 0, int max = 100, int step = 1)
    {
        return new SequentialNumberEnumerable(min, max, step);
    }

}