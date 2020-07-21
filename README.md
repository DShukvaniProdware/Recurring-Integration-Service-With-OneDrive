# Recurring-Integration-Service-With-OneDrive
This is created to showcase the flaw in the RIS when we embed Microsoft.Graph

# Showcase

I have created a project for handling OneDrive actions, such as authenticating Microsoft Graph client which will be used to:
1. Connect OneDrive
2. Create Folders
3. Move files between folders

In current example I just showcase the problem when the `GraphServiceClient` object is being initialized.
There is another project for that `OneDriveHelper`, where you will find `OneDriveAuthenticationHelper` class with method `GetAuthenticatedClient`.
This project is referernced in `Scheduler` and `Job.Import` projects, since these projects are using OneDrive capabilities at the time being.

The strange thing is:
1. When the `GraphServiceClient` object is being created from `Scheduler` project, all goes fine;
2. When the `GraphServiceClient` object is being created from `Job.Import` project, system is requiring loading Newtonsoft 6.0.0.0;

I think this is related to Quartz server. Somehow it's not allowing loading related assemblies. I even loaded assembly from file when code was running from `Job.Import`, but it didn't help.

# Setup

My code is placed on two locations:
1. Scheduler > ImportJobV3.cs line: 578
2. Job.Import > Import.cs line: 143

```
new OneDriveAuthenticationHelper(
                "", // TODO: Client Id of the OneDrive Azure app
                "", // TODO: Tenant Id of the OneDrive Azure app
                "", // TODO: Username of the user who's OneDrive will be used
                ""  // TODO: Password for the user
            );
```

You will need to create a OneDrive application on Azure with permission `Files.ReadWrite.All` under the same tenant as the RIS.
Make sure that OneDrive account is under the same tenant.