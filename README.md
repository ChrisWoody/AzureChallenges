# Azure Challenges

A website to teach concepts and features in Azure through a list of challenges. The user can complete them however they want but the website will be able to connect to and validate the service to make sure it is configured correctly for the challenge to be marked as completed.

## Notes

- Runs on .net7 server-hosted Blazor
- To run locally
  - Set 'StorageAccountKey' and 'TenantId' in the appsettings.development.json file
    - Can be local storage or create a Storage Account in Azure
  - Make sure you are logged in with your AD account in Visual Studio
    - And that your account has 'Reader' access to the Subscription you'll be testing in
- When running locally there are debug buttons available to clear the state cache (in memory) or the user's entire state from the backing storage
  - Just refresh the page to see state being cleared
  - Also AD Auth for the website isn't setup locally so it'll run as 'unauthenticated', but the website can be used as if it was authenticated
- There is a 'connection checker' website too that helps validate an App Service can connect to resources with its Managed Identity.
- When this is hosted on an App Service, I've configured 'easy auth' so that federated accounts can be used.

![image](https://user-images.githubusercontent.com/16053164/208029397-f8f1ee8d-f8bf-4c9e-8047-3835be8f598f.png)