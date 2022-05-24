# Imato.Api.Request

Generic helpers for REST API

### Using 

#### Try

```csharp
using Imato.Api.Request;

// Execution options
var options = new TryOptions
{
    RetryCount = 3,
    Delay = 20,
    Timeout = 2000
};

// Function to execute
var testFunc1 = (int id) =>
{
    if (id % 2 == 0)
    {
        return Task.FromResult(id);
    }
    throw new ArgumentException(nameof(id));
};

// Default behavior
var result = await Try
    .Function(() => testFunc1(2))
    .GetResult();

// Add exception handler
result = await Try
    .Function(() => testFunc1(2))
    .OnError((ex) => Console.WriteLine(ex.Message))
    .GetResult();

// With retry and over options
result = await Try
    .Function(() => testFunc1(2))
    .Setup(options)
    .OnError((ex) => Console.WriteLine(ex.Message))
    .GetResult();

// Without result
var errorCount = 0;
await Try
    .Function(() => testFunc2(1))
    .Function(() => testFunc2(2))
    .Function(() => testFunc2(3))
    .OnError((ex) => errorCount++)
    .OnError((ex) => Console.WriteLine(ex.Message))
    .Setup(options)
    .Execute();

```

#### API

```csharp
using Imato.Api.Request;

// Create service
var service = new ApiService(new ApiOptions
{
    ApiUrl = "https://www.boredapi.com/api",
    TryOptions = new TryOptions
    {
        Timeout = 3000,
        RetryCount = 3, 
        Delay = 500
    }
});

// Get
var getResult = await service.Get<NewActivity>(path: "/activity", queryParams: new { type = "education" });

// POST
var postResult = await service.Post<NewActivity>(path: "/activity", 
    queryParams: new { key = "100" },
    data: new NewActivity
    {
        Activity = "Test"
    });

// Or view result messages
var postMessage = await service.Post<ApiResult>(path: "/activity", 
    queryParams: new { key = "100" },
    data: new NewActivity
    {
        Activity = "Test"
    });

// Or without result
await service.Post(path: "/activity", 
    queryParams: new { key = "100" },
    data: new NewActivity
    {
        Activity = "Test"
    });

```


