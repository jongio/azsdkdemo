Just a reminder that when you want to debug this function app locally, you need to manually set the BASENAME env var in local.settings.json

```json
{
  "IsEncrypted": false,
  "Values": {
    "AzureWebJobsStorage": "UseDevelopmentStorage=true;",
    "FUNCTIONS_WORKER_RUNTIME": "dotnet",
    "BASENAME": "meme617"
  },
  "Host": {
    "CORS": "*",
    "CORSCredentials": false,
    "LocalHttpPort": 3080
  }
}
```