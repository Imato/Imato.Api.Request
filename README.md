# Imato.Api.Request

Generic helpers for REST API

### Using 

#### API

```csharp
using Imato.Api.Request;

// Create service
var service = new ApiService(new ApiOptions
{
    ApiUrl = "https://www.boredapi.com/api",
    Timeout = 3000,
    RetryCount = 3, 
    Delay = 500
});

// Get
var getResult = await service.GetAsync<NewActivity>(path: "/activity", queryParams: new { type = "education" });

// POST
var postResult = await service.PostAsync<NewActivity>(path: "/activity", 
    queryParams: new { key = "100" },
    data: new NewActivity
    {
        Activity = "Test"
    });

// Or view result messages
var postMessage = await service.PostAsync<ApiResult>(path: "/activity", 
    queryParams: new { key = "100" },
    data: new NewActivity
    {
        Activity = "Test"
    });

// Or without result
await service.PostAsync(path: "/activity", 
    queryParams: new { key = "100" },
    data: new NewActivity
    {
        Activity = "Test"
    });

```


