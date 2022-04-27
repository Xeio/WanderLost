# Running development server

## Initial Setup

For the authentication process to work, you will need to create a dev-only Discord OAuth Application at https://discord.com/developers/applications. It should look something like this:

![Oauth Setup](https://user-images.githubusercontent.com/25418/165634921-0c8c70a0-2913-433d-a4d1-97bf0a1a1d1f.png)

Take the ClientId and Client Secret and in Visual Studio, right click the "WanderLost.Server" project and select "Manage Secrets" in this file add the "DiscordClientSecret" and "DiscordClientId" to the file:

![Secrets](https://user-images.githubusercontent.com/25418/165635349-c30983da-5f90-452f-9ad7-337cfa125645.png)

## Run debug

In Visual Studio, select WanderLost.Server as your startup project and hit run

![image](https://user-images.githubusercontent.com/25418/165635539-f9fd6d88-2fcc-43bf-a175-a6bbc00b888e.png)

Alternatively from the command line you can use `dotenet run` in the WanderLost\Server folder then launch open the site from your browser.
