using MQTTnet;
using MQTTnet.Client;
using System.Text.Json;
using System.Text;

var factory = new MqttFactory();
var client = factory.CreateMqttClient();

var options = new MqttClientOptionsBuilder()
    .WithTcpServer("localhost", 1883)
    .Build();

Console.WriteLine("Edge.Simulator starting… connecting to MQTT at localhost:1883");
try
{
    await client.ConnectAsync(options);
    Console.WriteLine("✅ Connected to MQTT broker.");
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Failed to connect to MQTT broker: {ex.Message}");
    throw;
}

var rand = new Random();
var devices = new List<string>([
"dev-101", "dev-102", "dev-103", "dev-104", "dev-105", "dev-106", "dev-107", "dev-108", "dev-109", "dev-110"
]);


while (true)
{
    foreach (var d in devices)
    {
        var payload = new
        {
            deviceId = d,
            apiKey = $"{d}-key",
            timestamp = DateTimeOffset.UtcNow,
            metrics = new object[]
            {
                new { type = "temperature", value = 21.5 + rand.NextDouble(), unit = "C" },
                new { type = "co2", value = 900 + rand.Next(0, 700), unit = "ppm" }
            }
        };

        var topic = $"tenants/innovia/devices/{d}/measurements";
        var json = JsonSerializer.Serialize(payload);

        var message = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(Encoding.UTF8.GetBytes(json))
            .Build();
        await client.PublishAsync(message);
        Console.WriteLine($"[{DateTimeOffset.UtcNow:o}] Published to '{topic}': {json}"); 
    }

    await Task.Delay(TimeSpan.FromSeconds(10));
}
